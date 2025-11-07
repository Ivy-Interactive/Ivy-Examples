namespace PuppeteerSharpExample
{
    [App(icon: Icons.Image, title: "PuppeteerSharp")]
    public class PuppeteerSharpApp : ViewBase
    {
        private IState<string> url = null!;
        private IState<string?> screenshotPath = null!;
        private IState<string?> screenshotDownloadPath = null!;
        private IState<string?> pdfPath = null!;
        private IState<bool> isLoading = null!;

        public override object? Build()
        {
            // initialize states
            url = this.UseState("");
            screenshotPath = this.UseState<string?>();
            screenshotDownloadPath = this.UseState<string?>();
            pdfPath = this.UseState<string?>();
            isLoading = this.UseState(false);
            
            // Get client provider in Build method
            var client = this.UseService<IClientProvider>();

            return Layout.Horizontal().Gap(8).Padding(5)
                | new Card(
                    Layout.Vertical()
                    | Text.H2("Website Renderer")
                    | Text.Muted("Enter a URL and render the website using PuppeteerSharp.")
                    | RenderUrlInput()
                    | (Layout.Horizontal()
                        | RenderCaptureButton(client)
                        | (RenderDownloadButton(client) ?? null))
                    | new Spacer()
                    | Text.Small("This demo uses PuppeteerSharp library for rendering websites.")
                    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp)")
                   
                )

                | new Card(
                    Layout.Vertical()
                    | RenderScreenshotCard()
                ).Width(Size.Fit().Min(Size.Fraction(0.6f))).Height(Size.Fit().Min(Size.Full()));
        }

        private IView RenderUrlInput() =>
            Layout.Vertical(
                url.ToTextInput(placeholder: "https://example.com").Width(Size.Full())
            );

        private IWidget RenderCaptureButton(IClientProvider client)
        {
            return new Button("Render the website", async _ =>
            {
                await CaptureScreenshot(client);
            })
            .Icon(Icons.Camera)
            .Variant(ButtonVariant.Primary)
            .Width(Size.Full())
            .Loading(isLoading.Value);
        }

        private IWidget? RenderDownloadButton(IClientProvider client)
        {
            if (string.IsNullOrEmpty(screenshotDownloadPath.Value) && string.IsNullOrEmpty(pdfPath.Value))
                return null;

            return new Button("Save As", _ => { })
                .Icon(Icons.Download)
                .Variant(ButtonVariant.Secondary)
                .Width(Size.Full())
                .WithDropDown(
                    MenuItem.Default("Save as Screenshot")
                        .Icon(Icons.Image)
                        .HandleSelect(() =>
                        {
                            if (!string.IsNullOrEmpty(screenshotDownloadPath.Value))
                            {
                                client.OpenUrl(screenshotDownloadPath.Value);
                            }
                        })
                        .Disabled(string.IsNullOrEmpty(screenshotDownloadPath.Value)),
                    MenuItem.Default("Save as PDF")
                        .Icon(Icons.FileText)
                        .HandleSelect(() =>
                        {
                            if (!string.IsNullOrEmpty(pdfPath.Value))
                            {
                                client.OpenUrl(pdfPath.Value);
                            }
                        })
                        .Disabled(string.IsNullOrEmpty(pdfPath.Value))
                );
        }

        private IView RenderScreenshotCard()
        {
            if (string.IsNullOrEmpty(screenshotPath.Value))
            {
                return Layout.Vertical(
                    Text.H2("Preview of the website"),
                    Text.Muted("Enter a URL and click render to see the preview of the website here.")
                );
            }

            return Layout.Vertical(
                Text.H2("Preview of the website"),
                Text.Muted("Click the save as button to download the screenshot or PDF."),
                Layout.Center(
                    new Image(screenshotPath.Value)
                )
            );
        }

        // --- Action ---

        private async Task CaptureScreenshot(IClientProvider client)
        {
            var inputUrl = (url.Value ?? "").Trim();
            if (string.IsNullOrEmpty(inputUrl))
            {
                client.Toast("Please enter a valid URL", "Invalid URL");
                return;
            }

            // Basic URL validation
            if (!Uri.TryCreate(inputUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                client.Toast("Please enter a valid HTTP or HTTPS URL", "Invalid URL");
                return;
            }

            isLoading.Set(true);
            screenshotPath.Set((string?)null);
            screenshotDownloadPath.Set((string?)null);
            pdfPath.Set((string?)null);

            try
            {
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();
                await page.GoToAsync(inputUrl, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    Timeout = 30000
                });

                // Generate screenshot
                var screenshotFileName = $"screenshot-{Guid.NewGuid().ToString("N")}.png";
                var screenshotBytes = await page.ScreenshotDataAsync(new ScreenshotOptions { FullPage = true });
                var screenshotId = MemoryFileStore.Add(screenshotBytes, "image/png", screenshotFileName);
                // Use /view for preview (doesn't delete), /download for actual download
                var screenshotViewUrl = $"/view/{screenshotId}";
                var screenshotDownloadUrl = $"/download/{screenshotId}";
                screenshotPath.Set(screenshotViewUrl);
                screenshotDownloadPath.Set(screenshotDownloadUrl);

                // Generate PDF
                var pdfFileName = $"page-{Guid.NewGuid().ToString("N")}.pdf";
                var pdfBytes = await page.PdfDataAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true
                });
                var pdfId = MemoryFileStore.Add(pdfBytes, "application/pdf", pdfFileName);
                var pdfUrl = $"/download/{pdfId}";
                pdfPath.Set(pdfUrl);

                client.Toast("Website rendered successfully!", "Success");
            }
            catch (PuppeteerException ex)
            {
                Console.WriteLine("Puppeteer Error: " + ex.Message);
                var errorMessage = ex.Message.Contains("net::ERR") 
                    ? "Failed to load the website. Please check if the URL is correct and accessible." 
                    : $"Failed to render website: {ex.Message}";
                client.Toast(errorMessage, "Error");
                screenshotPath.Set((string?)null);
                screenshotDownloadPath.Set((string?)null);
                pdfPath.Set((string?)null);
            }
            catch (Exception ex)
            {
                // Fallback catch for any unexpected errors not covered by PuppeteerException
                client.Toast($"An error occurred: {ex.Message}", "Error");
                screenshotPath.Set((string?)null);
                screenshotDownloadPath.Set((string?)null);
                pdfPath.Set((string?)null);
            }
            finally
            {
                isLoading.Set(false);
            }
        }
    }
}

