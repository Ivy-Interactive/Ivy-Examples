namespace PrStagingDeploy.Apps;

using PrStagingDeploy.Models;
using PrStagingDeploy.Services;

/// <summary>
/// PR Staging Deploy — one table: PRs with Sliplane deploy status. Data from GitHub + Sliplane API.
/// </summary>
[App(icon: Icons.GitBranch, title: "PR Staging Deploy", searchHints: ["pr", "staging", "deploy", "samples", "docs"])]
public class PrStagingDeployApp : ViewBase
{
    private record PrRow(
        string HeadRef,
        int Number,
        string Title,
        string Status,
        Icons StatusIcon,
        string ExpiresAt,
        string DocsDisplay,
        string SamplesDisplay,
        string? HtmlUrl);

    public override object? Build()
    {
        var config = this.UseService<IConfiguration>();
        var github = this.UseService<GitHubApiClient>();
        var deploySvc = this.UseService<StagingDeployService>();
        var sliplane = this.UseService<SliplaneStagingClient>();
        var client = this.UseService<IClientProvider>();
        var refreshToken = this.UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();
        var message = this.UseState<(string Text, bool IsError)?>(() => null);

        var overviewQuery = this.UseQuery<List<PrRow>, string>(
            key: $"pr-overview:{config["Sliplane:ApiToken"] ?? ""}",
            fetcher: async ct =>
            {
                var token = config["Sliplane:ApiToken"] ?? "";
                var projId = config["Sliplane:ProjectId"] ?? "";
                refreshToken.Refresh();
                var prs = await github.GetPullRequestsAsync(
                    config["GitHub:Owner"] ?? "Ivy-Interactive",
                    config["GitHub:Repo"] ?? "Ivy-Examples",
                    config["GitHub:Token"] ?? "", "open");
                var deployments = string.IsNullOrEmpty(token)
                    ? new List<StagingDeployment>()
                    : await deploySvc.ListDeploymentsAsync(token);

                var rows = new List<PrRow>();
                foreach (var pr in prs)
                {
                    var branchSafe = SliplaneStagingClient.SanitizeBranchName(pr.HeadRef);
                    var dep = deployments.FirstOrDefault(d => d.BranchSafe == branchSafe);

                    string status;
                    Icons statusIcon;
                    string docsDisplay = "";
                    string samplesDisplay = "";
                    string expiresAt = "—";

                    if (dep != null)
                    {
                        expiresAt = dep.ExpiresAt.ToString("yyyy-MM-dd");
                        var docsEvents = !string.IsNullOrEmpty(projId) && !string.IsNullOrEmpty(dep.DocsServiceId)
                            ? await sliplane.GetServiceEventsAsync(token, projId, dep.DocsServiceId)
                            : new List<SliplaneServiceEvent>();
                        var samplesEvents = !string.IsNullOrEmpty(projId) && !string.IsNullOrEmpty(dep.SamplesServiceId)
                            ? await sliplane.GetServiceEventsAsync(token, projId, dep.SamplesServiceId)
                            : new List<SliplaneServiceEvent>();
                        var allEvents = docsEvents.Concat(samplesEvents).ToList();

                        var (statusLabel, icon) = GetStatusFromEvents(allEvents);
                        status = statusLabel;
                        statusIcon = icon;

                        docsDisplay = GetServiceDisplay(dep.DocsUrl, dep.DocsStatus, docsEvents, statusLabel);
                        samplesDisplay = GetServiceDisplay(dep.SamplesUrl, dep.SamplesStatus, samplesEvents, statusLabel);
                    }
                    else
                    {
                        status = "not deployed";
                        statusIcon = Icons.CircleX;
                    }

                    rows.Add(new PrRow(
                        pr.HeadRef, pr.Number, pr.Title,
                        status, statusIcon, expiresAt, docsDisplay, samplesDisplay, pr.HtmlUrl));
                }

                return rows.OrderByDescending(r => r.Number).ToList();
            },
            options: new QueryOptions
            {
                KeepPrevious = true,
                RefreshInterval = TimeSpan.FromSeconds(3),
                RevalidateOnMount = true
            });

        var apiToken = config["Sliplane:ApiToken"] ?? "";
        void ClearMessage() => message.Set(null);
        void ShowMessage(string text, bool isError = false) => message.Set((text, isError));

        async Task DeployBranchAsync(string branchName)
        {
            if (string.IsNullOrEmpty(apiToken)) { ShowMessage("Sliplane API token required.", true); return; }
            ClearMessage();
            try
            {
                var result = await deploySvc.DeployBranchAsync(apiToken, branchName);
                ShowMessage(result.Message, !result.Success);
                if (result.Success) overviewQuery.Mutator.Revalidate();
            }
            catch (Exception ex) { ShowMessage(ex.Message, true); }
        }

        async Task DeleteBranchAsync(string branchName)
        {
            if (string.IsNullOrEmpty(apiToken)) { ShowMessage("Sliplane API token required.", true); return; }
            ClearMessage();
            try
            {
                var result = await deploySvc.DeleteBranchAsync(apiToken, branchName);
                ShowMessage(result.Message, !result.Success);
                if (result.Success) overviewQuery.Mutator.Revalidate();
            }
            catch (Exception ex) { ShowMessage(ex.Message, true); }
        }

        if (string.IsNullOrEmpty(apiToken))
            return Layout.Center()
                | Text.H2("PR Staging Deploy")
                | Text.Muted("Configure Sliplane:ApiToken in appsettings or environment variables.");

        var rows = overviewQuery.Value ?? new List<PrRow>();

        if (overviewQuery.Loading && overviewQuery.Value == null && rows.Count == 0)
            return Layout.Center() | Text.Muted("Loading...");

        if (overviewQuery.Error is { } errEx)
            return new Callout($"Error: {errEx.Message}", variant: CalloutVariant.Error);

        var header = Layout.Vertical()
            | Text.H2("PR Staging Deploy");

        var table = rows
            .AsQueryable()
            .ToDataTable(r => r.HeadRef)
            .RefreshToken(refreshToken)
            .Height(Size.Full())
            .Header(r => r.Number, "# PR")
            .Header(r => r.Title, "Name PR")
            .Header(r => r.StatusIcon, "Icon")
            .Header(r => r.Status, "Status")
            .Header(r => r.ExpiresAt, "Expires")
            .Header(r => r.DocsDisplay, "Docs")
            .Header(r => r.SamplesDisplay, "Samples")
            .Width(r => r.Number, Size.Px(50))
            .Width(r => r.Title, Size.Px(300))
            .Width(r => r.StatusIcon, Size.Px(50))
            .Width(r => r.Status, Size.Px(90))
            .Width(r => r.ExpiresAt, Size.Px(100))
            .Width(r => r.DocsDisplay, Size.Px(450))
            .Width(r => r.SamplesDisplay, Size.Px(450))
            .Hidden(r => r.HeadRef)
            .Hidden(r => r.HtmlUrl)
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;

            })
            .RowActions(
                MenuItem.Default(Icons.Rocket, "Deploy").Tag("deploy"),
                MenuItem.Default(Icons.Trash2, "Delete").Tag("delete"),
                MenuItem.Default(Icons.ExternalLink, "Open PR").Tag("pr"))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args is null) return ValueTask.CompletedTask;
                var headRef = args.Id?.ToString();
                var tag = args.GetType().GetProperty("Tag")?.GetValue(args)?.ToString();
                if (string.IsNullOrEmpty(headRef)) return ValueTask.CompletedTask;

                if (tag == "deploy")
                {
                    var branch = headRef;
                    showAlert($"Deploy docs and samples for branch \"{branch}\"?", result =>
                    {
                        if (result.IsOk())
                        {
                            var updated = rows.Select(r => r.HeadRef == branch
                                ? r with { Status = "pending", StatusIcon = Icons.Clock, DocsDisplay = "Deploying...", SamplesDisplay = "Deploying..." }
                                : r).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                            _ = DeployBranchAsync(branch);
                        }
                    }, "Deploy", AlertButtonSet.OkCancel);
                }
                else if (tag == "delete")
                {
                    var branch = headRef;
                    showAlert($"Are you sure you want to delete deployment for branch \"{branch}\"?", result =>
                    {
                        if (result.IsOk())
                        {
                            var updated = rows.Select(r => r.HeadRef == branch
                                ? r with { Status = "not deployed", StatusIcon = Icons.CircleX, DocsDisplay = "", SamplesDisplay = "", ExpiresAt = "—" }
                                : r).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                            _ = DeleteBranchAsync(branch);
                        }
                    }, "Delete", AlertButtonSet.OkCancel);
                }
                else if (tag == "pr")
                {
                    var pr = rows.FirstOrDefault(r => r.HeadRef == headRef);
                    if (pr?.HtmlUrl != null) client.OpenUrl(pr.HtmlUrl);
                }
                return ValueTask.CompletedTask;
            })
            .Renderer(e => e.DocsDisplay, new LinkDisplayRenderer { Type = LinkDisplayType.Url })
            .Renderer(e => e.SamplesDisplay, new LinkDisplayRenderer { Type = LinkDisplayType.Url });

        return Layout.Vertical().Height(Size.Full())
            | header
            | (rows.Count == 0 ? Text.Muted("No open PRs.") : (object)table)
            | alertView;
    }

    private static bool IsDeployEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        if (type.Contains("deploy") || type.Contains("build")) return true;
        if (msg.Contains("deploy") || msg.Contains("deployed") || msg.Contains("build failed")) return true;
        return false;
    }

    private static bool IsSuccessEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type == "service_deploy_success" || msg.Contains("deployed successfully");
    }

    private static bool IsFailEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type is "service_deploy_failed" or "service_build_failed"
            || msg.Contains("deploy failed") || msg.Contains("deployment failed") || msg.Contains("build failed");
    }

    private static bool IsPendingEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type == "service_deploy" || msg.Contains("deploy started");
    }

    private static (string Status, Icons Icon) GetStatusFromEvents(List<SliplaneServiceEvent> events)
    {
        var deployEvents = events.Where(IsDeployEvent).OrderByDescending(e => e.CreatedAt).ToList();
        if (deployEvents.Count == 0) return ("deployed", Icons.Check);

        var lastEv = deployEvents.First();
        if (IsFailEvent(lastEv))
            return ("failed", Icons.CircleX);
        if (IsPendingEvent(lastEv))
            return ("pending", Icons.Clock);
        if (IsSuccessEvent(lastEv))
            return ("deployed", Icons.Check);

        return ("deployed", Icons.Check);
    }

    private static string FormatEventForLog(SliplaneServiceEvent e)
    {
        if (!string.IsNullOrWhiteSpace(e.Message))
            return e.Message.Trim();
        var parts = new List<string> { FormatEventType(e.Type), e.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy, HH:mm:ss") };
        if (!string.IsNullOrWhiteSpace(e.TriggeredBy))
            parts.Add($"triggered by {e.TriggeredBy}");
        if (!string.IsNullOrWhiteSpace(e.Reason))
            parts.Add(e.Reason);
        return string.Join("\n", parts);
    }

    private static string FormatEventType(string? type) => type switch
    {
        "service_deploy_success" => "Service deployed successfully",
        "service_deploy" => "Deploy started",
        "service_deploy_failed" => "Service deploy failed",
        "service_build_failed" => "Build failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };

    private static string GetServiceDisplay(string? url, string? svcStatus, List<SliplaneServiceEvent> events, string overallStatus)
    {
        var rawStatus = (svcStatus ?? "").ToLowerInvariant();
        var lastFail = events.Where(IsFailEvent).OrderByDescending(e => e.CreatedAt).FirstOrDefault();
        if (lastFail != null)
            return !string.IsNullOrWhiteSpace(lastFail.Message) ? lastFail.Message.Trim() : FormatEventType(lastFail.Type);
        if (rawStatus is "suspended" or "paused")
            return "suspended";
        if (rawStatus is "error" or "failed")
            return "error";

        if (overallStatus == "deployed" && !string.IsNullOrEmpty(url))
            return url;

        if (overallStatus == "pending" && events.Count > 0)
        {
            var logLines = events
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .Select(FormatEventForLog);
            return string.Join("\n\n", logLines);
        }

        if (overallStatus == "pending")
            return "Deploying...";

        return !string.IsNullOrEmpty(url) ? url : "";
    }

}
