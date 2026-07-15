using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_Assignment.Models.CourseRelatedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class StudentAssignmentsController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly AssignmentHandler _assignments;
        private readonly SubmissionHandler _submissions;
        private readonly IWebHostEnvironment _env;

        public StudentAssignmentsController(AssignmentHandler assignments, SubmissionHandler submissions, IWebHostEnvironment env)
        {
            _assignments = assignments;
            _submissions = submissions;
            _env = env;
        }

        // List all assignments for courses where the student is enrolled. If no studentId provided, allow choosing.
        public IActionResult Index(string? studentId)
        {
            ViewData["ActivePage"] = "StudentAssignments";
            var model = new StudentAssignmentsViewModel();

            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;

            model.StudentId = studentId ?? sessionStudent ?? "";
            if (string.IsNullOrEmpty(model.StudentId) && username == "sinhvien")
            {
                model.StudentId = _store.Students.FirstOrDefault()?.StudentId ?? "";
            }

            model.Students = _store.Students;

            var allAssignments = _assignments.GetAll();
            if (!string.IsNullOrEmpty(model.StudentId))
            {
                var enrolledCourseCodes = _store.Enrollments.Where(e => e.StudentId == model.StudentId).Select(e => e.CourseCode).ToHashSet();
                model.Assignments = allAssignments.Where(a => enrolledCourseCodes.Contains(a.CourseCode)).ToList();
            }
            else
            {
                model.Assignments = new List<Assignment>();
            }

            // attach course info
            model.Courses = _store.Courses;
            return View(model);
        }

        [HttpGet]
        public IActionResult Submit(string courseCode, string assignmentId)
        {
            var assignment = _assignments.GetAll().FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null) return NotFound();

            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            string studentId = sessionStudent;
            if (string.IsNullOrEmpty(studentId) && username == "sinhvien")
            {
                studentId = _store.Students.FirstOrDefault()?.StudentId;
            }

            var enrolledCodes = _store.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseCode)
                .ToHashSet();

            var enrolledCourses = _store.Courses
                .Where(c => enrolledCodes.Contains(c.Code))
                .ToList();

            var existingSubmission = _submissions.GetAll()
                .FirstOrDefault(s => s.StudentId == studentId && s.AssignmentTitle == assignment.Title);

            var vm = new SubmitViewModel
            {
                Assignment = assignment,
                Course = _store.Courses.FirstOrDefault(c => c.Code == courseCode) ?? new SIMS_WEB.Models.Course(),
                Students = _store.Students,
                StudentId = studentId ?? string.Empty,
                EnrolledCourses = enrolledCourses,
                ExistingSubmission = existingSubmission
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(string assignmentId, string studentId, string content, IFormFile? file)
        {
            var assignment = _assignments.GetAll().FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null) return NotFound();
            if (string.IsNullOrWhiteSpace(studentId))
            {
                TempData["Error"] = "Vui lòng chọn sinh viên";
                return RedirectToAction("Submit", new { courseCode = assignment.CourseCode, assignmentId });
            }

            // Find existing submission or create new
            var existing = _submissions.GetAll()
                .FirstOrDefault(s => s.StudentId == studentId && s.AssignmentTitle == assignment.Title);

            string submissionId = existing?.Id ?? Guid.NewGuid().ToString("N");
            string? filePath = existing?.FilePath;
            string? originalFileName = existing?.OriginalFileName;

            // Handle file upload if provided
            if (file != null && file.Length > 0)
            {
                const long maxSubmissionFileSize = 10 * 1024 * 1024;
                var uploadName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(uploadName);
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".zip" };

                if (file.Length > maxSubmissionFileSize ||
                    !allowedExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Error"] = "Tệp tải lên không hợp lệ hoặc vượt quá dung lượng cho phép.";
                    return RedirectToAction("Submit", new { courseCode = assignment.CourseCode, assignmentId });
                }

                try
                {
                    var storage = Path.Combine(_env.ContentRootPath, "DataStorage", "Submissions");
                    if (!Directory.Exists(storage)) Directory.CreateDirectory(storage);
                    var fileName = submissionId + extension;
                    var fullPath = Path.Combine(storage, fileName);
                    await using var fs = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(fs);
                    filePath = Path.Combine("DataStorage", "Submissions", fileName);
                    originalFileName = uploadName;
                    Console.WriteLine($"Saved uploaded submission file to {fullPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save submission file: {ex}");
                    TempData["Error"] = "Không thể lưu tệp được tải lên. Vui lòng thử lại hoặc liên hệ quản trị.";
                    return RedirectToAction("Submit", new { courseCode = assignment.CourseCode, assignmentId });
                }
            }

            if (existing != null)
            {
                existing.SubmissionDate = DateTime.Now;
                existing.Content = content ?? string.Empty;
                existing.FilePath = filePath;
                existing.OriginalFileName = originalFileName;
                _submissions.EditSubmission(existing);
            }
            else
            {
                var submission = new Submission
                {
                    Id = submissionId,
                    StudentId = studentId,
                    AssignmentTitle = assignment.Title,
                    SubmissionDate = DateTime.Now,
                    Content = content ?? string.Empty,
                    FilePath = filePath,
                    OriginalFileName = originalFileName
                };
                _submissions.AddSubmission(submission);
            }

            TempData["Success"] = "Nộp bài thành công";
            return RedirectToAction("Index", new { studentId });
        }
    }

    public class StudentAssignmentsViewModel
    {
        public string StudentId { get; set; } = "";
        public List<Student> Students { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
        public List<SIMS_WEB.Models.Course> Courses { get; set; } = new();
    }

    public class SubmitViewModel
    {
        public Assignment Assignment { get; set; } = new Assignment();
        public SIMS_WEB.Models.Course Course { get; set; } = new SIMS_WEB.Models.Course();
        public List<Student> Students { get; set; } = new();
        public string StudentId { get; set; } = string.Empty;
        public List<SIMS_WEB.Models.Course> EnrolledCourses { get; set; } = new();
        public Submission? ExistingSubmission { get; set; }
    }
}
