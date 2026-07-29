using SIMS_WEB.Models;
using SIMS_WEB.Storage;
using SIMS_Assignment.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMS_Assignment.Services
{
    public class StudentManager : IStudentManager
    {
        private readonly object _lock = new();
        private readonly IDataStorage _storage;

        private static readonly string[] ValidPrograms = new[]
        {
            "Information Technology",
            "Software Engineering",
            "Computer Science",
            "Business Administration",
            "Elective Program"
        };

        public StudentManager(IDataStorage storage)
        {
            _storage = storage;
        }

        public static string GenerateNextStudentId(IEnumerable<string> existingIds)
        {
            int maxId = 0;
            foreach (var id in existingIds)
            {
                if (!string.IsNullOrWhiteSpace(id) && id.StartsWith("SV", StringComparison.OrdinalIgnoreCase))
                {
                    var numPart = id.Length >= 9 ? id.Substring(6) : id.Substring(2);
                    if (int.TryParse(numPart, out int num))
                    {
                        if (num > maxId) maxId = num;
                    }
                }
            }
            int nextId = maxId + 1;
            return $"SV2024{nextId:D3}";
        }

        public static string SanitizeProgram(string rawProgram)
        {
            if (string.IsNullOrWhiteSpace(rawProgram)) return "Elective Program";
            var trimmed = rawProgram.Trim();

            foreach (var vp in ValidPrograms)
            {
                if (string.Equals(vp, trimmed, StringComparison.OrdinalIgnoreCase)) return vp;
            }

            if (trimmed.Contains("Công nghệ", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Information", StringComparison.OrdinalIgnoreCase))
                return "Information Technology";
            if (trimmed.Contains("Phần mềm", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Software", StringComparison.OrdinalIgnoreCase))
                return "Software Engineering";
            if (trimmed.Contains("Máy tính", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Computer", StringComparison.OrdinalIgnoreCase))
                return "Computer Science";
            if (trimmed.Contains("Kinh doanh", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Business", StringComparison.OrdinalIgnoreCase))
                return "Business Administration";

            return "Elective Program";
        }

        public static string SanitizeEmail(string rawEmail, string fallbackStudentId)
        {
            if (string.IsNullOrWhiteSpace(rawEmail)) return $"{fallbackStudentId.ToLower()}@univ.edu";
            var trimmed = rawEmail.Trim();

            if (Regex.IsMatch(trimmed, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return trimmed;
            }

            return $"{fallbackStudentId.ToLower()}@univ.edu";
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
                        if (u is SIMS_Assignment.Models.Student || string.Equals(u.Role, "Student", StringComparison.OrdinalIgnoreCase))
                        {
                            var existing = store.Students.FirstOrDefault(s =>
                                string.Equals(s.StudentId, u.Name, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(s.FullName, u.Name, StringComparison.OrdinalIgnoreCase)
                             || (!string.IsNullOrEmpty(u.Email) && string.Equals(s.Email, u.Email, StringComparison.OrdinalIgnoreCase)));

                            if (existing == null)
                            {
                                string newStudentId = u.Name;
                                if (!newStudentId.StartsWith("SV", StringComparison.OrdinalIgnoreCase))
                                {
                                    newStudentId = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
                                }

                                var newStudent = new Student
                                {
                                    StudentId = newStudentId,
                                    FullName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Name,
                                    Program = "Elective Program",
                                    Email = SanitizeEmail(u.Email, newStudentId),
                                    DateOfBirth = DateTime.Now.AddYears(-20)
                                };
                                store.Students.Add(newStudent);
                                changed = true;
                            }
                            else
                            {
                                if (!existing.StudentId.StartsWith("SV", StringComparison.OrdinalIgnoreCase))
                                {
                                    existing.StudentId = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
                                    changed = true;
                                }
                                existing.Program = SanitizeProgram(existing.Program);
                                if (!string.IsNullOrEmpty(u.Email))
                                {
                                    existing.Email = SanitizeEmail(u.Email, existing.StudentId);
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
                if (string.IsNullOrWhiteSpace(student.StudentId) || !student.StudentId.StartsWith("SV", StringComparison.OrdinalIgnoreCase))
                {
                    student.StudentId = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
                }
                student.Program = SanitizeProgram(student.Program);
                student.Email = SanitizeEmail(student.Email, student.StudentId);

                if (store.Students.Any(s => s.StudentId == student.StudentId)) return Task.FromResult(false);
                store.Students.Add(student);
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
                try
                {
                    var u = new SIMS_Assignment.Models.Student
                    {
                        Name = student.StudentId,
                        Role = "Student",
                        PasswordHash = string.Empty,
                        Email = student.Email,
                        FullName = student.FullName
                    };
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
                existing.Program = SanitizeProgram(student.Program);
                existing.Email = SanitizeEmail(student.Email, student.StudentId);
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

        public Task<int> ImportFromStreamAsync(Stream csvStream)
        {
            lock (_lock)
            {
                int count = 0;
                using var reader = new StreamReader(csvStream);
                string? line;
                bool isHeader = true;
                var store = SIMS_WEB.Models.SimsDataStore.Instance;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isHeader) { isHeader = false; continue; }
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    string rawId = parts[0].Trim();
                    string rawName = parts.Length > 1 ? parts[1].Trim() : rawId;
                    string rawProgram = parts.Length > 2 ? parts[2].Trim() : "";
                    string rawEmail = parts.Length > 3 ? parts[3].Trim() : "";

                    string finalId = rawId;
                    if (string.IsNullOrWhiteSpace(finalId) || !finalId.StartsWith("SV", StringComparison.OrdinalIgnoreCase))
                    {
                        finalId = GenerateNextStudentId(store.Students.Select(s => s.StudentId));
                    }

                    string finalProgram = SanitizeProgram(rawProgram);
                    string finalEmail = SanitizeEmail(rawEmail, finalId);

                    if (!store.Students.Any(x => x.StudentId == finalId))
                    {
                        var s = new Student
                        {
                            StudentId = finalId,
                            FullName = string.IsNullOrWhiteSpace(rawName) ? finalId : rawName,
                            Program = finalProgram,
                            Email = finalEmail
                        };
                        store.Students.Add(s);

                        try
                        {
                            var u = new SIMS_Assignment.Models.Student
                            {
                                Name = finalId,
                                Role = "Student",
                                PasswordHash = string.Empty,
                                Email = finalEmail,
                                FullName = s.FullName
                            };
                            _storage.SaveUserAsync(u).GetAwaiter().GetResult();
                        }
                        catch { }

                        count++;
                    }
                }
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
                return Task.FromResult(count);
            }
        }
    }
}
