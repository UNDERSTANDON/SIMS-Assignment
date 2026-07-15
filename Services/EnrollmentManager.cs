using SIMS_Assignment.Abstract;
using SIMS_WEB.Storage;
using SIMS_WEB.Models;

namespace SIMS_Assignment.Services
{
    public class EnrollmentManager : IEnrollmentManager
    {
        private readonly IDataStorage _storage;
        private readonly object _lock = new();

        public EnrollmentManager(IDataStorage storage)
        {
            _storage = storage;
        }

        public Task<List<Enrollment>> GetEnrollmentsAsync()
        {
            var list = SIMS_WEB.Models.SimsDataStore.Instance.Enrollments.ToList();
            return Task.FromResult(list);
        }

        public Task<(bool success, string message)> EnrollAsync(string studentId, string courseCode)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                var res = store.Enroll(studentId, courseCode);
                if (res.success)
                {
                    try { ModelFilePersistence.SaveCourses(store.Courses); } catch { }
                    try { ModelFilePersistence.SaveEnrollments(store.Enrollments); } catch { }
                    try
                    {
                        foreach (var c in store.Courses)
                        {
                            _storage.SaveCourseAsync(new SIMS_Assignment.Models.Course
                            {
                                CourseId = c.Code,
                                CourseName = c.Title,
                                Credits = c.Capacity,
                                LecturerId = 0,
                                EnrolledStudentIds = new List<int>()
                            }).GetAwaiter().GetResult();
                        }
                    }
                    catch { }
                }
                return Task.FromResult(res);
            }
        }

        public Task<bool> UnenrollAsync(string studentId, string courseCode)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                var enrollment = store.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode);
                if (enrollment == null) return Task.FromResult(false);
                store.Enrollments.Remove(enrollment);
                var course = store.Courses.FirstOrDefault(c => c.Code == courseCode);
                if (course != null && course.EnrolledCount > 0) course.EnrolledCount--;
                try { ModelFilePersistence.SaveCourses(store.Courses); } catch { }
                try { ModelFilePersistence.SaveEnrollments(store.Enrollments); } catch { }
                try
                {
                    foreach (var c in store.Courses)
                    {
                        _storage.SaveCourseAsync(new SIMS_Assignment.Models.Course
                        {
                            CourseId = c.Code,
                            CourseName = c.Title,
                            Credits = c.Capacity,
                            LecturerId = 0,
                            EnrolledStudentIds = new List<int>()
                        }).GetAwaiter().GetResult();
                    }
                }
                catch { }
                return Task.FromResult(true);
            }
        }

        public Task<List<Enrollment>> GetEnrollmentsByCourseAsync(string courseCode)
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            var enrollments = store.Enrollments
                .Where(e => e.CourseCode == courseCode && e.IsEnrolled)
                .ToList();
            return Task.FromResult(enrollments);
        }

        public Task<List<Student>> GetEnrolledStudentsByCourseAsync(string courseCode)
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            var enrolledStudentIds = store.Enrollments
                .Where(e => e.CourseCode == courseCode && e.IsEnrolled)
                .Select(e => e.StudentId)
                .ToHashSet();

            var students = store.Students
                .Where(s => enrolledStudentIds.Contains(s.StudentId))
                .ToList();

            return Task.FromResult(students);
        }

        public Task<int> GetEnrollmentCountAsync(string courseCode)
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            var count = store.Enrollments
                .Count(e => e.CourseCode == courseCode && e.IsEnrolled);
            return Task.FromResult(count);
        }

        public Task<bool> IsStudentEnrolledAsync(string studentId, string courseCode)
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            var isEnrolled = store.Enrollments
                .Any(e => e.StudentId == studentId && e.CourseCode == courseCode && e.IsEnrolled);
            return Task.FromResult(isEnrolled);
        }
    }
}
