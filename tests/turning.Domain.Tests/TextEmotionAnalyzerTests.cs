using System.Diagnostics;
using FluentAssertions;
using Turning.Domain.Emotions;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Domain.Tests;

/// <summary>
/// Pruebas del analizador emocional de texto (RF-AES-01).
/// </summary>
/// <remarks>
/// Usan un léxico de prueba, no el del proyecto: así estas pruebas verifican el algoritmo
/// —intensificadores, negación, tipo de oración— y no el contenido del diccionario, que
/// cambia cada vez que alguien añade palabras.
/// </remarks>
public class TextEmotionAnalyzerTests
{
    private sealed class FakeLexicon : IEmotionLexicon
    {
        private readonly Dictionary<string, (string, double)> _terms = new(StringComparer.Ordinal)
        {
            ["feliz"] = (EmotionLabel.Joy, 0.8),
            ["contento"] = (EmotionLabel.Joy, 0.7),
            ["triste"] = (EmotionLabel.Sadness, 0.8),
            ["enojado"] = (EmotionLabel.Anger, 0.9),
            ["sorprendido"] = (EmotionLabel.Surprise, 0.8),
            ["confundido"] = (EmotionLabel.Confusion, 0.8),
            ["no entiendo"] = (EmotionLabel.Confusion, 0.95)
        };

        private readonly Dictionary<string, double> _modifiers = new(StringComparer.Ordinal)
        {
            ["muy"] = 1.5,
            ["poco"] = 0.5
        };

        public string Version => "lexicon-test";

        public IReadOnlyList<string> MultiWordTerms => ["no entiendo"];

        public (string Emotion, double Weight)? Lookup(string term) =>
            _terms.TryGetValue(term, out var hit) ? hit : null;

        public double? Modifier(string term) => _modifiers.TryGetValue(term, out var f) ? f : null;

        public bool IsNegator(string term) => term is "no" or "nunca";

        public double Polarity(string emotion) => emotion switch
        {
            EmotionLabel.Joy => 1.0,
            EmotionLabel.Sadness => -0.9,
            EmotionLabel.Anger => -1.0,
            EmotionLabel.Surprise => 0.2,
            EmotionLabel.Confusion => -0.2,
            _ => 0
        };
    }

    private static TextEmotionAnalyzer CreateAnalyzer() => new(new FakeLexicon());

    [Theory]
    [InlineData("Estoy feliz", EmotionLabel.Joy)]
    [InlineData("Me siento triste", EmotionLabel.Sadness)]
    [InlineData("Estoy enojado con esto", EmotionLabel.Anger)]
    [InlineData("Quedé sorprendido", EmotionLabel.Surprise)]
    [InlineData("Estoy confundido", EmotionLabel.Confusion)]
    public void Analyze_ShouldDetectEachLabel(string text, string expected)
    {
        CreateAnalyzer().Analyze(text).EmotionId.Should().Be(expected);
    }

    [Fact]
    public void Analyze_ShouldScaleIntensityWithModifiers()
    {
        var analyzer = CreateAnalyzer();

        var intense = analyzer.Analyze("Estoy muy feliz").Intensity;
        var plain = analyzer.Analyze("Estoy feliz").Intensity;
        var mild = analyzer.Analyze("Estoy poco feliz").Intensity;

        // "muy feliz" > "feliz" > "poco feliz": lo que pide RF-AES-01 sobre intensificadores.
        intense.Should().BeGreaterThan(plain);
        plain.Should().BeGreaterThan(mild);
    }

    [Fact]
    public void Analyze_ShouldNotReportJoy_WhenItIsNegated()
    {
        var result = CreateAnalyzer().Analyze("No estoy feliz");

        // Negar una emoción no afirma la contraria, así que tampoco debe dar tristeza.
        result.EmotionId.Should().NotBe(EmotionLabel.Joy);
        result.EmotionId.Should().Be(EmotionLabel.Neutral);
        result.FallbackUsed.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldTreatMultiWordTermsBeforeTheirParts()
    {
        var result = CreateAnalyzer().Analyze("No entiendo lo que pasa");

        // Si "no entiendo" se partiera en tokens, la negación invertiría "entiendo".
        result.EmotionId.Should().Be(EmotionLabel.Confusion);
        result.FallbackUsed.Should().BeFalse();
    }

    [Fact]
    public void Analyze_ShouldLeanTowardConfusion_ForQuestions()
    {
        CreateAnalyzer().Analyze("¿Qué está pasando aquí?").EmotionId.Should().Be(EmotionLabel.Confusion);
    }

    [Fact]
    public void Analyze_ShouldRaiseIntensity_ForExclamations()
    {
        var analyzer = CreateAnalyzer();

        analyzer.Analyze("¡Estoy feliz!").Intensity
            .Should().BeGreaterThan(analyzer.Analyze("Estoy feliz").Intensity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("qwerty zxcvbn plm")]
    public void Analyze_ShouldFallBackToNeutral_WhenThereIsNoSignal(string? text)
    {
        var result = CreateAnalyzer().Analyze(text);

        // Fallback obligatorio de RF-AES-01: neutral con intensidad 0.3.
        result.EmotionId.Should().Be(EmotionLabel.Neutral);
        result.Intensity.Should().Be(EmotionLabel.FallbackIntensity);
        result.FallbackUsed.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ShouldIgnoreAccentsAndCase()
    {
        CreateAnalyzer().Analyze("ESTOY MUY FELÍZ").EmotionId.Should().Be(EmotionLabel.Joy);
    }

    [Fact]
    public void Analyze_ShouldReportPolarityConsistentWithTheLabel()
    {
        var analyzer = CreateAnalyzer();

        analyzer.Analyze("Estoy feliz").Polarity.Should().BeGreaterThan(0);
        analyzer.Analyze("Estoy triste").Polarity.Should().BeLessThan(0);
    }

    [Fact]
    public void Analyze_ShouldSeparateConfidenceFromIntensity()
    {
        var result = CreateAnalyzer().Analyze("Estoy feliz");

        // Son magnitudes distintas: antes el esquema las confundía en un solo Score.
        result.Confidence.Should().BeInRange(0, 1);
        result.Intensity.Should().BeInRange(0, 1);
        result.Scores.Values.Sum().Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void Analyze_ShouldStayWithinTheLatencyBudget()
    {
        var analyzer = CreateAnalyzer();
        const string text = "Estoy muy feliz pero tambien un poco triste y no entiendo por que";

        analyzer.Analyze(text); // descarta el coste de la primera compilación del regex

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++)
            analyzer.Analyze(text);
        stopwatch.Stop();

        // RF-AES-01 pide < 100 ms por análisis; medimos el promedio de 1000.
        (stopwatch.Elapsed.TotalMilliseconds / 1000).Should().BeLessThan(100);
    }
}
