
using System.Data;
using System.IO;

[App(icon: Icons.Sheet)]
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
        var users = UseState(() => new List<User>());
        var displayUsers = UseState(() => new List<User>());
        var isImport = UseState(false);
        var isDelete = UseState(false);
        var page = UseState(1);
        var totalPage = UseState(0);
        var client = UseService<IClientProvider>();

        // re-render when users, totalPager, or page change value
        UseEffect(() =>
        {
            (totalPage.Value, displayUsers.Value) = PaginationValue(page.Value, 20, users.Value);
        }, users, totalPage, page);

        // Load data from "Manpower.csv" file and save them to state variables by click "Import" button or changed filePath link . after finish, re-render page
        UseEffect(() =>
        {
            if (isImport.Value)
            {
                // Read source data file using FileStream and ExcelDataReader to convert data become DataTableCollection type
                try
                {
                    Console.WriteLine("Came to file Path: " + filePath);
                    using var stream = new FileStream(filePath.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = string.Equals(System.IO.Path.GetExtension(filePath.Value), ".csv", StringComparison.OrdinalIgnoreCase) ?
                    ExcelReaderFactory.CreateCsvReader(stream) : ExcelReaderFactory.CreateReader(stream);
                    // Use ExcelDataReader.DataSet to convert data to Dataset
                    DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });
                    // Convert Data to List<User> 
                    if (result != null && result.Tables.Count > 0)
                    {
                        users.Value = result.Tables[0].AsEnumerable()
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
                    else
                    {
                        users.Value = new List<User>();
                    }
                    // devide data to display on the screen, using pagination.
                    (totalPage.Value, displayUsers.Value) = PaginationValue(page.Value, 20, users.Value);
                    // Reset "Import" button and dislay alert
                    isImport.Set(false);
                    client.Toast("Import successfull", "Notification");

                }
                catch (Exception ex)
                {
                    client.Toast($"Import Error: {ex.Message}", "Error");
                }
            }
        }, isImport, filePath);
        // Delete all data
        UseEffect(() =>
        {
            users.Value = new List<User>();
            displayUsers.Value = new List<User>();
            isDelete.Set(false);
        }, isDelete);
        return Layout.Vertical(
            Layout.Horizontal().Align(Align.Right) |
            Layout.Horizontal(Text.H2("This is the example for Nuget exceldatareader")).Align(Align.Left) |
            new Button("Import", _ =>
            {
                isImport.Set(true);
            })
            | new Button("Delete", _ =>
            {
                isDelete.Set(true);
            }).Destructive()

        )
        | Layout.Vertical(
            displayUsers?.Value.Count > 0 ?
                 new Card(
                Layout.Vertical()
                    | displayUsers?.Value.ToTable().Width(Size.Full())
           | new Pagination(page.Value, totalPage.Value, newPage => page.Set(newPage.Value))

            ).Title("Employee List").Width(Size.Full()) : Text.Label("No employee")
            );
    }
}

