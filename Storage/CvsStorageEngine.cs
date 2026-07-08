using System.Text;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Storage
{
    /// <summary>
    /// Minimal CSV storage engine for users and courses. Files are stored in a "data" folder
    /// next to the application base directory. This implementation is intentionally small
    /// and synchronous-file-operation based for demo purposes.
    /// </summary>
    public class CvsStorageEngine : IDataStorage
    {
        private readonly string _dataDir;
        private readonly object _lock = new();

        public CvsStorageEngine(string? directory = null)
        {
            _dataDir = directory ?? Path.Combine(AppContext.BaseDirectory, "data");
            if (!Directory.Exists(_dataDir)) Directory.CreateDirectory(_dataDir);
        }

        private string UsersPath => Path.Combine(_dataDir, "users.csv");
        private string CoursesPath => Path.Combine(_dataDir, "courses.csv");

        public Task<User> GetUserByNameAsync(string name)
        {
            lock (_lock)
            {
                if (!File.Exists(UsersPath)) return Task.FromResult<User?>(null!);
                foreach (var line in File.ReadAllLines(UsersPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // Id,Name,Role,PasswordHash,Extra
                    var parts = line.Split(',');
                    if (parts.Length < 4) continue;
                    var uname = parts[1].Trim();
                    if (!string.Equals(uname, name, StringComparison.OrdinalIgnoreCase)) continue;
                    var id = int.TryParse(parts[0], out var iid) ? iid : 0;
                    var role = parts[2].Trim();
                    var pwd = parts[3].Trim();

                    User? user = role switch
                    {
                        "Admin" => new Admin { Id = id, Name = uname, Role = role, PasswordHash = pwd },
                        "Faculty" or "Lecturer" => new Lecturer { Id = id, Name = uname, Role = role, PasswordHash = pwd },
                        "Student" => new Student { Id = id, Name = uname, Role = role, PasswordHash = pwd },
                        _ => null
                    };

                    // additional fields may be present (ignored for now)
                    return Task.FromResult(user);
                }
            }
            return Task.FromResult<User?>(null!);
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
            // Id,Name,Role,PasswordHash
            return string.Join(',', new[] { u.Id.ToString(), Escape(u.Name), Escape(u.Role), Escape(u.PasswordHash) });
        }

        private static string SerializeCourse(Course c)
        {
            // CourseId,CourseName,Credits,LecturerId,EnrolledIds(semi-colon)
            var enrolled = c.EnrolledStudentIds != null && c.EnrolledStudentIds.Any()
                ? string.Join(';', c.EnrolledStudentIds)
                : string.Empty;
            return string.Join(',', new[] { Escape(c.CourseId), Escape(c.CourseName), c.Credits.ToString(), c.LecturerId.ToString(), Escape(enrolled) });
        }

        private static string Escape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace('\n', ' ').Replace('\r', ' ').Replace(',', ';');
        }
    }
}
