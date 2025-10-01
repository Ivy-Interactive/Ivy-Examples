using System;
using System.Collections.Generic;
using System.Linq;
using Ivy;
using UseOfMiniExcel.Models;
namespace UseOfMiniExcel.Apps
{
    [App(icon: Icons.PartyPopper, title: "AddStudentToExcel")]
    public class AddStudentToXlsx : ViewBase
    {
        // Model for Excel rows is now shared in Models/Student.cs
        // Persisted states (kept between UI re-renders)
        private readonly State<string> name;
        private readonly State<int> age;

        private readonly string file = "Students.xlsx";

        // Initialize state once when the component is created
        public AddStudentToXlsx()
        {
            name = new State<string>("");
            age = new State<int>(0);
        }

        private void SaveStudent()
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(name.Value) || age.Value <= 0)
                return;

            // Read existing rows (or start empty if no file yet)
            var rows = System.IO.File.Exists(file)
                ? MiniExcel.Query<Student>(file).ToList()
                : new List<Student>();

            // Add new student
            rows.Add(new Student
            {
                Name = name.Value.Trim(),
                Age = age.Value
            });

            // Save back to Excel (overwrite with new list)
            MiniExcel.SaveAs(file, rows, overwriteFile: true);

            // Reset inputs after saving
            name.Value = "";
            age.Value = 0;
        }

        public override object? Build()
        {
            // Build a card layout like the Ivy sample
            return Layout.Center()
                | (new Card(
                    Layout.Vertical().Gap(8).Padding(12)

                    | new Confetti(new IvyLogo()) // fun visual 🎉
                    | Text.H2("Add Student to Excel")

                    // Name input (state bound, keeps value until save)
                    | Text.Label("Name")
                    | new TextInput(state: name, placeholder: "Enter student name")

                    // Age input (state bound, keeps value until save)
                    | Text.Label("Age")
                    | new NumberInput<int>(state: age, placeholder: "Enter student age")

                    | new Separator()

                    // Save button
                    | new Button("Add Student", onClick: SaveStudent).Primary()

                    | new Separator()

                    // Live preview to confirm state binding works
                    | Text.Block($"Preview — Name: {name.Value} | Age: {age.Value}")
                )
                .Width(Size.Units(120).Max(500)));
        }
    }
}
