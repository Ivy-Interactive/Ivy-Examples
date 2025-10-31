using System;
using System.Collections.Generic;
using System.Linq;
namespace UseOfMiniExcel.Models
{
    public class Student
    {
        public Guid ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Course { get; set; } = string.Empty;
        public decimal Grade { get; set; }
    }
}