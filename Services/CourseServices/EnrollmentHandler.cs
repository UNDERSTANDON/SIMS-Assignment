using Microsoft.AspNetCore.Hosting;
using SIMS_Assignment.Storage;
using SIMS_WEB.Models;

namespace SIMS_Assignment.Services.CourseServices
{
    public class EnrollmentHandler
    {
        private readonly List<Enrollment> _enrollments;
        private readonly JsonStorage<Enrollment> _storage;
        private readonly object _lock = new();

        public EnrollmentHandler(IWebHostEnvironment env)
        {
            _storage = new JsonStorage<Enrollment>(env, "enrollments.json");
            _enrollments = _storage.Load();
        }

        public List<Enrollment> GetAll()
        {
            lock (_lock)
            {
                return _enrollments.ToList();
            }
        }

        public List<Enrollment> GetByCourse(string courseCode)
        {
            lock (_lock)
            {
                return _enrollments
                    .Where(e => e.CourseCode == courseCode && e.IsEnrolled)
                    .ToList();
            }
        }

        public List<Enrollment> GetByStudent(string studentId)
        {
            lock (_lock)
            {
                return _enrollments
                    .Where(e => e.StudentId == studentId && e.IsEnrolled)
                    .ToList();
            }
        }

        public bool IsEnrolled(string studentId, string courseCode)
        {
            lock (_lock)
            {
                return EnrollmentHelper.IsStudentEnrolled(_enrollments, studentId, courseCode);
            }
        }

        public (bool success, string message) Enroll(string studentId, string courseCode)
        {
            lock (_lock)
            {
                var existing = _enrollments.FirstOrDefault(
                    e => e.StudentId == studentId && e.CourseCode == courseCode);

                if (existing != null && existing.IsEnrolled)
                    return (false, "Sinh viên đã đăng ký khóa học này rồi");

                if (existing != null)
                {
                    existing.IsEnrolled = true;
                    existing.EnrolledAt = DateTime.Now;
                }
                else
                {
                    _enrollments.Add(new Enrollment
                    {
                        StudentId = studentId,
                        CourseCode = courseCode,
                        EnrolledAt = DateTime.Now,
                        IsEnrolled = true
                    });
                }

                _storage.Save(_enrollments);
                return (true, "Ghi danh thành công");
            }
        }

        public bool Unenroll(string studentId, string courseCode)
        {
            lock (_lock)
            {
                var enrollment = _enrollments.FirstOrDefault(
                    e => e.StudentId == studentId && e.CourseCode == courseCode && e.IsEnrolled);
                if (enrollment == null) return false;

                enrollment.IsEnrolled = false;
                _storage.Save(_enrollments);
                return true;
            }
        }

        public void UnenrollByStudent(string studentId)
        {
            lock (_lock)
            {
                var changed = false;
                foreach (var e in _enrollments.Where(e => e.StudentId == studentId && e.IsEnrolled))
                {
                    e.IsEnrolled = false;
                    changed = true;
                }
                if (changed) _storage.Save(_enrollments);
            }
        }

        public void UnenrollByCourse(string courseCode)
        {
            lock (_lock)
            {
                var changed = false;
                foreach (var e in _enrollments.Where(e => e.CourseCode == courseCode && e.IsEnrolled))
                {
                    e.IsEnrolled = false;
                    changed = true;
                }
                if (changed) _storage.Save(_enrollments);
            }
        }

        public void SyncToStore(SIMS_WEB.Models.SimsDataStore store)
        {
            lock (_lock)
            {
                store.Enrollments.Clear();
                store.Enrollments.AddRange(_enrollments.Where(e => e.IsEnrolled));
            }
        }
    }
}
