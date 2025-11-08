using Ivy.Chrome;
using Ivy.Core;
using Ivy.Views;

namespace CourseTemplate.Helpers;

public static class Hooks
{
    public static Action<string> UseLinks(this IView view)
    {
        var navigator = view.UseNavigation();
        return uri =>
        {
            navigator.Navigate(uri);
        };
    }
}
