using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.Building, path: ["Apps"])]
public class CompaniesApp : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseBlades(() => new CompanyListBlade(), "Search");
        return blades;
    }
}
