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
        var urlImagesState = UseState<string>("");
        var urlStructureState = UseState<string>("");
        var urlSocialState = UseState<string>("");
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
            catch (Exception) //invalid url
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

        var getImagesData = () =>
        {
            if (document == null)
                return string.Empty;
            string images = string.Empty;
            var imgTags = document.DocumentNode.SelectNodes("//img");
            if (imgTags != null)
            {
                foreach (var img in imgTags)
                {
                    var src = img.Attributes["src"]?.Value ?? "";
                    var alt = img.Attributes["alt"]?.Value ?? "";
                    var width = img.Attributes["width"]?.Value ?? "";
                    var height = img.Attributes["height"]?.Value ?? "";
                    
                    if (!string.IsNullOrEmpty(src))
                    {
                        images += $"{src}";
                        if (!string.IsNullOrEmpty(alt)) images += $" (Alt: {alt})";
                        if (!string.IsNullOrEmpty(width) || !string.IsNullOrEmpty(height)) 
                            images += $" [{width}x{height}]";
                        images += System.Environment.NewLine;
                    }
                }
            }
            return images;
        };

        var getStructureData = () =>
        {
            if (document == null)
                return string.Empty;
            string structure = string.Empty;
            
            // Headers
            for (int i = 1; i <= 6; i++)
            {
                var headers = document.DocumentNode.SelectNodes($"//h{i}");
                if (headers != null && headers.Count > 0)
                {
                    structure += $"H{i} Headers ({headers.Count}):" + System.Environment.NewLine;
                    foreach (var header in headers.Take(5)) // Show only first 5
                    {
                        var text = header.InnerText.Trim();
                        if (!string.IsNullOrEmpty(text))
                            structure += $"  • {text}" + System.Environment.NewLine;
                    }
                    if (headers.Count > 5) structure += $"  ... and {headers.Count - 5} more" + System.Environment.NewLine;
                }
            }
            
            // Paragraphs
            var paragraphs = document.DocumentNode.SelectNodes("//p");
            if (paragraphs != null && paragraphs.Count > 0)
            {
                structure += $"Paragraphs ({paragraphs.Count})" + System.Environment.NewLine;
            }
            
            // Lists
            var lists = document.DocumentNode.SelectNodes("//ul | //ol");
            if (lists != null && lists.Count > 0)
            {
                structure += $"Lists ({lists.Count})" + System.Environment.NewLine;
            }
            
            // Tables
            var tables = document.DocumentNode.SelectNodes("//table");
            if (tables != null && tables.Count > 0)
            {
                structure += $"Tables ({tables.Count})" + System.Environment.NewLine;
            }
            
            return structure;
        };

        var getSocialData = () =>
        {
            if (document == null)
                return string.Empty;
            string social = string.Empty;
            
            // Open Graph tags
            var ogTags = document.DocumentNode.SelectNodes("//meta[@property]");
            if (ogTags != null)
            {
                foreach (var tag in ogTags)
                {
                    var property = tag.Attributes["property"]?.Value ?? "";
                    var content = tag.Attributes["content"]?.Value ?? "";
                    if (property.StartsWith("og:") && !string.IsNullOrEmpty(content))
                    {
                        social += $"{property}: {content}" + System.Environment.NewLine;
                    }
                }
            }
            
            // Twitter Card tags
            var twitterTags = document.DocumentNode.SelectNodes("//meta[@name]");
            if (twitterTags != null)
            {
                foreach (var tag in twitterTags)
                {
                    var name = tag.Attributes["name"]?.Value ?? "";
                    var content = tag.Attributes["content"]?.Value ?? "";
                    if (name.StartsWith("twitter:") && !string.IsNullOrEmpty(content))
                    {
                        social += $"{name}: {content}" + System.Environment.NewLine;
                    }
                }
            }
            
            // Social media links
            var socialLinks = document.DocumentNode.SelectNodes("//a[@href]");
            if (socialLinks != null)
            {
                var socialDomains = new[] { "facebook.com", "twitter.com", "x.com", "linkedin.com", "instagram.com", "youtube.com", "tiktok.com" };
                foreach (var link in socialLinks)
                {
                    var href = link.Attributes["href"]?.Value ?? "";
                    var text = link.InnerText.Trim();
                    foreach (var domain in socialDomains)
                    {
                        if (href.Contains(domain))
                        {
                            social += $"{domain}: {href}";
                            if (!string.IsNullOrEmpty(text)) social += $" ({text})";
                            social += System.Environment.NewLine;
                            break;
                        }
                    }
                }
            }
            
            return social;
        };


        var eventHandler = (Event<Button> e) =>
        {
            urlTitleState.Set("");
            urlMetaState.Set("");
            urlLinksState.Set("");
            urlImagesState.Set("");
            urlStructureState.Set("");
            urlSocialState.Set("");
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
            urlImagesState.Set(getImagesData());
            urlStructureState.Set(getStructureData());
            urlSocialState.Set(getSocialData());
            parsingState.Set(false);
        };

        return Layout.Vertical().Gap(2).Padding(2)
                   | urlState.ToTextInput().WithLabel("Enter Site URL:")
                   | new Button("Parse Site HTML", eventHandler).Loading(parsingState.Value)
                   | (errorState.Value.Length > 0 ? Text.Block(errorState.Value) : null)
                   
                   // Basic information
                   | (urlTitleState.Value.Length > 0 ? Text.Block("Site Title:") : null)
                   | (urlTitleState.Value.Length > 0 ? Text.Code(urlTitleState.Value) : null)     
                   
                   // Meta data
                   | (urlMetaState.Value.Length > 0 ? Text.Block("Site Meta Data:") : null)
                   | (urlMetaState.Value.Length > 0 ? Text.Code(urlMetaState.Value) : null)
                   
                   // Images
                   | (urlImagesState.Value.Length > 0 ? Text.Block("Images Found:") : null)
                   | (urlImagesState.Value.Length > 0 ? Text.Code(urlImagesState.Value) : null)
                   
                   // Page structure
                   | (urlStructureState.Value.Length > 0 ? Text.Block("Page Structure:") : null)
                   | (urlStructureState.Value.Length > 0 ? Text.Code(urlStructureState.Value) : null)
                   
                   // Social media
                   | (urlSocialState.Value.Length > 0 ? Text.Block("Social Media & SEO:") : null)
                   | (urlSocialState.Value.Length > 0 ? Text.Code(urlSocialState.Value) : null)
                   
                   // External links
                   | (urlLinksState.Value.Length > 0 ? Text.Block("External Links:") : null)
                   | (urlLinksState.Value.Length > 0 ? Text.Code(urlLinksState.Value) : null)
                 ;
    }
}