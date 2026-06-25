using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

// One-shot cleanup for notifications whose case has been deleted.
//
// The Notification -> Case FK uses OnDelete(DeleteBehavior.SetNull), so removing a Case
// leaves its notifications behind with CaseId = NULL. Over time these accumulate and
// show up in the bell badge / notifications page even though clicking them goes nowhere
// useful (the linked case is gone).
//
// In this codebase every notification created via NotificationService.CreateAsync passes a
// concrete caseId (verified by grepping every CreateAsync call site), so a NULL CaseId is
// effectively the canonical "this notification's case was deleted" marker. Deleting those
// rows is exactly the "clear the notification which case are not present" cleanup the user
// asked for.
//
// A SystemSetting flag (`notification_orphan_cleanup_v1`) guards repeat execution so this
// is a true one-shot — re-running on every startup is safe and cheap because the flag check
// short-circuits, but it never silently wipes out newly-created notifications.
public static class NotificationCleanup
{
    private const string CleanupFlagKey = "notification_orphan_cleanup_v1";

    public static async Task CleanupOrphanedAsync(ProctorDbContext context, ILogger logger)
    {
        // 1. Guard: skip if we've already run successfully on this database.
        var flag = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == CleanupFlagKey);

        if (flag is { Value: "true" })
        {
            logger.LogInformation("[NotificationCleanup] Already completed in a previous run — skipping.");
            return;
        }

        // 2. Snapshot every Case id up front so the orphan check is a single in-memory
        //    HashSet lookup, not a per-row subquery.
        var existingCaseIdSet = (await context.Cases
            .AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync())
            .ToHashSet();

        // 3. Orphaned = CaseId was set and that case is no longer present. The
        //    SetNull FK behaviour means CaseId is NULL for these rows today, but we
        //    ALSO defensively reject any row whose CaseId points at a deleted case
        //    (shouldn't happen with the current FK config, but free insurance).
        var orphaned = await context.Notifications
            .Where(n => n.CaseId == null || !existingCaseIdSet.Contains(n.CaseId.Value))
            .ToListAsync();

        if (orphaned.Count == 0)
        {
            logger.LogInformation("[NotificationCleanup] No orphaned notifications found.");
        }
        else
        {
            context.Notifications.RemoveRange(orphaned);
            await context.SaveChangesAsync();
            logger.LogInformation(
                "[NotificationCleanup] Removed {Count} orphaned notification(s) referencing deleted cases.",
                orphaned.Count);
        }

        // 4. Mark the cleanup as done so we never re-run.
        context.SystemSettings.Add(new Domain.Entities.SystemSetting
        {
            Id = Guid.NewGuid(),
            Key = CleanupFlagKey,
            Value = "true",
            Category = "maintenance",
            Description = $"Set after one-shot cleanup of {orphaned.Count} orphaned notification(s)."
        });
        await context.SaveChangesAsync();
    }
}
