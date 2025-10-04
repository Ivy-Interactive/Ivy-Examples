using EnumsNET;
using EnumsNetApp.Apps.SampleEnums;
using System.ComponentModel;
/// <summary>
/// Enums.NET demo view for Ivy samples.
/// 
/// Demonstrates enumeration and flags features from Enums.NET:
/// - Enumerating enum members (All, Distinct, DisplayOrder, Flags) using Enums.GetMembers&lt;T&gt;.
/// - Presenting enum metadata (value, name, DescriptionAttribute, SymbolAttribute) via EnumMemberInfo.
/// - Flags operations on <see cref="DaysOfWeek"/> (HasAllFlags, HasAnyFlags, CombineFlags,
///   CommonFlags, RemoveFlags, GetFlags, ToggleFlags) with interactive UI controls.
/// - Simple parsing/formatting examples and validation helpers for other enums (e.g. NumericOperator, DayType).
/// 
/// Designed to be state-driven (Ivy `UseState`) and easily extensible with additional UI and logic.
/// </summary>
namespace EnumsNetApp.Apps
{
    [App(title: "Enums.NET Demo App", icon: Icons.Tag)]
    public class EnumsNetNewDemoApp : ViewBase
    {
        // helper enum 
        // 1.for which table is visible
        enum MembersView { All, Distinct, DisplayOrder, Flags }
        // for falg enums related operations
        enum flagOperations { HasAllFlags, HasAnyFlags, CombineFlags, CommonFlags, RemoveFlags, GetFlags, ToggleFlags }

        // Record to store enum member metadata
        public record EnumMemberInfo(int Value, string Name, string? Description, string? Symbol);

        public override object? Build()
        {
            var client = UseService<IClientProvider>();

            var inputString = UseState("Active");
            var parseResult = UseState(string.Empty);
            var selectedFormat = UseState(EnumFormat.Name);
            var showAllValues = UseState(true);


            // enum flag states 
            var selectedFlagView = UseState(flagOperations.HasAllFlags);
            var flagResult = UseState<object>(Text.P(""));
            var flagA = DaysOfWeek.Monday | DaysOfWeek.Wednesday | DaysOfWeek.Friday;
            var flagB = DaysOfWeek.Monday | DaysOfWeek.Wednesday;
            // state for DaysOfWeek flags
            var daysFlags = UseState(flagB);


            //Enumeration related state 
            var enumrationResult = UseState<string>(() => string.Empty);
            var selectedEnumrateionView = UseState(() => MembersView.All);
            var enumrationListExpanded = UseState(true);
            var enumrationList = UseState<List<EnumMemberInfo>>(() =>
                Enums.GetMembers<NumericOperator>()
                     .Select(m => new EnumMemberInfo(
                         (int)m.Value,
                         m.Name,
                         m.Attributes.Get<DescriptionAttribute>()?.Description,
                         m.Attributes.Get<SymbolAttribute>()?.Symbol
                     ))
                     .OrderBy(x => x.Value) // increasing value order
                     .ToList()
            );


            //Enumrate member of enums using Enums.GetMembers<T>()
            void selectedView(MembersView view)
            {
                try
                {
                    List<EnumMemberInfo> tableRows;
                    switch (view)
                    {
                        case MembersView.Distinct:
                            // Distinct — by description text for example (you can change distinct key as needed)
                            tableRows = Enums.GetMembers<NumericOperator>(EnumMemberSelection.Distinct)
                                         .Select(m => new EnumMemberInfo(
                                             (int)m.Value,
                                             m.Name,
                                             m.Attributes.Get<DescriptionAttribute>()?.Description,
                                             m.Attributes.Get<SymbolAttribute>()?.Symbol
                                         ))
                                         .OrderBy(x => x.Value) // increasing value order
                                         .ToList();
                            break;

                        case MembersView.DisplayOrder:
                            // DisplayOrder — sort by DisplayAttribute.Order (we captured as DisplayOrder in EnumMemberInfo)
                            // Second e.g for more clarity: Show PriorityLevel with Display(Order)
                            tableRows = Enums.GetMembers<PriorityLevel>(EnumMemberSelection.DisplayOrder)
                                        .Select(m => new EnumMemberInfo(
                                            (int)m.Value,
                                            m.Name,
                                            m.Attributes.Get<DisplayAttribute>()?.Name,
                                            null
                                        ))
                                        .ToList();

                            break;

                        case MembersView.Flags:
                            // Flags view — show only members whose value is power-of-two or that look like flags (example)
                            tableRows = Enums.GetMembers<NumericOperator>(EnumMemberSelection.Flags)
                                         .Select(m => new EnumMemberInfo(
                                             (int)m.Value,
                                             m.Name,
                                             m.Attributes.Get<DescriptionAttribute>()?.Description,
                                             m.Attributes.Get<SymbolAttribute>()?.Symbol
                                         ))
                                         .OrderBy(x => x.Value) // increasing value order
                                         .ToList();
                            break;

                        default: // MembersView.All
                            tableRows = Enums.GetMembers<NumericOperator>()
                                         .Select(m => new EnumMemberInfo(
                                             (int)m.Value,
                                             m.Name,
                                             m.Attributes.Get<DescriptionAttribute>()?.Description,
                                             m.Attributes.Get<SymbolAttribute>()?.Symbol
                                         ))
                                         .OrderBy(x => x.Value) // increasing value order
                                         .ToList();
                            break;

                    }
                    enumrationList.Set(tableRows);
                    selectedEnumrateionView.Set(view);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    enumrationResult.Set($"Error: {ex.Message}");
                }

            }

            // Toggle flags
            void ToggleDay(DaysOfWeek day)
            {
                var current = daysFlags.Value;
                daysFlags.Set(current.HasFlag(day) ? current & ~day : current | day);
            }

            void RunFlagOperation(flagOperations type)
            {
                try
                {
                    object result = type switch
                    {
                        flagOperations.HasAllFlags =>
                            Layout.Horizontal(
                                Text.Strong("FlagA:"),
                                Text.P($"{flagA}"),
                                Text.Strong("HasAllFlags(FlagB:"),
                                Text.P($"{flagB}"),
                                Text.Strong(") => Result:"),
                                Text.P($"{flagA.HasAllFlags(flagB)}")
                            ),

                        flagOperations.HasAnyFlags =>
                            Layout.Horizontal(
                                Text.Strong("DaysOfWeek.Monday:"),
                                Text.P($"{DaysOfWeek.Monday}"),
                                Text.Strong("HasAnyFlags(FlagB:"),
                                Text.P($"{flagB}"),
                                Text.Strong(") => Result:"),
                                Text.P($"{DaysOfWeek.Monday.HasAnyFlags(flagB)}")
                            ),

                        flagOperations.CombineFlags =>
                            Layout.Horizontal(
                                Text.Strong("CombineFlags FlagA:"),
                                Text.P($"{flagA}"),
                                Text.Strong("with FlagB:"),
                                Text.P($"{flagB}"),
                                Text.Strong("=> Result:"),
                                Text.P($"{flagA.CombineFlags(flagB)}")
                            ),

                        flagOperations.CommonFlags =>
                            Layout.Horizontal(
                                Text.Strong("CommonFlags in FlagA:"),
                                Text.P($"{flagA}"),
                                Text.Strong("and FlagB:"),
                                Text.P($"{flagB}"),
                                Text.Strong("=> Result:"),
                                Text.P($"{flagA.CommonFlags(flagB)}")
                            ),

                        flagOperations.RemoveFlags =>
                            Layout.Horizontal(
                                Text.Strong("RemoveFlags DaysOfWeek.Wednesday:"),
                                Text.P($"{DaysOfWeek.Wednesday}"),
                                Text.Strong("from FlagB:"),
                                Text.P($"{flagB}"),
                                Text.Strong("=> Result:"),
                                Text.P($"{flagB.RemoveFlags(DaysOfWeek.Wednesday)}")
                            ),

                        flagOperations.GetFlags =>
                            Layout.Horizontal(
                                Text.Strong("GetFlags of Weekend:"),
                                Text.P(string.Join(", ", DaysOfWeek.Weekend.GetFlags()))
                            ),

                        flagOperations.ToggleFlags =>
                            Layout.Horizontal(
                                Text.Strong("ToggleFlags current selection:"),
                                Text.P($"{daysFlags.Value}")
                            ),

                        _ => Text.P("Unsupported operation")
                    };

                    selectedFlagView.Set(type);
                    flagResult.Set(result);

                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    flagResult.Set($"Error: {ex.Message}");
                }
            }

            return
                Layout.Vertical(
                    //This is title Card 
                    new Card().Title("Enums.NET demo (Ivy)")
                                .Description("Demonstrates enumeration, flags, parsing, attributes, custom formats and validation.")
                                .BorderThickness(3)
                                .BorderStyle(BorderStyle.Dashed)
                                .BorderColor(Colors.Primary)
                                .BorderRadius(BorderRadius.Rounded)
                                .Width(Size.Grow()),

            // Members enumeration card
            new Card(Layout.Vertical(
                        // content area: conditional show/hide and render chosen table
                        Layout.Horizontal(
                            Text.Strong("NumericOperator members ").Width(Size.Shrink()),
                                new Button(enumrationListExpanded.Value ? "Collapse" : "Expand", _ => enumrationListExpanded.Set(!enumrationListExpanded.Value)),
                                        new Button("Check All Member", _ => selectedView(MembersView.All)),
                                        new Button("Check Distinct Member", _ => selectedView(MembersView.Distinct)),
                                        new Button("Check DisplayOrder Member", _ => selectedView(MembersView.DisplayOrder)),
                                        new Button("Check Flag Member", _ => selectedView(MembersView.Flags))
                                    ).Align(Align.Left),
                                    enumrationListExpanded.Value ?
                                                     (object)(enumrationList.Value.Any() ?
                                                                    Layout.Vertical(Text.H3($"Enumeration of Enums for selection : {selectedEnumrateionView.Value}"),
                                                                    // render rows (use ToTable() if you have it; otherwise show as vertical list)
                                                                    enumrationList.Value.ToTable().Width(Size.Full()))
                                                                    : Text.Muted("No members match this view"))
                                    : Text.Muted("Members collapsed"))).Title("Enum Enumerate in increasing value order"),

            // Card for Flags demo
            new Card(Layout.Vertical(Text.H2("Flag operations"),
                     Layout.Horizontal(
                        Text.Strong("DaysOfWeek Flags Demo").Width(Size.Grow()),
                        new Button("HasAllFlags", _ => RunFlagOperation(flagOperations.HasAllFlags)),
                        new Button("HasAnyFlags", _ => RunFlagOperation(flagOperations.HasAnyFlags)),
                        new Button("CombineFlags", _ => RunFlagOperation(flagOperations.CombineFlags)),
                        new Button("CommonFlags", _ => RunFlagOperation(flagOperations.CommonFlags)),
                        new Button("RemoveFlags", _ => RunFlagOperation(flagOperations.RemoveFlags)),
                        new Button("GetFlags", _ => RunFlagOperation(flagOperations.GetFlags)),
                        new Button("ToggleFlags", _ => { ToggleDay(DaysOfWeek.Saturday); RunFlagOperation(flagOperations.ToggleFlags); })
                    ).Align(Align.Center),
                    Layout.Vertical(
                        Text.H3($"Selected Operation: {selectedFlagView.Value}"),
                        flagResult.Value
                    ).Align(Align.Center),
                    new Separator(),
                    // Validation demonstration
                    Layout.Horizontal(new Button("Validate DayType examples", _ =>
                                        {
                                            var isDayValidOne = DayType.Weekday.IsValid();
                                            var isDaysValidOne = (DayType.Weekday | DayType.Holiday).IsValid();
                                            var IsDaysvalidTwo = (DayType.Weekday | DayType.Weekend).IsValid();
                                            client.Toast($"Weekday: {isDayValidOne}, Weekday|Holiday: {isDaysValidOne}, Weekday|Weekend: {IsDaysvalidTwo}", "Validation");
                                        }).Icon(Icons.Check),
                                        new Button("Validate Numeric Enums examples", _ =>
                                        {
                                            var isNumberValidOne = NumericOperator.LessThan.IsValid();
                                            var isNumberValidTwo = ((NumericOperator)20).IsValid();
                                            client.Toast($"Is Number One Valid: {isNumberValidOne}, Is Number Two valid: {isNumberValidTwo},", "Validation");
                                        }).Icon(Icons.Check))
                    )).Title("Flags & Validation"),


            // card for Attribute & parsing 
            new Card(
                        Layout.Vertical(
                                    Text.Strong("Attribute access & parsing"),
                                    Layout.Horizontal(
                                        new Button("Show NotEquals symbol", _ =>
                                        {
                                            var sym = NumericOperator.NotEquals.GetAttributes().Get<SymbolAttribute>()?.Symbol ?? "<none>";
                                            client.Toast($"NotEquals symbol: {sym}", "Attributes");
                                        }).Icon(Icons.Info),
                                    new Button("Parse 'Monday, Wednesday' Flags", _ =>
                                        {
                                            var parsed = Enums.Parse<DaysOfWeek>("Monday, Wednesday");
                                            client.Toast($"Parsed flags => {parsed}", "Parsing");
                                        }).Icon(Icons.Calendar)))
                    ).Title("Helpers & Parsing").Width(1f));
        }
    }
}
