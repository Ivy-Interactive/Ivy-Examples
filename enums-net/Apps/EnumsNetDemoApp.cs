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

            // Simple enum viewer state
            var selectedEnumType = UseState<Type>(() => typeof(NumericOperator));
            var simpleEnumList = UseState<List<EnumMemberInfo>>(() =>
                Enums.GetMembers<NumericOperator>()
                     .Select(m => new EnumMemberInfo(
                         (int)m.Value,
                         m.Name,
                         m.Attributes.Get<DescriptionAttribute>()?.Description,
                         m.Attributes.Get<SymbolAttribute>()?.Symbol
                     ))
                     .OrderBy(x => x.Value)
                     .ToList()
            );


            // Simple enum viewer function
            void ShowSimpleEnum(Type enumType)
            {
                try
                {
                    var members = Enums.GetMembers(enumType);
                    var memberInfo = members.Select(m => new EnumMemberInfo(
                        (int)m.Value,
                        m.Name,
                        m.Attributes.Get<DescriptionAttribute>()?.Description,
                        m.Attributes.Get<SymbolAttribute>()?.Symbol
                    )).OrderBy(x => x.Value).ToList();

                    simpleEnumList.Set(memberInfo);
                    selectedEnumType.Set(enumType);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }

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

            // Helper functions for creating Markdown results
            object CreateHasAllFlagsMarkdown()
            {
                var result = flagA.HasAllFlags(flagB);
                var markdown = $"### HasAllFlags Operation\n\n" +
                             $"**FlagA:** `{flagA}` (Value: {(int)flagA})\n\n" +
                             $"**FlagB:** `{flagB}` (Value: {(int)flagB})\n\n" +
                             $"**Operation:** `flagA.HasAllFlags(flagB)`\n\n" +
                             $"**Result:** `{result}`\n\n" +
                             $"**Explanation:** {(result ? "FlagA contains ALL flags from FlagB" : "FlagA does NOT contain all flags from FlagB")}";
                return Text.Markdown(markdown);
            }

            object CreateHasAnyFlagsMarkdown()
            {
                var result = DaysOfWeek.Monday.HasAnyFlags(flagB);
                var markdown = $"### HasAnyFlags Operation\n\n" +
                             $"**Monday:** `{DaysOfWeek.Monday}` (Value: {(int)DaysOfWeek.Monday})\n\n" +
                             $"**FlagB:** `{flagB}` (Value: {(int)flagB})\n\n" +
                             $"**Operation:** `Monday.HasAnyFlags(flagB)`\n\n" +
                             $"**Result:** `{result}`\n\n" +
                             $"**Explanation:** {(result ? "Monday shares at least one flag with FlagB" : "Monday shares NO flags with FlagB")}";
                return Text.Markdown(markdown);
            }

            object CreateCombineFlagsMarkdown()
            {
                var result = flagA.CombineFlags(flagB);
                var markdown = $"### CombineFlags Operation\n\n" +
                             $"**FlagA:** `{flagA}` (Value: {(int)flagA})\n\n" +
                             $"**FlagB:** `{flagB}` (Value: {(int)flagB})\n\n" +
                             $"**Operation:** `flagA.CombineFlags(flagB)`\n\n" +
                             $"**Result:** `{result}` (Value: {(int)result})\n\n" +
                             $"**Explanation:** Combines all flags from both FlagA and FlagB";
                return Text.Markdown(markdown);
            }

            object CreateCommonFlagsMarkdown()
            {
                var result = flagA.CommonFlags(flagB);
                var markdown = $"### CommonFlags Operation\n\n" +
                             $"**FlagA:** `{flagA}` (Value: {(int)flagA})\n\n" +
                             $"**FlagB:** `{flagB}` (Value: {(int)flagB})\n\n" +
                             $"**Operation:** `flagA.CommonFlags(flagB)`\n\n" +
                             $"**Result:** `{result}` (Value: {(int)result})\n\n" +
                             $"**Explanation:** Shows only flags that exist in BOTH FlagA and FlagB";
                return Text.Markdown(markdown);
            }

            object CreateRemoveFlagsMarkdown()
            {
                var result = flagB.RemoveFlags(DaysOfWeek.Wednesday);
                var markdown = $"### RemoveFlags Operation\n\n" +
                             $"**Original FlagB:** `{flagB}` (Value: {(int)flagB})\n\n" +
                             $"**Flag to Remove:** `{DaysOfWeek.Wednesday}` (Value: {(int)DaysOfWeek.Wednesday})\n\n" +
                             $"**Operation:** `flagB.RemoveFlags(DaysOfWeek.Wednesday)`\n\n" +
                             $"**Result:** `{result}` (Value: {(int)result})\n\n" +
                             $"**Explanation:** Removes Wednesday flag from FlagB";
                return Text.Markdown(markdown);
            }

            object CreateGetFlagsMarkdown()
            {
                var flags = DaysOfWeek.Weekend.GetFlags();
                var flagList = string.Join("\n", flags.Select(f => $"  - `{f}` (Value: {(int)f})"));
                var markdown = $"### GetFlags Operation\n\n" +
                             $"**Source:** `{DaysOfWeek.Weekend}` (Value: {(int)DaysOfWeek.Weekend})\n\n" +
                             $"**Operation:** `DaysOfWeek.Weekend.GetFlags()`\n\n" +
                             $"**Individual Flags:**\n{flagList}\n\n" +
                             $"**Total Flags Found:** {flags.Count}";
                return Text.Markdown(markdown);
            }

            object CreateToggleFlagsMarkdown()
            {
                var markdown = $"### ToggleFlags Operation\n\n" +
                             $"**Current Selection:** `{daysFlags.Value}` (Value: {(int)daysFlags.Value})\n\n" +
                             $"**Operation:** Toggled Saturday flag\n\n" +
                             $"**Explanation:** Saturday flag has been toggled (added/removed)";
                return Text.Markdown(markdown);
            }

            void RunFlagOperation(flagOperations type)
            {
                try
                {
                    object result = type switch
                    {
                        flagOperations.HasAllFlags => CreateHasAllFlagsMarkdown(),
                        flagOperations.HasAnyFlags => CreateHasAnyFlagsMarkdown(),
                        flagOperations.CombineFlags => CreateCombineFlagsMarkdown(),
                        flagOperations.CommonFlags => CreateCommonFlagsMarkdown(),
                        flagOperations.RemoveFlags => CreateRemoveFlagsMarkdown(),
                        flagOperations.GetFlags => CreateGetFlagsMarkdown(),
                        flagOperations.ToggleFlags => CreateToggleFlagsMarkdown(),
                        _ => Text.Markdown("### Unsupported Operation\n\nThis operation is not supported.")
                    };

                    selectedFlagView.Set(type);
                    flagResult.Set(result);

                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    flagResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            var simpleEnumViewer =
                Layout.Vertical().Gap(2)
                | new Expandable(
                    "Simple Enum Viewer",
                    Layout.Vertical().Gap(2)
                        | Text.Muted("Select an enum type to view its members with descriptions and symbols")
                        | Layout.Horizontal().Gap(2)
                            | new DropDownMenu(evt =>
                            {
                                var selectedType = evt.Value?.ToString();
                                var type = selectedType switch
                                {
                                    "NumericOperator" => typeof(NumericOperator),
                                    "DaysOfWeek" => typeof(DaysOfWeek),
                                    "DayType" => typeof(DayType),
                                    "PriorityLevel" => typeof(PriorityLevel),
                                    _ => typeof(NumericOperator)
                                };
                                ShowSimpleEnum(type);
                            },
                                    new Button($"View: {selectedEnumType.Value.Name}"),
                                    MenuItem.Default("NumericOperator").Tag("NumericOperator"),
                                    MenuItem.Default("DaysOfWeek").Tag("DaysOfWeek"),
                                    MenuItem.Default("DayType").Tag("DayType"),
                                    MenuItem.Default("PriorityLevel").Tag("PriorityLevel")
                                )
                            | Text.H4($"{selectedEnumType.Value.Name} Members:")
                            | (simpleEnumList.Value.Any()
                                ? simpleEnumList.Value.ToTable().Width(Size.Full())
                                : Text.Muted("Select an enum type above to view its members"))
                    );

            var enumEnumerationAndMembers =
                new Expandable(
                        "Enum Enumeration & Members",
                        Layout.Vertical().Gap(2)
                            | Text.Muted("Demonstrates various enumeration modes: All, Distinct, DisplayOrder, and Flags")
                            | new DropDownMenu(evt => {
                                var selectedMode = evt.Value?.ToString();
                                var mode = selectedMode switch {
                                    "All Members" => MembersView.All,
                                    "Distinct" => MembersView.Distinct,
                                    "Display Order" => MembersView.DisplayOrder,
                                    "Flags Only" => MembersView.Flags,
                                    _ => MembersView.All
                                };
                                selectedView(mode);
                            },
                                new Button($"Current Mode: {selectedEnumrateionView.Value}"),
                                MenuItem.Default("All Members").Tag("All Members"),
                                MenuItem.Default("Distinct").Tag("Distinct"),
                                MenuItem.Default("Display Order").Tag("Display Order"),
                                MenuItem.Default("Flags Only").Tag("Flags Only")
                            )
                            | Text.H4($"Current View: {selectedEnumrateionView.Value}")
                            | (enumrationList.Value.Any()
                                ? enumrationList.Value.ToTable().Width(Size.Full())
                                : Text.Muted("No members available"))
                    );

            var flagOperationsAndManipulation =
                new Expandable(
                        "Flag Operations & Manipulation",
                        Layout.Vertical().Gap(2)
                            | Text.Muted("Interactive demonstrations of flag operations on DaysOfWeek enum")
                            | new DropDownMenu(evt => {
                                var selectedOperation = evt.Value?.ToString();
                                var operation = selectedOperation switch {
                                    "HasAllFlags" => flagOperations.HasAllFlags,
                                    "HasAnyFlags" => flagOperations.HasAnyFlags,
                                    "CombineFlags" => flagOperations.CombineFlags,
                                    "CommonFlags" => flagOperations.CommonFlags,
                                    "RemoveFlags" => flagOperations.RemoveFlags,
                                    "GetFlags" => flagOperations.GetFlags,
                                    "ToggleFlags" => flagOperations.ToggleFlags,
                                    _ => flagOperations.HasAllFlags
                                };
                                if (operation == flagOperations.ToggleFlags)
                                {
                                    ToggleDay(DaysOfWeek.Saturday);
                                }
                                RunFlagOperation(operation);
                            },
                                new Button($"Flag Operation: {selectedFlagView.Value}"),
                                MenuItem.Default("HasAllFlags").Tag("HasAllFlags"),
                                MenuItem.Default("HasAnyFlags").Tag("HasAnyFlags"),
                                MenuItem.Default("CombineFlags").Tag("CombineFlags"),
                                MenuItem.Default("CommonFlags").Tag("CommonFlags"),
                                MenuItem.Default("RemoveFlags").Tag("RemoveFlags"),
                                MenuItem.Default("GetFlags").Tag("GetFlags"),
                                MenuItem.Default("ToggleFlags").Tag("ToggleFlags")
                            )
                            | flagResult.Value
                    );

            var validationAndErrorHandling =
                new Expandable(
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

            return Layout.Vertical().Gap(2)
                | new Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("Enums.NET")
                        | Text.Block("This demo showcases the Enums.NET library for working with enums and flags.")
                        | (Layout.Horizontal().Gap(2)
                            | new Card(
                                Layout.Vertical().Gap(2)
                                    | Text.H4("Actions & Operations")
                                    | Text.Muted("Perform operations and test enum functionality")
                                    | flagOperationsAndManipulation
                                    | validationAndErrorHandling
                            ).Width("50%")
                            | new Card(
                                Layout.Vertical().Gap(2)
                                    | Text.H4("Enum Viewer")
                                    | Text.Muted("Browse and explore enum types and their members")
                                    | simpleEnumViewer
                                    | enumEnumerationAndMembers
                            ).Width("50%"))
                ).Height(Size.Fit().Min(Size.Full()));
        }
    }
}
