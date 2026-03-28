namespace IvyAskStatistics;

public static class Questions
{
    public static readonly List<TestQuestion> All =
    [
        // Button
        new("button-easy-1",       "button",      "easy",   "how to create a Button with an onClick handler in Ivy?"),
        new("button-medium-1",     "button",      "medium", "how to change a Button variant to Destructive and add an icon in Ivy?"),
        new("button-hard-1",       "button",      "hard",   "how to make a Button open an external URL in a new tab with an icon on the right in Ivy?"),

        // Card
        new("card-easy-1",         "card",        "easy",   "how to create a Card widget in Ivy?"),
        new("card-medium-1",       "card",        "medium", "how to add a title, description and action buttons to a Card in Ivy?"),
        new("card-hard-1",         "card",        "hard",   "how to create a clickable Card that navigates to another page in Ivy?"),

        // DataTable
        new("datatable-easy-1",    "datatable",   "easy",   "how to display a list of objects in a DataTable in Ivy?"),
        new("datatable-medium-1",  "datatable",   "medium", "how to add sortable columns and a search filter to a DataTable in Ivy?"),
        new("datatable-hard-1",    "datatable",   "hard",   "how to add custom row action buttons to each row in a DataTable in Ivy?"),
        new("datatable-hard-2",    "datatable",   "hard",   "how to implement server-side pagination in a DataTable in Ivy?"),

        // Layout
        new("layout-easy-1",       "layout",      "easy",   "how to create a horizontal layout with multiple items in Ivy?"),
        new("layout-medium-1",     "layout",      "medium", "how to create a grid layout with multiple columns in Ivy?"),
        new("layout-hard-1",       "layout",      "hard",   "how to control gap, alignment and wrap behavior in a Layout in Ivy?"),

        // Text
        new("text-easy-1",         "text",        "easy",   "how to display a heading using Text.H1 in Ivy?"),
        new("text-medium-1",       "text",        "medium", "how to display inline code and a code block using Text widgets in Ivy?"),

        // Input
        new("input-easy-1",        "input",       "easy",   "how to create a text input bound to state in Ivy?"),
        new("input-medium-1",      "input",       "medium", "how to create a select dropdown input with a list of options in Ivy?"),
        new("input-hard-1",        "input",       "hard",   "how to validate a text input and display an inline error message in Ivy?"),

        // Badge
        new("badge-easy-1",        "badge",       "easy",   "how to display a Badge widget in Ivy?"),
        new("badge-medium-1",      "badge",       "medium", "how to change Badge variant color based on a status value in Ivy?"),

        // Progress
        new("progress-easy-1",     "progress",    "easy",   "how to show a Progress bar widget in Ivy?"),
        new("progress-medium-1",   "progress",    "medium", "how to update a Progress bar value dynamically from state in Ivy?"),

        // Sheet
        new("sheet-medium-1",      "sheet",       "medium", "how to open a Sheet side panel overlay in Ivy?"),
        new("sheet-hard-1",        "sheet",       "hard",   "how to create a Sheet with a form inside and a submit action in Ivy?"),

        // Navigation
        new("navigation-easy-1",   "navigation",  "easy",   "how to navigate to another page in an Ivy app?"),
        new("navigation-medium-1", "navigation",  "medium", "how to pass query parameters when navigating between pages in Ivy?"),

        // Callout
        new("callout-easy-1",      "callout",     "easy",   "how to show a Callout notification widget in Ivy?"),

        // Toast
        new("toast-easy-1",        "toast",       "easy",   "how to display a toast notification in Ivy?"),
        new("toast-medium-1",      "toast",       "medium", "how to show a success or error toast with a custom message in Ivy?"),

        // Tooltip
        new("tooltip-easy-1",      "tooltip",     "easy",   "how to add a Tooltip to a widget in Ivy?"),

        // State
        new("state-easy-1",        "state",       "easy",   "how to use UseState to manage state in an Ivy app?"),
        new("state-medium-1",      "state",       "medium", "how to share state between multiple components in Ivy?"),

        // Services
        new("services-easy-1",     "services",    "easy",   "how to inject and use a service with UseService in Ivy?"),

        // DropDownMenu
        new("dropdown-medium-1",   "dropdown",    "medium", "how to create a DropDownMenu with multiple action items in Ivy?"),

        // Expandable
        new("expandable-medium-1", "expandable",  "medium", "how to create an Expandable collapsible section in Ivy?"),
    ];
}
