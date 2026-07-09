using System.ComponentModel.DataAnnotations;

namespace SIMS_Assignment.Models
{
    public class Course
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int LecturerId { get; set; }
        public List<int> EnrolledStudentIds { get; set; } = new();
    }
}

namespace SIMS_WEB.Models
{
    public class Course
    {
        [Required(ErrorMessage = "Mã khóa học không được để trống")]
        [Display(Name = "Mã Khóa học")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [Display(Name = "Tên Khóa học")]
        public string Title { get; set; } = "";

        [Required]
        [Range(1, 500, ErrorMessage = "Sĩ số phải từ 1 đến 500")]
        [Display(Name = "Sĩ số tối đa")]
        public int Capacity { get; set; } = 30;

        [Display(Name = "Số SV đã đăng ký")]
        public int EnrolledCount { get; set; } = 0;

        [Display(Name = "Giảng viên")]
        public string Instructor { get; set; } = "";

        public int CapacityPercent => Capacity > 0 ? (EnrolledCount * 100 / Capacity) : 0;
        public bool IsFull => EnrolledCount >= Capacity;
    }

    public class Enrollment
    {
        public string StudentId { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public DateTime EnrolledAt { get; set; } = DateTime.Now;
        public bool IsEnrolled { get; set; } = true;
    }

    public static class EnrollmentHelper
    {
        public static bool IsStudentEnrolled(IEnumerable<Enrollment> enrollments, string studentId, string courseCode)
            => enrollments.Any(e => e.StudentId == studentId && e.CourseCode == courseCode && e.IsEnrolled);
    }

    public class GradeRecord
    {
        public string StudentId { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public double Score { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string GradeLabel => Score switch
        {
            >= 90 => "A - Xuất sắc",
            >= 80 => "B - Khá",
            >= 65 => "C - Trung bình",
            >= 50 => "D - Yếu",
            _ => "F - Không đạt"
        };

        public string GradeBadgeClass => Score switch
        {
            >= 90 => "badge-grade-a",
            >= 80 => "badge-grade-b",
            >= 65 => "badge-grade-c",
            >= 50 => "badge-grade-d",
            _ => "badge-grade-f"
        };
    }
}
