using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
   
public class WhatsAppProcessor(SellFlowApiDbContext db, IWhatsAppClient wa, IStoreReadinessService readiness,
    IOrderService orders, IOwnerAlertService ownerAlerts, IOwnerCommandHandler ownerCommands) : IWhatsAppProcessor
{
    private const string Menu = "menu", Cancel = "cancel", Human = "agent";
 
    public async Task ProcessAsync(InboundMessage m)
    {
        // 3. Route by phone_number_id (no store code needed)
        var account = await db.WhatsAppAccounts.FirstOrDefaultAsync(a => a.PhoneNumberId == m.PhoneNumberId);
        if (account is null) return;
        var businessId = account.BusinessId;

        // Owner's personal number: route to the owner assistant (payment approvals, delivery updates)
        var settingsRow = await db.BusinessSettings.FirstAsync(s => s.BusinessId == businessId);
        if (!string.IsNullOrEmpty(settingsRow.OwnerWhatsAppNumber) && settingsRow.OwnerWhatsAppNumber == m.From)
        {
            settingsRow.OwnerLastMessageAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await ownerCommands.HandleAsync(account, settingsRow, m.Input);
            return;
        }
 
        // 4. Store live?
        var ready = await readiness.CheckAsync(businessId);
        if (!ready.IsLive)
        {
            await wa.SendTextAsync(account, m.From, "This store is currently unavailable. Please try again later.");
            return;
        }
 
        // 5. Find or create customer
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.WhatsAppNumber == m.From);
        if (customer is null)
        {
            customer = new Customer { BusinessId = businessId, WhatsAppNumber = m.From };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }
 
        // 6. Load or create conversation; reset to menu if idle > 30 min
        var convo = await db.Conversations.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.CustomerId == customer.Id);
        if (convo is null)
        {
            convo = new Conversation { BusinessId = businessId, CustomerId = customer.Id };
            db.Conversations.Add(convo);
        }
        else if (convo.State != ConversationState.HumanSupport && DateTime.UtcNow - convo.LastMessageAt > TimeSpan.FromMinutes(30))
        {
            convo.State = ConversationState.MainMenu;
            convo.ContextJson = "{}";
        }
 
        // 2 + 7. Dedupe via unique index on WhatsAppMessageId, then log inbound
        db.Messages.Add(new Message { ConversationId = convo.Id, Sender = SenderType.Customer,
                                      Content = m.Input, WhatsAppMessageId = m.MessageId });
        var now = DateTime.UtcNow;
        convo.LastMessageAt = now;
        convo.LastCustomerMessageAt = now;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return; }   // duplicate webhook delivery, already processed
 
        // Receipt photo / PDF: attach to the customer's open bank-transfer order and alert the owner
        if (m.MediaId is not null)
        {
            await HandleReceiptAsync(account, m, convo, customer, businessId);
            return;
        }

        // 8. Human takeover: bot stays silent (merchant inbox handles it)
        if (convo.State == ConversationState.HumanSupport && !m.Input.Equals(Menu, StringComparison.OrdinalIgnoreCase))
            return;
 
        var ctx = JsonSerializer.Deserialize<ChatContext>(convo.ContextJson) ?? new();
        var send = async (string text) =>
        {
            await wa.SendTextAsync(account, m.From, text);
            db.Messages.Add(new Message { ConversationId = convo.Id, Sender = SenderType.Bot, Content = text });
        };
 
        try
        {
            await DispatchAsync(m.Input, convo, ctx, customer, businessId, send);
        }
        catch (DomainException ex)
        {
            await send($"⚠️ {ex.Message}");
        }
 
        convo.ContextJson = JsonSerializer.Serialize(ctx);
        await db.SaveChangesAsync();
    }
 
    private async Task DispatchAsync(string input, Conversation convo, ChatContext ctx, Customer customer,
                                     Guid businessId, Func<string, Task> send)
    {
        var lower = input.ToLowerInvariant();
 
        // Global commands work in any state
        if (lower is Menu or "hi" or "hello" or "start") { await ShowMenu(convo, ctx, businessId, send); return; }
        if (lower == Cancel) { await ShowMenu(convo, ctx, businessId, send, "Cancelled."); return; }
        if (lower is Human) { convo.State = ConversationState.HumanSupport;
            await send("Connecting you to the business. They'll reply here shortly. Type *menu* to return to the bot."); return; }
 
        switch (convo.State)
        {
            case ConversationState.MainMenu:
                await HandleMainMenu(input, convo, ctx, customer, businessId, send); break;
 
            case ConversationState.ViewingProducts:
                if (TryPick(input, ctx.ProductIds, out var pid))
                {
                    var p = await db.Products.FirstAsync(x => x.Id == pid && x.BusinessId == businessId);
                    ctx.ProductId = p.Id;
                    convo.State = ConversationState.SelectingQuantity;
                    await send($"*{p.Name}*\n{Naira(p.Price)}\n{p.Description}\nIn stock: {p.Available}\n\nHow many would you like? (send a number)");
                }
                else await Invalid(convo, send, "Reply with a product number from the list.");
                break;
 
            case ConversationState.SelectingQuantity:
                if (int.TryParse(input, out var qty) && qty > 0)
                {
                    var p = await db.Products.FirstAsync(x => x.Id == ctx.ProductId && x.BusinessId == businessId);
                    if (qty > p.Available) { await send($"Sorry, only {p.Available} available. Enter a smaller number."); break; }
 
                    ctx.Quantity = qty;
                    var s = (await db.BusinessSettings.FirstAsync(x => x.BusinessId == businessId));
                    var methods = new List<string>();
                    if (s.AcceptBankTransfer) methods.Add("1. Pay now");
                    if (s.AcceptPayOnDelivery) methods.Add("2. Pay on delivery");
                    convo.State = ConversationState.SelectingPayment;
                    await send($"{p.Name} × {qty} = {Naira(p.Price * qty)}\n(+ delivery fee at checkout)\n\nHow would you like to pay?\n{string.Join("\n", methods)}");
                }
                else await Invalid(convo, send, "Please send a valid quantity, e.g. 2");
                break;
 
            case ConversationState.SelectingPayment:
                var settings = await db.BusinessSettings.FirstAsync(x => x.BusinessId == businessId);
                ctx.Method = input switch
                {
                    "1" when settings.AcceptBankTransfer => PaymentMethod.BankTransfer,
                    "2" when settings.AcceptPayOnDelivery => PaymentMethod.PayOnDelivery,
                    _ => null
                };
                if (ctx.Method is null) { await Invalid(convo, send, "Reply 1 or 2."); break; }
 
                // Returning customer: offer saved address
                var saved = await db.CustomerAddresses.FirstOrDefaultAsync(a => a.CustomerId == customer.Id && a.IsDefault);
                if (saved is not null)
                {
                    ctx.Draft = saved;
                    convo.State = ConversationState.ConfirmingOrder;
                    await ShowSummary(ctx, businessId, send, $"Deliver to {saved.Address}, {saved.City}?");
                }
                else { ctx.AddressStep = 0; convo.State = ConversationState.CollectingAddress;
                       await send("Full name of the recipient?"); }
                break;
 
            case ConversationState.CollectingAddress:
                await CollectAddress(input, convo, ctx, customer, businessId, send); break;
 
            case ConversationState.ConfirmingOrder:
                if (input == "1") await PlaceOrder(convo, ctx, customer, businessId, send);
                else if (input == "2") { ctx.AddressStep = 0; ctx.Draft = new(); convo.State = ConversationState.CollectingAddress;
                                         await send("Okay. Full name of the recipient?"); }
                else await Invalid(convo, send, "Reply 1 to confirm, 2 to change address, or *cancel*.");
                break;
 
            case ConversationState.ViewingOrders:
                if (TryPick(input, ctx.OrderIds, out var oid))
                {
                    var o = await db.Orders.Include(x => x.Items).FirstAsync(x => x.Id == oid && x.CustomerId == customer.Id);
                    await send($"*{o.OrderNumber}*\n{string.Join("\n", o.Items.Select(i => $"{i.ProductName} × {i.Quantity}"))}\n" +
                               $"Total: {Naira(o.Total)}\nPayment: {o.PaymentMethod} ({o.PaymentStatus})\nStatus: {o.Status}");
                }
                else await Invalid(convo, send, "Reply with an order number from the list.");
                break;
 
            default:
                await ShowMenu(convo, ctx, businessId, send); break;
        }
    }
 
    private async Task HandleMainMenu(string input, Conversation convo, ChatContext ctx, Customer customer,
                                      Guid businessId, Func<string, Task> send)
    {
        switch (input)
        {
            case "1":
                var products = await db.Products.Where(p => p.BusinessId == businessId && p.IsActive
                                    && p.StockQuantity - p.ReservedQuantity > 0)
                                    .OrderBy(p => p.Name).Take(20).ToListAsync();
                ctx.ProductIds = products.Select(p => p.Id).ToList();
                convo.State = ConversationState.ViewingProducts;
                await send("🛍️ *Products*\n\n" + string.Join("\n", products.Select((p, i) => $"{i + 1}. {p.Name} — {Naira(p.Price)}"))
                           + "\n\nReply with the product number.");
                break;
 
            case "2":
                convo.State = ConversationState.HumanSupport;
                // TODO: create dashboard inbox alert "Customer needs assistance"
                await send("Connecting you to the business. Type *menu* to return to the bot.");
                break;
 
            case "3":
                var list = await db.Orders.Where(o => o.CustomerId == customer.Id && o.BusinessId == businessId)
                                   .OrderByDescending(o => o.CreatedAt).Take(10).ToListAsync();
                if (list.Count == 0) { await send("You have no orders yet."); break; }
                ctx.OrderIds = list.Select(o => o.Id).ToList();
                convo.State = ConversationState.ViewingOrders;
                await send("📦 *Your Orders*\n\n" + string.Join("\n", list.Select((o, i) => $"{i + 1}. {o.OrderNumber}: {Naira(o.Total)}: {o.Status}"))
                           + "\n\nReply with a number for details.");
                break;
 
            default:
                await ShowMenu(convo, ctx, businessId, send); break;
        }
    }
 
    private async Task CollectAddress(string input, Conversation convo, ChatContext ctx, Customer customer,
                                      Guid businessId, Func<string, Task> send)
    {
        var d = ctx.Draft;
        switch (ctx.AddressStep)
        {
            case 0: d.RecipientName = input; await send("Phone number for delivery?"); break;
            case 1: d.Phone = input; await send("State? (e.g. Lagos)"); break;
            case 2: d.State = input; await send("City / area?"); break;
            case 3: d.City = input; await send("Street address?"); break;
            case 4: d.Address = input; await send("Nearest landmark? (type 'none' to skip)"); break;
            case 5:
                d.Landmark = input.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : input;
                convo.State = ConversationState.ConfirmingOrder;
                await ShowSummary(ctx, businessId, send, $"Deliver to {d.RecipientName}, {d.Address}, {d.City}, {d.State}?");
                return;
        }
        ctx.AddressStep++;
    }
 
    private async Task ShowSummary(ChatContext ctx, Guid businessId, Func<string, Task> send, string addressLine)
    {
        var p = await db.Products.FirstAsync(x => x.Id == ctx.ProductId);
        var fee = (await db.BusinessSettings.FirstAsync(x => x.BusinessId == businessId)).DeliveryFee ?? 0;
        var sub = p.Price * ctx.Quantity;
        await send($"*Order summary*\n{p.Name} × {ctx.Quantity}: {Naira(sub)}\nDelivery: {Naira(fee)}\n*Total: {Naira(sub + fee)}*\n" +
                   $"Payment: {ctx.Method}\n{addressLine}\n\n1. Confirm\n2. Change address\nType *cancel* to stop.");
    }
 
    private async Task PlaceOrder(Conversation convo, ChatContext ctx, Customer customer, Guid businessId, Func<string, Task> send)
    {
        // Build a cart server-side from validated context
        var cart = new Cart { BusinessId = businessId, CustomerId = customer.Id };
        cart.Items.Add(new CartItem { ProductId = ctx.ProductId!.Value, Quantity = ctx.Quantity });
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
 
        var address = ctx.Draft;
        address.CustomerId = customer.Id;
        if (address.Id == Guid.Empty || !await db.CustomerAddresses.AnyAsync(a => a.Id == address.Id))
        {
            address.IsDefault = true;
            await db.CustomerAddresses.Where(a => a.CustomerId == customer.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
            address.Id = Guid.NewGuid();
            db.CustomerAddresses.Add(address);
            customer.FullName ??= address.RecipientName;
        }
 
        var result = await orders.CreateOrderAsync(businessId, customer.Id, cart.Id, ctx.Method!.Value, address);
 
        convo.State = ConversationState.MainMenu;
        var o = result.Order;
        await send(result.Bank is { } bank
            ? $"✅ *ORDER PLACED*\n━━━━━━━━━━━━━━\n*Order:* {o.OrderNumber}\n*Total:* {Naira(o.Total)}\n━━━━━━━━━━━━━━\n🏦 *Pay by bank transfer*\n*Bank:* {bank.BankName}\n*Account no:* {bank.AccountNumber}\n*Account name:* {bank.AccountName}\n*Narration:* {o.OrderNumber}\n━━━━━━━━━━━━━━\n📸 After paying, send a *screenshot of your receipt* here. Your items are reserved for 2 hours."
            : $"✅ *ORDER PLACED*\n━━━━━━━━━━━━━━\n*Order:* {o.OrderNumber}\n*Total:* {Naira(o.Total)} (pay on delivery)\n━━━━━━━━━━━━━━\nWe'll message you as soon as the business confirms your order.");
        ctx.ProductId = null; ctx.Quantity = 0; ctx.Draft = new();
        await ownerAlerts.NotifyNewOrderAsync(o.Id);
    }
 
    private async Task HandleReceiptAsync(WhatsAppAccount account, InboundMessage m, Conversation convo, Customer customer, Guid businessId)
    {
        async Task Reply(string text)
        {
            await wa.SendTextAsync(account, m.From, text);
            db.Messages.Add(new Message { ConversationId = convo.Id, Sender = SenderType.Bot, Content = text });
            await db.SaveChangesAsync();
        }

        var mime = m.MediaMime ?? "image/jpeg";
        if (!(mime.StartsWith("image/") || mime == "application/pdf"))
        {
            await Reply("Please send your receipt as a *photo* or *PDF* so we can verify it.");
            return;
        }
        var media = await wa.DownloadMediaAsync(account, m.MediaId!);
        if (media is null)
        {
            await Reply("⚠️ We couldn't open that file. Please send the receipt again as a clear photo.");
            return;
        }

        var proof = await orders.SubmitProofAsync(businessId, customer.Id, media.Value.Data, media.Value.Mime);
        if (proof is null)
        {
            await Reply("Thanks! We couldn't find an order awaiting payment. Reply *3* from the *menu* to view your orders, or *agent* to chat with us.");
            return;
        }

        var num = await db.Orders.Where(o => o.Id == proof.OrderId).Select(o => o.OrderNumber).FirstAsync();
        await Reply($"🧾 *RECEIPT RECEIVED*\n━━━━━━━━━━━━━━\n*Order:* {num}\n━━━━━━━━━━━━━━\nThank you! The business is verifying your payment. You'll get a confirmation here shortly.");
        await ownerAlerts.NotifyPaymentProofAsync(proof.OrderId, proof.Id);
    }

    private async Task ShowMenu(Conversation convo, ChatContext ctx, Guid businessId, Func<string, Task> send, string? prefix = null)
    {
        var name = await db.Businesses.Where(b => b.Id == businessId).Select(b => b.Name).FirstAsync();
        convo.State = ConversationState.MainMenu;
        convo.InvalidInputCount = 0;
        ctx.ProductId = null; ctx.Quantity = 0;
        await send($"{prefix}\n👋 *Welcome to {name}*\n━━━━━━━━━━━━━━\nHow can we help you today?\n\n*1.* 🛍️ View products\n*2.* 💬 Chat with us\n*3.* 📦 My orders\n\nReply with *1*, *2* or *3*.".TrimStart());
    }
 
    private async Task Invalid(Conversation convo, Func<string, Task> send, string hint)
    {
        convo.InvalidInputCount++;
        if (convo.InvalidInputCount >= 3)
        {
            convo.State = ConversationState.MainMenu; convo.InvalidInputCount = 0;
            await send("Let's start over. Reply 1 for Products, 2 to chat with the business, 3 for My Orders.");
        }
        else await send($"I didn't understand that. {hint}");
    }
 
    private static bool TryPick(string input, List<Guid> ids, out Guid id)
    {
        id = default;
        if (!int.TryParse(input, out var n) || n < 1 || n > ids.Count) return false;
        id = ids[n - 1]; return true;
    }
 
    private static string Naira(decimal naira) => $"₦{naira:N0}";
}
}