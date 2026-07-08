using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_WEB.Storage;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Services;

// Helper mapper for converting web course model to assignment course model
internal static class CourseMapper
{
    public static SIMS_Assignment.Models.Course ToAssignment(SIMS_WEB.Models.Course c)
    {
        return new SIMS_Assignment.Models.Course
        {
            CourseId = c.Code,
            CourseName = c.Title,
            Credits = c.Capacity,
            LecturerId = 0,
            EnrolledStudentIds = new List<int>()
        };
    }
}

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class DashboardController : Controller
    {
        private readonly IDataStorage _storage;

        public DashboardController(IDataStorage storage)
        {
            _storage = storage;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role") ?? "Admin";
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            ViewBag.Role = role;
            ViewBag.Username = username;
            var store = SimsDataStore.Instance;
            ViewBag.TotalStudents = store.Students.Count;
            ViewBag.TotalCourses = store.Courses.Count;
            ViewBag.TotalEnrolled = store.Enrollments.Count;
            return View();
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class StudentsController : Controller
    {
        private SimsDataStore Store => SimsDataStore.Instance;
        private readonly IStudentManager _students;

        public StudentsController(IStudentManager students)
        {
            _students = students;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var list = await _students.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(search))
                list = list.Where(s => s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || s.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            ViewBag.Search = search;
            ViewBag.Total = list.Count;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Student());

        [HttpPost]
        public async Task<IActionResult> Create(Student model)
        {
            if (!ModelState.IsValid) return View(model);
            var ok = await _students.CreateAsync(model);
            if (!ok)
            {
                ModelState.AddModelError("StudentId", "Mã sinh viên đã tồn tại trong hệ thống");
                return View(model);
            }
            TempData["Success"] = $"Đã thêm sinh viên {model.FullName} thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var student = await _students.GetByIdAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Student model)
        {
            if (!ModelState.IsValid) return View(model);
            var ok = await _students.UpdateAsync(model);
            if (!ok) return NotFound();
            TempData["Success"] = "Cập nhật sinh viên thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var removed = await _students.DeleteAsync(id);
            if (removed)
            {
                TempData["Success"] = "Đã xóa sinh viên thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy sinh viên để xóa";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ImportCsv(IFormFile? csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file CSV hợp lệ";
                return RedirectToAction("Index");
            }
            var count = await _students.ImportFromStreamAsync(csvFile.OpenReadStream());
            TempData["Success"] = $"Import thành công {count} sinh viên từ CSV!";
            return RedirectToAction("Index");
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class CoursesController : Controller
    {
        private readonly ICourseManager _courses;

        public CoursesController(ICourseManager courses)
        {
            _courses = courses;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _courses.GetAllAsync();
            ViewBag.Total = list.Count;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Course());

        [HttpPost]
        public async Task<IActionResult> Create(Course model)
        {
            if (!ModelState.IsValid) return View(model);
            var ok = await _courses.CreateAsync(model);
            if (!ok)
            {
                ModelState.AddModelError("Code", "Mã khóa học đã tồn tại");
                return View(model);
            }
            TempData["Success"] = $"Đã thêm khóa học {model.Title} thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var course = await _courses.GetByCodeAsync(id);
            if (course == null) return NotFound();
            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Course model)
        {
            if (!ModelState.IsValid) return View(model);
            var ok = await _courses.UpdateAsync(model);
            if (!ok) return NotFound();
            TempData["Success"] = "Cập nhật khóa học thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var removed = await _courses.DeleteAsync(id);
            if (removed)
            {
                TempData["Success"] = "Đã xóa khóa học thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy khóa học để xóa";
            }
            return RedirectToAction("Index");
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class EnrollmentController : Controller
    {
        private readonly IEnrollmentManager _enrollments;

        public EnrollmentController(IEnrollmentManager enrollments)
        {
            _enrollments = enrollments;
        }

        public async Task<IActionResult> Index()
        {
            var store = SimsDataStore.Instance;
            var vm = new EnrollmentViewModel
            {
                Students = store.Students,
                Courses = store.Courses,
                EnrolledList = await _enrollments.GetEnrollmentsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(EnrollmentViewModel model)
        {
            var (success, message) = await _enrollments.EnrollAsync(model.StudentId, model.CourseCode);
            TempData["Message"] = message;
            if (success)
                TempData["Success"] = message;
            else
                TempData["Error"] = message;

            // Redirect to Index so the page re-fetches fresh data from storage/managers
            return RedirectToAction(nameof(Index));
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin", "Faculty" })]
    public class GradesController : Controller
    {
        public IActionResult Index()
        {
            var store = SimsDataStore.Instance;
            var vm = new GradeViewModel
            {
                Courses = store.Courses,
                Students = store.Students,
                RecentGrades = store.Grades.OrderByDescending(g => g.UpdatedAt).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Save(GradeViewModel model)
        {
            var store = SimsDataStore.Instance;
            if (model.Score < 0 || model.Score > 100)
                ModelState.AddModelError("Score", "Điểm phải từ 0 đến 100");

            if (ModelState.IsValid)
            {
                store.SaveGrade(model.StudentId, model.CourseCode, model.Score);
                model.Message = $"Đã lưu điểm thành công! Observer đã gửi thông báo tới sinh viên.";
                model.IsSuccess = true;
            }
            model.Courses = store.Courses;
            model.Students = store.Students;
            model.RecentGrades = store.Grades.OrderByDescending(g => g.UpdatedAt).ToList();
            return View("Index", model);
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class StudentDashboardController : Controller
    {
        public IActionResult Index()
        {
            var store = SimsDataStore.Instance;
            ViewBag.StudentName = store.Students.FirstOrDefault()?.FullName ?? "Sinh viên";
            ViewBag.Grades = store.Grades.ToList();
            ViewBag.Courses = store.Courses;
            return View();
        }

        public IActionResult CheckUpdates(int count)
        {
            var total = SimsDataStore.Instance.Grades.Count;
            return Json(new { hasNew = total > count, count = total });
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class PaymentController : Controller
    {
        public IActionResult Index() => View(new PaymentViewModel());

        [HttpPost]
        public IActionResult Process(PaymentViewModel model)
        {
            // Strategy Pattern — minimal stub
            model.IsSuccess = true;
            model.Message = model.Method switch
            {
                "credit"  => $"Thanh toán thẻ tín dụng thành công! Tổng: {model.Total:N0} VNĐ (phí 2.5%)",
                "bank"    => $"Chuyển khoản ngân hàng thành công! Tổng: {model.Total:N0} VNĐ (miễn phí)",
                "ewallet" => $"Thanh toán ví điện tử thành công! Tổng: {model.Total:N0} VNĐ (phí 1.0%)",
                _         => "Thanh toán thành công!"
            };
            return View("Index", model);
        }
    }
}
