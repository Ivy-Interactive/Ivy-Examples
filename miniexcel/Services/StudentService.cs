namespace MiniExcelExample;

public static class StudentService
{
    // Global event for notifying all views about data changes
    public static event Action? DataChanged;
    
    // Static callback for MiniExcelViewApp to refresh data
    private static Action? _refreshCallback;

    public static void RegisterRefreshCallback(Action callback)
    {
        _refreshCallback = callback;
    }
    
    public static void UnregisterRefreshCallback()
    {
        _refreshCallback = null;
    }

    private static void NotifyDataChanged()
    {
        DataChanged?.Invoke();
        _refreshCallback?.Invoke();
    }

    private static ConcurrentDictionary<Guid, Student> _students = new(
        new[]
        {
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Alice Johnson",
                Email = "alice.johnson@university.edu",
                Age = 20,
                Course = "Computer Science",
                Grade = 95.5
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Bob Smith",
                Email = "bob.smith@university.edu",
                Age = 22,
                Course = "Mathematics",
                Grade = 88.0
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Carol Williams",
                Email = "carol.williams@university.edu",
                Age = 19,
                Course = "Physics",
                Grade = 92.3
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "David Brown",
                Email = "david.brown@university.edu",
                Age = 23,
                Course = "Computer Science",
                Grade = 76.5
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Emily Davis",
                Email = "emily.davis@university.edu",
                Age = 21,
                Course = "Engineering",
                Grade = 98.7
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Frank Miller",
                Email = "frank.miller@university.edu",
                Age = 25,
                Course = "Business Administration",
                Grade = 81.2
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Grace Wilson",
                Email = "grace.wilson@university.edu",
                Age = 20,
                Course = "Biology",
                Grade = 89.8
            },
            new Student
            {
                ID = Guid.NewGuid(),
                Name = "Henry Moore",
                Email = "henry.moore@university.edu",
                Age = 22,
                Course = "Computer Science",
                Grade = 94.1
            }
        }.ToDictionary(s => s.ID)
    );

    public static List<Student> GetStudents() => _students.Values.ToList();

    public static void UpdateStudents(List<Student> students)
    {
        _students = new ConcurrentDictionary<Guid, Student>(students.ToDictionary(s => s.ID));
        NotifyDataChanged();
    }

    public static void AddStudent(Student student)
    {
        if (student.ID == Guid.Empty)
        {
            student.ID = Guid.NewGuid();
        }
        _students.TryAdd(student.ID, student);
        NotifyDataChanged();
    }

    public static void UpdateStudent(Student student)
    {
        _students.AddOrUpdate(student.ID, student, (key, existing) =>
        {
            existing.Name = student.Name;
            existing.Email = student.Email;
            existing.Age = student.Age;
            existing.Course = student.Course;
            existing.Grade = student.Grade;
            return existing;
        });
        NotifyDataChanged();
    }

    public static void DeleteStudent(Guid studentId)
    {
        _students.TryRemove(studentId, out _);
        NotifyDataChanged();
    }
}

