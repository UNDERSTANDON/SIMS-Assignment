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
            try
            {
                var users = await _storage.GetAllUsersAsync();
                if (users != null && users.Any())
                {
                    var students = users.Where(u => u is SIMS_Assignment.Models.Student)
                        .Select(u => new Student
                        {
                            StudentId = u.Id > 0 ? $"U{u.Id}" : u.Name,
                            FullName = u.Name,
                            Program = string.Empty,
                            Email = string.Empty
                        }).ToList();
                    if (students.Any()) return students;
                }
            }
            catch { }

            return SIMS_WEB.Models.SimsDataStore.Instance.Students.ToList();
        }

        public async Task<Student?> GetByIdAsync(string studentId)
        {
            try
            {
                var users = await _storage.GetAllUsersAsync();
                if (users != null && users.Any())
                {
                    // studentId format U<id> or original id
                    if (studentId.StartsWith("U") && int.TryParse(studentId.Substring(1), out var iid))
                    {
                        var u = users.FirstOrDefault(x => x.Id == iid && x is SIMS_Assignment.Models.Student);
                        if (u != null) return new Student { StudentId = $"U{u.Id}", FullName = u.Name };
                    }
                    var byName = users.FirstOrDefault(x => string.Equals(x.Name, studentId, StringComparison.OrdinalIgnoreCase) && x is SIMS_Assignment.Models.Student);
                    if (byName != null) return new Student { StudentId = $"U{byName.Id}", FullName = byName.Name };
                }
            }
            catch { }

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
                    var u = new SIMS_Assignment.Models.Student { Name = student.FullName, Role = "Student", PasswordHash = string.Empty };
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
