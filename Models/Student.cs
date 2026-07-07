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

        [Required(ErrorMessage = "Mã sinh viên không được để trống")]
        [Display(Name = "Mã Sinh viên")]
        public string StudentId { get; set; } = "";

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
        [RegularExpression(@"^[\p{L}\s\d]+$", ErrorMessage = "Họ tên không được chứa ký tự đặc biệt")]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn chương trình học")]
        [Display(Name = "Chương trình học")]
        public string Program { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }
    }
}
