namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class WelcomeSlide : ViewBase
{
    private readonly GitHubStats _stats;

    public WelcomeSlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var userName = _stats.UserInfo.FullName ?? _stats.UserInfo.Id;

        return Layout.Vertical().Gap(5).AlignContent(Align.Center).Width(Size.Fraction(0.8f))
                   | (Layout.Vertical().Gap(4).AlignContent(Align.Center)
                      | (Layout.Vertical().Height(Size.Units(100)).Width(Size.Units(100))
                          | new Avatar(userName, _stats.UserInfo.AvatarUrl))
                      | Text.H1($"Hey, {userName}, your year on GitHub — wrapped.").Bold()
                      | Text.H2("Commits, pull requests, and the work you shipped in 2025.").Muted()
                      | Text.H3("Let's break down what made your year.").Muted());
    }
}
