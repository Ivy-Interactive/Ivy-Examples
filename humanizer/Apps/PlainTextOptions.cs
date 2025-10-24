namespace HumanizerExample;
public class PlainTextOptions : ViewBase
{
    readonly IState<string> InputText;
    IState<ImmutableArray<string>> HumanizedTexts;
    public IState<int> TruncateValue;
    readonly IState<string> SelectedTransformation;

    public PlainTextOptions(
        IState<string> inputText,
        IState<ImmutableArray<string>> humanizedTexts,
        IState<int> truncateValue,
        IState<string> selectedTransformation)
    {
        InputText = inputText;
        HumanizedTexts = humanizedTexts;
        TruncateValue = truncateValue;
        SelectedTransformation = selectedTransformation;
    }

    public override object? Build()
    {
        
        var transformationOptions = new[]
        {
            "Humanize (Sentence)",
            "Humanize (Title)", 
            "Humanize (All Caps)",
            "Humanize (Lower Case)",
            "Truncate (Sentence)",
            "Truncate (Title)",
            "Truncate (All Caps)",
            "Truncate (Lower Case)",
            "Pascalize",
            "Camelize",
            "Underscore",
            "Kebaberize"
        };

        // Function to process transformation
        void ProcessTransformation()
        {
            if (string.IsNullOrEmpty(SelectedTransformation.Value) || string.IsNullOrEmpty(InputText.Value))
                return;

            var currentText = InputText.Value;
            var selectedOption = SelectedTransformation.Value;
            
            switch (selectedOption)
            {
                case "Humanize (Sentence)":
                    currentText = currentText.Humanize(LetterCasing.Sentence);
                    break;
                case "Humanize (Title)":
                    currentText = currentText.Humanize(LetterCasing.Title);
                    break;
                case "Humanize (All Caps)":
                    currentText = currentText.Humanize(LetterCasing.AllCaps);
                    break;
                case "Humanize (Lower Case)":
                    currentText = currentText.Humanize(LetterCasing.LowerCase);
                    break;
                case "Truncate (Sentence)":
                    currentText = currentText.Humanize(LetterCasing.Sentence).Truncate(TruncateValue.Value, Truncator.FixedLength);
                    break;
                case "Truncate (Title)":
                    currentText = currentText.Humanize(LetterCasing.Title).Truncate(TruncateValue.Value, Truncator.FixedLength);
                    break;
                case "Truncate (All Caps)":
                    currentText = currentText.Humanize(LetterCasing.AllCaps).Truncate(TruncateValue.Value, Truncator.FixedLength);
                    break;
                case "Truncate (Lower Case)":
                    currentText = currentText.Humanize(LetterCasing.LowerCase).Truncate(TruncateValue.Value, Truncator.FixedLength);
                    break;
                case "Pascalize":
                    currentText = currentText.Pascalize();
                    break;
                case "Camelize":
                    currentText = currentText.Camelize();
                    break;
                case "Underscore":
                    currentText = currentText.Underscore();
                    currentText = NormalizeSeparator(currentText, '_');
                    break;
                case "Kebaberize":
                    currentText = currentText.Kebaberize();
                    currentText = NormalizeSeparator(currentText, '-');
                    break;
            }
            
            HumanizedTexts.Set(HumanizedTexts.Value.Add(currentText));
        }

        return Layout.Vertical().Gap(3)
            | Layout.Vertical().Gap(2)
                // Transformation Select
                | SelectedTransformation.ToSelectInput(transformationOptions.ToOptions())
                    .Placeholder("Select transformation...")
                    .Width(Size.Full())
                    .WithLabel("Transformation Options:")
                // Transform Button
                | new Button("Transform", onClick: _ => ProcessTransformation())
                    .Variant(ButtonVariant.Primary)
                    .Width(Size.Full())
                    .Disabled(string.IsNullOrEmpty(SelectedTransformation.Value) || string.IsNullOrEmpty(InputText.Value))
                // Clear History Button
                | new Button("Clear History", onClick: _ => { HumanizedTexts.Set([]); })
                    .Variant(ButtonVariant.Destructive)
                    .Width(Size.Full());
                
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