namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;

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
        var (sheetView, showSheet) = this.UseTrigger(
            (IState<bool> isOpen, SliplaneServer server) => new ServerDetailsSheet(isOpen, _apiToken, server));
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

        this.UseEffect(async () => await LoadServersAsync());

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

        if (list.Count == 0)
        {
            return Layout.Vertical().Gap(5)
                | Text.H2("Servers")
                | new Callout("No servers found.", variant: CalloutVariant.Info);
        }

        var cards = list
            .Select(s =>
            {
                var header = Layout.Vertical().Gap(1)
                    | Text.H4(s.Name)
                    | Text.Muted(s.Plan);

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

                var regionRow = Layout.Horizontal().Gap(2)
                    | Icons.MapPin.ToIcon()
                    | Text.Block(regionLabel);

                // Volumes count (preloaded above)
                var hasVolumeCount = volumeCounts.Value.TryGetValue(s.Id, out var volCount);
                var volumesLabel = hasVolumeCount
                    ? $"{volCount} Volume" + (volCount == 1 ? string.Empty : "s")
                    : "Volumes: —";

                var volumesRow = Layout.Horizontal().Gap(2)
                    | Icons.HardDrive.ToIcon()
                    | Text.Block(volumesLabel);

                var servicesCount = totalServices.Value;
                var servicesLabel = $"{servicesCount} Service" + (servicesCount == 1 ? string.Empty : "s");
                var servicesRow = Layout.Horizontal().Gap(2)
                    | Icons.Box.ToIcon()
                    | Text.Block(servicesLabel);


                var createdRow = Layout.Horizontal().Gap(2)
                    | Icons.Calendar.ToIcon()
                    | Text.Muted(s.CreatedAt.ToString("MM/dd/yyyy"));

                return new Card(
                        Layout.Vertical().Gap(2)
                        | header
                        | regionRow
                        | volumesRow
                        | servicesRow
                        | createdRow
                    )
                    .HandleClick(_ => showSheet(s));
            })
            .ToArray();

        return new Fragment(
            Layout.Vertical().Gap(5)
                | Text.H2("Servers")
                | (Layout.Grid().Columns(3) | cards),
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
            Layout.Vertical().Gap(4)
            | Text.H4($"Server: {server.Name}")
            | (volumes.Value?.Count > 0
                ? (object)(Layout.Vertical().Gap(1)
                    | volumes.Value!.Select(v =>
                        Layout.Horizontal().Gap(2)
                        | Text.Block(v.Name)
                        | Text.Block($"{v.SizeGb} GB")
                        | Text.InlineCode(v.MountPath)
                    ).ToArray())
                : Text.Muted("No volumes attached."))
        );
    }
}

public class ServerDetailsSheet(IState<bool> isOpen, string apiToken, SliplaneServer server) : ViewBase
{
    public override object? Build()
    {
        var client  = this.UseService<SliplaneApiClient>();
        var metrics = this.UseState<SliplaneServerMetrics?>();
        var volumes = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());

        this.UseEffect(async () =>
        {
            try
            {
                var m = await client.GetServerMetricsAsync(apiToken, server.Id);
                var v = await client.GetServerVolumesAsync(apiToken, server.Id);
                metrics.Set(m);
                volumes.Set(v);
            }
            catch
            {
                metrics.Set((SliplaneServerMetrics?)null);
                volumes.Set(new List<SliplaneVolume>());
            }
        });

        var content = new Card(
            Layout.Vertical()
            | Text.H3(server.Name)
            | Text.Muted($"{server.Region} • {server.Plan}")
            | (metrics.Value != null
                ? (object)(Layout.Vertical().Gap(1)
                    | Text.Block($"CPU usage: {metrics.Value.CpuUsagePercent:F1}%")
                    | Text.Block($"Memory: {metrics.Value.MemoryUsageMb:F0} / {metrics.Value.MemoryTotalMb:F0} MB"))
                : Text.Muted("Loading metrics..."))
            | (volumes.Value.Count > 0
                ? (object)(Layout.Vertical().Gap(1)
                    | volumes.Value.Select(v =>
                        Layout.Horizontal().Gap(2)
                        | Text.Block(v.Name)
                        | Text.Block($"{v.SizeGb} GB")
                        | Text.InlineCode(v.MountPath)
                    ).ToArray())
                : Text.Muted("No volumes attached."))
        );

        return !isOpen.Value
            ? null
            : new Sheet(_ => isOpen.Set(false), content, title: $"Server: {server.Name}");
    }
}
