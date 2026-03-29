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
        var prComments = this.UseService<PrStagingDeployCommentService>();
        var commentUpdateQueue = this.UseService<PrStagingDeployCommentUpdateQueue>();
        var sliplane = this.UseService<SliplaneStagingClient>();
        var client = this.UseService<IClientProvider>();
        var refreshToken = this.UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();
        var message = this.UseState<(string Text, bool IsError)?>(() => null);
        var pinnedTableRows = this.UseState<List<PrRow>?>(() => null);
        var deleteAllWaitingForFreshNoStaging = this.UseState(false);
        // Tracks branches for which a deploy was triggered but Sliplane hasn't created the service yet.
        // Prevents the row from flickering back to "not deployed" during the query refresh window.
        var deployingBranches = this.UseState(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

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
                    var dep = deployments.FirstOrDefault(d => d.BranchSafe == pr.Number.ToString());

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

                        var (statusLabel, icon) = GetCombinedRowStatus(
                            dep.DocsServiceId, docsEvents, dep.SamplesServiceId, samplesEvents);
                        status = statusLabel;
                        statusIcon = icon;

                        docsDisplay = GetServiceDisplay(dep.DocsUrl, dep.DocsStatus, docsEvents, statusLabel);
                        samplesDisplay = GetServiceDisplay(dep.SamplesUrl, dep.SamplesStatus, samplesEvents, statusLabel);
                    }
                    else
                    {
                        // Sliplane doesn't know about this branch yet.
                        // If we triggered a deploy and are waiting for the service to appear, keep "Deploying...".
                        if (deployingBranches.Value.Contains(pr.HeadRef))
                        {
                            status = "pending";
                            statusIcon = Icons.Clock;
                            docsDisplay = "Deploying...";
                            samplesDisplay = "Deploying...";
                        }
                        else
                        {
                            status = "not deployed";
                            statusIcon = Icons.CircleX;
                            docsDisplay = NotDeployedDocsSamplesHint;
                            samplesDisplay = NotDeployedDocsSamplesHint;
                        }
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
                        var prsToDeploy = rowList.Where(RowLooksLikeNoStagingYet)
                            .Select(r => (r.HeadRef, r.Number))
                            .ToList();
                        if (prsToDeploy.Count > 0)
                        {
                            var branchSet = prsToDeploy.Select(x => x.HeadRef).ToHashSet();
                            var updated = rowList.Select(r =>
                                branchSet.Contains(r.HeadRef)
                                    ? r with
                                    {
                                        Status = "pending",
                                        StatusIcon = Icons.Clock,
                                        DocsDisplay = "Deploying...",
                                        SamplesDisplay = "Deploying...",
                                        DocsUrl = null,
                                        SamplesUrl = null
                                    }
                                    : r).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                        }

                        ShowMessage($"Triggering deploy for {prsToDeploy.Count} PRs...", false);
                        foreach (var item in prsToDeploy)
                            _ = DeployBranchAsync(item.HeadRef, item.Number, clearMessageFirst: false);
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
                        var rowList = overviewQuery.Value ?? new List<PrRow>();
                        if (rowList.Count > 0)
                        {
                            var updated = rowList.Select(r =>
                                RowLooksLikeNoStagingYet(r)
                                    ? r
                                    : r with
                                    {
                                        Status = "pending",
                                        StatusIcon = Icons.Clock,
                                        DocsDisplay = DeletingStagingCellHint,
                                        SamplesDisplay = DeletingStagingCellHint,
                                        ExpiresAt = "—",
                                        DocsUrl = null,
                                        SamplesUrl = null
                                    }).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                            if (rowList.Any(r => !RowLooksLikeNoStagingYet(r)))
                            {
                                pinnedTableRows.Set(updated);
                                deleteAllWaitingForFreshNoStaging.Set(true);
                            }
                        }

                        ShowMessage("Deleting all staging services...", false);
                        try
                        {
                            if (string.IsNullOrEmpty(token))
                            {
                                deleteAllWaitingForFreshNoStaging.Set(false);
                                pinnedTableRows.Set(null);
                                return;
                            }

                            var projectId = config["Sliplane:ProjectId"] ?? "";
                            var res = await sliplane.DeleteAllServicesInProjectAsync(token, projectId);
                            ShowMessage($"Deleted {res.Deleted} services. Failed: {res.Failed}.", res.Failed > 0);

                            // Now update PR comments to "Deleted" for any PRs that had staging services
                            var owner = config["GitHub:Owner"] ?? "";
                            var repo = config["GitHub:Repo"] ?? "";
                            if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                            {
                                var prsThatHadServices = rowList.Where(r => !RowLooksLikeNoStagingYet(r)).ToList();
                                foreach (var pr in prsThatHadServices)
                                {
                                    _ = prComments.TryPostOrUpdateStagingCommentAsync(
                                        owner, repo, pr.Number, null, null, "Deleted", null);
                                }
                            }

                            overviewQuery.Mutator.Revalidate();
                            if (res.Failed > 0)
                            {
                                deleteAllWaitingForFreshNoStaging.Set(false);
                                pinnedTableRows.Set(null);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessage(ex.Message, true);
                            deleteAllWaitingForFreshNoStaging.Set(false);
                            pinnedTableRows.Set(null);
                        }
                    }
                    await Task.CompletedTask;
                }, "Delete All", AlertButtonSet.OkCancel);
            }
        }, EffectTrigger.OnBuild());

        this.UseEffect(() =>
        {
            if (!deleteAllWaitingForFreshNoStaging.Value) return;
            if (overviewQuery.Loading) return;
            var v = overviewQuery.Value;
            if (v == null || v.Count == 0)
            {
                deleteAllWaitingForFreshNoStaging.Set(false);
                pinnedTableRows.Set(null);
                return;
            }

            if (v.All(RowLooksLikeNoStagingYet))
            {
                deleteAllWaitingForFreshNoStaging.Set(false);
                pinnedTableRows.Set(null);
            }
        }, EffectTrigger.OnBuild());

        var apiToken = config["Sliplane:ApiToken"] ?? "";
        void ClearMessage() => message.Set(null);
        void ShowMessage(string text, bool isError = false) => message.Set((text, isError));

        static string TruncLine(string? s, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var line = s.Trim().Replace("\r", "").Replace("\n", " ");
            return line.Length <= maxLen ? line : line[..maxLen] + "...";
        }

        async Task DeployBranchAsync(string branchName, int prNumber, bool clearMessageFirst = true)
        {
            var t = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(t)) { ShowMessage("Sliplane API token required.", true); return; }
            if (clearMessageFirst) ClearMessage();

            // Mark this branch as "deploying" so the query fetcher keeps the row in Deploying... state
            // even before Sliplane registers the new service.
            deployingBranches.Set(prev =>
            {
                var next = new HashSet<string>(prev, StringComparer.OrdinalIgnoreCase) { branchName };
                return next;
            });

            try
            {
                var owner = config["GitHub:Owner"] ?? "";
                var repo = config["GitHub:Repo"] ?? "";

                var result = await deploySvc.DeployBranchAsync(t, branchName, prNumber);
                ShowMessage(result.Message, !result.Success);

                deployingBranches.Set(prev =>
                {
                    var next = new HashSet<string>(prev, StringComparer.OrdinalIgnoreCase);
                    next.Remove(branchName);
                    return next;
                });

                if (result.Success)
                {
                    overviewQuery.Mutator.Revalidate();

                    if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                    {
                        if (string.IsNullOrEmpty(result.DocsServiceId) && string.IsNullOrEmpty(result.SamplesServiceId))
                        {
                            await prComments.TryPostOrUpdateStagingCommentAsync(
                                owner, repo, prNumber,
                                docsUrl: null, samplesUrl: null,
                                status: "Deploy failed",
                                logLines: new[] { "No Sliplane services were created: " + TruncLine(result.Message, 200) },
                                forceNewComment: true);
                            return;
                        }

                        await commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                            Owner: owner,
                            Repo: repo,
                            PrNumber: prNumber,
                            BranchName: branchName,
                            DocsServiceId: result.DocsServiceId,
                            SamplesServiceId: result.SamplesServiceId));

                        return;
                    }
                }
                else if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                {
                    await prComments.TryPostOrUpdateStagingCommentAsync(
                        owner, repo, prNumber,
                        docsUrl: null, samplesUrl: null,
                        status: "Deploy failed",
                        logLines: new[] { TruncLine(result.Message, 240) },
                        forceNewComment: true);
                }
            }
            catch (Exception ex)
            {
                // On unexpected error, also remove from deployingBranches.
                deployingBranches.Set(prev =>
                {
                    var next = new HashSet<string>(prev, StringComparer.OrdinalIgnoreCase);
                    next.Remove(branchName);
                    return next;
                });
                ShowMessage(ex.Message, true);
            }
        }

        async Task DeleteBranchAsync(string branchName, int prNumber)
        {
            var t = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(t)) { ShowMessage("Sliplane API token required.", true); return; }
            ClearMessage();
            try
            {
                var owner = config["GitHub:Owner"] ?? "";
                var repo = config["GitHub:Repo"] ?? "";

                var result = await deploySvc.DeleteBranchAsync(t, prNumber);
                ShowMessage(result.Message, !result.Success);
                if (result.Success)
                {
                    overviewQuery.Mutator.Revalidate();
                    if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                    {
                        await prComments.TryPostOrUpdateStagingCommentAsync(
                            owner, repo, prNumber,
                            docsUrl: null, samplesUrl: null,
                            status: "Deleted",
                            logLines: null,
                            forceNewComment: true);
                    }
                }
                else if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                {
                    await prComments.TryPostOrUpdateStagingCommentAsync(
                        owner, repo, prNumber,
                        docsUrl: null, samplesUrl: null,
                        status: "Delete failed",
                        logLines: new[] { TruncLine(result.Message, 240) },
                        forceNewComment: true);
                }
            }
            catch (Exception ex) { ShowMessage(ex.Message, true); }
        }

        if (string.IsNullOrEmpty(apiToken))
            return Layout.Center()
                | Text.H2("PR Staging Deploy")
                | Text.Muted("Configure Sliplane:ApiToken in appsettings or environment variables.");

        var rows = pinnedTableRows.Value ?? overviewQuery.Value ?? new List<PrRow>();

        if (overviewQuery.Loading && overviewQuery.Value == null && rows.Count == 0 && pinnedTableRows.Value == null)
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
                    var prRow = rows.FirstOrDefault(r => r.HeadRef == branch);
                    if (prRow != null && !RowLooksLikeNoStagingYet(prRow))
                    {
                        // Services already exist — just inform the user, no action taken.
                        showAlert(
                            $"Staging services for branch \"{branch}\" already exist.",
                            _ => { }, "Services already deployed", AlertButtonSet.Ok);
                    }
                    else
                    {
                        showAlert($"Deploy docs and samples for branch \"{branch}\"?", result =>
                        {
                            if (result.IsOk())
                            {
                                var updated = rows.Select(r => r.HeadRef == branch
                                    ? r with { Status = "pending", StatusIcon = Icons.Clock, DocsDisplay = "Deploying...", SamplesDisplay = "Deploying...", DocsUrl = null, SamplesUrl = null }
                                    : r).ToList();
                                overviewQuery.Mutator.Mutate(updated, revalidate: false);
                                refreshToken.Refresh();
                                var newPrRow = rows.FirstOrDefault(r => r.HeadRef == branch);
                                if (newPrRow != null)
                                    _ = DeployBranchAsync(branch, newPrRow.Number);
                            }
                        }, "Deploy", AlertButtonSet.OkCancel);
                    }
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
                            var prRow = rows.FirstOrDefault(r => r.HeadRef == branch);
                            if (prRow != null)
                                _ = DeleteBranchAsync(branch, prRow.Number);
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

    private const string DeletingStagingCellHint = "Deleting staging…";

    /// <summary>Matches rows built in the query fetcher when there is no Sliplane deployment for the branch.</summary>
    private static bool RowLooksLikeNoStagingYet(PrRow r) =>
        r.Status == "not deployed"
        && string.Equals(r.DocsDisplay, NotDeployedDocsSamplesHint, StringComparison.Ordinal)
        && string.Equals(r.SamplesDisplay, NotDeployedDocsSamplesHint, StringComparison.Ordinal);

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

    /// <summary>Status from one service's event timeline (docs or samples).</summary>
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

    /// <summary>
    /// Row status only when every provisioned service (docs and/or samples) is <see cref="GetStatusFromEvents"/> deployed.
    /// Merging all events into one list made the row show deployed as soon as the latest event was success on one service.
    /// </summary>
    private static (string Status, Icons Icon) GetCombinedRowStatus(
        string? docsServiceId,
        List<SliplaneServiceEvent> docsEvents,
        string? samplesServiceId,
        List<SliplaneServiceEvent> samplesEvents)
    {
        var parts = new List<(string Status, Icons Icon)>();
        if (!string.IsNullOrEmpty(docsServiceId))
            parts.Add(GetStatusFromEvents(docsEvents));
        if (!string.IsNullOrEmpty(samplesServiceId))
            parts.Add(GetStatusFromEvents(samplesEvents));

        if (parts.Count == 0)
            return ("pending", Icons.Clock);

        if (parts.Exists(p => p.Status == "failed"))
            return ("failed", Icons.CircleX);
        if (parts.Exists(p => p.Status == "pending"))
            return ("pending", Icons.Clock);
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
