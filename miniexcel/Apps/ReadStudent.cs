using System;
using System.Linq;
using MiniExcelLibs;

namespace UseOfMiniExcel.Apps
{
    // Define a C# model (POCO) that matches the Excel file headers.
    // The Excel file must have columns: "Name" and "Age".
    public class Student
    {
        public string Name { get; set; }  // Student's name
        public int Age { get; set; }      // Student's age
    }

    // This class represents an Ivy application screen (UI page)
    [App(icon: Icons.PartyPopper, title: "ReadFromExcelFile")]
    public class ReadStudent : ViewBase
    {
        // The UI is built inside this method.
        public override object? Build()
        {
            // Path to your Excel file.
            // Make sure "Students.xlsx" is located in bin/Debug/net9.0/Students.xlsx
            var file = "Students.xlsx";

            // Read all rows from the Excel file and map them into a list of Student objects.
            // MiniExcel automatically matches Excel headers to Student class properties.
            var rows = MiniExcel.Query<Student>(file).ToList();

            // Create a responsive "wrap layout" where student cards flow into rows,
            // wrapping automatically when the screen is not wide enough.
            var studentCards = Layout.Wrap().Gap(12);

            // Loop through each student from the Excel file
            foreach (var student in rows)
            {
                // Build a card for each student
                studentCards |= new Card(
                    Layout.Vertical().Gap(6).Padding(6) // vertical layout inside the card
                        | Text.H3(student.Name)         // show the student's name
                        | Text.Block($"Age: {student.Age}") // show the student's age
                )
                .Width(180); // fix card width so all cards look consistent
            }

            // Center the entire UI content on the page
            // Build a vertical layout that contains:
            // 1. A header ("Students List")
            // 2. The grid of student cards
            return Layout.Center()
                | (Layout.Vertical().Gap(20)
                    | Text.H2("Students List") // main page title
                    | studentCards             // all student cards
                );
        }
    }
}
