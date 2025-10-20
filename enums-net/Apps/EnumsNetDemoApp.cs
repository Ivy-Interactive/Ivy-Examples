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
        // for falg enums related operations
        enum flagOperations { HasAllFlags, HasAnyFlags, CombineFlags, CommonFlags, RemoveFlags, GetFlags, ToggleFlags }

        // Record to store enum member metadata
        public record EnumMemberInfo(int Value, string Name, string? Description, string? Symbol, string? DisplayName, int? DisplayOrder);

        // Helper function to create dynamic table with only relevant columns
        object CreateDynamicEnumTable(List<EnumMemberInfo> members)
        {
            // Check which columns have data
            var hasDescription = members.Any(m => !string.IsNullOrEmpty(m.Description));
            var hasSymbol = members.Any(m => !string.IsNullOrEmpty(m.Symbol));
            var hasDisplayName = members.Any(m => !string.IsNullOrEmpty(m.DisplayName));
            var hasDisplayOrder = members.Any(m => m.DisplayOrder.HasValue);

            // Create table data based on available columns
            if (hasDescription && hasSymbol)
            {
                return members.Select(m => new
                {
                    Name = m.Name,
                    Value = m.Value,
                    Description = m.Description,
                    Symbol = m.Symbol
                }).ToTable().Width(Size.Full());
            }
            else if (hasDisplayName && hasDisplayOrder)
            {
                return members.Select(m => new
                {
                    Name = m.Name,
                    Value = m.Value,
                    DisplayName = m.DisplayName,
                    Order = m.DisplayOrder
                }).ToTable().Width(Size.Full());
            }
            else
            {
                return members.Select(m => new
                {
                    Name = m.Name,
                    Value = m.Value
                }).ToTable().Width(Size.Full());
            }
        }

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

            // Validation result state
            var validationResult = UseState<object>(() => Text.P("Select a validation option to see results"));


            // Simple enum viewer state
            var selectedEnumType = UseState<string>(() => "NumericOperator");
            var simpleEnumList = UseState<List<EnumMemberInfo>>(() => GetEnumMembers("NumericOperator"));

            // Helper function to get enum members
            List<EnumMemberInfo> GetEnumMembers(string enumTypeName)
            {
                var enumType = enumTypeName switch
                {
                    "NumericOperator" => typeof(NumericOperator),
                    "DaysOfWeek" => typeof(DaysOfWeek),
                    "DayType" => typeof(DayType),
                    "PriorityLevel" => typeof(PriorityLevel),
                    _ => typeof(NumericOperator)
                };

                return Enums.GetMembers(enumType)
                           .Select(m => new EnumMemberInfo(
                               (int)m.Value,
                               m.Name,
                               m.Attributes.Get<DescriptionAttribute>()?.Description,
                               m.Attributes.Get<SymbolAttribute>()?.Symbol,
                               m.Attributes.Get<DisplayAttribute>()?.Name,
                               m.Attributes.Get<DisplayAttribute>()?.Order
                           ))
                           .OrderBy(x => x.Value)
                           .ToList();
            }

            // Initialize and update enum list when selectedEnumType changes
            UseEffect(() =>
            {
                try
                {
                    var memberInfo = GetEnumMembers(selectedEnumType.Value);
                    simpleEnumList.Set(memberInfo);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }, selectedEnumType);


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

            // Validation functions
            void RunDayTypeValidation()
            {
                try
                {
                    var isWeekdayValid = DayType.Weekday.IsValid();
                    var isWeekdayHolidayValid = (DayType.Weekday | DayType.Holiday).IsValid();
                    var isWeekdayWeekendValid = (DayType.Weekday | DayType.Weekend).IsValid();
                    
                    var markdown = $"### DayType Validation\n\n" +
                                 $"**Test Cases:**\n\n" +
                                 $"1. **Weekday:** `{DayType.Weekday}` → **Valid:** `{isWeekdayValid}`\n\n" +
                                 $"2. **Weekday | Holiday:** `{DayType.Weekday | DayType.Holiday}` → **Valid:** `{isWeekdayHolidayValid}`\n\n" +
                                 $"3. **Weekday | Weekend:** `{DayType.Weekday | DayType.Weekend}` → **Valid:** `{isWeekdayWeekendValid}`\n\n" +
                                 $"**Explanation:** DayType uses custom validation - Weekday and Weekend are mutually exclusive, but Holiday can be combined with either.";
                    
                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunNumericOperatorValidation()
            {
                try
                {
                    var isLessThanValid = NumericOperator.LessThan.IsValid();
                    var isInvalidValueValid = ((NumericOperator)20).IsValid();
                    
                    var markdown = $"### NumericOperator Validation\n\n" +
                                 $"**Test Cases:**\n\n" +
                                 $"1. **Valid Value (LessThan):** `{NumericOperator.LessThan}` → **Valid:** `{isLessThanValid}`\n\n" +
                                 $"2. **Invalid Value (20):** `{(NumericOperator)20}` → **Valid:** `{isInvalidValueValid}`\n\n" +
                                 $"**Explanation:** Standard enum validation checks if the value is a defined enum member. Invalid values return false.";
                    
                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunDaysOfWeekValidation()
            {
                try
                {
                    var isWeekendValid = DaysOfWeek.Weekend.IsValid();
                    var isValidCombination = (DaysOfWeek.Sunday | DaysOfWeek.Wednesday).IsValid();
                    var isInvalidCombination = (DaysOfWeek.Sunday | DaysOfWeek.Wednesday | ((DaysOfWeek)(-1))).IsValid();
                    
                    var markdown = $"### DaysOfWeek Validation\n\n" +
                                 $"**Test Cases:**\n\n" +
                                 $"1. **Weekend:** `{DaysOfWeek.Weekend}` → **Valid:** `{isWeekendValid}`\n\n" +
                                 $"2. **Valid Combination:** `{DaysOfWeek.Sunday | DaysOfWeek.Wednesday}` → **Valid:** `{isValidCombination}`\n\n" +
                                 $"3. **Invalid Combination (with -1):** `{DaysOfWeek.Sunday | DaysOfWeek.Wednesday | (DaysOfWeek)(-1)}` → **Valid:** `{isInvalidCombination}`\n\n" +
                                 $"**Explanation:** Flag enum validation checks for valid flag combinations and defined enum members.";
                    
                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunCustomValidatorDemo()
            {
                try
                {
                    var markdown = $"### Custom Validator Demo\n\n" +
                                 $"**DayType Validator Rules:**\n\n" +
                                 $"1. **Weekday** and **Weekend** are mutually exclusive\n" +
                                 $"2. **Holiday** can be combined with either Weekday or Weekend\n" +
                                 $"3. **Invalid combinations:** Weekday | Weekend\n\n" +
                                 $"**Implementation:**\n```csharp\n" +
                                 $"class DayTypeValidatorAttribute : EnumValidatorAttribute<DayType>\n" +
                                 $"{{\n" +
                                 $"    public override bool IsValid(DayType value) =>\n" +
                                 $"        value.GetFlagCount(DayType.Weekday | DayType.Weekend) == 1 &&\n" +
                                 $"        FlagEnums.IsValidFlagCombination(value);\n" +
                                 $"}}\n```\n\n" +
                                 $"**Try the DayType validation above to see this in action!**";
                    
                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
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
                    | Layout.Horizontal().Gap(2)
                        | selectedEnumType.ToSelectInput(
                            new[] { "NumericOperator", "DaysOfWeek", "DayType", "PriorityLevel" }.ToOptions()
                        )
                    | Text.H4($"{selectedEnumType.Value} Members:")
                    | CreateDynamicEnumTable(simpleEnumList.Value);


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
                            | Text.Muted("Test enum validation with different enum types and combinations")
                            | new DropDownMenu(evt => {
                                var selectedValidation = evt.Value?.ToString();
                                switch (selectedValidation)
                                {
                                    case "DayType":
                                        RunDayTypeValidation();
                                        break;
                                    case "NumericOperator":
                                        RunNumericOperatorValidation();
                                        break;
                                    case "DaysOfWeek":
                                        RunDaysOfWeekValidation();
                                        break;
                                    case "CustomValidator":
                                        RunCustomValidatorDemo();
                                        break;
                                }
                            },
                                new Button("Select Validation Type"),
                                MenuItem.Default("DayType Validation").Tag("DayType"),
                                MenuItem.Default("NumericOperator Validation").Tag("NumericOperator"),
                                MenuItem.Default("DaysOfWeek Validation").Tag("DaysOfWeek"),
                                MenuItem.Default("Custom Validator Demo").Tag("CustomValidator")
                            )
                            | validationResult.Value
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
                                    | Text.Muted("Select an enum type to view its members with descriptions and symbols")
                                    | simpleEnumViewer
                            ).Width("50%"))
                        | new Spacer().Height(Size.Units(10))
                        | Text.Small("This demo uses the Enums.NET library to work with enums and flags.")
                        | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Enums.NET](https://github.com/TylerBrinkley/Enums.NET)")
                ).Height(Size.Fit().Min(Size.Full()));
        }
    }
}
