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
using System;

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

        private string GetCurrentStudentId()
        {
            var sessionStudent = HttpContext.Session.GetString("StudentId");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            if (!string.IsNullOrEmpty(sessionStudent)) return sessionStudent;

            var studentObj = _store.Students.FirstOrDefault(s => string.Equals(s.StudentId, username, StringComparison.OrdinalIgnoreCase));
            if (studentObj != null) return studentObj.StudentId;

            return username;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "StudentCourses";
            string studentId = GetCurrentStudentId();

            var enrolledCodes = _store.Enrollments
                .Where(e => e.StudentId == studentId && e.IsEnrolled)
                .Select(e => e.CourseCode)
                .ToHashSet();

            var allCourses = _store.Courses.ToList();
            var enrolledCourses = allCourses.Where(c => enrolledCodes.Contains(c.Code)).ToList();

            ViewBag.AllCourses = allCourses;
            ViewBag.EnrolledCodes = enrolledCodes;
            ViewBag.StudentId = studentId;

            return View(enrolledCourses);
        }

        public IActionResult Details(string id)
        {
            var course = _store.Courses.FirstOrDefault(c => c.Code == id);
            if (course == null) return NotFound();

            string studentId = GetCurrentStudentId();

            bool enrolled = false;
            if (!string.IsNullOrEmpty(studentId))
            {
                enrolled = _store.Enrollments.Any(e => e.StudentId == studentId && e.CourseCode == id && e.IsEnrolled);
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
            string studentId = GetCurrentStudentId();

            if (string.IsNullOrEmpty(studentId))
            {
                TempData["Error"] = "Student profile not found in session.";
                return RedirectToAction("Index");
            }

            var (success, message) = _store.Enroll(studentId, id);
            if (success)
            {
                try { ModelFilePersistence.SaveEnrollments(_store.Enrollments); } catch { }
                try { ModelFilePersistence.SaveCourses(_store.Courses); } catch { }
                TempData["Success"] = $"Successfully enrolled in course {id}!";
            }
            else
            {
                TempData["Error"] = message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Unenroll(string id)
        {
            string studentId = GetCurrentStudentId();
            var enrollment = _store.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == id && e.IsEnrolled);
            if (enrollment != null)
            {
                _store.Enrollments.Remove(enrollment);
                var course = _store.Courses.FirstOrDefault(c => c.Code == id);
                if (course != null && course.EnrolledCount > 0)
                {
                    course.EnrolledCount--;
                }
                try { ModelFilePersistence.SaveEnrollments(_store.Enrollments); } catch { }
                try { ModelFilePersistence.SaveCourses(_store.Courses); } catch { }
                TempData["Success"] = $"Successfully unenrolled from course {id}!";
            }
            else
            {
                TempData["Error"] = "Enrollment record not found.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult DownloadMaterial(string materialId)
        {
            try
            {
                var material = _materials.GetAll().FirstOrDefault(m => m.Id == materialId);
                if (material == null)
                    return NotFound("Material not found");

                var fileMapping = _materials.GetFileMapping(materialId);

                if (string.IsNullOrWhiteSpace(material.FilePath))
                    return BadRequest("Material has no attached file");

                var filePath = Path.Combine(_env.ContentRootPath, material.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found on server");

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
                return StatusCode(500, "File download error");
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
