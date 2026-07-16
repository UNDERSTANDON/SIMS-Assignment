using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_Assignment.Services;
using SIMS_Assignment.Models.CourseRelatedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using SIMS_Assignment.Abstract;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Faculty" })]
    public class LecturerController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly MaterialHandler _materials;
        private readonly AssignmentHandler _assignments;
        private readonly IEnrollmentManager _enrollmentManager;
        private readonly IWebHostEnvironment _env;
        private readonly IDataStorage _storage;
        private readonly ICourseManager _courseManager;

        public LecturerController(MaterialHandler materials, AssignmentHandler assignments, IEnrollmentManager enrollmentManager, IWebHostEnvironment env, IDataStorage storage, ICourseManager courseManager)
        {
            _materials = materials;
            _assignments = assignments;
            _enrollmentManager = enrollmentManager;
            _env = env;
            _storage = storage;
            _courseManager = courseManager;
        }

        public IActionResult Submissions(string assignmentId)
        {
            var assignment = _assignments.GetAll().FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null) return NotFound();
            var subsField = typeof(SubmissionHandler).GetField("_submissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            List<Submission> all = new();
            if (subsField != null)
            {
                var sh = HttpContext.RequestServices.GetService(typeof(SubmissionHandler)) as SubmissionHandler;
                if (sh != null)
                {
                    all = sh.GetByAssignment(assignment.Title);
                }
            }
            return View("Submissions", all);
        }

        private async Task<bool> IsCourseAssignedToLecturer(string courseCode, string username)
        {
            var user = await _storage.GetUserByNameAsync(username);
            if (user == null) return false;

            var course = _store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null) return false;

            var instructorList = course.Instructor?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(i => i.Trim()).ToList() ?? new List<string>();
            return instructorList.Any(inst =>
                inst.Equals(user.FullName, StringComparison.OrdinalIgnoreCase) ||
                inst.Equals(user.Name, StringComparison.OrdinalIgnoreCase)
            );
        }

        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            var user = await _storage.GetUserByNameAsync(username);

            ViewBag.Username = user?.FullName ?? username;
            ViewData["ActivePage"] = "FacultyCourses";

            if (user == null)
            {
                return View(new List<Course>());
            }

            var assignedCourses = _store.Courses.Where(course => {
                var instructorList = course.Instructor?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim()).ToList() ?? new List<string>();
                return instructorList.Any(inst =>
                    inst.Equals(user.FullName, StringComparison.OrdinalIgnoreCase) ||
                    inst.Equals(user.Name, StringComparison.OrdinalIgnoreCase)
                );
            }).ToList();

            return View(assignedCourses);
        }

        public async Task<IActionResult> Enrollments(string courseCode)
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            if (!await IsCourseAssignedToLecturer(courseCode, username))
            {
                TempData["Error"] = "Bạn không có quyền truy cập khóa học này.";
                return RedirectToAction(nameof(Index));
            }

            var course = _store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null) return NotFound();

            var enrolledStudents = await _enrollmentManager.GetEnrolledStudentsByCourseAsync(courseCode);
            var enrollmentCount = await _enrollmentManager.GetEnrollmentCountAsync(courseCode);

            var vm = new CourseEnrollmentsViewModel
            {
                Course = course,
                EnrolledStudents = enrolledStudents,
                EnrollmentCount = enrollmentCount
            };

            ViewData["ActivePage"] = "FacultyCourses";
            return View(vm);
        }

        public async Task<IActionResult> Manage(string id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            if (!await IsCourseAssignedToLecturer(id, username))
            {
                TempData["Error"] = "Bạn không có quyền truy cập khóa học này.";
                return RedirectToAction(nameof(Index));
            }

            var course = _store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();
            var vm = new LecturerCourseViewModel
            {
                Course = course,
                Materials = _materials.GetType().GetField("_materials", System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance) == null ? new List<Material>() : null
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
        public async Task<IActionResult> AddMaterial(string courseId, string title, string description, IFormFile? file)
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            if (!await IsCourseAssignedToLecturer(courseId, username))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Tiêu đề không được để trống";
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
                try
                {
                    // Use the content root path provided by the host environment so the app writes
                    // files relative to the application's folder and not to a runtime-specific base.
                    var storage = Path.Combine(_env.ContentRootPath, "DataStorage", "Materials");
                    if (!Directory.Exists(storage)) Directory.CreateDirectory(storage);
                    var fileName = mat.Id + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(storage, fileName);
                    await using var fs = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(fs);
                    mat.FilePath = Path.Combine("DataStorage", "Materials", fileName);
                    mat.OriginalFileName = file.FileName;
                    Console.WriteLine($"Saved uploaded file to {filePath}");

                    // Save file mapping to JSON for retrieval
                    _materials.SaveFileMapping(mat.Id, file.FileName, fileName, courseId);
                }
                catch (Exception ex)
                {
                    // Prevent an unhandled exception from taking down the process. Surface a friendly
                    // error message to the user and log the exception to console (startup logging will
                    // also capture it via the AppDomain handlers in Program.cs).
                    Console.WriteLine($"Failed to save uploaded file: {ex}");
                    TempData["Error"] = "Không thể lưu tệp được tải lên. Vui lòng thử lại hoặc liên hệ quản trị.";
                    return RedirectToAction("Manage", new { id = courseId });
                }
            }

            _materials.AddMaterial(mat);
            TempData["Success"] = "Đã thêm tài liệu thành công";
            return RedirectToAction("Manage", new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> AddAssignment(string courseCode, string title, string description, DateTime dueDate)
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            if (!await IsCourseAssignedToLecturer(courseCode, username))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Tiêu đề không được để trống";
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
            TempData["Success"] = "Đã tạo bài tập thành công";
            return RedirectToAction("Manage", new { id = courseCode });
        }

        public IActionResult DownloadMaterial(string materialId)
        {
            try
            {
                // Get the material from the handler
                var material = _materials.GetAll().FirstOrDefault(m => m.Id == materialId);
                if (material == null)
                    return NotFound("Tài liệu không tìm thấy");

                // Get the file mapping with original filename
                var fileMapping = _materials.GetFileMapping(materialId);

                // Build the full file path
                if (string.IsNullOrWhiteSpace(material.FilePath))
                    return BadRequest("Tài liệu này không có tệp đính kèm");

                var filePath = Path.Combine(_env.ContentRootPath, material.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("Tệp không tìm thấy trên máy chủ");

                // Read the file and return it with the original filename
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileName = fileMapping?.OriginalFileName
                    ?? material.OriginalFileName
                    ?? Path.GetFileName(material.FilePath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading material: {ex.Message}");
                return StatusCode(500, "Lỗi tải tệp");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        public IActionResult DownloadSubmission(string submissionId)
        {
            try
            {
                // Get the SubmissionHandler from dependency injection
                var submissionHandler = HttpContext.RequestServices.GetService(typeof(SubmissionHandler)) as SubmissionHandler;
                if (submissionHandler == null)
                    return StatusCode(500, "Không thể tải dịch vụ xử lý bài nộp");

                // Get the submission
                var submission = submissionHandler.GetById(submissionId);
                if (submission == null)
                    return NotFound("Bài nộp không tìm thấy");

                if (string.IsNullOrEmpty(submission.FilePath))
                    return BadRequest("Bài nộp này không có tệp đính kèm");

                // Build the full file path
                var filePath = Path.Combine(_env.ContentRootPath, submission.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("Tệp không tìm thấy trên máy chủ");

                // Read the file and return it with the original filename
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileName = submission.OriginalFileName ?? Path.GetFileName(submission.FilePath);
                var contentType = GetContentType(fileName);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading submission: {ex.Message}");
                return StatusCode(500, "Lỗi tải tệp");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            ViewData["ActivePage"] = "Profile";
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var user = await _storage.GetUserByNameAsync(username);
            if (user == null) return NotFound("Giảng viên không tồn tại trong hệ thống");
            return View("Profile", user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(string fullName, string email, string? avatarBase64)
        {
            ViewData["ActivePage"] = "Profile";
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var user = await _storage.GetUserByNameAsync(username);
            if (user == null) return NotFound("Giảng viên không tồn tại trong hệ thống");

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

            if (!string.IsNullOrEmpty(oldName) && oldName != user.FullName)
            {
                foreach (var course in _store.Courses)
                {
                    if (course.Instructor == oldName)
                    {
                        course.Instructor = user.FullName;
                    }
                }
                SIMS_WEB.Storage.ModelFilePersistence.SaveCourses(_store.Courses);
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

        [HttpGet]
        public IActionResult CreateCourse()
        {
            TempData["Error"] = "Bạn không có quyền thực hiện thao tác này. Chỉ Admin mới có quyền quản lý khóa học.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CreateCourse(Course model)
        {
            TempData["Error"] = "Bạn không có quyền thực hiện thao tác này. Chỉ Admin mới có quyền quản lý khóa học.";
            return RedirectToAction("Index");
        }

        public IActionResult CourseAssignments(string courseCode, string? assignmentId)
        {
            ViewData["ActivePage"] = "FacultyCourses";
            var course = _store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null) return NotFound("Khóa học không tồn tại");

            var allAssignments = _assignments.GetAll().Where(a => a.CourseCode == courseCode).ToList();
            var selectedAssignment = !string.IsNullOrEmpty(assignmentId) 
                ? allAssignments.FirstOrDefault(a => a.Id == assignmentId)
                : allAssignments.FirstOrDefault();

            var enrollmentStudentIds = _store.Enrollments.Where(e => e.CourseCode == courseCode).Select(e => e.StudentId).ToHashSet();
            var enrolledStudents = _store.Students.Where(s => enrollmentStudentIds.Contains(s.StudentId)).ToList();

            List<Submission> submissions = new();
            if (selectedAssignment != null)
            {
                var sh = HttpContext.RequestServices.GetService(typeof(SubmissionHandler)) as SubmissionHandler;
                if (sh != null)
                {
                    submissions = sh.GetByAssignment(selectedAssignment.Title);
                }
            }

            ViewBag.Course = course;
            ViewBag.Assignments = allAssignments;
            ViewBag.SelectedAssignment = selectedAssignment;
            ViewBag.Submissions = submissions;

            return View(enrolledStudents);
        }
    }

    // ViewModel for the lecturer course management page
    public class LecturerCourseViewModel
    {
        public SIMS_WEB.Models.Course Course { get; set; } = new SIMS_WEB.Models.Course();
        public List<Material> Materials { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
    }

    // ViewModel for the course enrollments page
    public class CourseEnrollmentsViewModel
    {
        public SIMS_WEB.Models.Course Course { get; set; } = new SIMS_WEB.Models.Course();
        public List<Student> EnrolledStudents { get; set; } = new();
        public int EnrollmentCount { get; set; }
    }
}
