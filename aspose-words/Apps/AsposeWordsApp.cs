using Aspose.Words;
using Aspose.Words.Drawing;
using Aspose.Words.Tables;

namespace AsposeWordsExample.Apps;

[App(icon: Icons.FileText, title: "Generate New Documents")]
public class CreateNewDocumentApp : ViewBase
{
    public record DocumentTemplate(string Name, string Description, Func<Document> Generator);

    public override object? Build()
    {
        var templates = new[]
        {
            new DocumentTemplate(
                "Simple Letter",
                "A basic business letter template",
                () => GenerateSimpleLetter()
            ),
            new DocumentTemplate(
                "Table Report",
                "Document with formatted tables and data",
                () => GenerateTableReport()
            ),
            new DocumentTemplate(
                "Invoice",
                "Professional invoice template",
                () => GenerateInvoice()
            ),
            new DocumentTemplate(
                "Form Letter",
                "Mail merge style form letter",
                () => GenerateFormLetter()
            )
        };

        // State for each template
        var generatedDocuments = UseState<Dictionary<string, Document>>(new Dictionary<string, Document>());
        var isGenerating = UseState<string?>("");
        var errorMessages = UseState<Dictionary<string, string>>(new Dictionary<string, string>());

        return new StackLayout([
            Text.H2("Aspose.Words for .NET Demo"),
            Text.Block("Generate and download Word documents using Aspose.Words."),
        
            Layout.Grid().Columns(2).Gap(3)
            | templates.Select(template =>
            {
                var isTemplateGenerated = generatedDocuments.Value.ContainsKey(template.Name);
                var isCurrentlyGenerating = isGenerating.Value == template.Name;
                
                // Create download functionality for each template
                var downloadUrl = this.UseDownload(
                    () =>
                    {
                        try
                        {
                            Document doc;
                            if (isTemplateGenerated)
                            {
                                // Use the stored document
                                doc = generatedDocuments.Value[template.Name];
                            }
                            else
                            {
                                doc = template.Generator();
                            }
                            
                            using var stream = new MemoryStream();
                            doc.Save(stream, SaveFormat.Docx);
                            return stream.ToArray();
                        }
                        catch (Exception ex)
                        {
                            var errorDoc = new Document();
                            var errorBuilder = new DocumentBuilder(errorDoc);
                            errorBuilder.Writeln($"Error generating {template.Name}:");
                            errorBuilder.Writeln(ex.Message);
                            
                            using var errorStream = new MemoryStream();
                            errorDoc.Save(errorStream, SaveFormat.Docx);
                            return errorStream.ToArray();
                        }
                    },
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    $"{template.Name.Replace(" ", "-").ToLower()}-{DateTime.Now:yyyy-MM-dd-HHmmss}.docx"
                );

                // Build card content dynamically
                var cardContent = new List<object>
                {
                    Text.H4(template.Name),
                    Text.Muted(template.Description)
                };
                
                // Step 1: Generate button (if not generated)
                if (!isTemplateGenerated && !isCurrentlyGenerating)
                {
                    cardContent.Add(new Button("Generate", _ => {
                        isGenerating.Set(template.Name);
                        
                        // Clear previous error
                        var clearedErrors = new Dictionary<string, string>(errorMessages.Value);
                        clearedErrors.Remove(template.Name);
                        errorMessages.Set(clearedErrors);
                        
                        try
                        {
                            var doc = template.Generator();
                            var updatedDocs = new Dictionary<string, Document>(generatedDocuments.Value)
                            {
                                [template.Name] = doc
                            };
                            generatedDocuments.Set(updatedDocs);
                        }
                        catch (Exception ex)
                        {
                            var updatedErrors = new Dictionary<string, string>(errorMessages.Value)
                            {
                                [template.Name] = $"Failed to generate document: {ex.Message}"
                            };
                            errorMessages.Set(updatedErrors);
                        }
                        finally
                        {
                            isGenerating.Set("");
                        }
                    })
                    .Primary()
                    .Icon(Icons.Play));
                }
                
                // Loading state
                if (isCurrentlyGenerating)
                {
                    cardContent.Add(new StackLayout([
                        Text.Muted("Generating...")
                    ], Ivy.Shared.Orientation.Horizontal, align: Align.Center, gap: 2));
                }
                
                // Error message
                if (errorMessages.Value.ContainsKey(template.Name))
                {
                    cardContent.Add(Text.Danger(errorMessages.Value[template.Name]));
                }
                
                // Step 2: Download button (if generated)
                if (isTemplateGenerated && !isCurrentlyGenerating)
                {
                    cardContent.Add(new Button("Download DOCX")
                        .Primary()
                        .Icon(Icons.Download)
                        .Url(downloadUrl.Value));
                }

                return new Card(new StackLayout(cardContent.ToArray(), gap: 2));
            }),
            new Spacer(),
            Text.Small("This demo uses Aspose.Words for .NET to create, manipulate, and export Word documents."),
            Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Aspose.Words for .NET](https://products.aspose.com/words/net/)")
        ], gap: 4);
    }

    private static Document GenerateSimpleLetter()
    {
        var doc = new Document();
        var builder = new DocumentBuilder(doc);

        // Add header
        builder.MoveToHeaderFooter(HeaderFooterType.HeaderPrimary);
        builder.Font.Size = 16;
        builder.Font.Bold = true;
        builder.Writeln("ACME Corporation");
        builder.Font.Size = 12;
        builder.Font.Bold = false;
        builder.Writeln("123 Business Street, City, State 12345");
        builder.Writeln("Phone: (555) 123-4567 | Email: info@acme.com");

        // Move to main document
        builder.MoveToDocumentEnd();
        builder.Writeln();
        builder.Writeln();

        // Date
        builder.Write("Date: ");
        builder.Writeln(DateTime.Now.ToString("MMMM dd, yyyy"));
        builder.Writeln();

        // Address
        builder.Writeln("John Smith");
        builder.Writeln("456 Customer Avenue");
        builder.Writeln("Customer City, State 67890");
        builder.Writeln();

        // Subject
        builder.Font.Bold = true;
        builder.Writeln("Re: Business Proposal");
        builder.Font.Bold = false;
        builder.Writeln();

        // Body
        builder.Writeln("Dear Mr. Smith,");
        builder.Writeln();
        builder.Writeln("Thank you for your interest in our services. We are pleased to present this business proposal for your consideration.");
        builder.Writeln();
        builder.Writeln("Our company has been providing exceptional solutions to businesses like yours for over 15 years. We believe our expertise and commitment to quality make us the ideal partner for your upcoming project.");
        builder.Writeln();
        builder.Writeln("Please find attached our detailed proposal outlining the scope of work, timeline, and investment required.");
        builder.Writeln();
        builder.Writeln("We look forward to the opportunity to work with you.");
        builder.Writeln();
        builder.Writeln("Sincerely,");
        builder.Writeln();
        builder.Writeln("Jane Doe");
        builder.Writeln("Business Development Manager");

        return doc;
    }

    private static Document GenerateTableReport()
    {
        var doc = new Document();
        var builder = new DocumentBuilder(doc);

        // Title
        builder.Font.Size = 18;
        builder.Font.Bold = true;
        builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;
        builder.Writeln("Quarterly Sales Report");
        builder.Font.Size = 12;
        builder.Font.Bold = false;
        builder.ParagraphFormat.Alignment = ParagraphAlignment.Left;
        builder.Writeln();

        // Introduction
        builder.Writeln("This report provides an overview of our sales performance for Q3 2024.");
        builder.Writeln();

        // Create table
        var table = builder.StartTable();

        // Header row
        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightBlue;
        builder.Font.Bold = true;
        builder.Write("Product");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightBlue;
        builder.Write("Q1 Sales");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightBlue;
        builder.Write("Q2 Sales");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightBlue;
        builder.Write("Q3 Sales");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightBlue;
        builder.Write("Growth %");
        builder.EndRow();

        // Data rows
        var products = new[]
        {
            ("Widget A", 15000, 18000, 22000),
            ("Widget B", 12000, 14000, 16500),
            ("Widget C", 8000, 9500, 11200),
            ("Widget D", 25000, 28000, 31500)
        };

        builder.Font.Bold = false;
        foreach (var (product, q1, q2, q3) in products)
        {
            var growth = Math.Round(((double)(q3 - q2) / q2) * 100, 1);

            builder.InsertCell();
            builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.White;
            builder.Write(product);

            builder.InsertCell();
            builder.Write($"${q1:N0}");

            builder.InsertCell();
            builder.Write($"${q2:N0}");

            builder.InsertCell();
            builder.Write($"${q3:N0}");

            builder.InsertCell();
            builder.Write($"{growth:+0.0;-0.0;0}%");
            builder.EndRow();
        }

        builder.EndTable();
        builder.Writeln();

        // Summary
        builder.Writeln("Summary:");
        builder.Writeln("• All products showed positive growth in Q3");
        builder.Writeln("• Widget D continues to be our top performer");
        builder.Writeln("• Overall revenue increased by 15% compared to Q2");

        return doc;
    }

    private static Document GenerateInvoice()
    {
        var doc = new Document();
        var builder = new DocumentBuilder(doc);

        // Invoice header
        builder.Font.Size = 24;
        builder.Font.Bold = true;
        builder.Font.Color = System.Drawing.Color.DarkBlue;
        builder.Writeln("INVOICE");
        builder.Font.Size = 12;
        builder.Font.Bold = false;
        builder.Font.Color = System.Drawing.Color.Black;
        builder.Writeln();

        // Company info
        builder.Font.Bold = true;
        builder.Writeln("ACME Services Ltd.");
        builder.Font.Bold = false;
        builder.Writeln("789 Service Road");
        builder.Writeln("Business City, BC 12345");
        builder.Writeln("Phone: (555) 987-6543");
        builder.Writeln("Email: billing@acme-services.com");
        builder.Writeln();

        // Invoice details
        builder.Font.Bold = true;
        builder.Write("Invoice #: ");
        builder.Font.Bold = false;
        builder.Writeln("INV-2024-001");
        builder.Font.Bold = true;
        builder.Write("Date: ");
        builder.Font.Bold = false;
        builder.Writeln(DateTime.Now.ToString("MM/dd/yyyy"));
        builder.Font.Bold = true;
        builder.Write("Due Date: ");
        builder.Font.Bold = false;
        builder.Writeln(DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"));

        builder.Writeln();
        builder.Writeln();

        // Bill to
        builder.Font.Bold = true;
        builder.Writeln("Bill To:");
        builder.Font.Bold = false;
        builder.Writeln("ABC Corporation");
        builder.Writeln("456 Client Street");
        builder.Writeln("Client City, CC 67890");
        builder.Writeln();

        // Services table
        var table = builder.StartTable();

        // Header row - must be added before setting table properties
        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightGray;
        builder.Font.Bold = true;
        builder.Write("Description");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightGray;
        builder.Write("Quantity");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightGray;
        builder.Write("Rate");

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightGray;
        builder.Write("Amount");
        builder.EndRow();

        // Set table width after adding the first row
        table.PreferredWidth = PreferredWidth.FromPercent(100);

        // Line items
        var items = new[]
        {
            ("Website Development", 1, 2500.00m),
            ("Logo Design", 1, 500.00m),
            ("SEO Optimization", 3, 200.00m),
            ("Content Writing", 5, 150.00m)
        };

        decimal total = 0;
        builder.Font.Bold = false;
        foreach (var (description, qty, rate) in items)
        {
            var amount = qty * rate;
            total += amount;

            builder.InsertCell();
            builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.White;
            builder.Write(description);

            builder.InsertCell();
            builder.Write(qty.ToString());

            builder.InsertCell();
            builder.Write($"${rate:F2}");

            builder.InsertCell();
            builder.Write($"${amount:F2}");
            builder.EndRow();
        }

        // Totals
        var tax = total * 0.08m;
        var grandTotal = total + tax;

        builder.InsertCell();
        builder.CellFormat.Shading.BackgroundPatternColor = System.Drawing.Color.LightGray;
        builder.Font.Bold = true;
        builder.Write("Subtotal");
        builder.InsertCell();
        builder.Write("");
        builder.InsertCell();
        builder.Write("");
        builder.InsertCell();
        builder.Write($"${total:F2}");
        builder.EndRow();

        builder.InsertCell();
        builder.Write("Tax (8%)");
        builder.InsertCell();
        builder.Write("");
        builder.InsertCell();
        builder.Write("");
        builder.InsertCell();
        builder.Write($"${tax:F2}");
        builder.EndRow();

        builder.InsertCell();
        builder.Write("TOTAL");
        builder.InsertCell();
        builder.Write("");
        builder.InsertCell();
        builder.Write("");
        builder.InsertCell();
        builder.Write($"${grandTotal:F2}");
        builder.EndRow();

        builder.EndTable();
        builder.Writeln();

        // Payment terms
        builder.Writeln("Payment Terms: Net 30 days");
        builder.Writeln("Thank you for your business!");

        return doc;
    }

    private static Document GenerateFormLetter()
    {
        var doc = new Document();
        var builder = new DocumentBuilder(doc);

        // Header
        builder.Font.Size = 14;
        builder.Font.Bold = true;
        builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;
        builder.Writeln("Customer Appreciation Letter");
        builder.Font.Size = 12;
        builder.Font.Bold = false;
        builder.ParagraphFormat.Alignment = ParagraphAlignment.Left;
        builder.Writeln();
        builder.Writeln();

        // Date
        builder.Writeln($"Date: {DateTime.Now:MMMM dd, yyyy}");
        builder.Writeln();

        // Customer placeholder (in real mail merge, these would be merge fields)
        builder.Writeln("[Customer Name]");
        builder.Writeln("[Customer Address]");
        builder.Writeln("[City, State ZIP]");
        builder.Writeln();

        // Salutation
        builder.Writeln("Dear [Customer Name],");
        builder.Writeln();

        // Body with merge field placeholders
        builder.Writeln("Thank you for being a valued customer of our company for the past [Years as Customer] years. Your loyalty and trust in our services mean everything to us.");
        builder.Writeln();

        builder.Writeln("As a token of our appreciation, we are pleased to offer you a special [Discount Percentage]% discount on your next purchase of [Product Category]. This exclusive offer is valid until [Expiry Date].");
        builder.Writeln();

        builder.Writeln("We have noticed that you particularly enjoy our [Favorite Product] products, so we wanted to let you know about our new [New Product] that we think you'll love.");
        builder.Writeln();

        builder.Writeln("To redeem this offer, simply visit our store or website and use the promo code: [Promo Code]");
        builder.Writeln();

        builder.Font.Bold = true;
        builder.Writeln("Special Benefits for You:");
        builder.Font.Bold = false;
        builder.Writeln("• Priority customer service");
        builder.Writeln("• Early access to new products");
        builder.Writeln("• Exclusive member-only discounts");
        builder.Writeln("• Free shipping on orders over $[Free Shipping Threshold]");
        builder.Writeln();

        builder.Writeln("Thank you again for your continued business. We look forward to serving you for many years to come.");
        builder.Writeln();

        builder.Writeln("Warmest regards,");
        builder.Writeln();
        builder.Writeln("The Customer Service Team");
        builder.Writeln("ACME Corporation");

        // Footer with merge field note
        builder.Writeln();
        builder.Font.Size = 10;
        builder.Font.Italic = true;
        builder.Writeln("Note: In a real mail merge scenario, the bracketed placeholders would be replaced with actual customer data from a database or data source.");

        return doc;
    }
}