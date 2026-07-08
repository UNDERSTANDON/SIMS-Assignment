using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_WEB.Storage;
using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class StudentCoursesController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly MaterialHandler _materials;
        private readonly AssignmentHandler _assignments;

        public StudentCoursesController(MaterialHandler materials, AssignmentHandler assignments)
        {
            _materials = materials;
            _assignments = assignments;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "StudentCourses";
            // Determine student id from session if present
            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;

            List<Course> courses;
            if (!string.IsNullOrEmpty(sessionStudent))
            {
                var enrolledCodes = _store.Enrollments.Where(e => e.StudentId == sessionStudent).Select(e => e.CourseCode).ToHashSet();
                courses = _store.Courses.Where(c => enrolledCodes.Contains(c.Code)).ToList();
            }
            else if (username == "sinhvien")
            {
                // default demo student account -> show all courses
                courses = _store.Courses;
            }
            else
            {
                // no mapping: show all courses but indicate enrollment status
                courses = _store.Courses;
            }

            return View(courses);
        }

        public IActionResult Details(string id)
        {
            ViewData["ActivePage"] = "StudentCourses";
            var course = _store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();

            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            string? studentId = sessionStudent;
            if (string.IsNullOrEmpty(studentId) && username == "sinhvien")
            {
                // demo student: map to first student record
                studentId = _store.Students.FirstOrDefault()?.StudentId;
            }

            bool enrolled = false;
            if (!string.IsNullOrEmpty(studentId))
            {
                enrolled = _store.Enrollments.Any(e => e.StudentId == studentId && e.CourseCode == id);
            }

            var vm = new StudentCourseDetailsViewModel
            {
                Course = course,
                IsEnrolled = enrolled,
                StudentId = studentId ?? string.Empty,
                Materials = _materials.GetAll().Where(m => m.CourseId == id).ToList(),
                Assignments = _assignments.GetAll().Where(a => a.CourseCode == id).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Enroll(string id)
        {
            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            string? studentId = sessionStudent;
            if (string.IsNullOrEmpty(studentId) && username == "sinhvien")
            {
                studentId = _store.Students.FirstOrDefault()?.StudentId;
            }
            if (string.IsNullOrEmpty(studentId))
            {
                TempData["Error"] = "Không xác ??nh sinh viên. Vui lòng ??ng nh?p b?ng tài kho?n sinh viên ho?c ch?n sinh viên.";
                return RedirectToAction("Details", new { id });
            }

            var (success, message) = _store.Enroll(studentId, id);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }
            // persist courses (enrolled counts changed)
            ModelFilePersistence.SaveCourses(_store.Courses);
            return RedirectToAction("Details", new { id });
        }
    }

    public class StudentCourseDetailsViewModel
    {
        public SIMS_WEB.Models.Course Course { get; set; } = new SIMS_WEB.Models.Course();
        public bool IsEnrolled { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public List<SIMS_Assignment.Models.CourseRelatedModels.Material> Materials { get; set; } = new();
        public List<SIMS_Assignment.Models.CourseRelatedModels.Assignment> Assignments { get; set; } = new();
    }
}
