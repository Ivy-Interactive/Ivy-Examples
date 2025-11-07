namespace MiniExcelExample;

public class StudentCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, IState<List<Student>> students) : ViewBase
{
    private record StudentCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; init; } = "";

        [Range(18, 110)]
        public int Age { get; init; } = 0;

        [Required]
        public string Course { get; init; } = "";

        [Range(0, 100)]
        public double Grade { get; init; } = 0;
    }

    public override object? Build()
    {
        var student = UseState(() => new StudentCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                // Check if form is valid (all required fields filled)
                if (!string.IsNullOrWhiteSpace(student.Value.Name) &&
                    !string.IsNullOrWhiteSpace(student.Value.Email) &&
                    student.Value.Age > 0 &&
                    !string.IsNullOrWhiteSpace(student.Value.Course))
                {
                    var newStudent = new Student
                    {
                        ID = Guid.NewGuid(),
                        Name = student.Value.Name,
                        Email = student.Value.Email,
                        Age = student.Value.Age,
                        Course = student.Value.Course,
                        Grade = student.Value.Grade
                    };

                    StudentService.AddStudent(newStudent);
                    students.Set(StudentService.GetStudents()); // Trigger update
                    refreshToken.Refresh(); // Sync with other pages
                    
                    client.Toast($"Student '{newStudent.Name}' added");
                    
                    // Reset form - ToDialog will auto-close
                    student.Set(new StudentCreateRequest());
                }
            }
            catch (Exception ex)
            {
                client.Toast($"Add error: {ex.Message}", "Error");
            }
        }, [student]);

        return student
            .ToForm()
            .Place(s => s.Name, s => s.Email)
            .Required(s => s.Name, s => s.Email, s => s.Course)
            .Builder(s => s.Email, e => e.ToEmailInput())
            .Builder(s => s.Age, e => e.ToNumberInput().Min(18).Max(110))
            .Builder(s => s.Grade, e => e.ToNumberInput().Min(0).Max(100).Step(0.1))
            .ToDialog(isOpen, title: "Add Student", submitTitle: "Add");
    }
}

