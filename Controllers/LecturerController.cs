﻿using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_Assignment.Models.CourseRelatedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Faculty" })]
    public class LecturerController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly MaterialHandler _materials;
        private readonly AssignmentHandler _assignments;
        private readonly IWebHostEnvironment _env;

        public LecturerController(MaterialHandler materials, AssignmentHandler assignments, IWebHostEnvironment env)
        {
            _materials = materials;
            _assignments = assignments;
            _env = env;
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

        public IActionResult Index()
        {
            // For now show all courses; in future filter by lecturer identity
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Giảng viên";
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
        public IActionResult AddAssignment(string courseCode, string title, string description, DateTime dueDate)
        {
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
    }

    // ViewModel for the lecturer course management page
    public class LecturerCourseViewModel
    {
        public SIMS_WEB.Models.Course Course { get; set; } = new SIMS_WEB.Models.Course();
        public List<Material> Materials { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
    }
}
