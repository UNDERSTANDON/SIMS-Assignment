using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Faculty" })]
    public class LecturerController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly MaterialHandler _materials;
        private readonly AssignmentHandler _assignments;

        public LecturerController(MaterialHandler materials, AssignmentHandler assignments)
        {
            _materials = materials;
            _assignments = assignments;
        }

        public IActionResult Index()
        {
            // For now show all courses; in future filter by lecturer identity
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Gi?ng viên";
            ViewData["ActivePage"] = "FacultyCourses";
            return View(_store.Courses);
        }

        public IActionResult Manage(string id)
        {
            var course = _store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();
            var vm = new LecturerCourseViewModel
            {
                Course = course,
                Materials = _materials.GetType().GetField("_materials", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) == null ? new List<Material>() : null
            };
            // Since MaterialHandler keeps private list, we'll expose materials via reflection for this demo
            var field = typeof(MaterialHandler).GetField("_materials", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                vm.Materials = field.GetValue(_materials) as List<Material> ?? new List<Material>();
            }

            var afield = typeof(AssignmentHandler).GetField("_assignments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            vm.Assignments = afield != null ? afield.GetValue(_assignments) as List<Assignment> ?? new List<Assignment>() : new List<Assignment>();

            // Filter lists by course id/code
            vm.Materials = vm.Materials.Where(m => m.CourseId == id).ToList();
            vm.Assignments = vm.Assignments.Where(a => a.CourseCode == id).ToList();

            ViewData["ActivePage"] = "FacultyCourses";
            return View(vm);
        }

        [HttpPost]
        public IActionResult AddMaterial(string courseId, string title, string description, IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Tiêu ?? không ???c ?? tr?ng";
                return RedirectToAction("Manage", new { id = courseId });
            }

            var mat = new Material
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title,
                Description = description ?? string.Empty,
                CourseId = courseId,
                UploadDate = DateTime.Now
            };

            if (file != null && file.Length > 0)
            {
                var storage = Path.Combine(AppContext.BaseDirectory, "DataStorage", "Materials");
                if (!Directory.Exists(storage)) Directory.CreateDirectory(storage);
                var fileName = mat.Id + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(storage, fileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                file.CopyTo(fs);
                mat.FilePath = Path.Combine("DataStorage", "Materials", fileName);
            }

            _materials.AddMaterial(mat);
            TempData["Success"] = "?ã thêm tài li?u thành công";
            return RedirectToAction("Manage", new { id = courseId });
        }

        [HttpPost]
        public IActionResult AddAssignment(string courseCode, string title, string description, DateTime dueDate)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Tiêu ?? không ???c ?? tr?ng";
                return RedirectToAction("Manage", new { id = courseCode });
            }
            var a = new Assignment
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title,
                Description = description ?? string.Empty,
                DueDate = dueDate,
                CourseCode = courseCode
            };
            _assignments.AddAssignment(a);
            TempData["Success"] = "?ã t?o bài t?p thành công";
            return RedirectToAction("Manage", new { id = courseCode });
        }
    }

    // ViewModel for the lecturer course management page
    public class LecturerCourseViewModel
    {
        public SIMS_WEB.Models.Course Course { get; set; } = new SIMS_WEB.Models.Course();
        public List<Material> Materials { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
    }
}
