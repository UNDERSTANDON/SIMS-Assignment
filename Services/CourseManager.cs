using SIMS_Assignment.Abstract;
using SIMS_WEB.Storage;
using SIMS_WEB.Models;

namespace SIMS_Assignment.Services
{
    public class CourseManager : ICourseManager
    {
        private readonly IDataStorage _storage;
        private readonly object _lock = new();

        public CourseManager(IDataStorage storage)
        {
            _storage = storage;
        }

        public async Task<List<Course>> GetAllAsync()
        {
            try
            {
                var assignment = await _storage.GetAllCoursesAsync();
                if (assignment != null && assignment.Any())
                {
                    var mapped = assignment.Select(a => new Course
                    {
                        Code = a.CourseId,
                        Title = a.CourseName,
                        Capacity = a.Credits,
                        EnrolledCount = a.EnrolledStudentIds?.Count ?? 0,
                        Instructor = ""
                    }).ToList();
                    return mapped;
                }
            }
            catch { }

            // fallback to in-memory store
            return SIMS_WEB.Models.SimsDataStore.Instance.Courses.ToList();
        }

        public async Task<Course?> GetByCodeAsync(string code)
        {
            try
            {
                var assignment = await _storage.GetAllCoursesAsync();
                if (assignment != null && assignment.Any())
                {
                    var a = assignment.FirstOrDefault(x => x.CourseId == code);
                    if (a != null)
                    {
                        return new Course
                        {
                            Code = a.CourseId,
                            Title = a.CourseName,
                            Capacity = a.Credits,
                            EnrolledCount = a.EnrolledStudentIds?.Count ?? 0,
                            Instructor = ""
                        };
                    }
                }
            }
            catch { }

            return SIMS_WEB.Models.SimsDataStore.Instance.Courses.FirstOrDefault(x => x.Code == code);
        }

        public Task<bool> CreateAsync(Course course)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                if (store.Courses.Any(c => c.Code == course.Code)) return Task.FromResult(false);
                store.Courses.Add(course);
                try { ModelFilePersistence.SaveCourses(store.Courses); } catch { }
                // persist to IDataStorage
                try
                {
                    var a = ToAssignmentCourse(course);
                    _storage.SaveCourseAsync(a).GetAwaiter().GetResult();
                }
                catch { }
                return Task.FromResult(true);
            }
        }

        public Task<bool> UpdateAsync(Course course)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                var existing = store.Courses.FirstOrDefault(c => c.Code == course.Code);
                if (existing == null) return Task.FromResult(false);
                existing.Title = course.Title;
                existing.Capacity = course.Capacity;
                existing.Instructor = course.Instructor;
                try { ModelFilePersistence.SaveCourses(store.Courses); } catch { }
                try
                {
                    var a = ToAssignmentCourse(existing);
                    _storage.SaveCourseAsync(a).GetAwaiter().GetResult();
                }
                catch { }
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteAsync(string code)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                var removed = store.RemoveCourse(code);
                if (removed)
                {
                    try
                    {
                        ModelFilePersistence.SaveCourses(store.Courses);
                        ModelFilePersistence.SaveStudents(store.Students);
                    }
                    catch { }
                    try
                    {
                        // remove from IDataStorage by saving remaining courses
                        foreach (var c in store.Courses)
                        {
                            _storage.SaveCourseAsync(ToAssignmentCourse(c)).GetAwaiter().GetResult();
                        }
                    }
                    catch { }
                }
                return Task.FromResult(removed);
            }
        }

        private SIMS_Assignment.Models.Course ToAssignmentCourse(Course c)
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
}
