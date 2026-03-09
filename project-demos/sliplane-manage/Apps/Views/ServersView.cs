namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;
using Ivy.Helpers;

/// <summary>
/// Servers view: list all servers with metrics, reboot, and delete.
/// </summary>
public class ServersView : ViewBase
{
    private readonly string _apiToken;

    public ServersView(string apiToken)
    {
        _apiToken = apiToken;
    }

    public override object? Build()
    {
        var client  = this.UseService<SliplaneApiClient>();
        var servers = this.UseState<List<SliplaneServer>>();
        var loading = this.UseState(true);
        var error   = this.UseState<string?>();
        var busy    = this.UseState(false);
        var refresh = this.UseRefreshToken();
        var reloadCounter = this.UseState(0);
        var (sheetView, showSheet) = this.UseTrigger(
            (IState<bool> isOpen, SliplaneServer server) => new ServerDetailsSheet(isOpen, _apiToken, server, reloadCounter));
        var volumeCounts   = this.UseState<Dictionary<string, int>>(() => new Dictionary<string, int>());
        var totalServices = this.UseState(0);

        async Task LoadServersAsync()
        {
            try
            {
                var list = await client.GetServersAsync(_apiToken);
                servers.Set(list);
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
            }
            finally
            {
                loading.Set(false);
            }
        }

        this.UseEffect(async () => await LoadServersAsync(), EffectTrigger.OnMount(), reloadCounter);

        // Preload volume counts so they are visible on server cards
        this.UseEffect(async () =>
        {
            var current = servers.Value;
            if (current == null || current.Count == 0) return;

            var map = new Dictionary<string, int>();

            foreach (var s in current)
            {
                try
                {
                    var vols = await client.GetServerVolumesAsync(_apiToken, s.Id);
                    map[s.Id] = vols?.Count ?? 0;
                }
                catch
                {
                    map[s.Id] = 0;
                }
            }

            volumeCounts.Set(map);
        }, [servers]);

        // Total services count (across all projects; API has no per-server services)
        this.UseEffect(async () =>
        {
            try
            {
                var overview = await client.GetOverviewAsync(_apiToken);
                var total = overview?.ServicesByProject?.Values.Sum(svcs => svcs?.Count ?? 0) ?? 0;
                totalServices.Set(total);
            }
            catch
            {
                totalServices.Set(0);
            }
        });

        if (loading.Value)
            return Layout.Center() | Text.Muted("Loading servers...");

        if (error.Value is { Length: > 0 })
            return new Callout($"Error: {error.Value}", variant: CalloutVariant.Error);

        var list = servers.Value ?? new List<SliplaneServer>();

        var addServerBtn = new Button("Add server").Icon(Icons.Plus).Large().Secondary().BorderRadius(BorderRadius.Full)
        .Url("https://sliplane.io/app/servers?dialog=dialogCreateServer");
        var addServerFloat = new FloatingPanel(addServerBtn, Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

        if (list.Count == 0)
        {
            return new Fragment(
                Layout.Vertical()
                    | Text.H2("Servers")
                    | new Callout("No servers found.", variant: CalloutVariant.Info),
                addServerFloat
            );
        }

        var cards = list
            .Select(s =>
            {
                var header = Layout.Horizontal().Align(Align.Center)
                    | (Layout.Vertical().Align(Align.Left)
                        | Text.H3(s.Name))
                    | (Layout.Vertical().Align(Align.Right).Width(Size.Fit())
                        | Icons.Server.ToIcon());

                // Sliplane returns short location codes (e.g. "fsn", "sin").
                // Map common ones to friendly names, otherwise fall back to the raw code.
                var regionLabel = s.Region switch
                {
                    "fsn" or "fsn1" => "Falkenstein, DE",
                    "sin" or "sin1" => "Singapore",
                    "hel" or "hel1" => "Helsinki, FI",
                    "nbg" or "nbg1" => "Nuremberg, DE",
                    _               => s.Region
                };

                var regionRow = Layout.Horizontal()
                    | Icons.MapPin.ToIcon()
                    | Text.Block(regionLabel);

                // Volumes count (preloaded above)
                var hasVolumeCount = volumeCounts.Value.TryGetValue(s.Id, out var volCount);
                var volumesLabel = hasVolumeCount
                    ? $"{volCount} Volume" + (volCount == 1 ? string.Empty : "s")
                    : "Volumes: —";

                var volumesRow = Layout.Horizontal()
                    | Icons.HardDrive.ToIcon()
                    | Text.Block(volumesLabel);

                var servicesCount = totalServices.Value;
                var servicesLabel = $"{servicesCount} Service" + (servicesCount == 1 ? string.Empty : "s");
                var servicesRow = Layout.Horizontal()
                    | Icons.Box.ToIcon()
                    | Text.Block(servicesLabel);


                var createdRow = Layout.Horizontal()
                    | Icons.Calendar.ToIcon()
                    | Text.Muted(s.CreatedAt.ToString("MM/dd/yyyy"));

                return new Card(
                        Layout.Vertical()
                        | header
                        | Text.Muted($"Plan: {s.Plan}")
                        | regionRow
                        | volumesRow
                        | servicesRow
                        | createdRow
                    )
                    .OnClick(_ => showSheet(s));
            })
            .ToArray();

        return new Fragment(
            Layout.Vertical()
                | Text.H2("Servers")
                | (Layout.Grid().Columns(3) | cards),
            addServerFloat,
            sheetView
        );
    }

    private object BuildServerDetail(SliplaneApiClient client, SliplaneServer server)
    {
        var metrics = this.UseState<SliplaneServerMetrics?>();
        var volumes = this.UseState<List<SliplaneVolume>>();

        async Task LoadServerDetailsAsync()
        {
            var m = await client.GetServerMetricsAsync(_apiToken, server.Id);
            var v = await client.GetServerVolumesAsync(_apiToken, server.Id);
            metrics.Set(m);
            volumes.Set(v);
        }

        this.UseEffect(async () => await LoadServerDetailsAsync());

        return new Card(
            Layout.Vertical()
            | Text.H4($"Server: {server.Name}")
            | (volumes.Value?.Count > 0
                ? (object)(Layout.Vertical()
                    | volumes.Value!.Select(v =>
                        Layout.Horizontal()
                        | Text.Block(v.Name)
                        | Text.Block($"{v.SizeGb} GB")
                        | Text.InlineCode(v.MountPath)
                    ).ToArray())
                : Text.Muted("No volumes attached."))
        );
    }
}

public class ServerDetailsSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly SliplaneServer _server;
    private readonly IState<int> _reloadCounter;

    public ServerDetailsSheet(IState<bool> isOpen, string apiToken, SliplaneServer server, IState<int> reloadCounter)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _server = server;
        _reloadCounter = reloadCounter;
    }

    public override object? Build()
    {
        var client  = this.UseService<SliplaneApiClient>();
        var metrics  = this.UseState<SliplaneServerMetrics?>();
        var volumes  = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());
        var busy     = this.UseState(false);

        this.UseEffect(async () =>
        {
            try
            {
                var m = await client.GetServerMetricsAsync(_apiToken, _server.Id);
                var v = await client.GetServerVolumesAsync(_apiToken, _server.Id);
                metrics.Set(m);
                volumes.Set(v);
            }
            catch
            {
                metrics.Set((SliplaneServerMetrics?)null);
                volumes.Set(new List<SliplaneVolume>());
            }
        });

        var serverModel = new
        {
            Name = _server.Name,
            Id = _server.Id,
            Status = _server.Status,
            Location = _server.Region,
            InstanceType = _server.Plan,
            Ipv4 = _server.Ipv4 ?? "—",
            Ipv6 = _server.Ipv6 ?? "—",
            CreatedAt = _server.CreatedAt.ToString("yyyy-MM-dd HH:mm")
        };

        var m = metrics.Value;
        var metricsModel = m != null ? new
        {
            CpuUsage = $"{m.CpuUsagePercent:F1}%",
            UsedMemoryMb = $"{m.MemoryUsageMb:F0}",
            FreeMemoryMb = m.FreeMemoryMb.HasValue ? $"{m.FreeMemoryMb.Value:F0}" : "—",
            TotalMemoryMb = $"{m.MemoryTotalMb:F0}",
            MetricsAt = m.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—"
        } : null;

        var content = Layout.Vertical()
            | Text.H4("Server")
            | serverModel.ToDetails()
            | (metricsModel != null
                ? (object)(Layout.Vertical()
                    | Text.H4("Metrics (1h)")
                    | metricsModel.ToDetails())
                : Text.Muted("Loading metrics..."))
            | (volumes.Value.Count > 0
                ? (object)(Layout.Vertical()
                    | Text.H4("Volumes")
                    | (Layout.Vertical() | volumes.Value.Select(v => Text.Block($"{v.Name} — {v.SizeGb} GB — {v.MountPath}")).ToArray()))
                : Layout.Vertical());

        async Task DeleteAsync()
        {
            if (busy.Value) return;
            busy.Set(true);
            try
            {
                await client.DeleteServerAsync(_apiToken, _server.Id);
                _reloadCounter.Set(_reloadCounter.Value + 1);
                _isOpen.Set(false);
            }
            finally { busy.Set(false); }
        }

        var footer = Layout.Horizontal()
            | new Button("Closed", onClick: _ => _isOpen.Set(false)).Variant(ButtonVariant.Outline)
            | new Button("Delete", onClick: async _ => await DeleteAsync())
                .Icon(Icons.Trash).Variant(ButtonVariant.Destructive).Loading(busy.Value)
                .WithConfirm("Are you sure you want to delete this server?", "Delete server");

        if (!_isOpen.Value)
            return null;

        var sheetBody = new FooterLayout(footer, content);
        return new Sheet(_ => _isOpen.Set(false), sheetBody, title: $"Server: {_server.Name}");
    }
}
