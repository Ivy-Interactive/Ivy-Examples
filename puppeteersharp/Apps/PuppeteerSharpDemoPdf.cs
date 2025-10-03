using PuppeteerSharp.Media;

namespace PuppeteerSharpDemo.Apps
{
    [App(icon: Icons.FileText)]
    public class PuppeteerSharpDemoPdf : ViewBase
    {
        private IState<string> url = null!;
        private IState<string?> pdfPath = null!;
        private IState<bool> isLoading = null!;

        public override object? Build()
        {
            url = this.UseState("");
            pdfPath = this.UseState("");
            isLoading = this.UseState(false);

            return new Card()
                .Title("Website PDF Generator")
                .Description("Generate a PDF from a website using PuppeteerSharp.")
            | Layout.Vertical(
                RenderUrlInput(),
                RenderGenerateButton(),
                RenderStatus()
            );
        }

        private IView RenderUrlInput() =>
            Layout.Vertical(
                Text.Muted("Website URL"),
                url.ToTextInput(placeholder: "https://example.com").Width(Size.Full())
            );

        private IWidget RenderGenerateButton(){
            var client = this.UseService<IClientProvider>();

            return new Button("Generate PDF", async _ =>
            {
                await GeneratePdf();
                client.OpenUrl(pdfPath.Value);
            })
            .Icon(Icons.FileText)
            .Variant(ButtonVariant.Primary)
            .Width(Size.Full());
        }

        private IView RenderStatus() =>
            isLoading.Value
                ? Text.Muted("Generating PDF...")
                : Text.Muted("");

        private async Task GeneratePdf()
        {
            var target = (url.Value ?? "").Trim();
            if (string.IsNullOrEmpty(target))
            {
                pdfPath.Set("Please enter a valid URL.");
                return;
            }

            isLoading.Set(true);
            pdfPath.Set(string.Empty);

            try
            {
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();
                await page.GoToAsync(target);

                var fileName = $"page-{Guid.NewGuid().ToString("N")}.pdf";

                var bytes = await page.PdfDataAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true
                });

                var id = MemoryFileStore.Add(bytes, "application/pdf", fileName);

                // URL for browser download
                var downloadUrl = $"/download/{id}";
                pdfPath.Set(downloadUrl);
            }
            catch (Exception ex)
            {
                pdfPath.Set($"Error: {ex.Message}");
            }
            finally
            {
                isLoading.Set(false);
            }
        }
    }
}
