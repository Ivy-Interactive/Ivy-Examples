using Ivy.Chrome;
using Ivy.Core;
using Ivy.Core.Hooks;
using Ivy.Views;

namespace CourseTemplate.Helpers;

public static class Hooks
{
    public static Action<string> UseLinks(this IViewContext ctx)
    {
        var navigator = ctx.UseNavigation();
        return uri => navigator.Navigate(uri);
    }

    // Convenience overload for ViewBase - delegates to IViewContext version
    public static Action<string> UseLinks(this ViewBase view)
    {
        return view.Context.UseLinks();
    }
}
