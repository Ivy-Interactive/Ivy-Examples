namespace MiniExcelExample;

[App(icon: Icons.Sheet, title: "MiniExcel - Edit")]
public class MiniExcelEditApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new StudentsListBlade(), "Students");
    }
}

public class StudentsListBlade : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var searchTerm = this.UseState("");
        var students = this.UseState(() => StudentService.GetStudents());

        // Reload students when refresh token changes
        this.UseEffect(() =>
        {
            students.Set(StudentService.GetStudents());
        }, [refreshToken]);

        // Filter students based on search term
        var filteredStudents = string.IsNullOrWhiteSpace(searchTerm.Value)
            ? students.Value
            : students.Value.Where(s =>
                s.Name.Contains(searchTerm.Value, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(searchTerm.Value, StringComparison.OrdinalIgnoreCase) ||
                s.Course.Contains(searchTerm.Value, StringComparison.OrdinalIgnoreCase) ||
                s.Grade.ToString().Contains(searchTerm.Value, StringComparison.OrdinalIgnoreCase) ||
                s.Age.ToString().Contains(searchTerm.Value)
            ).ToList();

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var student = (Student)e.Sender.Tag!;
            blades.Push(this, new StudentDetailBlade(student.ID), student.Name);
        });

        var items = filteredStudents.Select(student =>
            new ListItem(
                title: student.Name,
                subtitle: $"{student.Course} • Grade: {student.Grade}",
                onClick: onItemClick,
                tag: student
            )
        );

        var addButton = Icons.Plus
            .ToButton()
            .Primary()
            .ToTrigger((isOpen) => new StudentCreateDialog(isOpen, refreshToken, students));

        return BladeHelper.WithHeader(
            Layout.Horizontal().Gap(2)
                | searchTerm.ToTextInput().Placeholder("Search students...").Width(Size.Grow())
                | addButton
            ,
            filteredStudents.Count > 0
                ? new List(items)
                : students.Value.Count > 0
                    ? Layout.Center()
                        | Text.Muted($"No students found matching '{searchTerm.Value}'")
                    : Layout.Center()
                        | Text.Muted("No students. Add the first record.")
        );
    }
}

public class StudentDetailBlade(Guid studentId) : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var student = this.UseState<Student?>(() => StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId));
        var (alertView, showAlert) = this.UseAlert();

        // Reload student when refresh token changes
        this.UseEffect(() =>
        {
            student.Set(StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId));
        }, [refreshToken]);

        // If student was deleted, close the blade
        if (student.Value == null)
        {
            return null; // Blade will be closed automatically
        }

        var studentValue = student.Value;

        var onEdit = new Action(() =>
        {
            blades.Push(this, new StudentEditBlade(studentId), $"Edit {studentValue.Name}");
        });

        var onDelete = new Action(() =>
        {
            showAlert($"Are you sure you want to delete {studentValue.Name}?", result =>
            {
                if (result.IsOk())
                {
                    StudentService.DeleteStudent(studentId);
                    refreshToken.Refresh(); // Update other pages and trigger parent blade refresh
                    blades.Pop(refresh: true); // Close blade and refresh parent list
                }
            }, "Delete Student", AlertButtonSet.OkCancel);
        });

        return new Fragment()
            | BladeHelper.WithHeader(
                Text.H4(studentValue.Name)
                ,
                Layout.Vertical().Gap(4)
                    | new Card(
                        Layout.Vertical().Gap(3)
                        | new {
                            Email = studentValue.Email,
                            Age = studentValue.Age,
                            Course = studentValue.Course,
                            Grade = studentValue.Grade
                        }.ToDetails(),
                        Layout.Horizontal()
                        | new Button("Edit")
                            .Icon(Icons.Pencil)
                            .Secondary()
                            .HandleClick(onEdit)
                        | new Button("Delete")
                            .Icon(Icons.Trash)
                            .Destructive()
                            .HandleClick(onDelete)
                    )
            )
            | alertView;
    }
}

public class StudentEditBlade(Guid studentId) : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var student = this.UseState(() => StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId));
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                // Only update if form is valid
                if (string.IsNullOrWhiteSpace(student.Value.Name) ||
                    string.IsNullOrWhiteSpace(student.Value.Email) ||
                    string.IsNullOrWhiteSpace(student.Value.Course))
                {
                    return;
                }

                // Check if student was actually changed
                var existing = StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId);
                if (existing != null && (existing.Name != student.Value.Name ||
                                         existing.Email != student.Value.Email ||
                                         existing.Age != student.Value.Age ||
                                         existing.Course != student.Value.Course ||
                                         existing.Grade != student.Value.Grade))
                {
                    StudentService.UpdateStudent(student.Value);
                    refreshToken.Refresh(); // Update parent blade and other pages
                    client.Toast("Student updated");
                    blades.Pop(refresh: true); // Close blade and refresh parent list
                }
            }
            catch (Exception ex)
            {
                client.Toast($"Update error: {ex.Message}", "Error");
            }
        }, [student]);

        return BladeHelper.WithHeader(
            Text.H4($"Edit {student.Value.Name}")
            ,
            student
                .ToForm()
                .Place(s => s.Name, s => s.Email)
                .Remove(s => s.ID)
                .Required(s => s.Name, s => s.Email, s => s.Course)
                .Builder(s => s.Email, e => e.ToEmailInput())
                .Builder(s => s.Age, e => e.ToNumberInput().Min(1).Max(150))
                .Builder(s => s.Grade, e => e.ToNumberInput().Min(0).Max(100).Step(0.1))
        );
    }
}

[App(icon: Icons.Sheet, title: "MiniExcel - View")]
public class MiniExcelViewApp : ViewBase
{
    public override object? Build()
    {
        var refreshToken = this.UseRefreshToken();
        
        // Load students from shared service
        var students = this.UseState(() => StudentService.GetStudents());

        // Reload students when refresh token changes
        this.UseEffect(() =>
        {
            students.Set(StudentService.GetStudents());
        }, [refreshToken]);

        return BuildTableViewPage(students, refreshToken);
    }

    private object BuildTableViewPage(IState<List<Student>> students, RefreshToken refreshToken)
    {
        var client = UseService<IClientProvider>();
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
                    
                    // Merge imported students with existing ones (by ID)
                    var currentStudents = StudentService.GetStudents();
                    foreach (var importedStudent in imported)
                    {
                        var existing = currentStudents.FirstOrDefault(s => s.ID == importedStudent.ID);
                        if (existing != null)
                        {
                            // Update existing
                            existing.Name = importedStudent.Name;
                            existing.Email = importedStudent.Email;
                            existing.Age = importedStudent.Age;
                            existing.Course = importedStudent.Course;
                            existing.Grade = importedStudent.Grade;
                        }
                        else
                        {
                            // Add new
                            if (importedStudent.ID == Guid.Empty)
                            {
                                importedStudent.ID = Guid.NewGuid();
                            }
                            currentStudents.Add(importedStudent);
                        }
                    }
                    
                    StudentService.UpdateStudents(currentStudents);
                    students.Set(StudentService.GetStudents()); // Trigger update
                    refreshToken.Refresh(); // Sync with other pages
                    client.Toast($"Imported {imported.Count} students");
                }
                catch (Exception ex)
                {
                    client.Toast($"Import error: {ex.Message}", "Error");
                }
            },
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "imported-file"
        );

        return Layout.Vertical().Gap(4)
            | new Card(
                Layout.Vertical().Gap(3)
                | Text.H3("View and Import/Export")
                | Text.Muted("Table of all students with import and export functionality")
                | new Separator()
                | Layout.Horizontal().Gap(3)
                    | new Button("Export to Excel")
                        .Icon(Icons.Download)
                        .Primary()
                        .Url(downloadUrl.Value)
                        .Width(Size.Auto())
                    | new Separator()
                    | Text.Label("Import from Excel:")
                | fileInput.ToFileInput(uploadUrl, "Choose File")
                    .Accept(".xlsx")
                        .Width(Size.Auto())
                | new Separator()
                | Text.Label($"Total records: {students.Value.Count}")
            )
            | new Card(
                students.Value.Count > 0
                    ? students.Value.ToTable()
                        .Width(Size.Full())
                        .Builder(s => s.Name, b => b.Text())
                        .Builder(s => s.Email, b => b.Text())
                        .Builder(s => s.Age, b => b.Default())
                        .Builder(s => s.Course, b => b.Text())
                        .Builder(s => s.Grade, b => b.Default())
                    : Layout.Center()
                        | Text.Muted("No data to display")
            );
    }
}

