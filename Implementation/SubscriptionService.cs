using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;    
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Interfaces;
using SellFlowApi.Models;

namespace SellFlowApi.Implementation
{
    
public class SubscriptionService(SellFlowApiDbContext db, IPaymentGateway gateway) : ISubscriptionService
{
    public async Task<bool> HasActiveAccessAsync(Guid businessId) =>
        await db.Subscriptions.AnyAsync(s => s.BusinessId == businessId
            && s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow);

    // Step 1: merchant clicks "Pay ₦200"
    public async Task<string> StartPaymentAsync(Guid businessId, string merchantEmail, Guid? planId = null)
    {
        var plan = planId is null
            ? await db.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.Price).FirstAsync()
            : await db.SubscriptionPlans.FirstAsync(p => p.Id == planId && p.IsActive);

        var payment = new Payment
        {
            BusinessId = businessId, Purpose = PaymentPurpose.Subscription, PlanId = plan.Id,
            Reference = $"SUB-{Guid.NewGuid():N}", Amount = plan.Price
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // No subaccount: subscription money is platform revenue.
        return await gateway.InitializeAsync(payment.Reference, payment.Amount, merchantEmail, subaccount: null);
    }

    // Step 2: called ONLY from verified webhook processing
    public async Task ActivateAsync(Guid businessId, Guid planId)
    {
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Id == planId);

        // Renewal extends from the later of now / current end, so early renewals lose nothing.
        var currentEnd = await db.Subscriptions
            .Where(s => s.BusinessId == businessId && s.Status == SubscriptionStatus.Active)
            .MaxAsync(s => (DateTime?)s.EndDate);

        var start = currentEnd is { } e && e > DateTime.UtcNow ? e : DateTime.UtcNow;

        db.Subscriptions.Add(new Subscription
        {
            BusinessId = businessId, PlanId = plan.Id,
            StartDate = start, EndDate = start.AddDays(plan.DurationInDays),
            Status = SubscriptionStatus.Active
        });
    }

    // Daily job
    public Task ExpireAsync() =>
        db.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SubscriptionStatus.Expired));

    public async Task<List<PlanResponse>> GetPlansAsync()
    {
        var plans = await db.SubscriptionPlans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Price).ToListAsync();
        return plans.Select(p => new PlanResponse(p.Id, p.Name, p.Price, p.DurationInDays)).ToList();
    }

    public async Task<SubscriptionStatusResponse> GetStatusAsync(Guid businessId)
    {
        var sub = await db.Subscriptions.AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.EndDate).FirstOrDefaultAsync();
        if (sub is null) return new SubscriptionStatusResponse(false, null, null);
        var plan = await db.SubscriptionPlans.AsNoTracking().FirstAsync(p => p.Id == sub.PlanId);
        return new SubscriptionStatusResponse(true, sub.EndDate, plan.Name);
    }
}
}