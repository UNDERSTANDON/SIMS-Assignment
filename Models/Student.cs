using System.ComponentModel.DataAnnotations;

namespace SIMS_Assignment.Models
{
    public class Student : User
    {
        public string CurrentProgram { get; set; } = string.Empty;
        public Dictionary<string, bool> AcademicRecords { get; set; } = new();
        public List<Course> Courses { get; set; } = new();
    }
}

namespace SIMS_WEB.Models
{
    public class Student
    {
        public string Id { get; set; } = "";

        [Required(ErrorMessage = "Student ID is required")]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = "";

        [Required(ErrorMessage = "Full Name is required")]
        [MaxLength(255, ErrorMessage = "Full Name cannot exceed 255 characters")]
        [RegularExpression(@"^[\p{L}\s\d]+$", ErrorMessage = "Full Name cannot contain special characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Please select a program")]
        [Display(Name = "Program")]
        public string Program { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }
    }
}
