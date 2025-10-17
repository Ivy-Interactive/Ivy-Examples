
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Dynamic;
namespace ExcelDataReaderExample;

[App(icon: Icons.Sheet, title: "Bad ExcelDataReader")]
public class ExcelDataReaderApp : ViewBase
{
    public record User
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? Department { get; set; }
        public string? Level { get; set; }
    }

    // Universal model for dynamic data
    public class DynamicDataRow
    {
        public Dictionary<string, object> Data { get; set; } = new();
        public List<string> Columns { get; set; } = new();
    }


    /// <summary>
    /// Applies pagination to a list of users and returns the paginated results along with total page count
    /// </summary>
    /// <param name="page">The current page number (1-based index)</param>
    /// <param name="totalItem">Number of items to display per page</param>
    /// <param name="users">The complete list of users to paginate</param>
    /// <returns>
    /// A tuple containing:
    /// - int: Total number of pages available
    /// - List<User>: The subset of users for the requested page
    /// </returns>
    /// <example>
    /// <code>
    /// var (totalPages, currentPageUsers) = PaginationValue(2, 10, allUsers);
    /// // Returns page 2 with 10 users per page
    /// </code>
    /// </example>
    private (int, List<User>) PaginationValue(int page, int totalItem, List<User> users)
    {
        if (users == null || users.Count == 0)
            return (0, new List<User>());
        var totalPage = (int)Math.Ceiling((double)users.Count / totalItem);
        // Ensure page is within valid bounds
        page = (page >= 1) ? page : 1;
        page = (page <= totalPage) ? page : totalPage;
        // Return the total pages and the subset of users for the requested page
        return (totalPage, users.Skip((page - 1) * totalItem).Take(totalItem).ToList());
    }

    /// <summary>
    /// Applies pagination to dynamic data rows
    /// </summary>
    private (int, List<DynamicDataRow>) PaginationDynamicValue(int page, int totalItem, List<DynamicDataRow> data)
    {
        if (data == null || data.Count == 0)
            return (0, new List<DynamicDataRow>());
        var totalPage = (int)Math.Ceiling((double)data.Count / totalItem);
        page = (page >= 1) ? page : 1;
        page = (page <= totalPage) ? page : totalPage;
        return (totalPage, data.Skip((page - 1) * totalItem).Take(totalItem).ToList());
    }

    public override object? Build()
    {
        // Set Source file url 
        var FilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Manpower.csv");
        var filePath = UseState(() =>
        {
            if (!File.Exists(FilePath))
            {
                File.WriteAllLines(FilePath, new List<string>
               {
                "ID,Name,Email,Phone Number,Address,Gender,Department,Level",
                "EMP384729,James Smith,j.smith@factory.com,348572918,1234 Industrial Ave,Male,Production,Director",
                "EMP529183,Maria Garcia,maria.garcia@factory.com,592837461,5678 Factory St,Female,Quality Control,Manager",
                "EMP672819,Robert Johnson,r.johnson@factory.com,672819345,9012 Manufacturing Dr,Male,Warehouse,Team Leader",
               });
                return FilePath;
            }
            return FilePath;
        });
        
        // Dynamic data for adaptive table
        var dynamicData = UseState(() => new List<DynamicDataRow>());
        var displayDynamicData = UseState(() => new List<DynamicDataRow>());
        var columns = UseState(() => new List<string>());
        
        // Legacy static data for backward compatibility
        var users = UseState(() => new List<User>());
        var displayUsers = UseState(() => new List<User>());
        
        var isImport = UseState(false);
        var isDelete = UseState(false);
        var page = UseState(1);
        var totalPage = UseState(0);
        var useDynamicMode = UseState(true); // Dynamic data mode by default
        var fileInputState = UseState<FileInput?>(() => null);
        var client = UseService<IClientProvider>();

        // Upload URL for files
        var uploadUrl = this.UseUpload(
            uploadedBytes =>
            {
                try
                {
                    // Save file temporarily and set path
                    var tempPath = System.IO.Path.GetTempFileName();
                    File.WriteAllBytes(tempPath, uploadedBytes);
                    filePath.Set(tempPath);
                    isImport.Set(true);
                }
                catch (Exception ex)
                {
                    client.Toast($"File upload error: {ex.Message}", "Error");
                }
            },
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel,text/csv",
            "uploaded-file"
        );

        // re-render when users, totalPager, or page change value
        UseEffect(() =>
        {
            if (useDynamicMode.Value)
            {
                (totalPage.Value, displayDynamicData.Value) = PaginationDynamicValue(page.Value, 20, dynamicData.Value);
            }
            else
            {
                (totalPage.Value, displayUsers.Value) = PaginationValue(page.Value, 20, users.Value);
            }
        }, users, dynamicData, totalPage, page, useDynamicMode);

        // Load data from file and save them to state variables by click "Import" button or changed filePath link
        UseEffect(() =>
        {
            if (isImport.Value)
            {
                try
                {
                    Console.WriteLine("Importing file: " + filePath.Value);
                    var fileExtension = System.IO.Path.GetExtension(filePath.Value);
                    Console.WriteLine("File extension: " + fileExtension);
                    
                    // Detect file type by content, not just extension
                    var isCsvFile = IsCsvFile(filePath.Value);
                    Console.WriteLine("Detected as CSV: " + isCsvFile);
                    
                    DataSet result;
                    if (isCsvFile)
                    {
                        Console.WriteLine("Processing CSV file with custom parser");
                        result = ParseCsvFile(filePath.Value);
                    }
                    else
                    {
                        Console.WriteLine("Creating Excel reader");
                        using var stream = new FileStream(filePath.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var reader = ExcelReaderFactory.CreateReader(stream);
                        result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });
                    }
                    
                    if (result != null && result.Tables.Count > 0)
                    {
                        var table = result.Tables[0];
                        var columnNames = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                        Console.WriteLine($"Found {table.Rows.Count} rows and {columnNames.Count} columns");
                        Console.WriteLine($"Columns: {string.Join(", ", columnNames)}");
                        
                        // Dynamic mode - create universal rows
                        if (useDynamicMode.Value)
                        {
                            dynamicData.Value = table.AsEnumerable()
                                .Select(row => new DynamicDataRow
                                {
                                    Data = columnNames.ToDictionary(
                                        col => col, 
                                        col => (object)(row[col]?.ToString() ?? "")
                                    ),
                                    Columns = columnNames
                                })
                                .ToList();
                            columns.Value = columnNames;
                        }
                        else
                        {
                            // Static mode - use fixed User model
                            users.Value = table.AsEnumerable()
                                .Select(u => new User
                                {
                                    ID = u.Field<string>("ID"),
                                    Name = u.Field<string>("Name"),
                                    Email = u.Field<string>("Email"),
                                    PhoneNumber = u.Field<string>("Phone Number"),
                                    Address = u.Field<string>("Address"),
                                    Gender = u.Field<string>("Gender"),
                                    Department = u.Field<string>("Department"),
                                    Level = u.Field<string>("Level")
                                })
                                .ToList();
                        }
                    }
                    else
                    {
                        if (useDynamicMode.Value)
                        {
                            dynamicData.Value = new List<DynamicDataRow>();
                            columns.Value = new List<string>();
                        }
                        else
                        {
                            users.Value = new List<User>();
                        }
                    }
                    
                    // Reset "Import" button and display alert
                    isImport.Set(false);
                    client.Toast("Import successful", "Notification");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Import Error: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    client.Toast($"Import Error: {ex.Message}", "Error");
                }
            }
        }, isImport, filePath);
        // Delete all data
        UseEffect(() =>
        {
            if (useDynamicMode.Value)
            {
                dynamicData.Value = new List<DynamicDataRow>();
                displayDynamicData.Value = new List<DynamicDataRow>();
                columns.Value = new List<string>();
            }
            else
            {
                users.Value = new List<User>();
                displayUsers.Value = new List<User>();
            }
            isDelete.Set(false);
        }, isDelete);
        return Layout.Vertical(
            Layout.Horizontal(Text.H2("Excel Data Reader - Adaptive Table")).Align(Align.Left),
            
            // Mode switcher
            Layout.Horizontal(
                Layout.Vertical(
                    Text.Label("Dynamic Mode"),
                    Text.Small("Support for any number of columns")
                )
            ).Align(Align.Left),
            
            // Mode toggle button
            Layout.Horizontal(
                new Button(useDynamicMode.Value ? "Enabled" : "Disabled", _ => useDynamicMode.Set(!useDynamicMode.Value))
                    .Variant(useDynamicMode.Value ? ButtonVariant.Primary : ButtonVariant.Secondary)
            ).Align(Align.Left),
            
            // FileInput for file upload
            Layout.Horizontal(
                fileInputState.ToFileInput(uploadUrl, "Select Excel/CSV file")
                    .Accept(".xlsx,.xls,.csv")
                    .Width(Size.Full())
            ),
            
            // Control buttons
            Layout.Horizontal(
                new Button("Import from file", _ => isImport.Set(true))
                    .Variant(ButtonVariant.Primary),
                new Button("Delete", _ => isDelete.Set(true))
                    .Destructive()
            ).Align(Align.Left),
            
            // Dynamic table
            useDynamicMode.Value ? (
                displayDynamicData?.Value.Count > 0 ?
                    new Card(
                        Layout.Vertical(
                            CreateDynamicTable(displayDynamicData.Value, columns.Value),
                            new Pagination(page.Value, totalPage.Value, newPage => page.Set(newPage.Value))
                        )
                    ).Title($"Data ({dynamicData.Value.Count} records)").Width(Size.Full()) 
                    : Text.Label("No data to display")
            ) : (
                // Static table
                displayUsers?.Value.Count > 0 ?
                    new Card(
                        Layout.Vertical(
                            displayUsers?.Value.ToTable().Width(Size.Full()),
                            new Pagination(page.Value, totalPage.Value, newPage => page.Set(newPage.Value))
                        )
                    ).Title("Employee List").Width(Size.Full()) 
                    : Text.Label("No employees")
            )
        );
    }

    /// <summary>
    /// Creates a dynamic table based on columns and data
    /// </summary>
    private object CreateDynamicTable(List<DynamicDataRow> data, List<string> columnNames)
    {
        if (data == null || data.Count == 0 || columnNames == null || columnNames.Count == 0)
            return Text.Label("No data to display");

        // Create table headers
        var headers = columnNames.Select(col => Text.Label(col)).ToArray();
        
        // Create data rows
        var rows = data.Select(row => 
            columnNames.Select(col => 
                Text.Label(row.Data.ContainsKey(col) ? row.Data[col]?.ToString() ?? "" : "")
            ).ToArray()
        ).ToArray();

        // Create table with headers and data
        var tableElements = new List<object>();
        
        // Add headers
        tableElements.Add(Layout.Horizontal(headers).Padding(8));
        
        // Add data rows
        foreach (var row in rows)
        {
            tableElements.Add(Layout.Horizontal(row).Padding(4));
        }

        return Layout.Vertical(tableElements.ToArray()).Width(Size.Full());
    }

    /// <summary>
    /// Custom CSV parser to handle CSV files reliably
    /// </summary>
    private DataSet ParseCsvFile(string filePath)
    {
        var dataSet = new DataSet();
        var dataTable = new DataTable();
        
        try
        {
            var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            if (lines.Length == 0)
                return dataSet;

            // Parse header row
            var headers = ParseCsvLine(lines[0]);
            foreach (var header in headers)
            {
                dataTable.Columns.Add(header.Trim());
            }

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                var row = dataTable.NewRow();
                
                for (int j = 0; j < Math.Min(values.Length, dataTable.Columns.Count); j++)
                {
                    row[j] = values[j].Trim();
                }
                
                dataTable.Rows.Add(row);
            }

            dataSet.Tables.Add(dataTable);
            Console.WriteLine($"CSV parsed successfully: {dataTable.Rows.Count} rows, {dataTable.Columns.Count} columns");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CSV parsing error: {ex.Message}");
        }

        return dataSet;
    }

    /// <summary>
    /// Parse a single CSV line handling quoted values and commas
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        
        result.Add(current);
        return result.ToArray();
    }

    /// <summary>
    /// Detect if a file is CSV by analyzing its content
    /// </summary>
    private bool IsCsvFile(string filePath)
    {
        try
        {
            // First check by extension
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".csv")
                return true;

            // If it's a .tmp file or unknown extension, check content
            var firstLine = File.ReadLines(filePath, System.Text.Encoding.UTF8).FirstOrDefault();
            if (string.IsNullOrEmpty(firstLine))
                return false;

            // Check if the first line contains commas (typical CSV delimiter)
            // and doesn't look like binary Excel content
            var commaCount = firstLine.Count(c => c == ',');
            var hasCommas = commaCount > 0;
            
            // Check if it contains typical Excel binary signatures
            var isBinary = firstLine.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t');
            
            // If it has commas and doesn't look binary, it's likely CSV
            return hasCommas && !isBinary;
        }
        catch
        {
            return false;
        }
    }
}

