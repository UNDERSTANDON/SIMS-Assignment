using System.ComponentModel.DataAnnotations;

namespace SIMS_WEB.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Display(Name = "Role")]
        public string Role { get; set; } = "Admin";

        public string? ErrorMessage { get; set; }
        public int FailedAttempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
        public int LockRemainingSeconds { get; set; } = 0;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(4, ErrorMessage = "Username must be at least 4 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Student";

        public string? SuccessMessage { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(4, ErrorMessage = "Username must be at least 4 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Student";

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class EnrollmentViewModel
    {
        [Required(ErrorMessage = "Please select a student")]
        [Display(Name = "Student")]
        public string StudentId { get; set; } = "";

        [Required(ErrorMessage = "Please select a course")]
        [Display(Name = "Course")]
        public string CourseCode { get; set; } = "";

        public List<Student> Students { get; set; } = new();
        public List<Course> Courses { get; set; } = new();
        public List<Enrollment> EnrolledList { get; set; } = new();
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class GradeViewModel
    {
        [Display(Name = "Course")]
        public string CourseCode { get; set; } = "";

        [Display(Name = "Student")]
        public string StudentId { get; set; } = "";

        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
        [Display(Name = "Score")]
        public double Score { get; set; }

        public List<Course> Courses { get; set; } = new();
        public List<Student> Students { get; set; } = new();
        public List<GradeRecord> RecentGrades { get; set; } = new();
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        // New properties for editing flow
        public string? EditStudentId { get; set; }
        public string? EditStudentName { get; set; }
    }

    public class PaymentViewModel
    {
        [Display(Name = "Payment Method")]
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
