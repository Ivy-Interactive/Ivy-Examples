namespace MiniExcelExample.Apps;

[App(icon: Icons.Sheet, title: "MiniExcel")]
public class MiniExcelApp : ViewBase
{
    public override object? Build()
    {
        var selectedTab = this.UseState(0);
        var client = UseService<IClientProvider>();

        // In-memory data store (no files needed!)
        var students = this.UseState(() => new List<Student>
        {
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Alice Johnson",
                Email = "alice.johnson@university.edu",
                Age = 20,
                Course = "Computer Science",
                Grade = 95.5m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Bob Smith",
                Email = "bob.smith@university.edu",
                Age = 22,
                Course = "Mathematics",
                Grade = 88.0m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Carol Williams",
                Email = "carol.williams@university.edu",
                Age = 19,
                Course = "Physics",
                Grade = 92.3m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "David Brown",
                Email = "david.brown@university.edu",
                Age = 23,
                Course = "Computer Science",
                Grade = 76.5m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Emily Davis",
                Email = "emily.davis@university.edu",
                Age = 21,
                Course = "Engineering",
                Grade = 98.7m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Frank Miller",
                Email = "frank.miller@university.edu",
                Age = 25,
                Course = "Business Administration",
                Grade = 81.2m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Grace Wilson",
                Email = "grace.wilson@university.edu",
                Age = 20,
                Course = "Biology",
                Grade = 89.8m
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "Henry Moore",
                Email = "henry.moore@university.edu",
                Age = 22,
                Course = "Computer Science",
                Grade = 94.1m
            }
        });

        return Layout.Vertical()
            | Text.H2("MiniExcel Library Demo")
            | Text.Muted("Fast, Low-Memory, Easy Excel helper for .NET. All operations use MemoryStream - no files required!")
            
            // Tab navigation
            | Layout.Tabs(
                new Tab("1. Strongly Typed", BuildStronglyTypedTab(client, students)),
                new Tab("2. LINQ Extensions", BuildLinqTab(client, students)),
                new Tab("3. Import & Export", BuildImportExportTab(client, students))
            ).Variant(TabsVariant.Tabs)

            | new Spacer()
            | Text.Small("This demo uses MiniExcel library with Ivy Framework - all data stored in memory using MemoryStream.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [MiniExcel](https://github.com/mini-software/MiniExcel)");
    }

    private object BuildStronglyTypedTab(IClientProvider client, IState<List<Student>> students)
    {
        // Query from MemoryStream
        List<Student> queriedStudents;
        using (var stream = new MemoryStream())
        {
            MiniExcel.SaveAs(stream, students.Value);
            stream.Position = 0;
            queriedStudents = MiniExcel.Query<Student>(stream).ToList();
        }

        return Layout.Horizontal().Gap(6)
            | new Card(
                Layout.Vertical().Gap(5)
                | Text.H3("Strongly Typed Query")
                | Text.Muted("MiniExcel.Query<T> from MemoryStream")
                | Text.Block($"✓ Total records: {queriedStudents.Count}")
                | Text.Success("✓ All data loaded from memory")
                | new Separator()
                | Text.Small("Features:")
                | Text.Small("• Automatic property mapping")
                | Text.Small("• Type conversion (Guid, DateTime, bool)")
                | Text.Small("• Low memory usage")
                | new Separator()
                | Text.Code(@"using (var stream = new MemoryStream()) {
    MiniExcel.SaveAs(stream, students);
    stream.Position = 0;
    var rows = MiniExcel.Query<T>(stream);
}")
            ).Width(Size.Fraction(0.35f))

            | new Card(
                queriedStudents.Take(10).ToTable()
                    .Width(Size.Full())
                    .Builder(s => s.Name, b => b.Text())
                    .Builder(s => s.Email, b => b.Text())
                    .Builder(s => s.Age, b => b.Default())
                    .Builder(s => s.Course, b => b.Text())
                    .Builder(s => s.Grade, b => b.Default())
            ).Width(Size.Fraction(0.65f));
    }

    private object BuildLinqTab(IClientProvider client, IState<List<Student>> students)
    {
        // Query with LINQ from MemoryStream
        int total = 0;
        Student? first = null;
        List<Student> top5 = new();
        int whereCount = 0;
        
        using (var stream = new MemoryStream())
        {
            MiniExcel.SaveAs(stream, students.Value);
            stream.Position = 0;
            
            total = MiniExcel.Query<Student>(stream).Count();
            stream.Position = 0;
            first = MiniExcel.Query<Student>(stream).FirstOrDefault();
            stream.Position = 0;
            top5 = MiniExcel.Query<Student>(stream).Take(5).ToList();
            stream.Position = 0;
            whereCount = MiniExcel.Query<Student>(stream).Where(s => s.Age > 20).Count();
        }

        return Layout.Horizontal().Gap(6)
            | new Card(
                Layout.Vertical().Gap(5)
                | Text.H3("LINQ Extensions")
                | Text.Muted("Query supports First, Take, Skip, Where, OrderBy etc.")
                | new Separator()
                | Layout.Vertical().Gap(3)
                    | Text.Block($"Total: {total}")
                    | Text.Block($"Age > 20: {whereCount}")
                    | Text.Block($"First: {first?.Name ?? "None"}")
                | new Separator()
                | Text.Small("Features:")
                | Text.Small("• First(), FirstOrDefault()")
                | Text.Small("• Take(), Skip()")
                | Text.Small("• Where(), OrderBy()")
                | Text.Small("• Count(), Any()")
                | new Separator()
                | Text.Code(@"stream.Query<T>()
    .Where(x => x.Age > 20)
    .OrderByDescending(x => x.Grade)
    .Take(10)")
            ).Width(Size.Fraction(0.35f))

            | new Card(
                top5.Count > 0
                    ? top5.ToTable()
                        .Width(Size.Full())
                        .Builder(s => s.Name, b => b.Text())
                        .Builder(s => s.Age, b => b.Default())
                        .Builder(s => s.Grade, b => b.Default())
                    : Layout.Center()
                        | Text.Muted("No data to display")
            ).Width(Size.Fraction(0.65f));
    }

    private object BuildImportExportTab(IClientProvider client, IState<List<Student>> students)
    {
        var importedStudents = this.UseState<List<Student>>(() => new List<Student>());
        var fileInput = this.UseState<FileInput?>(() => null);

        // Export download from MemoryStream
        var downloadUrl = this.UseDownload(
            async () =>
            {
                await using var ms = new MemoryStream();
                MiniExcel.SaveAs(ms, students.Value);
                return ms.ToArray();
            },
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"students-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.xlsx"
        );

        // Import upload to MemoryStream
        var uploadUrl = this.UseUpload(
            uploadedBytes =>
            {
                try
                {
                    using var ms = new MemoryStream(uploadedBytes);
                    var imported = MiniExcel.Query<Student>(ms).ToList();
                    importedStudents.Set(imported);
                    client.Toast($"✓ Imported {imported.Count} students");
                }
                catch (Exception ex)
                {
                    client.Toast($"Import failed: {ex.Message}", "Error");
                }
            },
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "imported-file"
        );

        return Layout.Horizontal().Gap(6)
            | new Card(
                Layout.Vertical().Gap(5)
                | Text.H3("Import & Export")
                | Text.Muted("Stream-based operations")
                | new Button("Export Excel")
                    .Url(downloadUrl.Value)
                    .Icon(Icons.Download)
                    .Primary()
                    .Width(Size.Full())
                | new Separator()
                | Text.Label("Import Excel:")
                | fileInput.ToFileInput(uploadUrl, "Choose File")
                    .Accept(".xlsx")
                | new Separator()
                | Text.Small("Features:")
                | Text.Small("• Memory-efficient streams")
                | Text.Small("• Upload/Download")
                | Text.Small("• Error handling")
                | new Separator()
                | Text.Code(@"// Export
using var ms = new MemoryStream();
MiniExcel.SaveAs(ms, data);

// Import
using var ms = new MemoryStream(bytes);
var rows = MiniExcel.Query<T>(ms);")
            ).Width(Size.Fraction(0.4f))

            | new Card(
                importedStudents.Value.Count > 0
                    ? importedStudents.Value.ToTable()
                        .Width(Size.Full())
                        .Builder(s => s.Name, b => b.Text())
                        .Builder(s => s.Age, b => b.Default())
                        .Builder(s => s.Grade, b => b.Default())
                    : Layout.Center()
                        | Text.Muted("Imported data appears here")
            ).Width(Size.Fraction(0.6f));
    }
}

