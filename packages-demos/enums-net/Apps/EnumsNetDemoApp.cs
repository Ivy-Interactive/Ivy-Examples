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
///   CommonFlags, RemoveFlags, GetFlags) with interactive UI controls.
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
        enum flagOperations { HasAllFlags, HasAnyFlags, CombineFlags, CommonFlags, RemoveFlags, GetFlags }

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
            var selectedFlagView = UseState<string>(() => "HasAllFlags");
            
            var flagA = DaysOfWeek.Monday | DaysOfWeek.Wednesday | DaysOfWeek.Friday;
            var flagB = DaysOfWeek.Monday | DaysOfWeek.Wednesday;
            // state for DaysOfWeek flags
            var daysFlags = UseState(flagB);
            var flagResult = UseState<object>(() => CreateHasAllFlagsMarkdown());
            // Validation result state
            var validationResult = UseState<object>(() => Text.P("Select a validation option to see results"));


            // Simple enum viewer state
            var selectedEnumType = UseState<string>(() => "DaysOfWeek");
            var simpleEnumList = UseState<List<EnumMemberInfo>>(() => GetEnumMembers("DaysOfWeek"));

            // Demo selection state
            var selectedDemo = UseState<string>(() => "");

            // Helper function to get enum members
            List<EnumMemberInfo> GetEnumMembers(string enumTypeName)
            {
                var enumType = enumTypeName switch
                {
                    "NumericOperator" => typeof(NumericOperator),
                    "DaysOfWeek" => typeof(DaysOfWeek),
                    "DayType" => typeof(DayType),
                    "PriorityLevel" => typeof(PriorityLevel),
                    _ => throw new ArgumentException($"Invalid enum type name: '{enumTypeName}'", nameof(enumTypeName))
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

            // Update flag operation when selectedFlagView changes
            UseEffect(() =>
            {
                try
                {
                    RunFlagOperation(selectedFlagView.Value);
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }, selectedFlagView);
            
            // Update demo when selectedDemo changes
            UseEffect(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(selectedDemo.Value))
                    {
                        validationResult.Set(Text.Muted("Select a demo from the dropdown to see results"));
                        return;
                    }
                    switch (selectedDemo.Value)
                    {
                        case "Enumeration":
                            RunEnumerationDemo();
                            break;
                        case "StringFormatting":
                            RunStringFormattingDemo();
                            break;
                        case "FlagOperations":
                            RunFlagOperationsDemo();
                            break;
                        case "Parsing":
                            RunParsingDemo();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                }
            }, selectedDemo);

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

            // Demo functions
            void RunEnumerationDemo()
            {
                try
                {
                    var allMembers = Enums.GetMembers<NumericOperator>().ToList();
                    var distinctValues = Enums.GetValues<NumericOperator>(EnumMemberSelection.Distinct).ToList();

                    var markdown = $"### Enumeration Demo\n\n" +
                                 $"**NumericOperator Enum Members:**\n\n" +
                                 $"**All Members ({allMembers.Count}):**\n" +
                                 string.Join(", ", allMembers.Select(m => m.Name)) + "\n\n" +
                                 $"**Distinct Values ({distinctValues.Count}):**\n" +
                                 string.Join(", ", distinctValues.Select(v => v.GetName())) + "\n\n" +
                                 $"**Explanation:** `GetMembers()` returns all enum members, while `GetValues(Distinct)` returns only unique values (removes duplicates like `NotLessThan = GreaterThanOrEquals`).";

                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunStringFormattingDemo()
            {
                try
                {
                    var equalsName = NumericOperator.Equals.AsString();
                    var equalsDescription = NumericOperator.Equals.AsString(EnumFormat.Description);
                    var lessThanName = NumericOperator.LessThan.AsString(EnumFormat.Description, EnumFormat.Name);
                    var invalidName = ((NumericOperator)(-1)).AsString();

                    var markdown = $"### String Formatting Demo\n\n" +
                                 $"**NumericOperator Examples:**\n\n" +
                                 $"1. **Equals.Name:** `{equalsName}`\n\n" +
                                 $"2. **Equals.Description:** `{equalsDescription}`\n\n" +
                                 $"3. **LessThan (Description or Name):** `{lessThanName}`\n\n" +
                                 $"4. **Invalid Value (-1):** `{invalidName}`\n\n" +
                                 $"**Explanation:** Enums.NET provides flexible string formatting with fallback options for missing attributes.";

                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunFlagOperationsDemo()
            {
                try
                {
                    var mondayWednesday = DaysOfWeek.Monday | DaysOfWeek.Wednesday;
                    var hasAllFlags = mondayWednesday.HasAllFlags(DaysOfWeek.Monday);
                    var hasAnyFlags = DaysOfWeek.Monday.HasAnyFlags(mondayWednesday);
                    var combinedFlags = DaysOfWeek.Monday.CombineFlags(DaysOfWeek.Wednesday);
                    var commonFlags = mondayWednesday.CommonFlags(DaysOfWeek.Monday);
                    var removedFlags = mondayWednesday.RemoveFlags(DaysOfWeek.Monday);
                    var weekendFlags = DaysOfWeek.Weekend.GetFlags();

                    var markdown = $"### Flag Operations Demo\n\n" +
                                 $"**DaysOfWeek Flag Operations:**\n\n" +
                                 $"1. **HasAllFlags:** `{mondayWednesday}.HasAllFlags({DaysOfWeek.Monday})` → `{hasAllFlags}`\n\n" +
                                 $"2. **HasAnyFlags:** `{DaysOfWeek.Monday}.HasAnyFlags({mondayWednesday})` → `{hasAnyFlags}`\n\n" +
                                 $"3. **CombineFlags:** `{DaysOfWeek.Monday}.CombineFlags({DaysOfWeek.Wednesday})` → `{combinedFlags}`\n\n" +
                                 $"4. **CommonFlags:** `{mondayWednesday}.CommonFlags({DaysOfWeek.Monday})` → `{commonFlags}`\n\n" +
                                 $"5. **RemoveFlags:** `{mondayWednesday}.RemoveFlags({DaysOfWeek.Monday})` → `{removedFlags}`\n\n" +
                                 $"6. **GetFlags:** `{DaysOfWeek.Weekend}.GetFlags()` → `[{string.Join(", ", weekendFlags.Select(f => f.ToString()))}]`\n\n" +
                                 $"**Explanation:** Flag operations provide powerful bitwise manipulation for flag enums.";

                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunParsingDemo()
            {
                try
                {
                    var parsedByName = Enums.Parse<NumericOperator>("GreaterThan");
                    var parsedByValue = Enums.Parse<NumericOperator>("1");
                    var parsedByDescription = Enums.Parse<NumericOperator>("Is", ignoreCase: false, EnumFormat.Description);
                    var parsedFlags = Enums.Parse<DaysOfWeek>("Monday, Wednesday");
                    var parsedFlagsWithDelimiter = FlagEnums.ParseFlags<DaysOfWeek>("Tuesday | Thursday", ignoreCase: false, delimiter: "|");

                    var markdown = $"### Parsing Demo\n\n" +
                                 $"**Parsing Examples:**\n\n" +
                                 $"1. **Parse by Name:** `Enums.Parse<NumericOperator>(\"GreaterThan\")` → `{parsedByName}`\n\n" +
                                 $"2. **Parse by Value:** `Enums.Parse<NumericOperator>(\"1\")` → `{parsedByValue}`\n\n" +
                                 $"3. **Parse by Description:** `Enums.Parse<NumericOperator>(\"Is\", EnumFormat.Description)` → `{parsedByDescription}`\n\n" +
                                 $"4. **Parse Flags:** `Enums.Parse<DaysOfWeek>(\"Monday, Wednesday\")` → `{parsedFlags}`\n\n" +
                                 $"5. **Parse with Custom Delimiter:** `FlagEnums.ParseFlags<DaysOfWeek>(\"Tuesday | Thursday\", delimiter: \"|\")` → `{parsedFlagsWithDelimiter}`\n\n" +
                                 $"**Explanation:** Enums.NET supports parsing by name, value, description, and custom formats with flexible delimiters.";

                    validationResult.Set(Text.Markdown(markdown));
                }
                catch (Exception ex)
                {
                    client.Error(ex);
                    validationResult.Set(Text.Markdown($"### Error\n\n**Message:** {ex.Message}"));
                }
            }

            void RunFlagOperation(string operationName)
            {
                try
                {
                    object result = operationName switch
                    {
                        "HasAllFlags" => CreateHasAllFlagsMarkdown(),
                        "HasAnyFlags" => CreateHasAnyFlagsMarkdown(),
                        "CombineFlags" => CreateCombineFlagsMarkdown(),
                        "CommonFlags" => CreateCommonFlagsMarkdown(),
                        "RemoveFlags" => CreateRemoveFlagsMarkdown(),
                        "GetFlags" => CreateGetFlagsMarkdown(),
                        _ => Text.Markdown("### Unsupported Operation\n\nThis operation is not supported.")
                    };

                    selectedFlagView.Set(operationName);
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
            new Card(
                Layout.Vertical().Gap(2)
                    | selectedFlagView.ToSelectInput(
                        new[] { "HasAllFlags", "HasAnyFlags", "CombineFlags", "CommonFlags", "RemoveFlags", "GetFlags" }.ToOptions()
                    )
                    | flagResult.Value);

            var validationAndErrorHandling =
                new Expandable(
                        "Enums.NET Demos",
                        Layout.Vertical().Gap(2)
                            | Text.Muted("Explore different Enums.NET features with practical examples")
                            | new Card(
                                Layout.Vertical().Gap(2)
                                | selectedDemo.ToSelectInput(new[] { "Enumeration", "StringFormatting", "FlagOperations", "Parsing" }.ToOptions())
                                    .Variant(SelectInputs.Toggle)
                                | validationResult.Value)
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
                                    | Text.Muted("Interactive demonstrations of flag operations on DaysOfWeek enum")
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
