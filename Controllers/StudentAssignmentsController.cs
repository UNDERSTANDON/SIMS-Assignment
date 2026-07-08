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
            model.StudentId = studentId ?? "";
            model.Students = _store.Students;

            var allAssignments = _assignments.GetAll();
            if (!string.IsNullOrEmpty(studentId))
            {
                var enrolledCourseCodes = _store.Enrollments.Where(e => e.StudentId == studentId).Select(e => e.CourseCode).ToHashSet();
                model.Assignments = allAssignments.Where(a => enrolledCourseCodes.Contains(a.CourseCode)).ToList();
            }
            else
            {
                // show none until student selected
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

            var vm = new SubmitViewModel
            {
                Assignment = assignment,
                Course = _store.Courses.FirstOrDefault(c => c.Code == courseCode) ?? new SIMS_WEB.Models.Course(),
                Students = _store.Students
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

            var submission = new Submission
            {
                Id = Guid.NewGuid().ToString("N"),
                StudentId = studentId,
                AssignmentTitle = assignment.Title,
                SubmissionDate = DateTime.Now,
                Content = content ?? string.Empty
            };

            // Handle file upload if provided
            if (file != null && file.Length > 0)
            {
                const long maxSubmissionFileSize = 10 * 1024 * 1024;
                var originalFileName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(originalFileName);
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
                    var fileName = submission.Id + extension;
                    var filePath = Path.Combine(storage, fileName);
                    await using var fs = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(fs);
                    submission.FilePath = Path.Combine("DataStorage", "Submissions", fileName);
                    submission.OriginalFileName = originalFileName;                    Console.WriteLine($"Saved uploaded submission file to {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save submission file: {ex}");
                    TempData["Error"] = "Không thể lưu tệp được tải lên. Vui lòng thử lại hoặc liên hệ quản trị.";
                    return RedirectToAction("Submit", new { courseCode = assignment.CourseCode, assignmentId });
                }
            }

            _submissions.AddSubmission(submission);
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
    }
}
