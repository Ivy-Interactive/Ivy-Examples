namespace MiniExcelExample;

public class StudentEditSheet(IState<bool> isOpen, Guid studentId, RefreshToken? refreshToken = null, Action? onClose = null) : ViewBase
{
    public override object? Build()
    {
        var student = this.UseState(() => StudentService.GetStudents().FirstOrDefault(s => s.ID == studentId));
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                StudentService.UpdateStudent(student.Value!);
                client.Toast("Student updated");
                refreshToken?.Refresh();
                onClose?.Invoke();
            }
			catch (InvalidOperationException ex)
			{
				client.Toast($"Update error: {ex.Message}", "Error");
			}
			catch (ArgumentException ex)
			{
				client.Toast($"Update error: {ex.Message}", "Error");
			}
			catch (Exception ex)
			{
				client.Toast($"Update error: {ex.Message}", "Error");
			}
        }, [student]);

        if (student.Value == null)
        {
            return null;
        }

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

