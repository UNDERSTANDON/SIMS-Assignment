using System.ComponentModel.DataAnnotations;

namespace SIMS_WEB.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Admin";

        public string? ErrorMessage { get; set; }
        public int FailedAttempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
        public int LockRemainingSeconds { get; set; } = 0;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MinLength(4, ErrorMessage = "Tên đăng nhập tối thiểu 4 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Student";

        public string? SuccessMessage { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MinLength(4, ErrorMessage = "Tên đăng nhập tối thiểu 4 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Student";

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class EnrollmentViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sinh viên")]
        [Display(Name = "Sinh viên")]
        public string StudentId { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        [Display(Name = "Khóa học")]
        public string CourseCode { get; set; } = "";

        public List<Student> Students { get; set; } = new();
        public List<Course> Courses { get; set; } = new();
        public List<Enrollment> EnrolledList { get; set; } = new();
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class GradeViewModel
    {
        [Display(Name = "Khóa học")]
        public string CourseCode { get; set; } = "";

        [Display(Name = "Sinh viên")]
        public string StudentId { get; set; } = "";

        [Range(0, 100, ErrorMessage = "Điểm phải từ 0 đến 100")]
        [Display(Name = "Điểm số")]
        public double Score { get; set; }

        public List<Course> Courses { get; set; } = new();
        public List<Student> Students { get; set; } = new();
        public List<GradeRecord> RecentGrades { get; set; } = new();
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class PaymentViewModel
    {
        [Display(Name = "Phương thức thanh toán")]
        public string Method { get; set; } = "credit";

        // Credit card
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCvv { get; set; }

        // Bank transfer
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }

        // E-wallet
        public string? WalletType { get; set; }
        public string? WalletId { get; set; }

        public double BaseAmount { get; set; } = 15_000_000;
        public double Fee => Method switch
        {
            "credit" => BaseAmount * 0.025,
            "bank" => 0,
            "ewallet" => BaseAmount * 0.01,
            _ => 0
        };
        public double Total => BaseAmount + Fee;
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}
