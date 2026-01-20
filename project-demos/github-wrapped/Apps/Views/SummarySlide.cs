namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;
using Ivy.Helpers;
using SkiaSharp;

public class SummarySlide : ViewBase
{
    private readonly GitHubStats _stats;

    public SummarySlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var userName = _stats.UserInfo.FullName ?? _stats.UserInfo.Id;
        var userStatus = DetermineUserStatus(_stats);
        var topLanguage = _stats.LanguageBreakdown
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();
        
        // Animated values
        var animatedCommits = this.UseState(0);
        var animatedPRs = this.UseState(0);
        var animatedStreak = this.UseState(0);
        var animatedDays = this.UseState(0);
        var refresh = this.UseRefreshToken();
        
        this.UseEffect(() =>
        {
            var scheduler = new JobScheduler(maxParallelJobs: 2);
            var steps = 50;
            var delayMs = 20;
            
            scheduler.CreateJob("Animate Stats")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentProgress = i / (double)steps;
                        animatedCommits.Set((int)(_stats.TotalCommits * currentProgress));
                        animatedPRs.Set((int)(_stats.PullRequestsCreated * currentProgress));
                        animatedStreak.Set((int)(_stats.LongestStreak * currentProgress));
                        animatedDays.Set((int)(_stats.TotalContributionDays * currentProgress));
                        refresh.Refresh();
                        progress.Report(currentProgress);
                        await Task.Delay(delayMs, token);
                    }
                    animatedCommits.Set(_stats.TotalCommits);
                    animatedPRs.Set(_stats.PullRequestsCreated);
                    animatedStreak.Set(_stats.LongestStreak);
                    animatedDays.Set(_stats.TotalContributionDays);
                    refresh.Refresh();
                })
                .Build();
            
            _ = Task.Run(async () => await scheduler.RunAsync());
        });
        
        return Layout.Vertical().Gap(4).Align(Align.Center)
                | Text.H1("My2025").Bold().WithConfetti(AnimationTrigger.Auto)
                | (Layout.Horizontal().Gap(4).Align(Align.Stretch).Width(Size.Fraction(0.8f)).Height(Size.Full())
                    | BuildStatsCard(animatedCommits.Value, animatedPRs.Value, animatedDays.Value)
                    | (Layout.Vertical().Gap(3).Height(Size.Full())
                        | BuildStatusCard(userStatus)
                        | BuildTopLanguageCard(topLanguage)));
    }
    
    public static byte[] GenerateSummaryImage(GitHubStats stats)
    {
        var userStatus = DetermineUserStatus(stats);
        var topLanguage = stats.LanguageBreakdown
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();
        return GenerateSummaryImageInternal(userStatus, topLanguage, stats);
    }
    
    private static byte[] GenerateSummaryImageInternal(
        (string Title, string MainText, string SubText, string Narrative) userStatus,
        KeyValuePair<string, long> topLanguage,
        GitHubStats stats)
    {
        const int width = 1200;
        const int height = 800;
        const int padding = 60;
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        
        var paint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };
        
        var titlePaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Geist", SKFontStyle.Bold),
            TextSize = 48,
            Color = SKColors.Black
        };
        
        var headingPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextSize = 36,
            Color = SKColors.Black
        };
        
        var subHeadingPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
            TextSize = 32,
            Color = SKColors.Black
        };
        
        var bodyPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
            TextSize = 20,
            Color = new SKColor(0x66, 0x66, 0x66)
        };
        
        var yPos = padding;
        
        // Title
        canvas.DrawText("My2025", width / 2f, yPos + 50, titlePaint);
        yPos += 80;
        
        // Main content area
        var contentWidth = width - (padding * 2);
        var leftCardWidth = contentWidth * 0.35f;
        var rightCardWidth = contentWidth * 0.65f;
        var cardSpacing = contentWidth * 0.015f;
        var cardHeight = height - yPos - padding - 40;
        
        var leftCardX = padding;
        var rightCardX = padding + leftCardWidth + cardSpacing;
        var cardY = yPos;
        
        // Draw cards background
        var cardPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        
        var strokePaint = new SKPaint
        {
            Color = new SKColor(0xE0, 0xE0, 0xE0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        // Left card - Stats (full height)
        var leftCardRect = new SKRect(leftCardX, cardY, leftCardX + leftCardWidth, cardY + cardHeight);
        canvas.DrawRoundRect(leftCardRect, 12, 12, cardPaint);
        canvas.DrawRoundRect(leftCardRect, 12, 12, strokePaint);
        
        // Right side - two separate cards
        var statusCardHeight = cardHeight * 0.49f;
        var languageCardHeight = cardHeight * 0.49f;
        var cardGap = cardHeight * 0.02f;;
        
        // Status card (top right)
        var statusCardRect = new SKRect(rightCardX, cardY, rightCardX + rightCardWidth, cardY + statusCardHeight);
        canvas.DrawRoundRect(statusCardRect, 12, 12, cardPaint);
        canvas.DrawRoundRect(statusCardRect, 12, 12, strokePaint);
        
        // Language card (bottom right)
        var languageCardY = cardY + statusCardHeight + cardGap;
        var languageCardRect = new SKRect(rightCardX, languageCardY, rightCardX + rightCardWidth, languageCardY + languageCardHeight);
        canvas.DrawRoundRect(languageCardRect, 12, 12, cardPaint);
        canvas.DrawRoundRect(languageCardRect, 12, 12, strokePaint);
        
        // Left card - Stats (evenly distributed with padding)
        var leftCardTextX = leftCardX + leftCardWidth / 2f;
        var cardPadding = 40f; // Padding from top and bottom
        var usableHeight = cardHeight - (cardPadding * 2f);
        var sectionHeight = usableHeight / 3f; // Divide usable area into 3 equal sections
        
        // Smaller heading for better spacing
        var statsHeadingPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextSize = 32,
            Color = SKColors.Black
        };
        
        var statsBodyPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
            TextSize = 20,
            Color = new SKColor(0x66, 0x66, 0x66)
        };
        
        // Section 1: Active Days
        var section1Y = cardY + cardPadding + sectionHeight / 2f - 10;
        canvas.DrawText($"{stats.TotalContributionDays} days", leftCardTextX, section1Y, statsHeadingPaint);
        canvas.DrawText("You showed up again and again", leftCardTextX, section1Y + 35, statsBodyPaint);
        
        // Section 2: Commits
        var section2Y = cardY + cardPadding + sectionHeight + sectionHeight / 2f - 10;
        canvas.DrawText($"{stats.TotalCommits} commits", leftCardTextX, section2Y, statsHeadingPaint);
        canvas.DrawText("Progress in small steps", leftCardTextX, section2Y + 35, statsBodyPaint);
        
        // Section 3: PRs
        var section3Y = cardY + cardPadding + sectionHeight * 2f + sectionHeight / 2f - 10;
        canvas.DrawText($"{stats.PullRequestsCreated} PRs", leftCardTextX, section3Y, statsHeadingPaint);
        canvas.DrawText("You didn't just code — you shipped", leftCardTextX, section3Y + 35, statsBodyPaint);
        
        // Status card content
        var rightCardTextX = rightCardX + rightCardWidth / 2f;
        var statusTextY = cardY + 60;
        
        var smallLabelPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
            TextSize = 16,
            Color = new SKColor(0x99, 0x99, 0x99)
        };
        
        var mainTextPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
            TextSize = 20,
            Color = SKColors.Black
        };
        
        var subTextPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
            TextSize = 20,
            Color = new SKColor(0x99, 0x99, 0x99)
        };
        
        // Draw trophy icon (full height of status card)
        var iconSize = statusCardHeight - 120f; // Full height minus padding
        var iconX = rightCardX + 40f;
        var iconY = cardY + (statusCardHeight - iconSize) / 2f; // Center vertically
        DrawTrophyIconStatic(canvas, iconX, iconY, iconSize);
        
        // Calculate text area (to the right of icon)
        var iconRight = iconX + iconSize;
        var textAreaLeft = iconRight + 50f; // Gap after icon (increased)
        var textAreaWidth = rightCardX + rightCardWidth - textAreaLeft - 40f; // Width minus right padding
        var textCenterX = textAreaLeft + textAreaWidth / 2f; // Center of text area
        
        canvas.DrawText("Your Developer Status — 2025", textCenterX, statusTextY, smallLabelPaint);
        statusTextY += 45;
        canvas.DrawText(userStatus.Title.ToUpper(), textCenterX, statusTextY, headingPaint);
        statusTextY += 70;
        
        // Draw main text with wrapping
        var mainTextLines = WrapTextStatic(userStatus.MainText, textAreaWidth, mainTextPaint);
        foreach (var line in mainTextLines)
        {
            canvas.DrawText(line, textCenterX, statusTextY, mainTextPaint);
            statusTextY += 30;
        }
        
        statusTextY += 20;
        
        // Draw sub text with wrapping
        var subTextLines = WrapTextStatic(userStatus.SubText, textAreaWidth, subTextPaint);
        foreach (var line in subTextLines)
        {
            canvas.DrawText(line, textCenterX, statusTextY, subTextPaint);
            statusTextY += 30;
        }
        
        // Language card content
        var totalBytes = stats.LanguageBreakdown.Values.Sum();
        var languagePercentage = totalBytes > 0
            ? Math.Round((topLanguage.Value / (double)totalBytes) * 100, 0) 
            : 0;
        var languageName = topLanguage.Key ?? "N/A";
        
        // Calculate motivational text (same logic as BuildTopLanguageCard)
        var motivationalText = languagePercentage >= 70
            ? "This is your superpower! You created most of your code with it."
            : languagePercentage >= 50
                ? "More than half of your code was written in this language. You know what you're doing!"
                : "This language helped you realize most of your ideas this year. Keep it up!";
        
        var langPadding = 100f; // Padding from top
        var langTextY = languageCardY + langPadding;
        var languageTitleText = $"{languageName.ToUpper()} - THIS WAS YOUR LANGUAGE";
        canvas.DrawText(languageTitleText, rightCardTextX, langTextY, headingPaint);
        langTextY += 55;
        canvas.DrawText($"{languagePercentage}% of your code was written in {languageName}", rightCardTextX, langTextY, bodyPaint);
        langTextY += 45;
        canvas.DrawText(motivationalText, rightCardTextX, langTextY, bodyPaint);
        
        // Add Ivy Framework link at the bottom
        var linkPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextSize = 20,
            Color = new SKColor(0x01, 0xD1, 0x8E) // #01D18E
        };
        
        var linkY = height - padding + 30f; // Position at bottom with padding
        var linkText = "Built with Ivy Framework - github.com/Ivy-Interactive/Ivy-Framework";
        canvas.DrawText(linkText, rightCardTextX + 30f, linkY - 30f, linkPaint);
        
        // Encode to PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    
    private static List<string> WrapTextStatic(string text, float maxWidth, SKPaint paint)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";
        
        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var width = paint.MeasureText(testLine);
            
            if (width <= maxWidth)
            {
                currentLine = testLine;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
        }
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }
        
        return lines.Count > 0 ? lines : new List<string> { text };
    }
    
    private static void DrawTrophyIconStatic(SKCanvas canvas, float x, float y, float size)
    {
        try
        {
            // Try multiple paths where SVG might be located
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "svg", "trophy.svg"),
                Path.Combine(Directory.GetCurrentDirectory(), "svg", "trophy.svg"),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "svg", "trophy.svg")
            };
            
            string? svgPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    svgPath = path;
                    break;
                }
            }
            
            if (svgPath != null)
            {
                var svg = new SkiaSharp.Extended.Svg.SKSvg();
                using var stream = File.OpenRead(svgPath);
                var picture = svg.Load(stream);
                
                if (picture != null)
                {
                    var bounds = picture.CullRect;
                    var scale = size / Math.Max(bounds.Width, bounds.Height);
                    
                    canvas.Save();
                    canvas.Translate(x, y);
                    canvas.Scale(scale, scale);
                    canvas.DrawPicture(picture);
                    canvas.Restore();
                }
            }
        }
        catch (Exception ex)
        {
            // If SVG fails to load, draw nothing or log error
            Console.WriteLine($"Failed to load trophy SVG: {ex.Message}");
        }
    }

    private static (string Title, string MainText, string SubText, string Narrative) DetermineUserStatus(GitHubStats stats)
    {
        var commits = stats.TotalCommits;
        var prs = stats.PullRequestsCreated;
        var prMerged = stats.PullRequestsMerged;
        var streak = stats.LongestStreak;
        var activeDays = stats.TotalContributionDays;
        var languages = stats.LanguageBreakdown.Count;
        
        // Code Master
        if (commits >= 300 && activeDays >= 100 && prs >= 50)
        {
            return ("Code Master", 
                "You showed up, you shipped, you collaborated.",
                "Master-level developer with exceptional dedication",
                "In 2025 you balanced high activity with consistency, building mastery through daily practice.");
        }

        // Productivity Champion
        if (commits >= 500)
        {
            return ("Productivity Champion",
                "You turned ideas into code at an incredible pace.",
                "Your keyboard was on fire this year",
                "You shipped an incredible amount of code, turning ideas into reality faster than most.");
        }

        // Collaboration Hero
        if (prs >= 80 && prMerged >= 60)
        {
            return ("Collaboration Hero",
                "You worked with others, reviewed code, and shipped together.",
                "Code review was your superpower",
                "You worked with others, reviewed code, and shipped together. That's how great products are built.");
        }

        // Streak Legend
        if (streak >= 60)
        {
            return ("Consistency Beast",
                "Day after day, you showed up and shipped.",
                $"A {streak}-day streak isn't luck — it's dedication",
                "You showed up day after day. That's how mastery is built.");
        }

        // Language Explorer
        if (languages >= 7)
        {
            return ("Polyglot",
                "You explored the ecosystem without limits.",
                $"{languages} languages, countless possibilities",
                "You didn't pick favorites — you explored the entire ecosystem.");
        }

        // Consistent Contributor
        if (commits >= 150 && activeDays >= 50)
        {
            return ("Consistent",
                "You built momentum and kept it going.",
                "Steady progress wins the race",
                "You built momentum and kept it going throughout the year.");
        }

        // Focused Builder
        if (commits >= 100)
        {
            return ("Focused",
                "You picked your battles and won them.",
                "Quality over quantity",
                "You stayed focused and turned ideas into working code.");
        }

        // Active Learner
        if (commits >= 30 && activeDays >= 15)
        {
            return ("Growing",
                "You're building your foundation.",
                "Every commit counts",
                "You're building your foundation, one commit at a time.");
        }

        // Getting Started
        return ("Starting",
            "Your coding journey is taking shape.",
            "The best is yet to come",
            "You kept building and improving throughout the year.");
    }

    private object BuildStatusCard((string Title, string MainText, string SubText, string Narrative) userStatus)
    {
        return new Card(Layout.Horizontal().Gap(3).Height(Size.Full())
            | (Layout.Vertical().Gap(3).Align(Align.Center).Width(Size.Fit()).Padding(3)
                | Icons.Trophy.ToIcon().Height(40).Width(40))
            | (Layout.Vertical().Gap(3).Align(Align.Center)
                | Text.Block("Your Developer Status — 2025").Muted()
                | Text.H1(userStatus.Title.ToUpper()).Bold()
                | Text.Block(userStatus.MainText)
                | Text.Block(userStatus.SubText).Muted()))
            .Width(Size.Full());
    }

    private object BuildStatsCard(int animatedCommits, int animatedPRs, int animatedDays)
    {
        return new Card(Layout.Vertical()
            | (Layout.Vertical().Align(Align.Center)
                | Text.H2($"{animatedDays.ToString()} days").Bold()
                | Text.Block("You showed up again and again").Muted())
            | (Layout.Vertical().Gap(2).Align(Align.Center)
                | Text.H2($"{animatedCommits.ToString()} commits").Bold()
                | Text.Block("Progress in small steps").Muted())
            | (Layout.Vertical().Gap(2).Align(Align.Center)
                | Text.H2($"{animatedPRs.ToString()} PRs").Bold()
                | Text.Block("You didn't just code — you shipped").Muted())).Width(Size.Fraction(0.5f));
    }

    private object BuildTopLanguageCard(KeyValuePair<string, long> topLanguage)
    {
        var totalBytes = _stats.LanguageBreakdown.Values.Sum();
        var languagePercentage = totalBytes > 0
            ? Math.Round((topLanguage.Value / (double)totalBytes) * 100, 0)
            : 0;
        
        var languageName = topLanguage.Key ?? "N/A";
        var mainText = languagePercentage > 0
            ? "THIS WAS YOUR LANGUAGE"
            : "Your main programming language";
        var subText = languagePercentage > 0
            ? $"{languagePercentage}% of your code was written in {languageName}"
            : $"{languageName} — your comfort zone and powerful tool";
        var motivationalText = languagePercentage >= 70
            ? "This is your superpower! You created most of your code with it."
            : languagePercentage >= 50
                ? "More than half of your code was written in this language. You know what you're doing!"
                : "This language helped you realize most of your ideas this year. Keep it up!";

        return new Card(Layout.Vertical().Gap(4).Align(Align.Center).Height(Size.Full())
            | Text.H1($"{languageName.ToUpper()} - {mainText} ").Bold()
            | Text.Block(subText).Muted()
            | Text.Block(motivationalText).Muted())
            .Width(Size.Full());
    }
}
