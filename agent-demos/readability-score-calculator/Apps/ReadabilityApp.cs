using Ivy;

namespace Readability.Score.Calculator.Apps;

[App(icon: Icons.BookOpen)]
public class ReadabilityApp : ViewBase
{
    public override object Build()
    {
        var text = UseState("");

        var result = string.IsNullOrWhiteSpace(text.Value)
            ? null
            : ReadabilityAnalyzer.Analyze(text.Value);

        var hasResult = result is not null;

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10).Height(Size.Full())
                | Text.H2("Readability Score Calculator")
                | Text.Muted("Paste or type text to analyze its readability across multiple indices.")
                | new Separator()
                | (Layout.Horizontal().Height(Size.Full())
                    | (Layout.Vertical().Width(Size.Half())
                        | Text.H3("Input Text")
                        | text.ToTextareaInput()
                            .Placeholder("Paste or type your text here...")
                            .Height(Size.Units(80))
                    )
                    | (Layout.Vertical().Width(Size.Half())
                        | Text.H3("Results")
                        | (hasResult
                            ? BuildResults(result!)
                            : (object)Text.Muted("Enter some text to see readability scores."))
                    )
                )
            );
    }

    private static object BuildResults(ReadabilityResult result)
    {
        var (label, variant) = GetFleschLabel(result.FleschReadingEase);

        return Layout.Vertical()
            | (Layout.Horizontal()
                | Text.P($"Flesch Reading Ease: {result.FleschReadingEase}").Bold()
                | new Badge(label).Variant(variant)
            )
            | new Separator()
            | Text.H3("Score Details")
            | (Layout.Grid().Columns(2)
                | BuildScoreCard("Flesch Reading Ease", result.FleschReadingEase, "Higher = easier to read")
                | BuildScoreCard("Flesch-Kincaid Grade", result.FleschKincaidGradeLevel, "US school grade level")
                | BuildScoreCard("Gunning Fog Index", result.GunningFogIndex, "Years of education needed")
                | BuildScoreCard("Coleman-Liau Index", result.ColemanLiauIndex, "US school grade level")
                | BuildScoreCard("SMOG Index", result.SmogIndex, "Years of education needed")
                | BuildScoreCard("Automated Readability", result.AutomatedReadabilityIndex, "US school grade level")
            )
            | new Separator()
            | Text.H3("Text Statistics")
            | (Layout.Grid().Columns(3)
                | BuildStatCard("Words", result.Statistics.WordCount.ToString())
                | BuildStatCard("Sentences", result.Statistics.SentenceCount.ToString())
                | BuildStatCard("Syllables", result.Statistics.SyllableCount.ToString())
                | BuildStatCard("Characters", result.Statistics.CharacterCount.ToString())
                | BuildStatCard("Avg Words/Sentence", result.Statistics.AvgWordsPerSentence.ToString("F1"))
                | BuildStatCard("Avg Syllables/Word", result.Statistics.AvgSyllablesPerWord.ToString("F2"))
            );
    }

    private static Card BuildScoreCard(string title, double score, string description)
    {
        return new Card()
            | (Layout.Vertical().Gap(2)
                | Text.Muted(title)
                | Text.H2(score.ToString("F1"))
                | Text.Muted(description)
            );
    }

    private static Card BuildStatCard(string title, string value)
    {
        return new Card()
            | (Layout.Vertical().Gap(2)
                | Text.Muted(title)
                | Text.P(value).Bold()
            );
    }

    private static (string Label, BadgeVariant Variant) GetFleschLabel(double score) => score switch
    {
        >= 90 => ("Very Easy (5th grade)", BadgeVariant.Success),
        >= 80 => ("Easy (6th grade)", BadgeVariant.Success),
        >= 70 => ("Fairly Easy (7th grade)", BadgeVariant.Info),
        >= 60 => ("Standard (8th-9th grade)", BadgeVariant.Primary),
        >= 50 => ("Fairly Difficult (10th-12th grade)", BadgeVariant.Warning),
        >= 30 => ("Difficult (College)", BadgeVariant.Warning),
        _ => ("Very Confusing (College Graduate)", BadgeVariant.Destructive),
    };
}
