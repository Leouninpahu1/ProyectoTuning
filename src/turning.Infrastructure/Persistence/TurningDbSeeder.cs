using Microsoft.EntityFrameworkCore;
using Turning.Domain.Entities;

namespace Turning.Infrastructure.Persistence;

public static class TurningDbSeeder
{
    public static async Task SeedAsync(TurningDbContext db, CancellationToken ct = default)
    {
        if (await db.UserAccounts.AnyAsync(ct))
            return;

        var admin = UserAccount.Create("admin@turning.local", "Admin Turning", "$2a$11$seed.hash.placeholder.admin", "Administrator");
        var researcher = UserAccount.Create("researcher@turning.local", "Researcher Demo", "$2a$11$seed.hash.placeholder.researcher", "Researcher");
        db.UserAccounts.AddRange(admin, researcher);

        var session1 = ExperimentSession.Create(admin.Id, ExperimentalCondition.Human);
        var session2 = ExperimentSession.Create(researcher.Id, ExperimentalCondition.AI);
        db.ExperimentSessions.AddRange(session1, session2);

        await db.SaveChangesAsync(ct);
    }
}
