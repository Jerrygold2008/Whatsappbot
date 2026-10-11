using SellFlowApi.Interfaces;

namespace SellFlowApi.Jobs
{
public class SubscriptionExpiryJob(ISubscriptionService subs)
{
    // Runs daily: marks subscriptions past their end date as expired.
    public Task RunAsync() => subs.ExpireAsync();

    // TODO: send 3-day and 1-day expiry reminders
}
}
