namespace IvyAskStatistics.Apps;

/// <summary>Lightweight placeholders while queries load. Nested <c>Height(Full)</c> on skeletons was removed
/// because it can thrash layout and look like loading never finishes.</summary>
internal static class TabLoadingSkeletons
{
    public static object Dashboard()
    {
        var kpi = Layout.Grid().Columns(5).Height(Size.Fit())
                  | new Skeleton().Height(Size.Units(12))
                  | new Skeleton().Height(Size.Units(12))
                  | new Skeleton().Height(Size.Units(12))
                  | new Skeleton().Height(Size.Units(12))
                  | new Skeleton().Height(Size.Units(12));

        var charts1 = Layout.Grid().Columns(3).Height(Size.Fit())
                      | new Skeleton().Height(Size.Units(70))
                      | new Skeleton().Height(Size.Units(70))
                      | new Skeleton().Height(Size.Units(70));

        var charts2 = Layout.Grid().Columns(3).Height(Size.Fit())
                      | new Skeleton().Height(Size.Units(70))
                      | new Skeleton().Height(Size.Units(70))
                      | new Skeleton().Height(Size.Units(70));

        return Layout.Vertical().Height(Size.Fit())
               | new Skeleton().Height(Size.Units(10)).Width(Size.Px(160))
               | new Skeleton().Height(Size.Units(7)).Width(Size.Px(220))
               | kpi
               | charts1
               | charts2
               | new Skeleton().Height(Size.Units(36)).Width(Size.Fraction(1f));
    }

    public static object RunTab()
    {
        return Layout.Vertical().Height(Size.Fit())
               | Layout.Horizontal().Height(Size.Fit())
                   | new Skeleton().Height(Size.Units(14)).Width(Size.Px(200))
                   | new Skeleton().Height(Size.Units(14)).Width(Size.Px(120))
                   | new Skeleton().Height(Size.Units(14)).Width(Size.Px(100))
               | new Skeleton().Height(Size.Units(280)).Width(Size.Fraction(1f));
    }

    public static object QuestionsTab()
    {
        return Layout.Vertical().Height(Size.Fit())
               | Layout.Horizontal().Height(Size.Fit())
                   | new Skeleton().Height(Size.Units(12)).Width(Size.Px(140))
                   | new Skeleton().Height(Size.Units(12)).Width(Size.Px(140))
                   | new Skeleton().Height(Size.Units(12)).Width(Size.Px(140))
               | new Skeleton().Height(Size.Units(280)).Width(Size.Fraction(1f));
    }

    public static object DialogTable()
    {
        return Layout.Vertical().Width(Size.Fraction(1f)).Height(Size.Fit())
               | new Skeleton().Height(Size.Units(10)).Width(Size.Fraction(1f))
               | new Skeleton().Height(Size.Units(10)).Width(Size.Fraction(1f))
               | new Skeleton().Height(Size.Units(10)).Width(Size.Fraction(1f))
               | new Skeleton().Height(Size.Units(80)).Width(Size.Fraction(1f));
    }
}
