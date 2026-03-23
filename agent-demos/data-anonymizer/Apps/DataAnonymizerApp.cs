using System.Security.Cryptography;
using System.Text;
using Ivy;

namespace Data.Anonymizer.Apps;

public enum AnonymizationStrategy
{
    Mask,
    Hash,
    Randomize,
    Redact
}

public record ColumnConfig(string Name, bool Enabled, AnonymizationStrategy Strategy);

[App(icon: Icons.ShieldCheck)]
public class DataAnonymizerApp : ViewBase
{
    public override object? Build()
    {
        var fileState = UseState<FileUpload<string>?>();
        var columns = UseState<ColumnConfig[]>([]);
        var parsedRows = UseState<string[][]>([]);
        var headers = UseState<string[]>([]);
        var anonymized = UseState(false);
        var anonymizedCsv = UseState("");
        var uploadCtx = UseUpload(MemoryStreamUploadHandler.Create(fileState));
        var downloadUrl = UseDownload(
            factory: () => Encoding.UTF8.GetBytes(anonymizedCsv.Value),
            mimeType: "text/csv",
            fileName: "anonymized.csv"
        );

        UseEffect(() =>
        {
            var file = fileState.Value;
            if (file?.Content is not { } csv || string.IsNullOrWhiteSpace(csv))
            {
                headers.Set([]);
                parsedRows.Set([]);
                columns.Set([]);
                anonymized.Set(false);
                anonymizedCsv.Set("");
                return;
            }

            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            var hdrs = ParseCsvLine(lines[0]);
            headers.Set(hdrs);

            var rows = lines.Skip(1).Select(ParseCsvLine).ToArray();
            parsedRows.Set(rows);

            columns.Set(hdrs.Select(h =>
                new ColumnConfig(h, false, AnonymizationStrategy.Mask)).ToArray());
            anonymized.Set(false);
            anonymizedCsv.Set("");
        }, fileState);

        var upload = uploadCtx.Accept(".csv").MaxFileSize(FileSize.FromMegabytes(10));

        var body = Layout.Vertical().Width(Size.Full().Max(200));

        if (fileState.Value?.Content is null)
        {
            return Layout.TopCenter()
                | (body.Margin(10)
                    | (Layout.Vertical().Align(Align.Center).Gap(2)
                        | new Icon(Icons.ShieldCheck).Large()
                        | Text.H2("Data Anonymizer")
                        | Text.Muted("Upload a CSV file to anonymize sensitive data"))
                    | fileState.ToFileInput(upload).Placeholder("Drop your CSV file here"));
        }

        body = body
            | (Layout.Horizontal().Align(Align.Left)
                | new Badge(fileState.Value.FileName)
                | new Button("Clear").Variant(ButtonVariant.Destructive).Small()
                    .OnClick(() => fileState.Set(null)));

        if (headers.Value.Length > 0 && parsedRows.Value.Length > 0)
        {
            var previewRows = parsedRows.Value.Take(5).ToArray();
            var preview = previewRows.Select(r =>
            {
                var dict = new Dictionary<string, string>();
                for (var i = 0; i < headers.Value.Length; i++)
                    dict[headers.Value[i]] = i < r.Length ? r[i] : "";
                return dict;
            }).ToArray();

            body = body
                | Text.H2("Data Preview")
                | Text.Muted($"Showing {previewRows.Length} of {parsedRows.Value.Length} rows")
                | preview.ToTable();
        }

        if (columns.Value.Length > 0)
        {
            body = body
                | new Separator()
                | Text.H2("Column Configuration")
                | Text.Muted("Select columns to anonymize and choose a strategy");

            for (var i = 0; i < columns.Value.Length; i++)
            {
                body = body | new ColumnConfigRow(columns, i) { Key = $"col-{i}" };
            }
        }

        body = body
            | new Separator()
            | new Button("Anonymize & Download", () =>
            {
                var result = new StringBuilder();
                result.AppendLine(string.Join(",", headers.Value.Select(EscapeCsvField)));

                foreach (var row in parsedRows.Value)
                {
                    var fields = new string[headers.Value.Length];
                    for (var j = 0; j < headers.Value.Length; j++)
                    {
                        var value = j < row.Length ? row[j] : "";
                        var col = j < columns.Value.Length ? columns.Value[j] : null;
                        fields[j] = col is { Enabled: true }
                            ? ApplyAnonymization(value, col.Strategy)
                            : value;
                    }
                    result.AppendLine(string.Join(",", fields.Select(EscapeCsvField)));
                }

                anonymizedCsv.Set(result.ToString());
                anonymized.Set(true);
            }).Primary().Icon(Icons.ShieldCheck);

        if (anonymized.Value && downloadUrl.Value is not null)
        {
            body = body
                | Callout.Success("Your data has been anonymized successfully.")
                | new Button("Download Anonymized CSV").Url(downloadUrl.Value)
                    .Icon(Icons.Download);
        }

        return Layout.TopCenter() | (body.Margin(10));
    }

    internal static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',')
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                }
                else if (c != '\r') current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }

    internal static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    internal static string ApplyAnonymization(string value, AnonymizationStrategy strategy)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return strategy switch
        {
            AnonymizationStrategy.Mask => value.Length <= 2 ? "***" : $"{value[0]}***{value[^1]}",
            AnonymizationStrategy.Hash =>
                Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..8],
            AnonymizationStrategy.Randomize => RandomizeValue(value),
            AnonymizationStrategy.Redact => "[REDACTED]",
            _ => value
        };
    }

    private static string RandomizeValue(string value)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, value.Length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}

internal class ColumnConfigRow : ViewBase
{
    private readonly IState<ColumnConfig[]> _columns;
    private readonly int _index;

    public ColumnConfigRow(IState<ColumnConfig[]> columns, int index)
    {
        _columns = columns;
        _index = index;
    }

    public override object? Build()
    {
        var enabledState = UseState(false);
        var strategyState = UseState(AnonymizationStrategy.Mask.ToString());

        UseEffect(() =>
        {
            var updated = _columns.Value.ToArray();
            var strat = Enum.TryParse<AnonymizationStrategy>(strategyState.Value, out var s)
                ? s : AnonymizationStrategy.Mask;
            updated[_index] = new ColumnConfig(_columns.Value[_index].Name, enabledState.Value, strat);
            _columns.Set(updated);
        }, enabledState, strategyState);

        var col = _columns.Value[_index];
        var strategyOptions = Enum.GetNames<AnonymizationStrategy>();

        return Layout.Horizontal().Align(Align.Left)
            | Text.Block(col.Name).Width(Size.Units(40))
            | enabledState.ToSwitchInput(label: "Anonymize")
            | strategyState.ToSelectInput(strategyOptions, placeholder: "Strategy")
                .Disabled(!enabledState.Value);
    }
}
