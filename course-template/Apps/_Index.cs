using Ivy;
using Ivy.Apps;
using Ivy.Shared;
using Ivy.Core;
using static Ivy.Views.Layout;
using static Ivy.Views.Text;

namespace CourseTemplate.Apps;

[App(order: 0, title: "Apps", icon: Icons.PanelLeft, groupExpanded: true)]
public class _IndexApp() : ViewBase
{
    public override object? Build()
    {
        // Пустая страница-индекс раздела Apps. Только нужен для появления группы в меню.
        return Layout.Vertical() | Lead("Applications");
    }
}


