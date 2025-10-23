namespace HumanizerExample;
public class PlainTextOptions : ViewBase
{
    readonly IState<string> InputText;
    IState<ImmutableArray<string>> HumanizedTexts;
    public IState<int> TruncateValue;

    public PlainTextOptions(
        IState<string> inputText,
        IState<ImmutableArray<string>> humanizedTexts,
        IState<int> truncateValue)
    {
        InputText = inputText;
        HumanizedTexts = humanizedTexts;
        TruncateValue = truncateValue;
    }

    public override object? Build()
    {
        return Layout.Horizontal().Width(Size.Full())
                // Humanize
                | new DropDownMenu(@evt =>
                    {
                        if (Enum.TryParse<LetterCasing>(@evt.Value.ToString(), out var selected))
                        {
                            var currentText = InputText.Value;
                            currentText = currentText.Humanize(selected);
                            HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
                        }
                    },
                    new Button("Humanize"), // The button label
                    MenuItem.Default($"{LetterCasing.Sentence}"),
                    MenuItem.Default($"{LetterCasing.Title}"),
                    MenuItem.Default($"{LetterCasing.AllCaps}"),
                    MenuItem.Default($"{LetterCasing.LowerCase}")
                )
                // Truncate
                | new DropDownMenu(@evt =>
                    {
                        if (Enum.TryParse<LetterCasing>(@evt.Value.ToString(), out var selected))
                        {
                            var currentText = InputText.Value;
                            currentText = currentText
                                .Humanize(selected)
                                .Truncate(TruncateValue.Value, Truncator.FixedLength);
                            HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
                        }
                    },
                    new Button("Truncate"),
                    MenuItem.Default($"{LetterCasing.Sentence}"),
                    MenuItem.Default($"{LetterCasing.Title}"),
                    MenuItem.Default($"{LetterCasing.AllCaps}"),
                    MenuItem.Default($"{LetterCasing.LowerCase}")
                )
                // Pascalize
                | new Button("Pascalize", onClick: _ =>
                    {
                        var currentText = InputText.Value;
                        currentText = currentText.Pascalize();
                        HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
                    }).Variant(ButtonVariant.Primary)
                // Camelize
                | new Button("Camelize", onClick: _ =>
                    {
                        var currentText = InputText.Value;
                        currentText = currentText.Camelize();
                        HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
                    }).Variant(ButtonVariant.Primary)
                // Underscore
                | new Button("Underscore", onClick: _ =>
                    {
                        var currentText = InputText.Value;
                        currentText = currentText.Underscore();
                        currentText = NormalizeSeparator(currentText, '_');
                        HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
                    }).Variant(ButtonVariant.Primary)
                // Kebaberize
                | new Button("Kebaberize", onClick: _ =>
                    {
                        var currentText = InputText.Value;
                        currentText = currentText.Kebaberize();
                        currentText = NormalizeSeparator(currentText, '-');
                        HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
                    }).Variant(ButtonVariant.Primary)
                // Clear History
                | new Button("Clear History", onClick: _ =>
                    {
                        HumanizedTexts.Set([]);
                    }).Variant(ButtonVariant.Destructive)
                 ;
    }

    /// <summary>
    /// Normalizes occurrences of a specified separator character in a string.
    /// It collapses multiple consecutive separators into a single one and
    /// trims any leading or trailing separators.
    /// </summary>
    /// <param name="input">The input string to normalize.</param>
    /// <param name="separator">The character to normalize (e.g. '_', '-', '.').</param>
    /// <returns>
    /// A new string where:
    /// - Multiple consecutive separators are replaced with a single separator.
    /// - Leading and trailing separators are removed.
    /// If <paramref name="input"/> is null or empty, it is returned unchanged.
    /// </returns>
    public static string NormalizeSeparator(string input, char separator)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Escape the char if it's special in regex (like . or +)
        string escaped = Regex.Escape(separator.ToString());

        // Collapse multiple occurrences
        string result = Regex.Replace(input, $"{escaped}+", separator.ToString());

        // Trim from start and end
        result = result.Trim(separator);

        return result;
    }
}