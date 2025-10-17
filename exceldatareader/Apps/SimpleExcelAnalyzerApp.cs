using System.Data;
using System.IO;
using System.Collections.Generic;
using ExcelDataReader;

namespace ExcelDataReaderExample;

[App(icon: Icons.Sheet, title: "Simple Excel Analyzer")]
public class SimpleExcelAnalyzerApp : ViewBase
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
        var fileInputState = UseState<FileInput?>(() => null);

        // Upload URL for files
        var uploadUrl = this.UseUpload(
            uploadedBytes =>
            {
                try
                {
                    var tempPath = System.IO.Path.GetTempFileName();
                    var extension = ".xlsx"; // Default
                    
                    // Determine extension based on file content
                    if (uploadedBytes.Length >= 4)
                    {
                        if (uploadedBytes[0] == 0x50 && uploadedBytes[1] == 0x4B) // ZIP signature
                        {
                            extension = ".xlsx";
                        }
                        else if (uploadedBytes[0] == 0xD0 && uploadedBytes[1] == 0xCF) // OLE signature
                        {
                            extension = ".xls";
                        }
                        else
                        {
                            var content = System.Text.Encoding.UTF8.GetString(uploadedBytes.Take(100).ToArray());
                            if (content.Contains(',') && !content.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                            {
                                extension = ".csv";
                            }
                        }
                    }
                    
                    var finalPath = tempPath + extension;
                    File.WriteAllBytes(finalPath, uploadedBytes);
                    filePath.Set(finalPath);
                }
                catch (Exception ex)
                {
                    client.Toast($"File upload error: {ex.Message}", "Error");
                }
            },
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel,text/csv",
            "uploaded-file"
        );

        // File analysis on upload
        UseEffect(() =>
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
        }, filePath);

        return Layout.Vertical(
            // Header
            Layout.Horizontal(Text.H2("Simple Excel Analyzer")).Align(Align.Left),
            Text.Small("Analysis of Excel/CSV file structure using ExcelDataReader API"),

            // File upload
            Layout.Vertical(
                Text.Label("Upload file for analysis:"),
                fileInputState.ToFileInput(uploadUrl, "Select Excel/CSV file")
                    .Accept(".xlsx,.xls,.csv")
                    .Width(Size.Full())
            ).Padding(16),

            // Analysis indicator
            isAnalyzing.Value ? 
                Layout.Horizontal(
                    Text.Label("Analyzing file...")
                ).Align(Align.Left) : null,

            // Analysis results
            fileAnalysis.Value != null ? (
                Layout.Vertical(
                    // General file information
                    new Card(
                        Layout.Vertical(
                            Text.H3("General Information"),
                            Layout.Horizontal(
                                Layout.Vertical(
                                    Text.Label($"File name: {fileAnalysis.Value.FileName}"),
                                    Text.Label($"File type: {fileAnalysis.Value.FileType}"),
                                    Text.Label($"File size: {FormatFileSize(fileAnalysis.Value.FileSize)}"),
                                    Text.Label($"Number of sheets: {fileAnalysis.Value.TotalSheets}")
                                ),
                                Layout.Vertical(
                                    Text.Label($"Total rows: {fileAnalysis.Value.Sheets.Sum(s => s.RowCount)}"),
                                    Text.Label($"Maximum columns: {fileAnalysis.Value.Sheets.Max(s => s.FieldCount)}"),
                                    Text.Label($"Average rows per sheet: {fileAnalysis.Value.Sheets.Average(s => s.RowCount):F1}"),
                                    Text.Label($"Average columns: {fileAnalysis.Value.Sheets.Average(s => s.FieldCount):F1}")
                                )
                            )
                        )
                    ).Title("File Statistics"),

                    // Sheet list
                    Layout.Vertical(
                        Text.H3("Sheets in file:"),
                        Layout.Vertical(
                            fileAnalysis.Value.Sheets.Select((sheet, index) =>
                                new Card(
                                    Layout.Vertical(
                                        Layout.Horizontal(
                                            Layout.Vertical(
                                                Text.Label($"Name: {sheet.Name}"),
                                                Text.Label($"Code name: {sheet.CodeName}"),
                                                Text.Label($"Columns: {sheet.FieldCount}"),
                                                Text.Label($"Rows: {sheet.RowCount}")
                                            ),
                                            Layout.Vertical(
                                                Text.Label("Headers:"),
                                                Text.Small(string.Join(", ", sheet.Headers.Take(8))),
                                                sheet.Headers.Count > 8 ? 
                                                    Text.Small($"... and {sheet.Headers.Count - 8} more columns") : null
                                            )
                                        ),
                                        // Additional sheet information
                                        Layout.Vertical(
                                            Text.Label("Additional properties:"),
                                            Layout.Horizontal(
                                                sheet.Properties.Select(kvp => 
                                                    Text.Small($"{kvp.Key}: {kvp.Value}")
                                                ).ToArray()
                                            )
                                        )
                                    )
                                ).Title($"Sheet {index + 1}")
                            ).ToArray()
                        )
                    ),

                    // Export analysis button
                    Layout.Horizontal(
                        new Button("Export analysis to JSON", _ => {
                            try
                            {
                                var json = System.Text.Json.JsonSerializer.Serialize(fileAnalysis.Value, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                                var tempFile = System.IO.Path.GetTempFileName() + ".json";
                                File.WriteAllText(tempFile, json);
                                client.Toast($"Analysis saved to: {tempFile}", "Success");
                            }
                            catch (Exception ex)
                            {
                                client.Toast($"Export error: {ex.Message}", "Error");
                            }
                        }).Variant(ButtonVariant.Outline),
                        
                        new Button("New Analysis", _ => {
                            fileAnalysis.Value = null;
                            filePath.Value = null;
                            selectedSheetIndex.Set(0);
                        }).Variant(ButtonVariant.Secondary)
                    ).Align(Align.Left)
                )
            ) : (
                filePath.Value != null ? 
                    Text.Label("File uploaded, analyzing...") : 
                    Text.Label("Upload file to start analysis")
            )
        );
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
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // Collect information about all sheets
            do
            {
                var sheetAnalysis = new SheetAnalysis
                {
                    Name = reader.Name,
                    CodeName = reader.CodeName,
                    FieldCount = reader.FieldCount,
                    RowCount = reader.RowCount
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
                    }
                }
                catch { }

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
