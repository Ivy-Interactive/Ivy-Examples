using System.Text.RegularExpressions;

namespace Readability.Score.Calculator.Apps;

public record TextStatistics(
    int WordCount,
    int SentenceCount,
    int SyllableCount,
    int CharacterCount,
    double AvgWordsPerSentence,
    double AvgSyllablesPerWord);

public record ReadabilityResult(
    double FleschReadingEase,
    double FleschKincaidGradeLevel,
    double GunningFogIndex,
    double ColemanLiauIndex,
    double SmogIndex,
    double AutomatedReadabilityIndex,
    TextStatistics Statistics);

public static partial class ReadabilityAnalyzer
{
    public static ReadabilityResult Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            var emptyStats = new TextStatistics(0, 0, 0, 0, 0, 0);
            return new ReadabilityResult(0, 0, 0, 0, 0, 0, emptyStats);
        }

        var words = TokenizeWords(text);
        var sentences = CountSentences(text);
        var wordCount = words.Length;
        var sentenceCount = Math.Max(sentences, 1);
        var syllableCounts = words.Select(CountSyllables).ToArray();
        var totalSyllables = syllableCounts.Sum();
        var characterCount = words.Sum(w => w.Length);
        var complexWordCount = syllableCounts.Count(s => s >= 3);

        var avgWordsPerSentence = (double)wordCount / sentenceCount;
        var avgSyllablesPerWord = wordCount > 0 ? (double)totalSyllables / wordCount : 0;


        var fleschEase = 206.835 - 1.015 * avgWordsPerSentence - 84.6 * avgSyllablesPerWord;


        var fleschKincaid = 0.39 * avgWordsPerSentence + 11.8 * avgSyllablesPerWord - 15.59;


        var complexRatio = wordCount > 0 ? (double)complexWordCount / wordCount : 0;
        var gunningFog = 0.4 * (avgWordsPerSentence + 100.0 * complexRatio);


        var l = wordCount > 0 ? (double)characterCount / wordCount * 100 : 0;
        var s = wordCount > 0 ? (double)sentenceCount / wordCount * 100 : 0;
        var colemanLiau = 0.0588 * l - 0.296 * s - 15.8;


        var smog = sentenceCount > 0
            ? 3.0 + Math.Sqrt((double)complexWordCount * 30.0 / sentenceCount)
            : 0;


        var avgCharsPerWord = wordCount > 0 ? (double)characterCount / wordCount : 0;
        var ari = 4.71 * avgCharsPerWord + 0.5 * avgWordsPerSentence - 21.43;

        var stats = new TextStatistics(
            wordCount, sentenceCount, totalSyllables, characterCount,
            Math.Round(avgWordsPerSentence, 2),
            Math.Round(avgSyllablesPerWord, 2));

        return new ReadabilityResult(
            Math.Round(fleschEase, 2),
            Math.Round(fleschKincaid, 2),
            Math.Round(gunningFog, 2),
            Math.Round(colemanLiau, 2),
            Math.Round(smog, 2),
            Math.Round(ari, 2),
            stats);
    }

    private static string[] TokenizeWords(string text)
    {
        return WordRegex().Matches(text)
            .Select(m => m.Value)
            .Where(w => w.Length > 0)
            .ToArray();
    }

    private static int CountSentences(string text)
    {
        var count = SentenceRegex().Matches(text).Count;
        return Math.Max(count, 1);
    }

    private static int CountSyllables(string word)
    {
        word = word.ToLowerInvariant();
        if (word.Length <= 2) return 1;

        var vowels = "aeiouy";
        var count = 0;
        var prevIsVowel = false;

        for (var i = 0; i < word.Length; i++)
        {
            var isVowel = vowels.Contains(word[i]);
            if (isVowel && !prevIsVowel)
            {
                count++;
            }
            prevIsVowel = isVowel;
        }


        if (word.EndsWith('e') && count > 1)
        {
            count--;
        }

        return Math.Max(count, 1);
    }

    [GeneratedRegex(@"[a-zA-Z]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[.!?]+")]
    private static partial Regex SentenceRegex();
}
