using SIMS_WEB.Models;
using SIMS_WEB.Storage;
using SIMS_Assignment.Abstract;
using SIMS_WEB.Models;


namespace SIMS_Assignment.Services
{
    public class StudentManager : IStudentManager
    {
        private readonly object _lock = new();
        private readonly IDataStorage _storage;

        public StudentManager(IDataStorage storage)
        {
            _storage = storage;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            bool changed = false;

            try
            {
                var users = await _storage.GetAllUsersAsync();
                if (users != null)
                {
                    foreach (var u in users)
                    {
                        if (u is SIMS_Assignment.Models.Student)
                        {
                            var exists = store.Students.Any(s => string.Equals(s.StudentId, u.Name, StringComparison.OrdinalIgnoreCase)
                                                              || string.Equals(s.FullName, u.Name, StringComparison.OrdinalIgnoreCase));
                            if (!exists)
                            {
                                var newStudent = new Student
                                {
                                    StudentId = u.Name,
                                    FullName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Name,
                                    Program = "Chương trình tự chọn",
                                    Email = u.Email,
                                    DateOfBirth = DateTime.Now.AddYears(-20)
                                };
                                store.Students.Add(newStudent);
                                changed = true;
                            }
                            else
                            {
                                var existing = store.Students.FirstOrDefault(s => string.Equals(s.StudentId, u.Name, StringComparison.OrdinalIgnoreCase)
                                                                                || string.Equals(s.FullName, u.Name, StringComparison.OrdinalIgnoreCase));
                                if (existing != null && string.IsNullOrEmpty(existing.Email) && !string.IsNullOrEmpty(u.Email))
                                {
                                    existing.Email = u.Email;
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (changed)
            {
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
            }

            return store.Students.ToList();
        }

        public async Task<Student?> GetByIdAsync(string studentId)
        {
            await GetAllAsync();
            return SIMS_WEB.Models.SimsDataStore.Instance.Students.FirstOrDefault(x => x.StudentId == studentId);
        }

        public Task<bool> CreateAsync(Student student)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                if (store.Students.Any(s => s.StudentId == student.StudentId)) return Task.FromResult(false);
                store.Students.Add(student);
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
                try
                {
                    // create a user record for authentication (no password yet)
                    var u = new SIMS_Assignment.Models.Student { Name = student.StudentId, Role = "Student", PasswordHash = string.Empty, Email = student.Email };
                    _storage.SaveUserAsync(u).GetAwaiter().GetResult();
                }
                catch { }
                return Task.FromResult(true);
            }
        }

        public Task<bool> UpdateAsync(Student student)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                var existing = store.Students.FirstOrDefault(s => s.StudentId == student.StudentId);
                if (existing == null) return Task.FromResult(false);
                existing.FullName = student.FullName;
                existing.Program = student.Program;
                existing.Email = student.Email;
                existing.DateOfBirth = student.DateOfBirth;
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteAsync(string studentId)
        {
            lock (_lock)
            {
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                var removed = store.RemoveStudent(studentId);
                if (removed)
                {
                    try
                    {
                        ModelFilePersistence.SaveStudents(store.Students);
                        ModelFilePersistence.SaveCourses(store.Courses);
                        ModelFilePersistence.SaveEnrollments(store.Enrollments);
                        ModelFilePersistence.SaveGrades(store.Grades);
                    }
                    catch { }

                    try
                    {
                        _storage.DeleteUserByNameAsync(studentId).GetAwaiter().GetResult();
                    }
                    catch { }
                }
                return Task.FromResult(removed);
            }
        }

        public Task<int> ImportFromStreamAsync(System.IO.Stream csvStream)
        {
            lock (_lock)
            {
                int count = 0;
                using var reader = new System.IO.StreamReader(csvStream);
                string? line;
                bool isHeader = true;
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
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
                    if (!store.Students.Any(x => x.StudentId == s.StudentId))
                    {
                        store.Students.Add(s);
                        count++;
                    }
                }
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
                return Task.FromResult(count);
            }
        }
    }
}
