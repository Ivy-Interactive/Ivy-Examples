namespace HtmlAgilityPack.Apps;

using HtmlAgilityPack;

[App(icon: Icons.Code, title: "HtmlAgilityPack Demo")]
public class HtmlAgilityPackApp : ViewBase
{
    public override object? Build()
    {
        var urlState = UseState("https://ivy.app");
        var urlMetaState = UseState<string>("");
        var urlLinksState = UseState<string>("");
        var urlTitleState = UseState<string>("");
        var errorState = UseState<string>("");
        var parsingState = UseState(false);

        HtmlDocument? document = null;
        var loadURL = (string url) =>
        {
            var webGet = new HtmlWeb();
            try
            {
                document = webGet.Load(url);
            }
            catch //invalid url
            {
                return;
            }
        };

        var getTitleData = () =>
        {
            if (document == null)
                return string.Empty;
            string title = string.Empty;
            var titleNode = document.DocumentNode.SelectSingleNode("//head/title");
            if (titleNode != null)
            {
                title = titleNode.InnerText.Trim();
            }
            return title;
        };

        var getMetaData = () =>
        {
            if (document == null)
                return string.Empty;
            string meta = string.Empty;
            var metaTags = document.DocumentNode.SelectNodes("//meta");
            if (metaTags != null)
            {
                foreach (var tag in metaTags)
                {
                    if (tag.Attributes["name"] != null && tag.Attributes["content"] != null && tag.Attributes["name"].Value.ToLower() == "description")
                    {
                        meta += tag.Attributes["content"].Value + System.Environment.NewLine;
                    }
                }
            }
            else
            {
                meta = string.Empty;
            }
            return meta;
        };

        var getLinksData = () =>
        {
            if (document == null)
                return string.Empty;
            string links = string.Empty;
            var metaTags = document.DocumentNode.SelectNodes("//a");
            if (metaTags != null)
            {
                foreach (var tag in metaTags)
                {
                    if (tag.Attributes["href"] != null && (tag.Attributes["href"].Value.StartsWith("https://") || tag.Attributes["href"].Value.StartsWith("http://")))
                        links += tag.Attributes["href"].Value + System.Environment.NewLine;
                }
            }
            else
            {
                links = string.Empty;
            }
            return links;
        };


        var eventHandler = (Event<Button> e) =>
        {
            urlTitleState.Set("");
            urlMetaState.Set("");
            urlLinksState.Set("");
            errorState.Set("");
            parsingState.Set(true);
            loadURL(urlState.Value);
            if (document == null)
            {
                parsingState.Set(false);
                errorState.Set("Invalid URL !");
                return;
            }
            urlTitleState.Set(getTitleData());
            urlMetaState.Set(getMetaData());
            urlLinksState.Set(getLinksData());
            parsingState.Set(false);
        };

        return Layout.Vertical().Gap(2).Padding(2)
                   | urlState.ToTextInput().WithLabel("Enter Site URL:")
                   | new Button("Parse Site HTML", eventHandler).Loading(parsingState.Value)
                   | (errorState.Value.Length > 0 ? Text.Block(errorState.Value) : null)
                   | (urlTitleState.Value.Length > 0 ? Text.Block("Site Title:") : null)
                   | (urlTitleState.Value.Length > 0 ? Text.Code(urlTitleState.Value) : null)     
                   | (urlMetaState.Value.Length > 0 ? Text.Block("Site Meta Data:") : null)
                   | (urlMetaState.Value.Length > 0 ? Text.Code(urlMetaState.Value) : null)
                   | (urlLinksState.Value.Length > 0 ? Text.Block("External Links:") : null)
                   | (urlLinksState.Value.Length > 0 ? Text.Code(urlLinksState.Value) : null)
                 ;
    }
}