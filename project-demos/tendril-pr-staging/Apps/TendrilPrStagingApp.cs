namespace TendrilPrStaging.Apps;

using System.Linq.Expressions;
using System.Text;
using TendrilPrStaging.Models;
using TendrilPrStaging.Services;

[App(id: "tendril-pr-staging-app", icon: Icons.GitBranch, title: "Tendril PR Staging", searchHints: ["pr", "staging", "tendril", "deploy"])]
public class TendrilPrStagingApp : ViewBase
{
    private record PrRow(
        string Id,
        string RepoKey,
        string RepoLabel,
        string HeadRef,
        int Number,
        string Title,
        string Status,
        Icons StatusIcon,
        string ExpiresAt,
        string DeployDisplay,
        string? HtmlUrl,
        string? ServiceUrl);

    public override object? Build()
    {
        var config = this.UseService<IConfiguration>();
        var github = this.UseService<GitHubApiClient>();
        var deploySvc = this.UseService<TendrilStagingDeployService>();
        var prComments = this.UseService<TendrilPrCommentService>();
        var errorWatcher = this.UseService<TendrilErrorWatcherQueue>();
        var sliplane = this.UseService<SliplaneStagingClient>();
        var reposProvider = this.UseService<TendrilReposProvider>();
        var client = this.UseService<IClientProvider>();
        var refreshToken = this.UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();
        var message = this.UseState<(string Text, bool IsError)?>(() => null);
        var pinnedTableRows = this.UseState<List<PrRow>?>(() => null);
        var deleteAllWaiting = this.UseState(false);
        var deployingItems = this.UseState(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var overviewQuery = this.UseQuery<List<PrRow>, string>(
            key: $"tendril-pr-overview:{config["Sliplane:ApiToken"] ?? ""}",
            fetcher: async ct =>
            {
                var token = config["Sliplane:ApiToken"] ?? "";
                var projId = config["Sliplane:ProjectId"] ?? "";
                refreshToken.Refresh();

                var ghToken = config["GitHub:Token"] ?? "";
                var allRepos = reposProvider.All;

                var deployments = string.IsNullOrEmpty(token)
                    ? new List<TendrilStagingDeployment>()
                    : await deploySvc.ListDeploymentsAsync(token);

                var rows = new List<PrRow>();

                foreach (var rc in allRepos)
                {
                    var prs = await github.GetPullRequestsAsync(rc.Owner, rc.Repo, ghToken, "open");
                    foreach (var pr in prs)
                    {
                        var rowId = $"{rc.Key}/{pr.Number}";
                        var dep = deployments.FirstOrDefault(d =>
                            d.RepoKey.Equals(rc.Key, StringComparison.OrdinalIgnoreCase) &&
                            d.PrNumber == pr.Number);

                        string status;
                        Icons statusIcon;
                        string deployDisplay;
                        string expiresAt = "—";

                        if (dep != null)
                        {
                            expiresAt = dep.ExpiresAt.ToString("yyyy-MM-dd");
                            var events = !string.IsNullOrEmpty(projId)
                                ? await sliplane.GetServiceEventsAsync(token, projId, dep.ServiceId)
                                : new List<SliplaneServiceEvent>();

                            (status, statusIcon) = GetStatusFromEvents(events);
                            deployDisplay = events.Count > 0
                                ? FormatEventLogColumn(events, 40)
                                : PreparingStagingLogMessage;
                        }
                        else if (deployingItems.Value.Contains(rowId))
                        {
                            status = "pending";
                            statusIcon = Icons.Clock;
                            deployDisplay = "Deploying...";
                        }
                        else
                        {
                            status = "not deployed";
                            statusIcon = Icons.CircleX;
                            deployDisplay = NotDeployedHint;
                        }

                        rows.Add(new PrRow(
                            Id: rowId,
                            RepoKey: rc.Key,
                            RepoLabel: rc.Repo,
                            HeadRef: pr.HeadRef,
                            Number: pr.Number,
                            Title: pr.Title,
                            Status: status,
                            StatusIcon: statusIcon,
                            ExpiresAt: expiresAt,
                            DeployDisplay: deployDisplay,
                            HtmlUrl: pr.HtmlUrl,
                            ServiceUrl: dep?.ServiceUrl));
                    }
                }

                return rows
                    .OrderBy(r => r.RepoLabel, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(r => r.Number)
                    .ToList();
            },
            options: new QueryOptions
            {
                KeepPrevious = true,
                RefreshInterval = TimeSpan.FromSeconds(3),
                RevalidateOnMount = true
            });

        this.UseEffect(() =>
        {
            var p = TendrilPrStagingFooterBridge.Consume();
            if (p == null) return;
            var token = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(token))
            {
                client.Toast("Configure Sliplane:ApiToken first.", "Tendril PR Staging");
                return;
            }

            if (p == "deploy-all")
            {
                showAlert("Are you sure you want to deploy ALL open PRs?", async result =>
                {
                    if (result.IsOk())
                    {
                        var rowList = overviewQuery.Value ?? new List<PrRow>();
                        var toDeploy = rowList.Where(RowNotDeployedYet).ToList();
                        if (toDeploy.Count > 0)
                        {
                            var idSet = toDeploy.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            var updated = rowList.Select(r =>
                                idSet.Contains(r.Id)
                                    ? r with { Status = "pending", StatusIcon = Icons.Clock, DeployDisplay = "Deploying...", ServiceUrl = null }
                                    : r).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                        }

                        ShowMessage($"Triggering deploy for {toDeploy.Count} PRs...", false);
                        foreach (var item in toDeploy)
                            _ = DeployRowAsync(item, clearMessageFirst: false);
                    }
                    await Task.CompletedTask;
                }, "Deploy All", AlertButtonSet.OkCancel);
            }
            else if (p == "delete-all")
            {
                showAlert("Are you sure you want to delete ALL Tendril staging services?", async result =>
                {
                    if (result.IsOk())
                    {
                        var rowList = overviewQuery.Value ?? new List<PrRow>();
                        if (rowList.Count > 0)
                        {
                            var updated = rowList.Select(r =>
                                RowNotDeployedYet(r) ? r :
                                r with { Status = "pending", StatusIcon = Icons.Clock, DeployDisplay = DeletingHint, ExpiresAt = "—", ServiceUrl = null }
                            ).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                            if (rowList.Any(r => !RowNotDeployedYet(r)))
                            {
                                pinnedTableRows.Set(updated);
                                deleteAllWaiting.Set(true);
                            }
                        }

                        ShowMessage("Deleting all Tendril staging services...", false);
                        try
                        {
                            var projectId = config["Sliplane:ProjectId"] ?? "";
                            var res = await sliplane.DeleteAllServicesInProjectAsync(token, projectId);
                            ShowMessage($"Deleted {res.Deleted} services. Failed: {res.Failed}.", res.Failed > 0);

                            var prsThatHadServices = rowList.Where(r => !RowNotDeployedYet(r)).ToList();
                            foreach (var pr in prsThatHadServices)
                            {
                                var rc = reposProvider.FindByKey(pr.RepoKey);
                                if (rc != null)
                                    _ = prComments.TryPostStagingRemovedAsync(rc.Owner, rc.Repo, pr.Number);
                            }

                            overviewQuery.Mutator.Revalidate();
                            if (res.Failed > 0)
                            {
                                deleteAllWaiting.Set(false);
                                pinnedTableRows.Set(null);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessage(ex.Message, true);
                            deleteAllWaiting.Set(false);
                            pinnedTableRows.Set(null);
                        }
                    }
                    await Task.CompletedTask;
                }, "Delete All", AlertButtonSet.OkCancel);
            }
        }, EffectTrigger.OnBuild());

        this.UseEffect(() =>
        {
            if (!deleteAllWaiting.Value) return;
            if (overviewQuery.Loading) return;
            var v = overviewQuery.Value;
            if (v == null || v.Count == 0 || v.All(RowNotDeployedYet))
            {
                deleteAllWaiting.Set(false);
                pinnedTableRows.Set(null);
            }
        }, EffectTrigger.OnBuild());

        var apiToken = config["Sliplane:ApiToken"] ?? "";
        void ShowMessage(string text, bool isError = false) => message.Set((text, isError));

        async Task DeployRowAsync(PrRow row, bool clearMessageFirst = true)
        {
            var t = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(t)) { ShowMessage("Sliplane API token required.", true); return; }
            if (clearMessageFirst) message.Set(null);

            var rc = reposProvider.FindByKey(row.RepoKey);
            if (rc == null) { ShowMessage($"Repo {row.RepoKey} not configured.", true); return; }

            deployingItems.Set(prev =>
            {
                var next = new HashSet<string>(prev, StringComparer.OrdinalIgnoreCase) { row.Id };
                return next;
            });

            try
            {
                var result = await deploySvc.DeployBranchAsync(t, rc, row.HeadRef, row.Number);

                deployingItems.Set(prev =>
                {
                    var next = new HashSet<string>(prev, StringComparer.OrdinalIgnoreCase);
                    next.Remove(row.Id);
                    return next;
                });

                if (result.SkippedBecausePrNotOpen) { ShowMessage(result.Message, false); return; }

                ShowMessage(result.Message, !result.Success);

                if (result.Success)
                {
                    overviewQuery.Mutator.Revalidate();
                    await prComments.TryPostStagingAsync(rc.Owner, rc.Repo, row.Number,
                        result.ServiceUrl, result.Success ? null : result.Message);
                    if (result.ServiceId != null)
                        await errorWatcher.EnqueueAsync(new TendrilErrorWatchRequest(rc.Key, rc.Owner, rc.Repo, row.Number, result.ServiceId));
                }
                else
                {
                    await prComments.TryPostStagingAsync(rc.Owner, rc.Repo, row.Number, null, TruncLine(result.Message, 500));
                }
            }
            catch (Exception ex)
            {
                deployingItems.Set(prev =>
                {
                    var next = new HashSet<string>(prev, StringComparer.OrdinalIgnoreCase);
                    next.Remove(row.Id);
                    return next;
                });
                ShowMessage(ex.Message, true);
            }
        }

        async Task DeleteRowAsync(PrRow row)
        {
            var t = config["Sliplane:ApiToken"] ?? "";
            if (string.IsNullOrEmpty(t)) { ShowMessage("Sliplane API token required.", true); return; }
            message.Set(null);

            var rc = reposProvider.FindByKey(row.RepoKey);
            if (rc == null) { ShowMessage($"Repo {row.RepoKey} not configured.", true); return; }

            try
            {
                var result = await deploySvc.DeleteBranchAsync(t, rc, row.Number);
                ShowMessage(result.Message, !result.Success);
                if (result.Success)
                {
                    overviewQuery.Mutator.Revalidate();
                    await prComments.TryPostStagingRemovedAsync(rc.Owner, rc.Repo, row.Number);
                }
                else
                {
                    await prComments.TryPostStagingAsync(rc.Owner, rc.Repo, row.Number, null, TruncLine(result.Message, 500));
                }
            }
            catch (Exception ex) { ShowMessage(ex.Message, true); }
        }

        if (string.IsNullOrEmpty(apiToken))
            return Layout.Center()
                | Text.H2("Tendril PR Staging")
                | Text.Muted("Configure Sliplane:ApiToken in appsettings or environment variables.");

        if (reposProvider.All.Count == 0)
            return Layout.Center()
                | Text.H2("Tendril PR Staging")
                | Text.Muted("No repos configured. Set GitHub:Owner/Repo or Repos[] in config.");

        var rows = pinnedTableRows.Value ?? overviewQuery.Value ?? new List<PrRow>();

        if (overviewQuery.Loading && overviewQuery.Value == null && rows.Count == 0 && pinnedTableRows.Value == null)
            return Layout.Center() | Text.Muted("Loading...");

        if (overviewQuery.Error is { } errEx)
            return new Callout($"Error: {errEx.Message}", variant: CalloutVariant.Error);

        var header = Layout.Horizontal().Height(Size.Fit()) | Text.H2("Tendril PR Staging");

        if (message.Value.HasValue)
        {
            var (txt, isErr) = message.Value.Value;
            header = header | new Callout(txt, variant: isErr ? CalloutVariant.Error : CalloutVariant.Info);
        }

        List<Expression<Func<PrRow, object>>> hiddenColumns = [
            r => r.Id,
            r => r.RepoKey,
            r => r.HeadRef,
            r => r.HtmlUrl!,
            r => r.ServiceUrl!,
        ];

        var table = rows
            .AsQueryable()
            .ToDataTable(r => r.Id)
            .RefreshToken(refreshToken)
            .Height(Size.Full())
            .Header(r => r.RepoLabel, "Repo")
            .Header(r => r.Number, "# PR")
            .Header(r => r.Title, "Title")
            .Header(r => r.StatusIcon, "Icon")
            .Header(r => r.Status, "Status")
            .Header(r => r.ExpiresAt, "Expires")
            .Header(r => r.DeployDisplay, "Deploy log")
            .Width(r => r.RepoLabel, Size.Px(160))
            .Width(r => r.Number, Size.Px(50))
            .Width(r => r.Title, Size.Px(300))
            .Width(r => r.StatusIcon, Size.Px(50))
            .Width(r => r.Status, Size.Px(100))
            .Width(r => r.ExpiresAt, Size.Px(100))
            .Width(r => r.DeployDisplay, Size.Px(500))
            .Hidden(hiddenColumns)
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;
            })
            .RowActions(
                MenuItem.Default(Icons.Rocket, "Deploy").Tag("deploy"),
                MenuItem.Default(Icons.Trash2, "Delete").Tag("delete"),
                MenuItem.Default(Icons.ExternalLink, "Open").Tag("open-dd")
                    .Children(
                        MenuItem.Default(Icons.GitBranch, "pr").Label("Open PR").Tag("pr"),
                        MenuItem.Default(Icons.ExternalLink, "service").Label("Open Tendril").Tag("service")))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args is null) return ValueTask.CompletedTask;
                var rowId = args.Id?.ToString();
                var tag = args.Tag?.ToString();
                if (string.IsNullOrEmpty(rowId)) return ValueTask.CompletedTask;
                var row = rows.FirstOrDefault(r => r.Id == rowId);
                if (row == null) return ValueTask.CompletedTask;

                if (tag == "deploy")
                {
                    if (!RowNotDeployedYet(row))
                    {
                        showAlert($"Tendril staging for PR #{row.Number} already exists.", _ => { }, "Already deployed", AlertButtonSet.Ok);
                    }
                    else
                    {
                        showAlert($"Deploy Tendril staging for {row.RepoLabel} PR #{row.Number}?", result =>
                        {
                            if (result.IsOk())
                            {
                                var captured = row;
                                var updated = rows.Select(r => r.Id == captured.Id
                                    ? r with { Status = "pending", StatusIcon = Icons.Clock, DeployDisplay = "Deploying...", ServiceUrl = null }
                                    : r).ToList();
                                overviewQuery.Mutator.Mutate(updated, revalidate: false);
                                refreshToken.Refresh();
                                _ = DeployRowAsync(captured);
                            }
                        }, "Deploy", AlertButtonSet.OkCancel);
                    }
                }
                else if (tag == "delete")
                {
                    showAlert($"Delete Tendril staging for {row.RepoLabel} PR #{row.Number}?", result =>
                    {
                        if (result.IsOk())
                        {
                            var captured = row;
                            var updated = rows.Select(r => r.Id == captured.Id
                                ? r with { Status = "not deployed", StatusIcon = Icons.CircleX, DeployDisplay = NotDeployedHint, ExpiresAt = "—", ServiceUrl = null }
                                : r).ToList();
                            overviewQuery.Mutator.Mutate(updated, revalidate: false);
                            refreshToken.Refresh();
                            _ = DeleteRowAsync(captured);
                        }
                    }, "Delete", AlertButtonSet.OkCancel);
                }
                else if (tag == "pr")
                {
                    if (row.HtmlUrl != null) client.OpenUrl(row.HtmlUrl);
                }
                else if (tag == "service")
                {
                    if (!string.IsNullOrEmpty(row.ServiceUrl)) client.OpenUrl(row.ServiceUrl!);
                }

                return ValueTask.CompletedTask;
            });

        return Layout.Vertical().Height(Size.Full())
            | header
            | (rows.Count == 0 ? Text.Muted("No open PRs.") : (object)table)
            | alertView;
    }

    private const string NotDeployedHint =
        "No Tendril staging service yet.\n\nUse Deploy (rocket) in the row menu to create one.";

    private const string DeletingHint = "Deleting staging…";

    private const string PreparingStagingLogMessage =
        "Preparing…\nBuild and deploy events will appear here shortly.";

    private static bool RowNotDeployedYet(PrRow r) =>
        r.Status == "not deployed";

    private static (string Status, Icons Icon) GetStatusFromEvents(List<SliplaneServiceEvent> events)
    {
        if (events.Count == 0) return ("pending", Icons.Clock);
        var deployEvents = events.Where(IsDeployEvent).OrderByDescending(e => e.CreatedAt).ToList();
        if (deployEvents.Count == 0) return ("pending", Icons.Clock);
        var last = deployEvents.First();
        if (IsFailEvent(last)) return ("failed", Icons.CircleX);
        if (IsPendingEvent(last)) return ("pending", Icons.Clock);
        return ("deployed", Icons.Check);
    }

    private static bool IsDeployEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        if (type is "service_resume_success" or "service_suspend_success") return true;
        if (type.Contains("deploy") || type.Contains("build")) return true;
        if (msg.Contains("deploy") || msg.Contains("build failed")) return true;
        return false;
    }

    private static bool IsSuccessEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        return type is "service_deploy_success" or "service_resume_success";
    }

    private static bool IsFailEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        return type is "service_deploy_failed" or "service_build_failed";
    }

    private static bool IsPendingEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        return type == "service_deploy";
    }

    private static string FormatEventBlockForTable(SliplaneServiceEvent e)
    {
        var sb = new StringBuilder();
        sb.AppendLine(FormatEventType(e.Type));
        sb.AppendLine(e.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy, HH:mm:ss"));
        if (!string.IsNullOrWhiteSpace(e.Message)) sb.AppendLine(e.Message.Trim());
        if (!string.IsNullOrWhiteSpace(e.Reason)) sb.AppendLine(e.Reason.Trim());
        if (!string.IsNullOrWhiteSpace(e.TriggeredBy)) sb.Append($"triggered by {e.TriggeredBy}");
        return sb.ToString().TrimEnd();
    }

    private static string FormatEventLogColumn(List<SliplaneServiceEvent> events, int maxEvents) =>
        string.Join("\n\n", events.OrderByDescending(e => e.CreatedAt).Take(maxEvents).Select(FormatEventBlockForTable));

    private static string FormatEventType(string? type) => type switch
    {
        "service_deploy_success" => "Service deployed successfully",
        "service_resume_success" => "Service resumed successfully",
        "service_build" => "Service build",
        "service_deploy" => "Deploy started",
        "service_deploy_failed" => "Service deploy failed",
        "service_build_failed" => "Build failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };

    private static string TruncLine(string? s, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var line = s.Trim().Replace("\r", "").Replace("\n", " ");
        return line.Length <= maxLen ? line : line[..maxLen] + "...";
    }
}
