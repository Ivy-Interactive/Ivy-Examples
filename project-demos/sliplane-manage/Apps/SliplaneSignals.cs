namespace SliplaneManage.Apps;

/// <summary>
/// Broadcast signal to notify all Sliplane apps in this workspace
/// that underlying Sliplane data (projects/services/servers) has changed.
/// Used to trigger cross-app refreshes.
/// </summary>
[Signal(BroadcastType.App)]
public class SliplaneRefreshSignal : AbstractSignal<string, Unit>
{
}

