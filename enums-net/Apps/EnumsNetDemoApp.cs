using EnumsNET;
using EnumsNetApp.Apps;
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
    [App(title: "Enums.NET", icon: Icons.Tag)]
    public class EnumsNetDemoApp : ViewBase
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

            // enum flag states 
            var selectedFlagView = UseState(flagOperations.HasAllFlags);
            var flagResult = UseState<object>(Text.P(""));
            var flagA = DaysOfWeek.Monday | DaysOfWeek.Wednesday | DaysOfWeek.Friday;
            var flagB = DaysOfWeek.Monday | DaysOfWeek.Wednesday;
            // state for DaysOfWeek flags
            var daysFlags = UseState(flagB);

            //Enumeration related state 
            var selectedEnumrateionView = UseState(() => MembersView.All);
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
                Layout.Vertical().Gap(2)
                    | new Expandable(
                        "Enum Enumeration & Members",
                        Layout.Vertical().Gap(2)
                            | Text.Muted("Explore different ways to enumerate enum members using Enums.NET")
                            | Text.P("Demonstrates various enumeration modes: All, Distinct, DisplayOrder, and Flags")
                            | Layout.Horizontal().Gap(1)
                                | new Button("All Members", _ => selectedView(MembersView.All)).Primary()
                                | new Button("Distinct", _ => selectedView(MembersView.Distinct)).Secondary()
                                | new Button("Display Order", _ => selectedView(MembersView.DisplayOrder)).Secondary()
                                | new Button("Flags Only", _ => selectedView(MembersView.Flags)).Secondary()
                            | Text.H4($"Current View: {selectedEnumrateionView.Value}")
                            | (enumrationList.Value.Any() 
                                ? enumrationList.Value.ToTable().Width(Size.Full())
                                : Text.Muted("No members available"))
                    )

                    | new Expandable(
                        "Flag Operations & Manipulation",
                        Layout.Vertical().Gap(2)
                            | Text.Muted("Learn how to work with flag enums using advanced Enums.NET operations")
                            | Text.P("Interactive demonstrations of flag operations on DaysOfWeek enum")
                            | Layout.Horizontal().Gap(1).Wrap(true)
                                | new Button("HasAllFlags", _ => RunFlagOperation(flagOperations.HasAllFlags)).Primary()
                                | new Button("HasAnyFlags", _ => RunFlagOperation(flagOperations.HasAnyFlags)).Secondary()
                                | new Button("CombineFlags", _ => RunFlagOperation(flagOperations.CombineFlags)).Secondary()
                                | new Button("CommonFlags", _ => RunFlagOperation(flagOperations.CommonFlags)).Secondary()
                                | new Button("RemoveFlags", _ => RunFlagOperation(flagOperations.RemoveFlags)).Secondary()
                                | new Button("GetFlags", _ => RunFlagOperation(flagOperations.GetFlags)).Secondary()
                                | new Button("ToggleFlags", _ => { ToggleDay(DaysOfWeek.Saturday); RunFlagOperation(flagOperations.ToggleFlags); }).Secondary()
                            | new Separator()
                            | Text.H4($"Operation: {selectedFlagView.Value}")
                            | flagResult.Value
                    )

                    | new Expandable(
                        "Validation & Error Handling",
                        Layout.Vertical().Gap(2)
                            | Text.Muted("Validate enum values and handle invalid enum scenarios")
                            | Text.P("Test enum validation with different enum types and combinations")
                            | Layout.Horizontal().Gap(1)
                                | new Button("Validate DayType", _ =>
                                    {
                                        var isDayValidOne = DayType.Weekday.IsValid();
                                        var isDaysValidOne = (DayType.Weekday | DayType.Holiday).IsValid();
                                        var IsDaysvalidTwo = (DayType.Weekday | DayType.Weekend).IsValid();
                                        client.Toast($"Weekday: {isDayValidOne}, Weekday|Holiday: {isDaysValidOne}, Weekday|Weekend: {IsDaysvalidTwo}", "DayType Validation");
                                    }).Icon(Icons.Check).Primary()
                                | new Button("Validate NumericOperator", _ =>
                                    {
                                        var isNumberValidOne = NumericOperator.LessThan.IsValid();
                                        var isNumberValidTwo = ((NumericOperator)20).IsValid();
                                        client.Toast($"LessThan valid: {isNumberValidOne}, Invalid value (20) valid: {isNumberValidTwo}", "NumericOperator Validation");
                                    }).Icon(Icons.Check).Secondary()
                    );
        }
    }
}
