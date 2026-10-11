using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Implementation
{
public class StoreReadinessService(SellFlowApiDbContext db, ISubscriptionService subs) : IStoreReadinessService
{
 
    public async Task<Readiness> CheckAsync(Guid businessId)
    {
        var missing = new List<string>();
        var b = await db.Businesses.Include(x => x.Settings).Include(x => x.WhatsApp).FirstAsync(x => x.Id == businessId);
 
        if (!await subs.HasActiveAccessAsync(businessId)) missing.Add("Active subscription");
        if (b.WhatsApp?.Status != WhatsAppStatus.Connected) missing.Add("Connect WhatsApp");
        if (!await db.Products.AnyAsync(p => p.BusinessId == businessId && p.IsActive && p.StockQuantity - p.ReservedQuantity > 0))
            missing.Add("Add at least one product in stock");
        if (!b.Settings.AcceptBankTransfer && !b.Settings.AcceptPayOnDelivery) missing.Add("Enable a payment method");
        if (b.Settings.DeliveryFee is null) missing.Add("Set a delivery fee");
 
        return new Readiness(missing.Count == 0, missing);
    }
}
}