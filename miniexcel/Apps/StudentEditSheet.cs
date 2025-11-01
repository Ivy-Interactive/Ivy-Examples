namespace MiniExcelExample;

public class StudentEditSheet(IState<bool> isOpen, RefreshToken refreshToken, Guid studentId, IState<List<Student>> students) : ViewBase
{
    public override object? Build()
    {
        var student = UseState(() => students.Value.FirstOrDefault(s => s.ID == studentId));
        var client = UseService<IClientProvider>();

        if (student.Value == null)
        {
            return Layout.Center()
                | Text.Danger("Student not found");
        }

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

                // Find and update student in the list
                var existing = students.Value.FirstOrDefault(s => s.ID == studentId);
                if (existing != null && (existing.Name != student.Value.Name ||
                                         existing.Email != student.Value.Email ||
                                         existing.Age != student.Value.Age ||
                                         existing.Course != student.Value.Course ||
                                         existing.Grade != student.Value.Grade))
                {
                    existing.Name = student.Value.Name;
                    existing.Email = student.Value.Email;
                    existing.Age = student.Value.Age;
                    existing.Course = student.Value.Course;
                    existing.Grade = student.Value.Grade;
                    
                    // Trigger state update
                    StudentService.UpdateStudent(existing);
                    students.Set(StudentService.GetStudents());
                    refreshToken.Refresh(); // Sync with other pages
                    client.Toast("Student updated");
                    isOpen.Set(false);
                }
            }
            catch (Exception ex)
            {
                client.Toast($"Update error: {ex.Message}", "Error");
            }
        }, [student]);

        return student
            .ToForm()
            .Place(s => s.Name, s => s.Email)
            .Remove(s => s.ID)
            .Required(s => s.Name, s => s.Email, s => s.Course)
            .Builder(s => s.Email, e => e.ToEmailInput())
            .Builder(s => s.Age, e => e.ToNumberInput().Min(1).Max(150))
            .Builder(s => s.Grade, e => e.ToNumberInput().Min(0).Max(100).Step(0.1))
            .ToSheet(isOpen, "Edit Student");
    }
}

