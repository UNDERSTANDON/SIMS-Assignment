using Microsoft.AspNetCore.Mvc;
using SIMS_WEB.Filters;
using SIMS_WEB.Models;
using SIMS_Assignment.Services.CourseServices;
using SIMS_WEB.Storage;
using SIMS_Assignment.Models.CourseRelatedModels;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.Collections.Generic;

namespace SIMS_WEB.Controllers
{
    [RequireLogin(AllowedRoles = new[] { "Student" })]
    public class StudentCoursesController : Controller
    {
        private readonly SimsDataStore _store = SimsDataStore.Instance;
        private readonly MaterialHandler _materials;
        private readonly AssignmentHandler _assignments;
        private readonly IWebHostEnvironment _env;

        public StudentCoursesController(MaterialHandler materials, AssignmentHandler assignments, IWebHostEnvironment env)
        {
            _materials = materials;
            _assignments = assignments;
            _env = env;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "StudentCourses";
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
                courses = _store.Courses;
            }
            else
            {
                courses = _store.Courses;
            }

            return View(courses);
        }

        public IActionResult Details(string id)
        {
            var course = _store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();

            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            string studentId = sessionStudent;
            if (string.IsNullOrEmpty(studentId) && username == "sinhvien")
            {
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

        public IActionResult Enroll(string id)
        {
            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            string studentId = sessionStudent;
            if (string.IsNullOrEmpty(studentId) && username == "sinhvien")
            {
                studentId = _store.Students.FirstOrDefault()?.StudentId;
            }

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Details", new { id });
            }

            var (success, message) = _store.Enroll(studentId, id);
            if (success)
            {
                try { ModelFilePersistence.SaveEnrollments(_store.Enrollments); } catch { }
            }
            ModelFilePersistence.SaveCourses(_store.Courses);
            return RedirectToAction("Details", new { id });
        }

        public IActionResult DownloadMaterial(string materialId)
        {
            try
            {
                var material = _materials.GetAll().FirstOrDefault(m => m.Id == materialId);
                if (material == null)
                    return NotFound("Tài liệu không tìm thấy");

                var fileMapping = _materials.GetFileMapping(materialId);

                if (string.IsNullOrWhiteSpace(material.FilePath))
                    return BadRequest("Tài liệu này không có tệp đính kèm");

                var filePath = Path.Combine(_env.ContentRootPath, material.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("Tệp không tìm thấy trên máy chủ");

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileName = fileMapping?.OriginalFileName
                    ?? material.OriginalFileName
                    ?? Path.GetFileName(material.FilePath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType, fileName);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error downloading material: {ex.Message}");
                return StatusCode(500, "Lỗi tải tệp");
            }
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
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
