using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Turning.Domain.Entities;

namespace Turning.Infrastructure.Persistence;

/// <summary>
/// Siembra datos iniciales para desarrollo y pruebas locales.
/// Idempotente: si ya existen usuarios, no vuelve a insertar nada.
/// Carga el dataset sintético (data/synthetic-v1.csv) con 20 conversaciones
/// (10 Human + 10 AI) para que el equipo pueda trabajar sin depender de datos reales.
/// </summary>
public static class TurningDbSeeder
{
    public static async Task SeedAsync(TurningDbContext db, CancellationToken ct = default)
    {
        if (await db.UserAccounts.AnyAsync(ct))
            return;

        // --- Usuarios base ---
        var admin = UserAccount.Create("admin@turning.local", "Admin Turning", "$2a$11$seed.hash.placeholder.admin", "Administrator");
        var researcher = UserAccount.Create("researcher@turning.local", "Researcher Demo", "$2a$11$seed.hash.placeholder.researcher", "Researcher");
        db.UserAccounts.AddRange(admin, researcher);

        // --- Cargar dataset sintético desde data/synthetic-v1.csv ---
        var csvPath = FindSyntheticDatasetPath();

        if (csvPath is null)
        {
            // Fallback: si no se encuentra el CSV (p. ej. entorno sin acceso al repo raíz),
            // se siembran 2 sesiones mínimas para no bloquear el arranque.
            var fallbackHuman = ExperimentSession.Create(admin.Id, ExperimentalCondition.Human);
            var fallbackAi = ExperimentSession.Create(researcher.Id, ExperimentalCondition.AI);
            db.ExperimentSessions.AddRange(fallbackHuman, fallbackAi);
            await db.SaveChangesAsync(ct);
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath, ct);
        // header: SessionId,SessionCode,Condition,SequenceNumber,Sender,Message,CreatedAtUtc
        var rows = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(ParseCsvLine).ToList();

        var sessionGroups = rows.GroupBy(r => r.SessionId);
        var owners = new[] { admin.Id, researcher.Id };
        var ownerIndex = 0;

        foreach (var group in sessionGroups)
        {
            var first = group.First();
            var condition = first.Condition.Equals("AI", StringComparison.OrdinalIgnoreCase)
                ? ExperimentalCondition.AI
                : ExperimentalCondition.Human;

            var ownerId = owners[ownerIndex % owners.Length];
            ownerIndex++;

            var session = ExperimentSession.Create(ownerId, condition, TimeSpan.FromMinutes(30), first.SessionId);
            db.ExperimentSessions.Add(session);

            foreach (var row in group.OrderBy(r => r.SequenceNumber))
            {
                var sender = row.Sender.Equals("Participant", StringComparison.OrdinalIgnoreCase)
                    ? ConversationActor.Participant
                    : ConversationActor.Interlocutor;

                var turn = ConversationTurn.Create(session.Id, row.SequenceNumber, sender, row.Message);
                db.ConversationTurns.Add(turn);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private sealed record SyntheticRow(
        Guid SessionId,
        string SessionCode,
        string Condition,
        int SequenceNumber,
        string Sender,
        string Message,
        DateTime CreatedAtUtc);

    private static SyntheticRow ParseCsvLine(string line)
    {
        var fields = SplitCsvRespectingQuotes(line);
        return new SyntheticRow(
            SessionId: Guid.Parse(fields[0]),
            SessionCode: fields[1],
            Condition: fields[2],
            SequenceNumber: int.Parse(fields[3], CultureInfo.InvariantCulture),
            Sender: fields[4],
            Message: fields[5],
            CreatedAtUtc: DateTime.Parse(fields[6], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));
    }

    /// <summary>
    /// Parser simple de CSV que respeta comillas dobles (para mensajes con comas).
    /// </summary>
    private static string[] SplitCsvRespectingQuotes(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    /// <summary>
    /// Busca data/synthetic-v1.csv subiendo desde el directorio base hasta encontrar la raíz del repo.
    /// </summary>
    private static string? FindSyntheticDatasetPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "synthetic-v1.csv");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
