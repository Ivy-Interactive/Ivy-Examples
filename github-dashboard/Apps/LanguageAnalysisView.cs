using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

public class LanguageAnalysisView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;
    private readonly int _refreshTrigger;

    public LanguageAnalysisView(IGitHubApiService gitHubService, string owner, string repo, int refreshTrigger)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
        _refreshTrigger = refreshTrigger;
    }

    public override object? Build()
    {
        var languages = this.UseState<List<LanguageInfo>?>(() => null);
        var isLoading = this.UseState(true);

        this.UseEffect(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    isLoading.Set(true);
                    var response = await _gitHubService.GetLanguagesAsync(_owner, _repo);
                    
                    if (response.Success)
                    {
                        languages.Set(response.Data);
                    }
                }
                catch (Exception)
                {
                    // Handle error silently for now
                }
                finally
                {
                    isLoading.Set(false);
                }
            });
        });

        if (isLoading.Value)
        {
            return Layout.Vertical().Gap(4)
                | Text.H2("Language Analysis")
                | Text.Muted("Loading language data...");
        }

        if (languages.Value == null || !languages.Value.Any())
        {
            return Layout.Vertical().Gap(4)
                | Text.H2("Language Analysis")
                | Text.Muted("No language data available");
        }

        return Layout.Vertical().Gap(4)
            | Text.H2("Language Analysis")
            | LanguageDistributionCard(languages.Value)
            | LanguageDetailsCard(languages.Value);
    }

    private object LanguageDistributionCard(List<LanguageInfo> languages)
    {
        var topLanguages = languages.Take(10).ToList();
        var totalBytes = languages.Sum(l => l.Bytes);

        return new Card(
            Layout.Vertical().Gap(3)
                | Text.H3($"Language Distribution ({languages.Count} languages)")
                | Layout.Vertical().Gap(2)
                    | topLanguages.Select((language, index) => 
                        Layout.Horizontal().Gap(3)
                            | Text.Small($"#{index + 1}")
                            | Text.P(language.Name)
                            | Text.Muted($"{language.Percentage:F1}%")
                            | Text.Small(FormatBytes(language.Bytes))
                    ).ToArray()
        ).Title("Top Languages");
    }

    private object LanguageDetailsCard(List<LanguageInfo> languages)
    {
        var totalBytes = languages.Sum(l => l.Bytes);
        var primaryLanguage = languages.FirstOrDefault();
        var otherLanguages = languages.Skip(1).ToList();

        return new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Language Breakdown")
                | Layout.Vertical().Gap(2)
                    | (primaryLanguage != null ? 
                        Layout.Horizontal().Gap(3)
                            | Text.Strong("Primary:")
                            | Text.P(primaryLanguage.Name)
                            | Text.Muted($"{primaryLanguage.Percentage:F1}%")
                            | Text.Small($"({FormatBytes(primaryLanguage.Bytes)})")
                        : Text.Muted("No primary language"))
                    | Layout.Horizontal().Gap(3)
                        | Text.Strong("Others:")
                        | Text.P($"{otherLanguages.Count} languages")
                        | Text.Muted($"{otherLanguages.Sum(l => l.Percentage):F1}%")
                    | Layout.Horizontal().Gap(3)
                        | Text.Strong("Total:")
                        | Text.P(FormatBytes(totalBytes))
        ).Title("Language Details");
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}