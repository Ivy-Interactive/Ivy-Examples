using System.Net;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using Ivy;

namespace Html.Agility.Pack.Scraper.Apps;

[App(title: "Web Scraper", icon: Icons.Globe)]
public class ScraperApp : ViewBase
{
    public override object? Build()
    {
        var url = UseState("https://news.ycombinator.com");
        var selector = UseState("a.titlelink, .titleline a");
        var attribute = UseState("");
        var results = UseState(() => new List<ScrapedItem>());
        var error = UseState<string?>(null);
        var loading = UseState(false);

        var jsonDownload = UseDownload(
            factory: () =>
            {
                var json = JsonSerializer.Serialize(results.Value, new JsonSerializerOptions { WriteIndented = true });
                return Encoding.UTF8.GetBytes(json);
            },
            mimeType: "application/json",
            fileName: $"scrape-{DateTime.Now:yyyy-MM-dd-HHmmss}.json"
        );

        var csvDownload = UseDownload(
            factory: () =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Index,Text,Attribute,OuterHtml");
                foreach (var item in results.Value)
                {
                    sb.AppendLine($"{item.Index},{CsvEscape(item.Text)},{CsvEscape(item.Attribute)},{CsvEscape(item.OuterHtml)}");
                }
                return Encoding.UTF8.GetBytes(sb.ToString());
            },
            mimeType: "text/csv",
            fileName: $"scrape-{DateTime.Now:yyyy-MM-dd-HHmmss}.csv"
        );

        return Layout.Vertical()
            | Text.H2("Web Scraper")
            | Text.Muted("Enter a URL and CSS selector to extract structured data from any web page.")
            | BuildInputPanel(url, selector, attribute, results, error, loading)
            | BuildResultsPanel(results, error, jsonDownload, csvDownload);
    }

    private object BuildInputPanel(
        IState<string> url,
        IState<string> selector,
        IState<string> attribute,
        IState<List<ScrapedItem>> results,
        IState<string?> error,
        IState<bool> loading)
    {
        return new Card(
            Layout.Vertical()
                | url.ToTextInput().Placeholder("https://example.com").WithField().Label("URL")
                | (Layout.Horizontal()
                    | selector.ToTextInput().Placeholder("div.item, h2 > a, .class-name").WithField().Label("CSS Selector").Width(Size.Full())
                    | attribute.ToTextInput().Placeholder("e.g. href, src, title (leave blank for text)").WithField().Label("Attribute (optional)").Width(Size.Full()))
                | (Layout.Horizontal()
                    | new Button(loading.Value ? "Scraping..." : "Scrape", async () =>
                    {
                        if (string.IsNullOrWhiteSpace(url.Value) || string.IsNullOrWhiteSpace(selector.Value))
                        {
                            error.Set("Please enter both a URL and a CSS selector.");
                            return;
                        }
                        loading.Set(true);
                        error.Set(null);
                        results.Set(new());
                        try
                        {
                            var items = await ScrapeAsync(url.Value, selector.Value, attribute.Value);
                            results.Set(items);
                        }
                        catch (Exception ex)
                        {
                            error.Set(ex.Message);
                        }
                        finally
                        {
                            loading.Set(false);
                        }
                    }).Primary().Disabled(loading.Value).Icon(Icons.Search))
        );
    }

    private object BuildResultsPanel(
        IState<List<ScrapedItem>> results,
        IState<string?> error,
        IState<string?> jsonDownload,
        IState<string?> csvDownload)
    {
        if (error.Value is { } err)
            return Callout.Error(err, "Scrape Error");

        if (results.Value.Count == 0)
            return Callout.Info("Enter a URL and CSS selector above, then click Scrape to extract data.");

        return Layout.Vertical()
            | (Layout.Horizontal()
                | Text.H3($"Results ({results.Value.Count} items)")
                | new Spacer()
                | (jsonDownload.Value != null
                    ? new Button("Export JSON").Url(jsonDownload.Value).Icon(Icons.Download).Small()
                    : (object)Text.Muted("Preparing..."))
                | (csvDownload.Value != null
                    ? new Button("Export CSV").Url(csvDownload.Value).Icon(Icons.Download).Small()
                    : (object)Text.Muted("Preparing...")))
            | BuildResultsTable(results.Value);
    }

    private object BuildResultsTable(List<ScrapedItem> items)
    {
        var rows = new List<TableRow>();

        rows.Add(new TableRow(
            new TableCell("#").IsHeader(),
            new TableCell("Text").IsHeader(),
            new TableCell("Attribute").IsHeader(),
            new TableCell("HTML").IsHeader()
        ));

        foreach (var item in items.Take(200))
        {
            rows.Add(new TableRow(
                new TableCell(item.Index.ToString()),
                new TableCell(Truncate(item.Text, 120)),
                new TableCell(Truncate(item.Attribute, 120)),
                new TableCell(Truncate(item.OuterHtml, 80)).Small()
            ));
        }

        return new Table(rows.ToArray()).Width(Size.Full());
    }

    private static async Task<List<ScrapedItem>> ScrapeAsync(string url, string cssSelector, string attribute)
    {
        var web = new HtmlWeb();
        web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        var doc = await web.LoadFromWebAsync(url);
        var nodes = doc.DocumentNode.QuerySelectorAll(cssSelector).ToList();

        var items = new List<ScrapedItem>();
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var text = WebUtility.HtmlDecode(node.InnerText?.Trim() ?? "");
            var attrValue = !string.IsNullOrWhiteSpace(attribute)
                ? node.GetAttributeValue(attribute, "")
                : "";

            items.Add(new ScrapedItem
            {
                Index = i + 1,
                Text = text,
                Attribute = attrValue,
                OuterHtml = node.OuterHtml?.Trim() ?? ""
            });
        }

        return items;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

public record ScrapedItem
{
    public int Index { get; init; }
    public string Text { get; init; } = "";
    public string Attribute { get; init; } = "";
    public string OuterHtml { get; init; } = "";
}
