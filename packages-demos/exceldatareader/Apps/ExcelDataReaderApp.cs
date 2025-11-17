using System.Data;
using System.IO;
using System.Collections.Generic;
using ExcelDataReader;

namespace ExcelDataReaderExample;

[App(icon: Icons.Sheet, title: "ExcelDataReader")]
public class ExcelDataReaderApp : ViewBase
{
    // Model for storing file analysis
    public record FileAnalysis
    {
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public long FileSize { get; set; }
        public int TotalSheets { get; set; }
        public List<SheetAnalysis> Sheets { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public record SheetAnalysis
    {
        public string Name { get; set; } = "";
        public string CodeName { get; set; } = "";
        public int FieldCount { get; set; }
        public int RowCount { get; set; }
        public int MergeCellsCount { get; set; }
        public List<string> Headers { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public override object? Build()
    {
        var filePath = UseState<string?>(() => null);
        var fileAnalysis = UseState<FileAnalysis?>(() => null);
        var isAnalyzing = UseState(false);
        var selectedSheetIndex = UseState(0);
        var client = UseService<IClientProvider>();
        
        // Upload state for files
        var uploadState = UseState<FileUpload<byte[]>?>();
        var uploadContext = this.UseUpload(MemoryStreamUploadHandler.Create(uploadState))
            .Accept(".xlsx,.xls,.csv")
            .MaxFileSize(50 * 1024 * 1024);
        
        var fileName = uploadState.Value?.FileName;

        // When a file is uploaded, save it to temp file
        UseEffect(() =>
        {
            if (uploadState.Value?.Content is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    var tempPath = System.IO.Path.GetTempFileName();
                    var extension = ".xlsx"; // Default

                    // Simple extension detection
                    if (bytes.Length >= 4)
                    {
                        if (bytes[0] == 0x50 && bytes[1] == 0x4B)
                        {
                            extension = ".xlsx";
                        }
                        else if (bytes[0] == 0xD0 && bytes[1] == 0xCF)
                        {
                            extension = ".xls";
                        }
                        else
                        {
                            var content = System.Text.Encoding.UTF8.GetString(bytes.Take(100).ToArray());
                            if (content.Contains(','))
                            {
                                extension = ".csv";
                            }
                        }
                    }

                    var finalPath = tempPath + extension;
                    File.WriteAllBytes(finalPath, bytes);
                    filePath.Set(finalPath);
                }
                catch (Exception ex)
                {
                    client.Toast($"File upload error: {ex.Message}", "Error");
                }
            }
        }, [uploadState]);

        // Clear analysis when file is removed
        UseEffect(() =>
        {
            if (filePath.Value == null)
            {
                fileAnalysis.Set((FileAnalysis?)null);
                uploadState.Reset();
            }
        }, filePath);

        // Manual analysis trigger
        var startAnalysis = () =>
        {
            if (filePath.Value != null && !isAnalyzing.Value)
            {
                isAnalyzing.Set(true);
                Task.Run(() =>
                {
                    try
                    {
                        var result = AnalyzeFile(filePath.Value);
                        fileAnalysis.Set(result);
                        client.Toast($"File analyzed successfully! Found {result.TotalSheets} sheets.", "Success");
                    }
                    catch (Exception ex)
                    {
                        client.Toast($"Analysis error: {ex.Message}", "Error");
                        Console.WriteLine($"Analysis Error: {ex.Message}");
                    }
                    finally
                    {
                        isAnalyzing.Set(false);
                    }
                });
            }
        };

        return Layout.Horizontal(
            // Left Card - Functionality and File Input
            new Card(
                Layout.Vertical(
                    Text.H3("Excel File Analyzer"),
                    Text.Muted("Upload Excel (.xlsx, .xls) or CSV files to analyze their structure, sheets, and data organization."),
                    uploadState.ToFileInput(uploadContext)
                        .Placeholder("Select Excel/CSV file")
                        .Width(Size.Full()),

                    // Action buttons
                    Layout.Horizontal(
                        new Button("Analyze", _ => startAnalysis())
                            .Disabled(filePath.Value == null || isAnalyzing.Value),

                        new Button("Clear", _ =>
                        {
                            filePath.Set((string?)null);
                            selectedSheetIndex.Set(0);
                            uploadState.Reset();
                        })
                        .Destructive()
                        .Disabled(filePath.Value == null || isAnalyzing.Value)
                    ).Align(Align.Left),

                    new Spacer(),
                    Text.Small("This demo uses the ExcelDataReader NuGet package to read Excel and CSV files."),
                    Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [ExcelDataReader](https://github.com/ExcelDataReader/ExcelDataReader)"),

                    // Analysis indicator
                    isAnalyzing.Value ?
                        Layout.Horizontal(
                            Text.Label("Analyzing file...")
                        ).Align(Align.Left) : null
                )
            ).Width(Size.Fraction(0.4f)).Height(Size.Fit().Min(Size.Full())),

            // Right Card - Analysis Results
            new Card(
                fileAnalysis.Value != null ? (
                    Layout.Vertical(
                        Text.H3("File Analysis Results"),
                        Text.Muted("Detailed analysis of your Excel/CSV file structure, including sheets, headers, and statistics."),
                        Layout.Vertical(
                            new Markdown($"""                                
                                | Property | Value |
                                |----------|-------|
                                | **File name** | `{fileName}` |
                                | **File type** | `{fileAnalysis.Value.FileType}` |
                                | **File size** | `{FormatFileSize(fileAnalysis.Value.FileSize)}` |
                                | **Number of sheets** | `{fileAnalysis.Value.TotalSheets}` |
                                | **Total rows** | `{fileAnalysis.Value.Sheets.Sum(s => s.RowCount)}` |
                                | **Maximum columns** | `{fileAnalysis.Value.Sheets.Max(s => s.FieldCount)}` |
                                | **Total merged cells** | `{fileAnalysis.Value.Sheets.Sum(s => s.MergeCellsCount)}` |
                                | **Average rows per sheet** | `{fileAnalysis.Value.Sheets.Average(s => s.RowCount):F1}` |
                                | **Average columns** | `{fileAnalysis.Value.Sheets.Average(s => s.FieldCount):F1}` |
                                """)
                        ),

                        // Sheet list
                        Layout.Vertical(
                            fileAnalysis.Value.Sheets.Select((sheet, index) =>

                                    new Expandable(
                                        Text.Label($"Sheet {index + 1}: {sheet.Name}"),
                                        new Markdown($"""
                                            | Property | Value |
                                            |----------|-------|
                                            | **Name** | `{sheet.Name}` |
                                            | **Columns** | `{sheet.FieldCount}` |
                                            | **Rows** | `{sheet.RowCount}` |
                                            | **Merged Cells** | `{sheet.MergeCellsCount}` |
                                            | **Headers** | {string.Join(", ", sheet.Headers.Select(header => $"`{header}`"))} |
                                            
                                            """)
                                    )

                            ).ToArray()
                        )
                    )
                ) : (
                        Layout.Vertical(
                            Text.H3("What This Program Does"),
                            Layout.Vertical(
                                Text.Muted("• Analyzes Excel and CSV file structure"),
                                Text.Muted("• Shows sheet information and headers"),
                                Text.Muted("• Displays file statistics and metadata"),
                                Text.Muted("• Provides detailed data organization insights")
                            ).Padding(8),

                            Layout.Vertical(
                                Text.Muted("Quick Start:"),
                                Text.Muted("1. Upload file → 2. Click 'Analyze' → 3. View results")
                            ).Padding(8),

                            Layout.Vertical(
                                Text.Muted("Supported Files:"),
                                Text.Muted("• Excel files (.xlsx, .xls)"),
                                Text.Muted("• CSV files (.csv)")
                            ).Padding(8)
                        )
                )
            ).Width(Size.Fraction(0.6f)).Height(Size.Fit().Min(Size.Full()))
        ).Gap(16);
    }

    /// <summary>
    /// Analyzes file and returns detailed information about its structure
    /// </summary>
    private FileAnalysis AnalyzeFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var analysis = new FileAnalysis
        {
            FileName = fileInfo.Name,
            FileType = fileInfo.Extension.ToUpperInvariant(),
            FileSize = fileInfo.Length
        };

        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = fileInfo.Extension.ToLowerInvariant() == ".csv" 
                ? ExcelReaderFactory.CreateCsvReader(stream)
                : ExcelReaderFactory.CreateReader(stream);
            
            if (reader == null)
            {
                throw new InvalidOperationException("Failed to create reader for the file");
            }

            // Collect information about all sheets
            do
            {
                var sheetAnalysis = new SheetAnalysis
                {
                    Name = reader.Name ?? "Unknown",
                    CodeName = reader.CodeName ?? "",
                    FieldCount = reader.FieldCount,
                    RowCount = reader.RowCount,
                    MergeCellsCount = reader.MergeCells?.Length ?? 0
                };

                // Collect headers (first row)
                if (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var header = reader.GetValue(i)?.ToString() ?? $"Column_{i}";
                        sheetAnalysis.Headers.Add(header);
                    }
                }

                // Collect additional sheet information
                sheetAnalysis.Properties["ResultsCount"] = reader.ResultsCount;
                sheetAnalysis.Properties["FieldCount"] = reader.FieldCount;
                sheetAnalysis.Properties["RowCount"] = reader.RowCount;
                sheetAnalysis.Properties["MergeCellsCount"] = reader.MergeCells?.Length ?? 0;

                // Try to get additional properties
                try
                {
                    if (reader.HeaderFooter != null)
                    {
                        sheetAnalysis.Properties["HasHeaderFooter"] = true;
                    }
                }
                catch { }

                try
                {
                    if (reader.MergeCells != null && reader.MergeCells.Length > 0)
                    {
                        sheetAnalysis.Properties["MergeCellsCount"] = reader.MergeCells.Length;
                        sheetAnalysis.MergeCellsCount = reader.MergeCells.Length;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error processing merge cells: {ex.Message}");
                }
                
                // For CSV files, set MergeCellsCount to 0 since CSV doesn't support merged cells
                if (fileInfo.Extension.ToLowerInvariant() == ".csv")
                {
                    sheetAnalysis.MergeCellsCount = 0;
                    sheetAnalysis.Properties["MergeCellsCount"] = 0;
                    sheetAnalysis.Properties["IsCSV"] = true;
                }

                analysis.Sheets.Add(sheetAnalysis);

                // Move to next sheet
            } while (reader.NextResult());

            analysis.TotalSheets = analysis.Sheets.Count;
            analysis.Metadata["AnalysisDate"] = DateTime.Now;
            analysis.Metadata["ReaderVersion"] = "ExcelDataReader";

            return analysis;
        }
        catch (Exception ex)
        {
            throw new Exception($"File analysis error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Formats file size in readable format
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

