using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Services;

public sealed class SessionOptions
{
    public int DurationSeconds { get; set; } = 300;
    public int InactivitySeconds { get; set; } = 120;
}

public sealed class SessionSchedulerService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SessionSchedulerService> _log;
    private readonly SessionOptions _opts;

    public SessionSchedulerService(IServiceProvider sp, ILogger<SessionSchedulerService> log, IOptions<SessionOptions> opts)
    {
        _sp = sp;
        _log = log;
        _opts = opts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await RecoverAsync(ct);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(ct))
            await ExpireAsync(ct);
    }

    private async Task RecoverAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TurningDbContext>();
        var now = DateTime.UtcNow;
        var actives = await db.ExperimentSessions.Where(s => s.Status == ExperimentSessionStatus.Active).ToListAsync(ct);
        var recovered = 0; var timedOut = 0;
        foreach (var s in actives)
        {
            if ((s.ExpiresAtUtc.HasValue && now >= s.ExpiresAtUtc.Value) || (s.LastActivityAtUtc.HasValue && (now - s.LastActivityAtUtc.Value).TotalSeconds >= _opts.InactivitySeconds && (now - s.ActivatedAtUtc!.Value).TotalSeconds > 60))
            {
                var prev = s.Status;
                s.Expire(now);
                db.SessionAuditEntries.Add(SessionAuditEntry.Create(s.Id, prev, s.Status, "Scheduler", reason: "RecoveredTimedOut"));
                timedOut++;
            }
            else recovered++;
        }
        if (timedOut > 0) await db.SaveChangesAsync(ct);
        _log.LogInformation("Session recovery: {Recovered} active preserved, {TimedOut} timed out", recovered, timedOut);
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TurningDbContext>();
            var now = DateTime.UtcNow;
            var actives = await db.ExperimentSessions.Where(s => s.Status == ExperimentSessionStatus.Active).ToListAsync(ct);
            var changed = false;
            foreach (var s in actives)
            {
                var expired = s.ExpiresAtUtc.HasValue && now >= s.ExpiresAtUtc.Value;
                var inactive = s.LastActivityAtUtc.HasValue && (now - s.LastActivityAtUtc.Value).TotalSeconds >= _opts.InactivitySeconds && s.ActivatedAtUtc.HasValue && (now - s.ActivatedAtUtc.Value).TotalSeconds > 60;
                if (expired || inactive)
                {
                    var prev = s.Status;
                    s.Expire(now);
                    db.SessionAuditEntries.Add(SessionAuditEntry.Create(s.Id, prev, s.Status, "Scheduler", reason: expired ? "DurationExpired" : "Inactivity"));
                    changed = true;
                }
            }
            if (changed) await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) { _log.LogError(ex, "Scheduler tick failed"); }
    }
}
