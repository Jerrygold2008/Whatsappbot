using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Jobs
{
// Daily stock reminder to every business owner who has set an owner WhatsApp number.
public class LowStockReminderJob(SellFlowApiDbContext db, IOwnerAlertService alerts)
{
    public async Task RunAsync()
    {
        var businessIds = await db.BusinessSettings.AsNoTracking()
            .Where(s => s.OwnerWhatsAppNumber != null)
            .Select(s => s.BusinessId).ToListAsync();
        foreach (var id in businessIds)
            await alerts.SendLowStockReminderAsync(id);
    }
}
}
