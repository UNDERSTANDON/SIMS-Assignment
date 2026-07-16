using System.Text;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;
using SIMS_Assignment.Models.CourseRelatedModels;
using SIMS_Assignment.Services.CourseServices;

namespace SIMS_Assignment.Storage
{
    /// <summary>
    /// Minimal CSV storage engine for users and courses. Files are stored in a "data" folder
    /// next to the application base directory. This implementation is intentionally small
    /// and synchronous-file-operation based for demo purposes.
    /// </summary>
    public class CvsStorageEngine : IDataStorage, IMaterialStorage
    {
        private readonly string _dataDir;
        private readonly object _lock = new();

        public CvsStorageEngine(string? directory = null)
        {
            _dataDir = directory ?? Path.Combine(AppContext.BaseDirectory, "data");
            if (!Directory.Exists(_dataDir)) Directory.CreateDirectory(_dataDir);
        }

        private string UsersPath => Path.Combine(_dataDir, "users.csv");
        // Use a separate file for assignment/course storage to avoid clashing with UI CSV format
        private string CoursesPath => Path.Combine(_dataDir, "assignment_courses.csv");
        private string MaterialsPath => Path.Combine(_dataDir, "materials.csv");
        private string FileMappingsPath => Path.Combine(_dataDir, "file_mappings.csv");

        public Task<List<User>> GetAllUsersAsync()
        {
            lock (_lock)
            {
                var result = new List<User>();
                if (!File.Exists(UsersPath)) return Task.FromResult(result);
                foreach (var line in File.ReadAllLines(UsersPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 4) continue;
                    var id = int.TryParse(parts[0], out var iid) ? iid : 0;
                    var uname = parts[1].Trim();
                    var role = parts[2].Trim();
                    var pwd = parts[3].Trim();
                    var email = parts.Length > 4 ? Unescape(parts[4].Trim()) : string.Empty;
                    var fullname = parts.Length > 5 ? Unescape(parts[5].Trim()) : string.Empty;

                    User? user = role switch
                    {
                        "Admin" => new Admin { Id = id, Name = uname, Role = role, PasswordHash = pwd, Email = email, FullName = fullname },
                        "Faculty" or "Lecturer" => new Lecturer { Id = id, Name = uname, Role = role, PasswordHash = pwd, Email = email, FullName = fullname },
                        "Student" => new Student { Id = id, Name = uname, Role = role, PasswordHash = pwd, Email = email, FullName = fullname },
                        _ => null
                    };

                    if (user != null) result.Add(user);
                }
                return Task.FromResult(result);
            }
        }

        public Task<List<Course>> GetAllCoursesAsync()
        {
            lock (_lock)
            {
                var result = new List<Course>();
                if (!File.Exists(CoursesPath)) return Task.FromResult(result);
                foreach (var line in File.ReadAllLines(CoursesPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 1) continue;
                    var id = Unescape(parts[0]);
                    var name = parts.Length > 1 ? Unescape(parts[1]) : string.Empty;
                    var credits = parts.Length > 2 && int.TryParse(parts[2], out var cr) ? cr : 0;
                    var lecturer = parts.Length > 3 && int.TryParse(parts[3], out var lid) ? lid : 0;
                    var enrolled = new List<int>();
                    if (parts.Length > 4)
                    {
                        var en = Unescape(parts[4]);
                        if (!string.IsNullOrEmpty(en))
                        {
                            var toks = en.Split(';');
                            foreach (var t in toks)
                            {
                                if (int.TryParse(t, out var ii)) enrolled.Add(ii);
                            }
                        }
                    }
                    result.Add(new Course { CourseId = id, CourseName = name, Credits = credits, LecturerId = lecturer, EnrolledStudentIds = enrolled });
                }
                return Task.FromResult(result);
            }
        }

        public Task<User> GetUserByNameAsync(string name)
        {
            lock (_lock)
            {
                if (!File.Exists(UsersPath)) return Task.FromResult<User?>(null!);
                foreach (var line in File.ReadAllLines(UsersPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 4) continue;
                    var uname = parts[1].Trim();
                    var role = parts[2].Trim();
                    var pwd = parts[3].Trim();
                    var email = parts.Length > 4 ? Unescape(parts[4].Trim()) : string.Empty;
                    var fullname = parts.Length > 5 ? Unescape(parts[5].Trim()) : string.Empty;

                    if (!string.Equals(uname, name, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(email, name, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(fullname, name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var id = int.TryParse(parts[0], out var iid) ? iid : 0;

                    User? user = role switch
                    {
                        "Admin" => new Admin { Id = id, Name = uname, Role = role, PasswordHash = pwd, Email = email, FullName = fullname },
                        "Faculty" or "Lecturer" => new Lecturer { Id = id, Name = uname, Role = role, PasswordHash = pwd, Email = email, FullName = fullname },
                        "Student" => new Student { Id = id, Name = uname, Role = role, PasswordHash = pwd, Email = email, FullName = fullname },
                        _ => null
                    };

                    return Task.FromResult(user);
                }
            }
            return Task.FromResult<User?>(null!);
        }

        public Task<bool> DeleteUserByNameAsync(string name)
        {
            lock (_lock)
            {
                if (!File.Exists(UsersPath)) return Task.FromResult(false);
                var lines = File.ReadAllLines(UsersPath, Encoding.UTF8).ToList();
                var remaining = lines.Where(line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return false;
                    var parts = line.Split(',');
                    if (parts.Length < 2) return false;
                    return !string.Equals(parts[1].Trim(), name, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                if (remaining.Count == lines.Count) return Task.FromResult(false);
                File.WriteAllLines(UsersPath, remaining, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        public Task<bool> SaveCourseAsync(Course course)
        {
            lock (_lock)
            {
                var lines = new List<string>();
                if (File.Exists(CoursesPath))
                {
                    lines.AddRange(File.ReadAllLines(CoursesPath, Encoding.UTF8));
                }

                // Use CourseId as key
                bool updated = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 0) continue;
                    if (parts[0] == course.CourseId)
                    {
                        lines[i] = SerializeCourse(course);
                        updated = true;
                        break;
                    }
                }

                if (!updated)
                {
                    lines.Add(SerializeCourse(course));
                }

                File.WriteAllLines(CoursesPath, lines, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteCourseAsync(string courseId)
        {
            lock (_lock)
            {
                if (!File.Exists(CoursesPath)) return Task.FromResult(false);

                var lines = File.ReadAllLines(CoursesPath, Encoding.UTF8).ToList();
                var remaining = lines.Where(line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return false;
                    var parts = line.Split(',');
                    return parts.Length == 0 || parts[0] != courseId;
                }).ToList();

                if (remaining.Count == lines.Count) return Task.FromResult(false);

                File.WriteAllLines(CoursesPath, remaining, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        public Task<bool> SaveUserAsync(User user)
        {
            lock (_lock)
            {
                var lines = new List<string>();
                if (File.Exists(UsersPath))
                {
                    lines.AddRange(File.ReadAllLines(UsersPath, Encoding.UTF8));
                }

                // ensure Id
                if (user.Id == 0)
                {
                    int max = 0;
                    foreach (var l in lines)
                    {
                        var p = l.Split(',');
                        if (p.Length == 0) continue;
                        if (int.TryParse(p[0], out var iid)) max = Math.Max(max, iid);
                    }
                    user.Id = max + 1;
                }

                bool updated = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 0) continue;
                    if (int.TryParse(parts[0], out var iid) && iid == user.Id)
                    {
                        lines[i] = SerializeUser(user);
                        updated = true;
                        break;
                    }
                }
                if (!updated)
                {
                    lines.Add(SerializeUser(user));
                }

                File.WriteAllLines(UsersPath, lines, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        private static string SerializeUser(User u)
        {
            // Id,Name,Role,PasswordHash,Email,FullName
            return string.Join(',', new[] { u.Id.ToString(), Escape(u.Name), Escape(u.Role), Escape(u.PasswordHash), Escape(u.Email), Escape(u.FullName) });
        }

        private static string SerializeCourse(Course c)
        {
            // CourseId,CourseName,Credits,LecturerId,EnrolledIds(semi-colon)
            var enrolled = c.EnrolledStudentIds != null && c.EnrolledStudentIds.Any()
                ? string.Join(';', c.EnrolledStudentIds)
                : string.Empty;
            return string.Join(',', new[] { Escape(c.CourseId), Escape(c.CourseName), c.Credits.ToString(), c.LecturerId.ToString(), Escape(enrolled) });
        }

        private static string SerializeMaterial(Material m)
        {
            // Id,Title,Description,FilePath,OriginalFileName,UploadDate,CourseId
            return string.Join(',', new[] {
                Escape(m.Id),
                Escape(m.Title),
                Escape(m.Description),
                Escape(m.FilePath),
                Escape(m.OriginalFileName),
                m.UploadDate.ToString("o"),
                Escape(m.CourseId)
            });
        }

        private static string SerializeFileMapping(FileMapping f)
        {
            // MaterialId,OriginalFileName,HashedFileName,CourseId,UploadDate
            return string.Join(',', new[] {
                Escape(f.MaterialId),
                Escape(f.OriginalFileName),
                Escape(f.HashedFileName),
                Escape(f.CourseId),
                f.UploadDate.ToString("o")
            });
        }

        public Task<List<Material>> GetAllMaterialsAsync()
        {
            lock (_lock)
            {
                var result = new List<Material>();
                if (!File.Exists(MaterialsPath)) return Task.FromResult(result);
                foreach (var line in File.ReadAllLines(MaterialsPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 1) continue;

                    var m = new Material
                    {
                        Id = Unescape(parts[0]),
                        Title = parts.Length > 1 ? Unescape(parts[1]) : string.Empty,
                        Description = parts.Length > 2 ? Unescape(parts[2]) : string.Empty,
                        FilePath = parts.Length > 3 ? Unescape(parts[3]) : string.Empty,
                        OriginalFileName = parts.Length > 4 ? Unescape(parts[4]) : string.Empty,
                        UploadDate = parts.Length > 5 && DateTime.TryParse(parts[5], out var dt) ? dt : DateTime.Now,
                        CourseId = parts.Length > 6 ? Unescape(parts[6]) : string.Empty
                    };
                    result.Add(m);
                }
                return Task.FromResult(result);
            }
        }

        public Task<bool> SaveMaterialAsync(Material material)
        {
            lock (_lock)
            {
                var lines = new List<string>();
                if (File.Exists(MaterialsPath))
                {
                    lines.AddRange(File.ReadAllLines(MaterialsPath, Encoding.UTF8));
                }

                bool updated = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 0) continue;
                    if (parts[0] == material.Id)
                    {
                        lines[i] = SerializeMaterial(material);
                        updated = true;
                        break;
                    }
                }

                if (!updated)
                {
                    lines.Add(SerializeMaterial(material));
                }

                File.WriteAllLines(MaterialsPath, lines, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteMaterialAsync(string id)
        {
            lock (_lock)
            {
                if (!File.Exists(MaterialsPath)) return Task.FromResult(false);
                var lines = File.ReadAllLines(MaterialsPath, Encoding.UTF8).ToList();
                var remaining = lines.Where(line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return false;
                    var parts = line.Split(',');
                    return parts.Length == 0 || parts[0] != id;
                }).ToList();

                if (remaining.Count == lines.Count) return Task.FromResult(false);
                File.WriteAllLines(MaterialsPath, remaining, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        public Task<List<FileMapping>> GetFileMappingsAsync()
        {
            lock (_lock)
            {
                var result = new List<FileMapping>();
                if (!File.Exists(FileMappingsPath)) return Task.FromResult(result);
                foreach (var line in File.ReadAllLines(FileMappingsPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 1) continue;

                    var f = new FileMapping
                    {
                        MaterialId = Unescape(parts[0]),
                        OriginalFileName = parts.Length > 1 ? Unescape(parts[1]) : string.Empty,
                        HashedFileName = parts.Length > 2 ? Unescape(parts[2]) : string.Empty,
                        CourseId = parts.Length > 3 ? Unescape(parts[3]) : string.Empty,
                        UploadDate = parts.Length > 4 && DateTime.TryParse(parts[4], out var dt) ? dt : DateTime.Now
                    };
                    result.Add(f);
                }
                return Task.FromResult(result);
            }
        }

        public Task<bool> SaveFileMappingAsync(FileMapping mapping)
        {
            lock (_lock)
            {
                var lines = new List<string>();
                if (File.Exists(FileMappingsPath))
                {
                    lines.AddRange(File.ReadAllLines(FileMappingsPath, Encoding.UTF8));
                }

                bool updated = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 0) continue;
                    if (parts[0] == mapping.MaterialId)
                    {
                        lines[i] = SerializeFileMapping(mapping);
                        updated = true;
                        break;
                    }
                }

                if (!updated)
                {
                    lines.Add(SerializeFileMapping(mapping));
                }

                File.WriteAllLines(FileMappingsPath, lines, Encoding.UTF8);
                return Task.FromResult(true);
            }
        }

        private static string Escape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace('\n', ' ').Replace('\r', ' ').Replace(',', ';');
        }

        private static string Unescape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace(';', ',');
        }
    }
}
