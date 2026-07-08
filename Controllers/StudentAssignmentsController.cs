using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class StudentAssignmentsController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly AssignmentHandler _assignments;
        private readonly SubmissionHandler _submissions;

        public StudentAssignmentsController(AssignmentHandler assignments, SubmissionHandler submissions)
        {
            _assignments = assignments;
            _submissions = submissions;
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
        public IActionResult Submit(string assignmentId, string studentId, string content)
        {
            var assignment = _assignments.GetAll().FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null) return NotFound();
            if (string.IsNullOrWhiteSpace(studentId))
            {
                TempData["Error"] = "Vui lòng ch?n sinh viên";
                return RedirectToAction("Submit", new { courseCode = assignment.CourseCode, assignmentId });
            }

            var submission = new Submission
            {
                StudentId = studentId,
                AssignmentTitle = assignment.Title,
                SubmissionDate = DateTime.Now,
                Content = content ?? string.Empty
            };
            _submissions.AddSubmission(submission);
            TempData["Success"] = "N?p bài thành công";
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
