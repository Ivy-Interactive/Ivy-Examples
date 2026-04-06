namespace IvyAskStatistics.Apps;

/// <summary>Lightweight placeholders while queries load. Nested <c>Height(Full)</c> on skeletons was removed
/// because it can thrash layout and look like loading never finishes.</summary>
internal static class TabLoadingSkeletons
{
    public static object Dashboard()
    {
        // Mirrors <see cref="DashboardApp"/>: 5 KPI cards → 3 version-compare charts → 4 env charts → Test runs table.
        static object KpiCard(string title, Icons icon) =>
            new Card(
                Layout.Vertical().AlignContent(Align.Center).Gap(1)
                    | (Layout.Horizontal().AlignContent(Align.Center).Gap(1)
                        | new Skeleton().Height(Size.Units(9)).Width(Size.Px(56))
                        | new Skeleton().Height(Size.Units(5)).Width(Size.Px(40)))
            ).Title(title).Icon(icon);

        var kpiRow = Layout.Grid().Columns(5).Height(Size.Fit())
            | KpiCard("Answer success", Icons.CircleCheck)
            | KpiCard("Avg latency", Icons.Timer)
            | KpiCard("No answer + errors", Icons.CircleX)
            | KpiCard("Weakest widget", Icons.Ban)
            | KpiCard("Ivy version", Icons.Tag);

        static object VersionChartCard(string title) =>
            new Card(
                Layout.Vertical().Gap(3)
                    | new Skeleton().Height(Size.Units(5)).Width(Size.Px(180))
                    | new Skeleton().Height(Size.Units(48)).Width(Size.Fraction(1f)))
                .Title(title)
                .Height(Size.Units(70));

        var versionChartsRow = Layout.Grid().Columns(3).Height(Size.Fit())
            | VersionChartCard("Success rate · production vs staging")
            | VersionChartCard("Avg response · production vs staging")
            | VersionChartCard("Outcomes · production vs staging");

        static object DetailChartCard(string title) =>
            new Card(
                Layout.Vertical().Gap(3)
                    | new Skeleton().Height(Size.Units(5)).Width(Size.Px(200))
                    | new Skeleton().Height(Size.Units(48)).Width(Size.Fraction(1f)))
                .Title(title)
                .Height(Size.Units(70));

        const string envLabel = "Production";
        var detailChartsRow = Layout.Grid().Columns(4).Height(Size.Fit())
            | DetailChartCard($"Worst widgets — rate % ({envLabel})")
            | DetailChartCard($"Latency by widget — avg / max ({envLabel})")
            | DetailChartCard($"Results by difficulty ({envLabel})")
            | DetailChartCard($"Result mix ({envLabel})");

        var testRunsCard = new Card(
                Layout.Vertical().Gap(2)
                    | new Skeleton().Height(Size.Units(5)).Width(Size.Fraction(1f))
                    | new Skeleton().Height(Size.Units(5)).Width(Size.Fraction(1f))
                    | new Skeleton().Height(Size.Units(5)).Width(Size.Fraction(1f))
                    | new Skeleton().Height(Size.Units(115)).Width(Size.Fraction(1f)))
            .Title("Test runs");

        return Layout.Vertical().Height(Size.Full())
            | kpiRow
            | versionChartsRow
            | detailChartsRow
            | (Layout.Vertical() | testRunsCard);
    }

    /// <summary>Four KPI cards while <see cref="RunApp"/> loads the last saved run (titles match <c>BuildOutcomeMetricCards</c>).</summary>
    public static object RunMetricsRow()
    {
        static object KpiCard(string title, Icons icon) =>
            new Card(
                Layout.Vertical().AlignContent(Align.Center).Gap(2)
                    | new Skeleton().Height(Size.Units(9)).Width(Size.Px(72))
                    | new Skeleton().Height(Size.Units(4)).Width(Size.Px(140)))
                .Title(title)
                .Icon(icon);

        return Layout.Grid().Columns(4).Height(Size.Fit())
            | KpiCard("Answer rate", Icons.CircleCheck)
            | KpiCard("No answer", Icons.Ban)
            | KpiCard("Errors", Icons.CircleX)
            | KpiCard("Avg response", Icons.Timer);
    }

    public static object RunTab() =>
        DataTableToolbarAndBody(tableBodyHeightUnits: 280);

    public static object QuestionsTab() =>
        DataTableToolbarAndBody(tableBodyHeightUnits: 260);

    /// <summary>Filter-sized square + single block for the DataTable body (<see cref="QuestionsApp"/>, <see cref="RunApp"/>).</summary>
    private static object DataTableToolbarAndBody(int tableBodyHeightUnits) =>
        Layout.Vertical().Height(Size.Full()).Gap(3)
            | new Skeleton().Height(Size.Px(36)).Width(Size.Px(36))
            | new Skeleton().Height(Size.Units(tableBodyHeightUnits)).Width(Size.Fraction(1f));

    public static object DialogTable()
    {
        return Layout.Vertical().Width(Size.Fraction(1f)).Height(Size.Fit())
               | new Skeleton().Height(Size.Units(10)).Width(Size.Fraction(1f))
               | new Skeleton().Height(Size.Units(10)).Width(Size.Fraction(1f))
               | new Skeleton().Height(Size.Units(10)).Width(Size.Fraction(1f))
               | new Skeleton().Height(Size.Units(80)).Width(Size.Fraction(1f));
    }
}
