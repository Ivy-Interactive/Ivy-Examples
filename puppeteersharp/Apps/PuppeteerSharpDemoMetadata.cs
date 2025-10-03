namespace PuppeteerSharpDemo.Apps
{
    [App(icon: Icons.Info)]
    public class PuppeteerSharpDemoMetadata : ViewBase
    {
        private IState<string> url = null!;
        private IState<string> title = null!;
        private IState<string> description = null!;
        private IState<string> status = null!;
        private IState<bool> isLoading = null!;

        public override object? Build()
        {
            url = this.UseState("");
            title = this.UseState("");
            description = this.UseState("");
            status = this.UseState("No metadata extracted yet.");
            isLoading = this.UseState(false);

            return new Card()
                .Title("Page Title & Metadata")
                .Description("Extract page title and meta description using PuppeteerSharp.")
            | Layout.Vertical(
                RenderUrlInput(),
                RenderExtractButton(),
                new Separator(),
                RenderStatus(),
                RenderMetadata()
            );
        }

        private IView RenderUrlInput() =>
            Layout.Vertical(
                Text.Muted("Website URL"),
                url.ToTextInput(placeholder: "https://example.com").Width(Size.Full())
            );

        private IWidget RenderExtractButton() =>
            new Button("Extract Metadata", async _ => await ExtractMetadata())
                .Icon(Icons.Info)
                .Variant(ButtonVariant.Primary)
                .Width(Size.Full());

        private IView RenderStatus() =>
            isLoading.Value
                ? Text.Muted("Extracting metadata...")
                : Text.Muted(status.Value);

        private IView RenderMetadata() =>
            Layout.Vertical(
                Text.Muted("Title:"),
                Text.Literal(title.Value).Width(Size.Full()),
                new Separator(),
                Text.Muted("Description:"),
                Text.Literal(description.Value).Width(Size.Full())
            );

        private async Task ExtractMetadata()
        {
            var target = (url.Value ?? "").Trim();
            if (string.IsNullOrEmpty(target))
            {
                status.Set("Please enter a valid URL.");
                return;
            }

            isLoading.Set(true);
            status.Set("");

            try
            {
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();
                await page.GoToAsync(target);

                // Extract title & description
                var pageTitle = await page.GetTitleAsync();
                var pageDesc = await page.EvaluateExpressionAsync<string>(
                    "document.querySelector('meta[name=\"description\"]')?.content || ''");

                title.Set(pageTitle);
                description.Set(pageDesc);
                status.Set("Metadata extracted successfully.");
            }
            catch (Exception ex)
            {
                status.Set($"Error: {ex.Message}");
                title.Set("");
                description.Set("");
            }
            finally
            {
                isLoading.Set(false);
            }
        }
    }
}
