namespace MiniExcelExample;

public static class StudentService
{
    private static List<Student> _students = new()
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
    };

    public static List<Student> GetStudents() => _students.ToList();

    public static void UpdateStudents(List<Student> students)
    {
        _students = students;
    }

    public static void AddStudent(Student student)
    {
        _students.Add(student);
    }

    public static void UpdateStudent(Student student)
    {
        var existing = _students.FirstOrDefault(s => s.ID == student.ID);
        if (existing != null)
        {
            existing.Name = student.Name;
            existing.Email = student.Email;
            existing.Age = student.Age;
            existing.Course = student.Course;
            existing.Grade = student.Grade;
        }
    }
}

