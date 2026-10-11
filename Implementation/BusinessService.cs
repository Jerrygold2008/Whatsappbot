using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Exceptions;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace SellFlowApi.Implementation
{
   
public class BusinessService(SellFlowApiDbContext db) : IBusinessService
{
    public async Task<BusinessResponse> CreateAsync(Guid userId, CreateBusinessRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new DomainException("Business name is required.");
        if (await db.Businesses.AnyAsync(b => b.OwnerId == userId))
            throw new DomainException("You already have a business.");          // MVP: one account = one business
 
        var business = new Business { OwnerId = userId, Name = r.Name.Trim(), Description = r.Description?.Trim() };
        business.Settings.BusinessId = business.Id;
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return Map(business);
    }
 
    public async Task<BusinessResponse> GetAsync(Guid businessId) => Map(await Load(businessId));
 
    public async Task<BusinessResponse> UpdateAsync(Guid businessId, UpdateBusinessRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new DomainException("Business name is required.");
        var b = await Load(businessId);
        b.Name = r.Name.Trim();
        b.Description = r.Description?.Trim();
        await db.SaveChangesAsync();
        return Map(b);
    }
 
    public async Task<BusinessResponse> UpdateSettingsAsync(Guid businessId, UpdateSettingsRequest r)
    {
        if (r.DeliveryFee is < 0 || r.MaxPodOrderValue is < 0) throw new DomainException("Amounts cannot be negative.");
        if (!r.AcceptBankTransfer && !r.AcceptPayOnDelivery) throw new DomainException("Enable at least one payment method.");
 
        var b = await Load(businessId);
        var s = b.Settings;
        s.AcceptBankTransfer = r.AcceptBankTransfer;
        s.AcceptPayOnDelivery = r.AcceptPayOnDelivery;
        s.DeliveryFee = r.DeliveryFee;
        s.MaxPodOrderValue = r.MaxPodOrderValue;
        s.PodRequiresReturningCustomer = r.PodRequiresReturningCustomer;
        s.BankName = r.BankName?.Trim();
        s.AccountNumber = r.AccountNumber?.Trim();
        s.AccountName = r.AccountName?.Trim();
        var owner = new string((r.OwnerWhatsAppNumber ?? "").Where(char.IsDigit).ToArray());
        if (owner.StartsWith('0') && owner.Length == 11) owner = "234" + owner[1..];   // local NG format
        if (owner.Length is > 0 and < 10) throw new DomainException("Enter the owner WhatsApp number in international format, e.g. 2348012345678.");
        s.OwnerWhatsAppNumber = owner.Length == 0 ? null : owner;
        if (s.AcceptBankTransfer && !s.HasBankDetails)
            throw new DomainException("Add your bank name, account number and account name to accept bank transfers.");
        await db.SaveChangesAsync();
        return Map(b);
    }
 
    private async Task<Business> Load(Guid id) =>
        await db.Businesses.Include(b => b.Settings).Include(b => b.WhatsApp).FirstOrDefaultAsync(b => b.Id == id)
        ?? throw new NotFoundException("Business not found.");
 
    private static BusinessResponse Map(Business b) => new(
        b.Id, b.Name, b.Description, b.WhatsApp?.DisplayPhoneNumber,
        b.WhatsApp?.Status == WhatsAppStatus.Connected,
        new UpdateSettingsRequest(b.Settings.AcceptBankTransfer, b.Settings.AcceptPayOnDelivery, b.Settings.DeliveryFee,
            b.Settings.MaxPodOrderValue, b.Settings.PodRequiresReturningCustomer,
            b.Settings.BankName, b.Settings.AccountNumber, b.Settings.AccountName, b.Settings.OwnerWhatsAppNumber));
}
}