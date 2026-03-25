using Ivy;

namespace QueryParamIdApp.Apps;

[App(icon: Icons.Info)]
public class IdExampleApp : ViewBase
{
    // Because "id" is now a reserved query parameter used by SignalR,
    // passing `?id=123` in the URL directly will NOT populate this property.
    // Instead, you must pass it inside the appArgs JSON parameter:
    // ?appArgs={"id":123}
    [Prop]
    public new int? Id { get; set; }

    [Prop]
    public string? ItemId { get; set; }

    public override object? Build()
    {
        var layout = Layout.Vertical().Padding(4).Gap(4);

        layout |= Text.H1("ID Query Param Example");
        layout |= Text.Block($"Received Id from appArgs: {Id?.ToString() ?? "null"}").Bold();
        layout |= Text.Block($"Received ItemId: {ItemId ?? "null"}").Bold();

        var calloutMessage = "Since `id` is a reserved query parameter for the SignalR connection token in Ivy, you cannot directly pass `?id=123` in the URL. If you do, it will be stripped out and ignored by the framework.";
        layout |= Callout.Warning(calloutMessage);

        layout |= Text.Block("To pass the `Id` parameter, you have two options:").Bold();
        layout |= Text.Block("1. Pass it via appArgs JSON: ?appArgs={\"id\":123}");
        layout |= Text.Block("2. Use a different property name like ItemId and pass it normally: ?ItemId=456");

        return layout;
    }
}
