namespace MeetingCostCalculator.Apps;

using Ivy;

[App(icon: Icons.DollarSign)]
public class MeetingCostCalculatorApp : ViewBase
{
    public override object? Build()
    {
        var attendees = UseState(5);
        var hourlyRate = UseState(75.00m);
        var elapsedSeconds = UseState(0);
        var isRunning = UseState(false);

        UseInterval(() =>
        {
            elapsedSeconds.Set(elapsedSeconds.Value + 1);
        }, isRunning.Value ? TimeSpan.FromSeconds(1) : null);

        var cost = attendees.Value * (hourlyRate.Value / 3600m) * elapsedSeconds.Value;
        var elapsed = TimeSpan.FromSeconds(elapsedSeconds.Value);

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | Text.H1("Meeting Cost Calculator")
                | Text.Muted("Track the real-time cost of your meetings")
                | new Separator()
                | (Layout.Horizontal()
                    | attendees.ToNumberInput().WithField().Label("Attendees")
                    | hourlyRate.ToMoneyInput("USD").WithField().Label("Avg. Hourly Rate"))
                | new Separator()
                | (Layout.Vertical().Align(Align.Center).Gap(2)
                    | (Layout.Horizontal().Align(Align.Center).Gap(2)
                        | new Icon(Icons.Clock).Small()
                        | Text.Muted(elapsed.ToString(@"hh\:mm\:ss")))
                    | Text.H1($"${cost:N2}").Color(Colors.Green)
                    | Text.Muted("Total meeting cost"))
                | new Separator()
                | (Layout.Horizontal().Align(Align.Center)
                    | (!isRunning.Value
                        ? new Button("Start", () => isRunning.Set(true))
                            .Primary()
                            .Icon(Icons.Play)
                        : new Button("Pause", () => isRunning.Set(false))
                            .Icon(Icons.Pause))
                    | new Button("Reset", () =>
                    {
                        isRunning.Set(false);
                        elapsedSeconds.Set(0);
                    }).Destructive().Icon(Icons.RotateCcw)));
    }
}
