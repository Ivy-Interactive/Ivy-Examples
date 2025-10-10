namespace Taskly.Connections;

using Taskly.Models;

// Simple signal to notify other apps to refresh data from database
[Signal(BroadcastType.Server)]
public class RefreshDataSignal : AbstractSignal<bool, bool> { }
