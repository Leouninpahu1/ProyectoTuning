using System.Diagnostics;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;

namespace Turning.Infrastructure.AI;

/// <summary>
/// Adapter base (baseline) para la generación de texto mientras no se conecte un proveedor real (p. ej. OpenAI).
/// Es determinístico y basado en reglas simples: no invoca servicios externos, por lo que
/// prácticamente no falla; aun así, mide latencia y reporta su estado como lo exige el contrato.
/// </summary>
public sealed class RuleBasedTextGenerationAdapter : ITextGenerationPort
{
    private const string ProviderName = "mock";

    /// <inheritdoc />
    public Task<TextGenerationResult> GenerateInterlocutorReplyAsync(ExperimentSession session, IReadOnlyList<ConversationTurn> conversationHistory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(conversationHistory);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var latestParticipantTurn = conversationHistory
                .LastOrDefault(turn => turn.Sender == ConversationActor.Participant)?.Message;

            string text;
            if (string.IsNullOrWhiteSpace(latestParticipantTurn))
            {
                text = "Estoy listo para continuar con la sesion experimental.";
            }
            else
            {
                var normalizedExcerpt = latestParticipantTurn.Trim();

                if (normalizedExcerpt.Length > 140)
                {
                    normalizedExcerpt = normalizedExcerpt[..140].TrimEnd() + "...";
                }

                text = $"Entiendo: \"{normalizedExcerpt}\". Sigamos con la siguiente intervencion del experimento.";
            }

            stopwatch.Stop();

            var result = new TextGenerationResult(
                Text: text,
                Provider: ProviderName,
                LatencyMs: stopwatch.ElapsedMilliseconds,
                Degraded: false);

            return Task.FromResult(result);
        }
        catch
        {
            // Modo degradado: nunca debe tumbar la conversación. Se reporta explícitamente
            // en vez de propagar la excepción, tal como exige el contrato del baseline.
            stopwatch.Stop();

            var degradedResult = new TextGenerationResult(
                Text: "No fue posible generar una respuesta en este momento.",
                Provider: ProviderName,
                LatencyMs: stopwatch.ElapsedMilliseconds,
                Degraded: true);

            return Task.FromResult(degradedResult);
        }
    }
}
