using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Turning.Application.Features.AI;
using Turning.Application.Interfaces;
using Turning.Infrastructure.AI;
using Turning.Infrastructure.AI.Providers;
using Xunit;

namespace Turning.Infrastructure.Tests;

/// <summary>
/// Pruebas de la cadena de proveedores y su caída ordenada.
/// </summary>
public class TextGenerationRouterTests
{
    private sealed class FakeProvider : ITextGenerationProvider
    {
        private readonly Func<ProviderReply> _behaviour;

        public FakeProvider(string name, Func<ProviderReply> behaviour, bool configured = true)
        {
            Name = name;
            _behaviour = behaviour;
            IsConfigured = configured;
        }

        public string Name { get; }
        public bool IsConfigured { get; }
        public int Calls { get; private set; }

        public Task<ProviderReply> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_behaviour());
        }
    }

    private static ProviderReply Ok(string text = "Respuesta real.") => ProviderReply.Ok(text, "modelo", 30, 1);

    private static ProviderReply Fail(ProviderFailureKind kind) =>
        ProviderReply.Failed(kind, kind.ToString(), 10, 1);

    private static TextGenerationRouter CreateRouter(
        IEnumerable<ITextGenerationProvider> providers,
        AiOptions? options = null,
        ProviderCircuitBreaker? breaker = null)
    {
        options ??= new AiOptions();
        return new TextGenerationRouter(
            providers,
            breaker ?? new ProviderCircuitBreaker(options.CircuitFailureThreshold, options.CircuitWindowSeconds, options.CircuitBreakSeconds),
            Options.Create(options),
            NullLogger<TextGenerationRouter>.Instance);
    }

    private static TextGenerationRequest Request() => new()
    {
        SessionId = Guid.NewGuid(),
        OriginatingTurnId = Guid.NewGuid(),
        UserInput = "Hola",
        ConversationHistory = []
    };

    [Fact]
    public async Task GenerateAsync_ShouldUseTheFirstProviderThatAnswers()
    {
        var openai = new FakeProvider("openai", () => Ok());
        var openrouter = new FakeProvider("openrouter", () => Ok());
        var rules = new FakeProvider("rule-based", () => Ok());

        var result = await CreateRouter([openai, openrouter, rules]).GenerateAsync(Request());

        result.Provider.Should().Be("openai");
        result.Degraded.Should().BeFalse();
        openrouter.Calls.Should().Be(0);
        rules.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_ShouldFallToOpenRouter_WhenOpenAiHasNoCredit()
    {
        // Es el escenario que motiva toda la cadena: sin saldo, hay que llegar al gratuito.
        var openai = new FakeProvider("openai", () => Fail(ProviderFailureKind.Quota));
        var openrouter = new FakeProvider("openrouter", () => Ok("Respuesta del gratuito."));
        var rules = new FakeProvider("rule-based", () => Ok());

        var result = await CreateRouter([openai, openrouter, rules]).GenerateAsync(Request());

        result.Provider.Should().Be("openrouter");
        result.Text.Should().Be("Respuesta del gratuito.");
        result.ProvidersTried.Should().ContainInOrder("openai", "openrouter");
        result.Degraded.Should().BeFalse();
        rules.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_ShouldFallToRules_AndMarkDegraded_WhenEveryProviderFails()
    {
        var openai = new FakeProvider("openai", () => Fail(ProviderFailureKind.Credentials));
        var openrouter = new FakeProvider("openrouter", () => Fail(ProviderFailureKind.Transient));
        var rules = new FakeProvider("rule-based", () => Ok("Respuesta de reglas."));

        var result = await CreateRouter([openai, openrouter, rules]).GenerateAsync(Request());

        // La conversación continúa, pero el experimento tiene que saber que ese turno no
        // vino del modelo que pretendía medir.
        result.Provider.Should().Be("rule-based");
        result.Degraded.Should().BeTrue();
        result.ProvidersTried.Should().ContainInOrder("openai", "openrouter", "rule-based");
    }

    [Fact]
    public async Task GenerateAsync_ShouldSkipProvidersWithoutConfiguration()
    {
        var openai = new FakeProvider("openai", () => Ok(), configured: false);
        var rules = new FakeProvider("rule-based", () => Ok("Reglas."));

        var result = await CreateRouter([openai, rules]).GenerateAsync(Request());

        openai.Calls.Should().Be(0);
        result.Provider.Should().Be("rule-based");
        result.ProvidersTried.Should().NotContain("openai");
    }

    [Fact]
    public async Task GenerateAsync_ShouldRespectTheConfiguredOrder()
    {
        var openai = new FakeProvider("openai", () => Ok());
        var openrouter = new FakeProvider("openrouter", () => Ok("Del router."));
        var rules = new FakeProvider("rule-based", () => Ok());
        var options = new AiOptions { ProviderOrder = ["openrouter", "openai", "rule-based"] };

        var result = await CreateRouter([openai, openrouter, rules], options).GenerateAsync(Request());

        result.Provider.Should().Be("openrouter");
        openai.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_ShouldStopCallingAProviderOnceItsCircuitOpens()
    {
        var breaker = new ProviderCircuitBreaker(failureThreshold: 3, windowSeconds: 60, breakSeconds: 30);
        var openai = new FakeProvider("openai", () => Fail(ProviderFailureKind.Transient));
        var rules = new FakeProvider("rule-based", () => Ok());
        var router = CreateRouter([openai, rules], breaker: breaker);

        for (var i = 0; i < 5; i++)
            await router.GenerateAsync(Request());

        // Tras alcanzar el umbral, el proveedor se salta sin gastar una llamada más.
        openai.Calls.Should().Be(3);
        breaker.GetState("openai").Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task GenerateAsync_ShouldKeepAProviderOutForTheRest_WhenItsKeyIsInvalid()
    {
        var breaker = new ProviderCircuitBreaker(failureThreshold: 10, windowSeconds: 60, breakSeconds: 30);
        var openai = new FakeProvider("openai", () => Fail(ProviderFailureKind.Credentials));
        var rules = new FakeProvider("rule-based", () => Ok());
        var router = CreateRouter([openai, rules], breaker: breaker);

        await router.GenerateAsync(Request());
        await router.GenerateAsync(Request());
        await router.GenerateAsync(Request());

        // Una sola vez: insistir con una clave inválida gastaría el presupuesto de todos
        // los turnos siguientes.
        openai.Calls.Should().Be(1);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReportNoProvider_WhenTheChainIsEmpty()
    {
        var result = await CreateRouter([]).GenerateAsync(Request());

        result.Provider.Should().Be("none");
        result.Degraded.Should().BeTrue();
        result.Text.Should().BeEmpty();
        result.FailureReason.Should().Be("NoProviderAvailable");
    }

    [Fact]
    public async Task GenerateAsync_ShouldSurviveAProviderThatThrows()
    {
        var broken = new FakeProvider("openai", () => throw new InvalidOperationException("boom"));
        var rules = new FakeProvider("rule-based", () => Ok("Reglas."));

        var result = await CreateRouter([broken, rules]).GenerateAsync(Request());

        result.Provider.Should().Be("rule-based");
        result.Degraded.Should().BeTrue();
    }
}
