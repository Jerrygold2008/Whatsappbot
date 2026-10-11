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

// Manual onboarding while Meta Embedded Signup is pending.
public class AdminService(SellFlowApiDbContext db, ITokenEncryptor crypto) : IAdminService
{
    public async Task LinkWhatsAppAsync(LinkWhatsAppRequest r)
    {
        if (!await db.Businesses.AnyAsync(b => b.Id == r.BusinessId)) throw new NotFoundException("Business not found.");
        if (await db.WhatsAppAccounts.AnyAsync(a => a.PhoneNumberId == r.PhoneNumberId && a.BusinessId != r.BusinessId))
            throw new DomainException("That phone number is already linked to another business.");

        var acc = await db.WhatsAppAccounts.FirstOrDefaultAsync(a => a.BusinessId == r.BusinessId);
        if (acc is null) { acc = new WhatsAppAccount { BusinessId = r.BusinessId }; db.WhatsAppAccounts.Add(acc); }

        acc.WabaId = r.WabaId;
        acc.PhoneNumberId = r.PhoneNumberId;
        acc.DisplayPhoneNumber = r.DisplayPhoneNumber;
        acc.EncryptedAccessToken = crypto.Encrypt(r.AccessToken);
        acc.Status = WhatsAppStatus.Connected;
        await db.SaveChangesAsync();
    }

    public async Task SetPaystackSubaccountAsync(Guid businessId, string subaccountCode)
    {
        var b = await db.Businesses.FirstOrDefaultAsync(x => x.Id == businessId)
            ?? throw new NotFoundException("Business not found.");
        b.PaystackSubaccountCode = subaccountCode;
        await db.SaveChangesAsync();
    }
}

}