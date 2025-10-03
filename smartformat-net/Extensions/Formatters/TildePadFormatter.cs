using SmartFormat.Core.Extensions;

/// <summary>Custom SmartFormat formatter that pads the current placeholder with a repeated character </summary>
public class TildePadFormatter : IFormatter
{
    public string Name => "tilde";

    string IFormatter.Name { get => Name; set { } }
    public bool CanAutoDetect { get; set; } = false;

    public bool TryEvaluateFormat(IFormattingInfo info)
    {
        if (info.Format?.RawText is string raw && int.TryParse(raw, out int totalLength))
        {
            int currentLength = info.FormatDetails.OriginalFormat.ToString().Length - info.Placeholder.Length;

            int toAdd = totalLength - currentLength;
            if (toAdd < 0) toAdd = 0;

            string line = new('_', toAdd);
            info.Write(line);
            return true;
        }

        return false;
    }
}
