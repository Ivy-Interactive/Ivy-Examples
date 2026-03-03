using ShowcaseCrm.Apps.Views;

namespace ShowcaseCrm.Apps;

[App(icon: Icons.Building, path: ["Apps"])]
public class CompaniesApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new CompanyListBlade(), "Search");
    }
}
