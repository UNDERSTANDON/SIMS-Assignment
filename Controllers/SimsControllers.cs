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

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role") ?? "Admin";
            var username = HttpContext.Session.GetString("Username") ?? "Admin";
            ViewBag.Role = role;
            ViewBag.Username = username;
            var store = SimsDataStore.Instance;
            ViewBag.TotalStudents = store.Students.Count;
            ViewBag.TotalCourses = store.Courses.Count;
            ViewBag.TotalEnrolled = store.Enrollments.Count;

            var allUsers = await _storage.GetAllUsersAsync();
            ViewBag.TotalLecturers = allUsers.Count(u => u.Role == "Faculty" || u.Role == "Lecturer");
            ViewBag.TotalAdmins = allUsers.Count(u => u.Role == "Admin");

            return View();
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class StudentsController : Controller
    {
        private SimsDataStore Store => SimsDataStore.Instance;
        private readonly IStudentManager _students;
        private readonly IDataStorage _storage;

        public StudentsController(IStudentManager students, IDataStorage storage)
        {
            _students = students;
            _storage = storage;
        }

        public async Task<IActionResult> Index(string? search, string tab = "student")
        {
            var students = await _students.GetAllAsync();
            var allUsers = await _storage.GetAllUsersAsync();
            var store = SimsDataStore.Instance;
            var lecturers = allUsers.Where(u => u.Role == "Faculty" || u.Role == "Lecturer")
                .Select(u => {
                    var fullname = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Name;
                    var username = u.Name;
                    
                    var matchedCourses = store.Courses.Where(c => 
                        (!string.IsNullOrEmpty(c.Instructor) && (
                            c.Instructor.Contains(fullname, StringComparison.OrdinalIgnoreCase) || 
                            c.Instructor.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                            fullname.Contains(c.Instructor, StringComparison.OrdinalIgnoreCase)
                        ))
                    ).Select(c => c.Title).ToList();

                    var coursesText = matchedCourses.Any() ? string.Join("|", matchedCourses) : "null";

                    return new SIMS_WEB.Models.Student
                    {
                        StudentId = username,
                        FullName = fullname,
                        Email = u.Email,
                        Program = coursesText
                    };
                }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                students = students.Where(s => s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                            || s.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase)
                                            || (s.Email != null && s.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                                            || (s.Program != null && s.Program.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();

                lecturers = lecturers.Where(l => l.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                              || (l.Email != null && l.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                                              || l.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase)
                                              || (l.Program != null && l.Program.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            ViewBag.Search = search;
            ViewBag.Tab = tab;
            ViewBag.Lecturers = lecturers;
            ViewBag.Students = students;
            ViewBag.StudentsCount = students.Count;
            ViewBag.LecturersCount = lecturers.Count;

            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var students = await _students.GetAllAsync();
            var nextId = AccountController.GenerateNextStudentId(students.Select(s => s.StudentId));
            return View(new Student { StudentId = nextId });
        }

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

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string username)
        {
            var ok = await _storage.DeleteUserByNameAsync(username);
            
            // Sync delete student profile if they also exist in student database
            var store = SimsDataStore.Instance;
            var studentObj = store.Students.FirstOrDefault(s => string.Equals(s.StudentId, username, StringComparison.OrdinalIgnoreCase)
                                                               || string.Equals(s.FullName, username, StringComparison.OrdinalIgnoreCase));
            if (studentObj != null)
            {
                await _students.DeleteAsync(studentObj.StudentId);
            }

            if (ok)
                TempData["Success"] = $"Đã xóa người dùng {username} thành công!";
            else
                TempData["Error"] = "Không tìm thấy người dùng để xóa";

            return RedirectToAction("Index", new { tab = "faculty" });
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin" })]
    public class CoursesController : Controller
    {
        private readonly ICourseManager _courses;
        private readonly IDataStorage _storage;

        public CoursesController(ICourseManager courses, IDataStorage storage)
        {
            _courses = courses;
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _courses.GetAllAsync();
            ViewBag.Total = list.Count;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var users = await _storage.GetAllUsersAsync();
            ViewBag.Lecturers = users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").ToList();
            return View(new Course());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Course model)
        {
            if (!ModelState.IsValid)
            {
                var users = await _storage.GetAllUsersAsync();
                ViewBag.Lecturers = users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").ToList();
                return View(model);
            }
            var ok = await _courses.CreateAsync(model);
            if (!ok)
            {
                var users = await _storage.GetAllUsersAsync();
                ViewBag.Lecturers = users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").ToList();
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

            var users = await _storage.GetAllUsersAsync();
            ViewBag.Lecturers = users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").ToList();

            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Course model)
        {
            if (!ModelState.IsValid)
            {
                var users = await _storage.GetAllUsersAsync();
                ViewBag.Lecturers = users.Where(u => u.Role == "Faculty" || u.Role == "Lecturer").ToList();
                return View(model);
            }
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

        public IActionResult Enrollments(string id)
        {
            var store = SimsDataStore.Instance;
            var course = store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();

            var enrolledStudents = store.Enrollments
                .Where(e => e.CourseCode == id && e.IsEnrolled)
                .Join(store.Students, 
                    e => e.StudentId, 
                    s => s.StudentId, 
                    (e, s) => new { Enrollment = e, Student = s })
                .Select(x => x.Student)
                .ToList();

            ViewBag.Course = course;
            ViewBag.TotalEnrolled = enrolledStudents.Count;
            ViewBag.EnrolledCount = course.EnrolledCount;
            return View(enrolledStudents);
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
            ViewBag.Grades = store.Grades.ToList();
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

        [HttpPost]
        public async Task<IActionResult> Unenroll(string studentId, string courseCode)
        {
            var ok = await _enrollments.UnenrollAsync(studentId, courseCode);
            if (ok)
                TempData["Success"] = "Đã hủy ghi danh thành công";
            else
                TempData["Error"] = "Hủy ghi danh thất bại hoặc không tìm thấy lượt ghi danh";

            return RedirectToAction(nameof(Index));
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Admin", "Faculty" })]
    public class GradesController : Controller
    {
        private readonly IEnrollmentManager _enrollments;
        private readonly IDataStorage _storage;

        public GradesController(IEnrollmentManager enrollments, IDataStorage storage)
        {
            _enrollments = enrollments;
            _storage = storage;
        }

        public async Task<IActionResult> Index(string? courseCode, string? editStudentId)
        {
            var store = SimsDataStore.Instance;
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username") ?? "";

            // 1. Get filtered list of courses
            var courses = store.Courses.ToList();
            if (role == "Faculty")
            {
                var user = await _storage.GetUserByNameAsync(username);
                if (user != null)
                {
                    courses = courses.Where(course => {
                        var instructorList = course.Instructor?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(i => i.Trim()).ToList() ?? new List<string>();
                        return instructorList.Any(inst =>
                            inst.Equals(user.FullName, StringComparison.OrdinalIgnoreCase) ||
                            inst.Equals(user.Name, StringComparison.OrdinalIgnoreCase)
                        );
                    }).ToList();
                }
                else
                {
                    courses = new List<Course>();
                }
            }

            // 2. Get students in the selected course (if any)
            var students = new List<Student>();
            if (!string.IsNullOrEmpty(courseCode))
            {
                students = await _enrollments.GetEnrolledStudentsByCourseAsync(courseCode);
            }

            // 3. Setup edit student info if requested
            string? editStudentName = null;
            double editScore = 0;
            if (!string.IsNullOrEmpty(courseCode) && !string.IsNullOrEmpty(editStudentId))
            {
                var editStudent = students.FirstOrDefault(s => s.StudentId == editStudentId);
                if (editStudent != null)
                {
                    editStudentName = editStudent.FullName;
                    var gradeRecord = store.Grades.FirstOrDefault(g => g.CourseCode == courseCode && g.StudentId == editStudentId);
                    editScore = gradeRecord?.Score ?? 0;
                }
            }

            var vm = new GradeViewModel
            {
                CourseCode = courseCode ?? "",
                Courses = courses,
                Students = students,
                RecentGrades = store.Grades.OrderByDescending(g => g.UpdatedAt).ToList(),
                EditStudentId = editStudentId,
                EditStudentName = editStudentName,
                Score = editScore
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Save(GradeViewModel model)
        {
            var store = SimsDataStore.Instance;
            if (model.Score < 0 || model.Score > 100)
                ModelState.AddModelError("Score", "Điểm phải từ 0 đến 100");

            if (ModelState.IsValid)
            {
                store.SaveGrade(model.StudentId, model.CourseCode, model.Score);
                TempData["Success"] = $"Đã lưu điểm cho sinh viên thành công! Observer đã gửi thông báo tới sinh viên.";
                return RedirectToAction(nameof(Index), new { courseCode = model.CourseCode });
            }

            // If invalid, reload index with validation errors
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username") ?? "";

            var courses = store.Courses.ToList();
            if (role == "Faculty")
            {
                var user = await _storage.GetUserByNameAsync(username);
                if (user != null)
                {
                    courses = courses.Where(course => {
                        var instructorList = course.Instructor?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(i => i.Trim()).ToList() ?? new List<string>();
                        return instructorList.Any(inst =>
                            inst.Equals(user.FullName, StringComparison.OrdinalIgnoreCase) ||
                            inst.Equals(user.Name, StringComparison.OrdinalIgnoreCase)
                        );
                    }).ToList();
                }
                else
                {
                    courses = new List<Course>();
                }
            }

            var students = new List<Student>();
            if (!string.IsNullOrEmpty(model.CourseCode))
            {
                students = await _enrollments.GetEnrolledStudentsByCourseAsync(model.CourseCode);
            }

            model.Courses = courses;
            model.Students = students;
            model.RecentGrades = store.Grades.OrderByDescending(g => g.UpdatedAt).ToList();
            return View("Index", model);
        }

        [HttpPost]
        public IActionResult DeleteGrade(string courseCode, string studentId)
        {
            var store = SimsDataStore.Instance;
            var removed = store.Grades.RemoveAll(g => g.StudentId == studentId && g.CourseCode == courseCode);
            if (removed > 0)
            {
                TempData["Success"] = "Đã xóa điểm số của sinh viên thành công.";
                try
                {
                    SIMS_WEB.Storage.ModelFilePersistence.SaveGrades(store.Grades);
                }
                catch { }
            }
            else
            {
                TempData["Error"] = "Không tìm thấy điểm số của sinh viên để xóa.";
            }
            return RedirectToAction(nameof(Index), new { courseCode = courseCode });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(string courseCode, string studentId)
        {
            var ok = await _enrollments.UnenrollAsync(studentId, courseCode);
            if (ok)
            {
                SimsDataStore.Instance.Grades.RemoveAll(g => g.StudentId == studentId && g.CourseCode == courseCode);
                TempData["Success"] = "Đã xóa sinh viên khỏi lớp học thành công.";
            }
            else
            {
                TempData["Error"] = "Xóa sinh viên khỏi lớp học thất bại.";
            }
            return RedirectToAction(nameof(Index), new { courseCode = courseCode });
        }
    }

    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class StudentDashboardController : Controller
    {
        private readonly IDataStorage _storage;
        private readonly IWebHostEnvironment _env;

        public StudentDashboardController(IDataStorage storage, IWebHostEnvironment env)
        {
            _storage = storage;
            _env = env;
        }

        public IActionResult Index()
        {
            var store = SimsDataStore.Instance;
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var student = store.Students.FirstOrDefault(s => string.Equals(s.StudentId, username, StringComparison.OrdinalIgnoreCase));
            ViewBag.StudentName = student?.FullName ?? "Sinh viên";
            ViewBag.Grades = store.Grades.Where(g => string.Equals(g.StudentId, username, StringComparison.OrdinalIgnoreCase)).ToList();
            ViewBag.Courses = store.Courses;
            return View();
        }

        public IActionResult CheckUpdates(int count)
        {
            var store = SimsDataStore.Instance;
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var total = store.Grades.Count(g => string.Equals(g.StudentId, username, StringComparison.OrdinalIgnoreCase));
            return Json(new { hasNew = total > count, count = total });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            ViewData["ActivePage"] = "StudentProfile";
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var user = await _storage.GetUserByNameAsync(username);
            if (user == null) return NotFound("Sinh viên không tồn tại trong hệ thống");
            return View("Profile", user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(string fullName, string email, string? avatarBase64)
        {
            ViewData["ActivePage"] = "StudentProfile";
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var user = await _storage.GetUserByNameAsync(username);
            if (user == null) return NotFound("Sinh viên không tồn tại trong hệ thống");

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Họ tên không được để trống";
                return View("Profile", user);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email không được để trống";
                return View("Profile", user);
            }

            var oldName = user.FullName;
            user.FullName = fullName.Trim();
            user.Email = email.Trim();

            await _storage.SaveUserAsync(user);

            // Maintain login username in session instead of overwriting with full name
            HttpContext.Session.SetString("Username", user.Name);

            var store = SimsDataStore.Instance;
            var studentObj = store.Students.FirstOrDefault(s => s.StudentId == user.Name);
            if (studentObj != null)
            {
                studentObj.FullName = user.FullName;
                studentObj.Email = user.Email;
                try
                {
                    SIMS_WEB.Storage.ModelFilePersistence.SaveStudents(store.Students);
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(avatarBase64))
            {
                try
                {
                    var base64Data = avatarBase64;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Split(',')[1];
                    }
                    var imageBytes = System.Convert.FromBase64String(base64Data);

                    var avatarsDir = Path.Combine(SIMS_WEB.Storage.ModelFilePersistence.DataDir, "ProfilePictures");
                    if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

                    var filePath = Path.Combine(avatarsDir, $"{user.Name}.png");
                    System.IO.File.WriteAllBytes(filePath, imageBytes);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"Error saving avatar: {ex.Message}");
                }
            }

            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction("Profile");
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
