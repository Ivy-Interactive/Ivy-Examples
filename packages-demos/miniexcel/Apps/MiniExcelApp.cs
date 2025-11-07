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
        }, [refreshToken.ToTrigger()]);

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
            blades.Push(this, new StudentDetailBlade(student.ID, () => refreshToken.Refresh()), student.Name);
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

public class StudentDetailBlade(Guid studentId, Action? onRefresh = null) : ViewBase
{
    public override object? Build()
    {
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();

        var initialStudent = StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId);
        
        if (initialStudent == null)
        {
            return null; // Blade will be closed automatically
        }

        var student = this.UseState(initialStudent);
        
        // Helper function to get current student from service
        Student? GetCurrentStudent() => StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId);
        
        // Targeted refresh function - only called when needed
        void RefreshStudentData()
        {
            var updatedStudent = GetCurrentStudent();
            if (updatedStudent != null)
            {
                student.Set(updatedStudent);
            }
        }
        
        // Update local data when refresh token changes (for external updates)
        this.UseEffect(() =>
        {
            RefreshStudentData();
        }, [refreshToken.ToTrigger()]);

        var studentValue = student.Value;

        var editButton = new Button("Edit")
            .Icon(Icons.Pencil)
            .Secondary()
            .ToTrigger((isOpen) => new StudentEditSheet(isOpen, studentId, refreshToken, () => 
            {
                RefreshStudentData();
                onRefresh?.Invoke();
            }));

        var onDelete = new Action(() =>
        {
            showAlert($"Are you sure you want to delete {studentValue.Name}?", result =>
            {
                if (result.IsOk())
                {
                    StudentService.DeleteStudent(studentId);
                    refreshToken.Refresh(); // Update other pages and trigger parent blade refresh
                    onRefresh?.Invoke(); // Notify parent to refresh
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
                        | editButton
                        | new Button("Delete")
                            .Icon(Icons.Trash)
                            .Destructive()
                            .HandleClick(onDelete)
                    )
            )
            | alertView;
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

        // Load data on init AND when manually refreshed (Best Practice from docs)
        this.UseEffect(() =>
        {
            students.Set(StudentService.GetStudents());
        }, [refreshToken.ToTrigger()]);

        // Listen to global data changes from StudentService (for cross-app sync)
        // Subscribe to event and refresh both state and token
        this.UseEffect(() =>
        {
            void OnDataChanged()
            {
                students.Set(StudentService.GetStudents());
                refreshToken.Refresh(); // Also trigger refresh for other effects
            }
            
            StudentService.DataChanged += OnDataChanged;
        }, []);

        return BuildTableViewPage(students, refreshToken);
    }

    private object BuildTableViewPage(IState<List<Student>> students, RefreshToken refreshToken)
    {
        var client = UseService<IClientProvider>();
        var uploadState = this.UseState<FileUpload<byte[]>?>();
        var uploadContext = this.UseUpload(MemoryStreamUploadHandler.Create(uploadState))
            .Accept(".xlsx")
            .MaxFileSize(50 * 1024 * 1024);
        var actionMode = this.UseState("Export");

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

        // When a file is uploaded, import it
        this.UseEffect(() =>
        {
            if (uploadState.Value?.Content is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(bytes);
                    var imported = MiniExcel.Query<Student>(ms).ToList();
                    
                    // Merge imported students with existing ones (by ID)
                    var currentStudents = StudentService.GetStudents();
                    var studentsById = currentStudents.ToDictionary(s => s.ID);
                    foreach (var importedStudent in imported)
                    {
                        if (importedStudent.ID != Guid.Empty && studentsById.TryGetValue(importedStudent.ID, out var existing))
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
                            studentsById[importedStudent.ID] = importedStudent;
                        }
                    }
                    
                    StudentService.UpdateStudents(currentStudents);
                    students.Set(StudentService.GetStudents()); // Trigger update
                    refreshToken.Refresh(); // Sync with other pages
                    client.Toast($"Imported {imported.Count} students");
                }
                catch (IOException ex)
                {
                    client.Toast($"Import error: {ex.Message}", "Error");
                }
                catch (FormatException ex)
                {
                    client.Toast($"Import error: {ex.Message}", "Error");
                }
                catch (SystemException ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException && ex is not ThreadAbortException)
                {
                    client.Toast($"Import error: {ex.Message}", "Error");
                }
                catch (Exception ex)
                {
                    client.Toast($"Import error: {ex.Message}", "Error");
                }
                finally
                {
                    uploadState.Reset();
                }
            }
        }, [uploadState]);

        object? actionWidget = actionMode.Value == "Export"
            ? new Button("Download Excel File")
                .Icon(Icons.Download)
                .Primary()
                .Url(downloadUrl.Value)
                .Width(Size.Full())
            : uploadState.ToFileInput(uploadContext)
                .Placeholder("Choose File");

        return Layout.Horizontal().Gap(4)
            | new Card(
                Layout.Vertical().Gap(3)
                | Text.H3("Data Management")
                | Text.Muted("Upload and download Excel files with students data")
                | actionMode.ToSelectInput(new[] { "Export", "Import" }.ToOptions())
                | actionWidget
                | new Spacer().Height(Size.Units(5))
                | Text.Small("This demo uses MiniExcel to manage students data.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [MiniExcel](https://github.com/mini-software/MiniExcel)")
            ).Width(Size.Fraction(0.4f))
            | new Card(
                Layout.Vertical()
                | Text.H3("Data Overview")
                | Text.Muted($"Search, filter and view all students data. Total records: {students.Value.Count}")
                | (students.Value.Count > 0
                    ? students.Value.AsQueryable().ToDataTable()
                        .Hidden(s => s.ID)
                        .Width(Size.Full())
                        .Height(Size.Fit())
                        .Key($"students-{students.Value.Count}-{students.Value.Sum(s => s.GetHashCode())}") // Force re-render when data changes
                    : Layout.Center()
                        | Text.Muted("No data to display")
                     
            )).Height(Size.Fit().Min(Size.Full()));
    }
}

