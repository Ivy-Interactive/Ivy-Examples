namespace PrStagingDeploy.Apps;

using System.Text;
using PrStagingDeploy.Models;
using PrStagingDeploy.Services;

/// <summary>
/// PR Staging Deploy — one table: PRs with Sliplane deploy status. Data from GitHub + Sliplane API.
/// </summary>
[App(id: "pr-staging-deploy-app", icon: Icons.GitBranch, title: "PR Staging Deploy", searchHints: ["pr", "staging", "deploy", "samples", "docs"])]
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
        string? HtmlUrl,
        string? DocsUrl,
        string? SamplesUrl);

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
                        docsDisplay = NotDeployedDocsSamplesHint;
                        samplesDisplay = NotDeployedDocsSamplesHint;
                    }

                    rows.Add(new PrRow(
                        pr.HeadRef, pr.Number, pr.Title,
                        status, statusIcon, expiresAt, docsDisplay, samplesDisplay, pr.HtmlUrl,
                        dep?.DocsUrl, dep?.SamplesUrl));
                }

                return rows.OrderByDescending(r => r.Number).ToList();
            },
            options: new QueryOptions
            {
                KeepPrevious = true,
                RefreshInterval = TimeSpan.FromSeconds(3),
                RevalidateOnMount = true
            });

        this.UseEffect(() =>
        {
            var p = PrStagingFooterBridge.Consume();
            if (p == null) return;
            var token = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(token))
            {
                client.Toast("Configure Sliplane:ApiToken first.", "PR Staging Deploy");
                return;
            }

            if (p == "deploy-all")
            {
                showAlert("Are you sure you want to trigger deploy for ALL open PRs?", async result =>
                {
                    if (result.IsOk())
                    {
                        var rowList = overviewQuery.Value ?? new List<PrRow>();
                        var branchesToDeploy = rowList.Where(r => r.Status != "deployed").Select(r => r.HeadRef).ToList();
                        ShowMessage($"Triggering deploy for {branchesToDeploy.Count} PRs...", false);
                        foreach (var b in branchesToDeploy) _ = DeployBranchAsync(b);
                    }
                    await Task.CompletedTask;
                }, "Deploy All", AlertButtonSet.OkCancel);
            }
            else if (p == "delete-all")
            {
                showAlert("Are you sure you want to delete ALL staging services in the project?", async result =>
                {
                    if (result.IsOk())
                    {
                        ShowMessage("Deleting all staging services...", false);
                        _ = Task.Run(async () =>
                        {
                            if (string.IsNullOrEmpty(token)) return;
                            try
                            {
                                var projectId = config["Sliplane:ProjectId"] ?? "";
                                var res = await sliplane.DeleteAllServicesInProjectAsync(token, projectId);
                                ShowMessage($"Deleted {res.Deleted} services. Failed: {res.Failed}.", res.Failed > 0);
                                overviewQuery.Mutator.Revalidate();
                            }
                            catch (Exception ex) { ShowMessage(ex.Message, true); }
                        });
                    }
                    await Task.CompletedTask;
                }, "Delete All", AlertButtonSet.OkCancel);
            }
        }, EffectTrigger.OnBuild());

        var apiToken = config["Sliplane:ApiToken"] ?? "";
        void ClearMessage() => message.Set(null);
        void ShowMessage(string text, bool isError = false) => message.Set((text, isError));

        async Task DeployBranchAsync(string branchName)
        {
            var t = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(t)) { ShowMessage("Sliplane API token required.", true); return; }
            ClearMessage();
            try
            {
                var result = await deploySvc.DeployBranchAsync(t, branchName);
                ShowMessage(result.Message, !result.Success);
                if (result.Success) overviewQuery.Mutator.Revalidate();
            }
            catch (Exception ex) { ShowMessage(ex.Message, true); }
        }

        async Task DeleteBranchAsync(string branchName)
        {
            var t = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(t)) { ShowMessage("Sliplane API token required.", true); return; }
            ClearMessage();
            try
            {
                var result = await deploySvc.DeleteBranchAsync(t, branchName);
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

        var header = Layout.Horizontal().Height(Size.Fit())
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
            .Hidden(r => r.DocsUrl)
            .Hidden(r => r.SamplesUrl)
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;

            })
            .RowActions(
                MenuItem.Default(Icons.Rocket, "Deploy").Tag("deploy"),
                MenuItem.Default(Icons.Trash2, "Delete").Tag("delete"),
                MenuItem.Default(Icons.ExternalLink, "open").Label("Open").Tag("open-dd")
                    .Children([
                        MenuItem.Default(Icons.GitBranch, "pr").Label("Open PR").Tag("pr"),
                        MenuItem.Default(Icons.FileText, "docs").Label("Open Docs").Tag("docs"),
                        MenuItem.Default(Icons.Box, "samples").Label("Open Samples").Tag("samples"),
                    ]))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args is null) return ValueTask.CompletedTask;
                var headRef = args.Id?.ToString();
                var tag = args.Tag?.ToString();
                if (string.IsNullOrEmpty(headRef)) return ValueTask.CompletedTask;

                if (tag == "deploy")
                {
                    var branch = headRef;
                    showAlert($"Deploy docs and samples for branch \"{branch}\"?", result =>
                    {
                        if (result.IsOk())
                        {
                            var updated = rows.Select(r => r.HeadRef == branch
                                ? r with { Status = "pending", StatusIcon = Icons.Clock, DocsDisplay = "Deploying...", SamplesDisplay = "Deploying...", DocsUrl = null, SamplesUrl = null }
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
                                ? r with { Status = "not deployed", StatusIcon = Icons.CircleX, DocsDisplay = NotDeployedDocsSamplesHint, SamplesDisplay = NotDeployedDocsSamplesHint, ExpiresAt = "—", DocsUrl = null, SamplesUrl = null }
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
                else if (tag == "docs")
                {
                    var pr = rows.FirstOrDefault(r => r.HeadRef == headRef);
                    if (!string.IsNullOrEmpty(pr?.DocsUrl)) client.OpenUrl(pr.DocsUrl!);
                }
                else if (tag == "samples")
                {
                    var pr = rows.FirstOrDefault(r => r.HeadRef == headRef);
                    if (!string.IsNullOrEmpty(pr?.SamplesUrl)) client.OpenUrl(pr.SamplesUrl!);
                }
                return ValueTask.CompletedTask;
            });

        return Layout.Vertical().Height(Size.Full())
            | header
            | (rows.Count == 0 ? Text.Muted("No open PRs.") : (object)table)
            | alertView;
    }

    private const string NotDeployedDocsSamplesHint =
        "No staging service yet.\n\n"
        + "Use Deploy (rocket) in the row menu — after Sliplane creates the service, deploy/build events appear here.";

    private const string PreparingStagingLogMessage =
        "Preparing…\nBuild and deploy events will appear here shortly.";

    private static bool IsDeployEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        if (type is "service_resume_success" or "service_suspend_success" or "service_suspend" or "service_resume")
            return true;
        if (type.Contains("deploy") || type.Contains("build")) return true;
        if (msg.Contains("deploy") || msg.Contains("deployed") || msg.Contains("build failed")) return true;
        return false;
    }

    private static bool IsSuccessEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type is "service_deploy_success" or "service_resume_success"
            || msg.Contains("deployed successfully");
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
        if (events.Count == 0)
            return ("pending", Icons.Clock);

        var deployEvents = events.Where(IsDeployEvent).OrderByDescending(e => e.CreatedAt).ToList();
        if (deployEvents.Count == 0)
            return ("pending", Icons.Clock);

        var lastEv = deployEvents.First();
        if (IsFailEvent(lastEv))
            return ("failed", Icons.CircleX);
        if (IsPendingEvent(lastEv))
            return ("pending", Icons.Clock);
        if (IsSuccessEvent(lastEv))
            return ("deployed", Icons.Check);

        return ("deployed", Icons.Check);
    }

    /// <summary>Multi-line block for one event (matches Sliplane UI timeline).</summary>
    private static string FormatEventBlockForTable(SliplaneServiceEvent e)
    {
        var sb = new StringBuilder();
        sb.AppendLine(FormatEventType(e.Type));
        sb.AppendLine(e.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy, HH:mm:ss"));
        if (!string.IsNullOrWhiteSpace(e.Message))
            sb.AppendLine(e.Message.Trim());
        if (!string.IsNullOrWhiteSpace(e.Reason))
            sb.AppendLine(e.Reason.Trim());
        if (!string.IsNullOrWhiteSpace(e.TriggeredBy))
            sb.Append($"triggered by {e.TriggeredBy}");
        return sb.ToString().TrimEnd();
    }

    private static string FormatEventLogColumn(List<SliplaneServiceEvent> events, int maxEvents)
    {
        var ordered = events
            .OrderByDescending(e => e.CreatedAt)
            .Take(maxEvents)
            .Select(FormatEventBlockForTable);
        return string.Join("\n\n", ordered);
    }

    private static string FormatEventType(string? type) => type switch
    {
        "service_deploy_success" => "Service deployed successfully",
        "service_resume_success" => "Service resumed successfully",
        "service_suspend_success" => "Service suspended successfully",
        "service_build" => "Service build",
        "service_deploy" => "Deploy started",
        "service_deploy_failed" => "Service deploy failed",
        "service_build_failed" => "Build failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };

    private static string GetServiceDisplay(string? url, string? svcStatus, List<SliplaneServiceEvent> events, string overallStatus)
    {
        const int maxEventsInCell = 40;
        var rawStatus = (svcStatus ?? "").ToLowerInvariant();
        if (rawStatus is "suspended" or "paused")
            return "suspended";
        if (rawStatus is "error" or "failed")
            return "error";

        if (events.Count > 0)
            return FormatEventLogColumn(events, maxEventsInCell);

        return PreparingStagingLogMessage;
    }

}
