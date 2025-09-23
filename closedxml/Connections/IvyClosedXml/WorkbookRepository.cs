using System.Collections.ObjectModel;
using System.Data;
using ClosedXML.Excel;

/// <summary>
/// Represents a repository for managing Excel workbook files using ClosedXML.
/// This class handles operations such as adding, removing, and manipulating workbook files,
/// including setting a current file for operations and saving data tables to worksheets.
/// </summary>
public class WorkbookRepository
{
    private List<WorkbookFileInfo> excelFiles { get; set; }
    private WorkbookFileInfo? currentFile { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbookRepository"/> class.
    /// </summary>
    public WorkbookRepository()
    {
        excelFiles = new List<WorkbookFileInfo>();
    }

    /// <summary>
    /// Sets the current file on which operations are performed.
    /// </summary>
    /// <param name="fileName">The name of the file to set as current.</param>
    public void SetCurrentFile(string fileName)
    {
        currentFile = GetFileByName(fileName);
    }

    /// <summary>
    /// Gets the current workbook file.
    /// </summary>
    /// <returns>The current <see cref="WorkbookFileInfo"/> instance.</returns>
    public WorkbookFileInfo? GetCurrentFile()
    {
        return currentFile;
    }

    /// <summary>
    /// Gets the first table from the current worksheet as a DataTable.
    /// If no table exists, returns a new DataTable with the name "FirstTable".
    /// </summary>
    /// <returns>A <see cref="DataTable"/> representing the first table in the worksheet.</returns>
    public DataTable GetCurrentTable()
    {
        var table = Worksheet.Tables.FirstOrDefault();
        if (table == null)
            return new DataTable() { TableName = "FirstTable" };

        return table.AsNativeDataTable();
    }

    /// <summary>
    /// Gets a read-only collection of all workbook files in the repository.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyCollection{WorkbookFileInfo}"/> of workbook files.</returns>
    public ReadOnlyCollection<WorkbookFileInfo> GetFiles()
    {
        return new ReadOnlyCollection<WorkbookFileInfo>(excelFiles);
    }

    /// <summary>
    /// Adds a new workbook file to the repository.
    /// </summary>
    /// <param name="fileName">The name of the file to add.</param>
    /// <exception cref="ArgumentException">Thrown if the file name already exists.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the file name is invalid.</exception>
    internal void AddNewFile(string fileName)
    {
        ValidateFileName(fileName);

        if (ContainsFile(fileName))
            throw new ArgumentException("File name already exists in the collection.");

        excelFiles.Add(new WorkbookFileInfo(fileName));
    }

    /// <summary>
    /// Validates the provided file name for invalid characters and formats.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the file name is empty, contains invalid characters, or ends with invalid characters.</exception>
    private void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentOutOfRangeException("File name cannot be empty!");

        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        if (fileName.Any(c => invalidChars.Contains(c)))
            throw new ArgumentOutOfRangeException("Invalid characters in file name!");

        // Check for trailing periods or spaces
        if (fileName.EndsWith(".") || fileName.EndsWith(" "))
            throw new ArgumentOutOfRangeException("File ends with invalid character!");
    }

    /// <summary>
    /// Removes a workbook file from the repository and disposes of its workbook.
    /// If no files remain, sets the current file to null.
    /// </summary>
    /// <param name="fileName">The name of the file to remove.</param>
    internal void RemoveFile(string fileName)
    {
        if (ContainsFile(fileName))
        {
            var fileToRemove = GetFileByName(fileName);
            fileToRemove.Workbook.Dispose();
            excelFiles.Remove(fileToRemove);

            if (excelFiles.Count == 0)
            {
                currentFile = null;
            }
        }
    }

    /// <summary>
    /// Checks if a file with the specified name exists in the repository.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    private bool ContainsFile(string fileName)
    {
        return excelFiles.Any(f => f.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Retrieves a workbook file by its name.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>The <see cref="WorkbookFileInfo"/> instance matching the file name.</returns>
    private WorkbookFileInfo GetFileByName(string fileName)
    {
        return excelFiles.Single(f => f.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Saves the provided DataTable to the current worksheet as a table named "FirstTable".
    /// Removes any existing table with the same name before saving.
    /// </summary>
    /// <param name="table">The <see cref="DataTable"/> to save.</param>
    internal void Save(DataTable table)
    {
        TryRemoveExistingTable();

        Worksheet
        .Cell(1, 1)
        .InsertTable(table, "FirstTable", true);

        var file = GetCurrentFile();
        if (file != null)
        {
            file.Workbook.SaveAs(file.FileName + ".xlsx");
        }
    }

    /// <summary>
    /// Gets the first worksheet from the current workbook.
    /// </summary>
    private IXLWorksheet Worksheet => currentFile.Workbook.Worksheets.FirstOrDefault();

    /// <summary>
    /// Attempts to remove the existing table named "FirstTable" from the worksheet.
    /// Clears the worksheet if the table exists.
    /// </summary>
    private void TryRemoveExistingTable()
    {
        try
        {
            var table = Worksheet.Tables.FirstOrDefault(f => f.Name == "FirstTable");

            if (table != null)
            {
                Worksheet.Tables.Remove(0);
                table.Clear(XLClearOptions.All);
                Worksheet.Clear(XLClearOptions.All);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in TryRemoveExistingTable: {ex}");
        }
    }
}