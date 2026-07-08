using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_WEB.Storage;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class DashboardController : Controller
    {
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

        public IActionResult Index(string? search)
        {
            var list = Store.Students.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                list = list.Where(s => s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || s.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase));
            ViewBag.Search = search;
            ViewBag.Total = Store.Students.Count;
            return View(list.ToList());
        }

        [HttpGet]
        public IActionResult Create() => View(new Student());

        [HttpPost]
        public IActionResult Create(Student model)
        {
            if (!ModelState.IsValid) return View(model);
            if (Store.Students.Any(s => s.StudentId == model.StudentId))
            {
                ModelState.AddModelError("StudentId", "Mã sinh viên đã tồn tại trong hệ thống");
                return View(model);
            }
            Store.Students.Add(model);
            // persist
            ModelFilePersistence.SaveStudents(Store.Students);
            TempData["Success"] = $"Đã thêm sinh viên {model.FullName} thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var student = Store.Students.FirstOrDefault(s => s.StudentId == id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        public IActionResult Edit(Student model)
        {
            if (!ModelState.IsValid) return View(model);
            var existing = Store.Students.FirstOrDefault(s => s.StudentId == model.StudentId);
            if (existing != null)
            {
                existing.FullName = model.FullName;
                existing.Program = model.Program;
                existing.Email = model.Email;
                existing.DateOfBirth = model.DateOfBirth;
            }
            // persist
            ModelFilePersistence.SaveStudents(Store.Students);
            TempData["Success"] = "Cập nhật sinh viên thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            var removed = Store.RemoveStudent(id);
            if (removed)
            {
                // persist students and courses because enrollments changed course counts
                ModelFilePersistence.SaveStudents(Store.Students);
                ModelFilePersistence.SaveCourses(Store.Courses);
                TempData["Success"] = "Đã xóa sinh viên thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy sinh viên để xóa";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ImportCsv(IFormFile? csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file CSV hợp lệ";
                return RedirectToAction("Index");
            }
            int count = 0;
            using var reader = new System.IO.StreamReader(csvFile.OpenReadStream());
            string? line;
            bool isHeader = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (isHeader) { isHeader = false; continue; }
                var parts = line.Split(',');
                if (parts.Length < 3) continue;
                var s = new Student
                {
                    StudentId = parts[0].Trim(),
                    FullName = parts[1].Trim(),
                    Program = parts[2].Trim(),
                    Email = parts.Length > 3 ? parts[3].Trim() : ""
                };
                if (!Store.Students.Any(x => x.StudentId == s.StudentId))
                {
                    Store.Students.Add(s);
                    count++;
                }
            }
            // persist
            ModelFilePersistence.SaveStudents(Store.Students);
            TempData["Success"] = $"Import thành công {count} sinh viên từ CSV!";
            return RedirectToAction("Index");
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class CoursesController : Controller
    {
        private SimsDataStore Store => SimsDataStore.Instance;

        public IActionResult Index()
        {
            ViewBag.Total = Store.Courses.Count;
            return View(Store.Courses);
        }

        [HttpGet]
        public IActionResult Create() => View(new Course());

        [HttpPost]
        public IActionResult Create(Course model)
        {
            if (!ModelState.IsValid) return View(model);
            if (Store.Courses.Any(c => c.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Mã khóa học đã tồn tại");
                return View(model);
            }
            Store.Courses.Add(model);
            // persist
            ModelFilePersistence.SaveCourses(Store.Courses);
            TempData["Success"] = $"Đã thêm khóa học {model.Title} thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var course = Store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();
            return View(course);
        }

        [HttpPost]
        public IActionResult Edit(Course model)
        {
            if (!ModelState.IsValid) return View(model);
            var existing = Store.Courses.FirstOrDefault(c => c.Code == model.Code);
            if (existing != null)
            {
                existing.Title = model.Title;
                existing.Capacity = model.Capacity;
                existing.Instructor = model.Instructor;
            }
            // persist
            ModelFilePersistence.SaveCourses(Store.Courses);
            TempData["Success"] = "Cập nhật khóa học thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            bool hasStudents = Store.Enrollments.Any(e => e.CourseCode == id);
            if (hasStudents)
            {
                TempData["Error"] = "Không thể xóa khóa học đang có sinh viên đăng ký!";
                return RedirectToAction("Index");
            }
            var removed = Store.RemoveCourse(id);
            if (removed)
            {
                ModelFilePersistence.SaveCourses(Store.Courses);
                // also persist students in case enrollments were removed
                ModelFilePersistence.SaveStudents(Store.Students);
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
        public IActionResult Index()
        {
            var store = SimsDataStore.Instance;
            var vm = new EnrollmentViewModel
            {
                Students = store.Students,
                Courses = store.Courses,
                EnrolledList = store.Enrollments
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Enroll(EnrollmentViewModel model)
        {
            var store = SimsDataStore.Instance;
            var (success, message) = store.Enroll(model.StudentId, model.CourseCode);
            model.Students = store.Students;
            model.Courses = store.Courses;
            model.EnrolledList = store.Enrollments;
            model.Message = message;
            model.IsSuccess = success;
            return View("Index", model);
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
