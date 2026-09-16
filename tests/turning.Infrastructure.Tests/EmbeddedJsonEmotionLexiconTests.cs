using FluentAssertions;
using Turning.Domain.Emotions;
using Turning.Domain.Entities;
using Turning.Infrastructure.AI.Lexicon;
using Xunit;

namespace Turning.Infrastructure.Tests;

/// <summary>
/// Pruebas del léxico real embebido en el ensamblado.
/// </summary>
/// <remarks>
/// Las pruebas del analizador usan un léxico falso, así que verifican el algoritmo pero no
/// que el archivo llegue a estar dentro del binario. Esa diferencia dejó pasar un fallo real:
/// el recurso se llamaba "emotion-lexicon.es.json" y MSBuild leyó el ".es" como código de
/// cultura, lo trató como recurso satélite y lo sacó del ensamblado principal. Todo compilaba
/// y todas las pruebas pasaban; la API reventaba en la primera petición.
/// </remarks>
public class EmbeddedJsonEmotionLexiconTests
{
    private static readonly EmbeddedJsonEmotionLexicon Lexicon = new();

    [Fact]
    public void Constructor_ShouldLoadTheEmbeddedResource()
    {
        Lexicon.Version.Should().NotBeNullOrWhiteSpace();
        Lexicon.Version.Should().StartWith("lexicon-");
    }

    [Theory]
    [InlineData("feliz", EmotionLabel.Joy)]
    [InlineData("triste", EmotionLabel.Sadness)]
    [InlineData("enojado", EmotionLabel.Anger)]
    [InlineData("sorpresa", EmotionLabel.Surprise)]
    [InlineData("confundido", EmotionLabel.Confusion)]
    public void Lookup_ShouldResolveTermsOfEveryLabel(string term, string expectedEmotion)
    {
        var hit = Lexicon.Lookup(term);

        hit.Should().NotBeNull();
        hit!.Value.Emotion.Should().Be(expectedEmotion);
        hit.Value.Weight.Should().BeInRange(0, 1);
    }

    [Fact]
    public void Lexicon_ShouldOnlyUseTheVocabularyOfTheRequirement()
    {
        // RF-AES-01 define seis etiquetas y ninguna más; una etiqueta fuera de esa lista
        // acabaría normalizada a neutral y la señal se perdería en silencio.
        var terms = new[] { "feliz", "triste", "enojado", "sorpresa", "confundido", "nervioso", "miedo" };

        foreach (var term in terms)
        {
            var hit = Lexicon.Lookup(term);
            if (hit is null) continue;

            EmotionLabel.IsKnown(hit.Value.Emotion).Should().BeTrue(
                "la etiqueta '{0}' del término '{1}' no pertenece al vocabulario de RF-AES-01",
                hit.Value.Emotion, term);
        }
    }

    [Fact]
    public void Modifier_ShouldRecognizeIntensifiersAndDiminishers()
    {
        Lexicon.Modifier("muy").Should().BeGreaterThan(1);
        Lexicon.Modifier("poco").Should().BeLessThan(1);
        Lexicon.Modifier("mesa").Should().BeNull();
    }

    [Fact]
    public void IsNegator_ShouldRecognizeNegations()
    {
        Lexicon.IsNegator("no").Should().BeTrue();
        Lexicon.IsNegator("nunca").Should().BeTrue();
        Lexicon.IsNegator("feliz").Should().BeFalse();
    }

    [Fact]
    public void MultiWordTerms_ShouldComeFromLongestToShortest()
    {
        Lexicon.MultiWordTerms.Should().NotBeEmpty();
        Lexicon.MultiWordTerms.Should().Contain("no entiendo");

        var wordCounts = Lexicon.MultiWordTerms.Select(t => t.Count(c => c == ' ')).ToArray();
        wordCounts.Should().BeInDescendingOrder();
    }

    [Fact]
    public void Polarity_ShouldBeConsistentWithTheLabels()
    {
        Lexicon.Polarity(EmotionLabel.Joy).Should().BeGreaterThan(0);
        Lexicon.Polarity(EmotionLabel.Anger).Should().BeLessThan(0);
        Lexicon.Polarity(EmotionLabel.Sadness).Should().BeLessThan(0);
    }

    [Fact]
    public void Analyzer_ShouldWorkEndToEndWithTheRealLexicon()
    {
        var analyzer = new TextEmotionAnalyzer(Lexicon);

        analyzer.Analyze("Estoy muy triste y me siento solo hoy").EmotionId.Should().Be(EmotionLabel.Sadness);
        analyzer.Analyze("Wow, esto es increible, me encanta!").EmotionId.Should().Be(EmotionLabel.Joy);
        analyzer.Analyze("No entiendo nada de lo que me dices").EmotionId.Should().Be(EmotionLabel.Confusion);
    }
}
