using System.Text;
using SIMS_WEB.Models;
using System.Text;

namespace SIMS_WEB.Storage
{
    public static class ModelFilePersistence
    {
        private static string? _dataDir;
        public static string DataDir
        {
            get => _dataDir ?? Path.Combine(AppContext.BaseDirectory, "DataStorage");
            set => _dataDir = value;
        }

        public static void EnsureDataDir()
        {
            if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        }

        public static void SaveStudents(IEnumerable<Student> students)
        {
            EnsureDataDir();
            var path = Path.Combine(DataDir, "students.csv");
            var lines = new List<string>();
            // Header
            lines.Add("StudentId,FullName,Program,Email,DateOfBirth");
            foreach (var s in students)
            {
                var dob = s.DateOfBirth?.ToString("o") ?? "";
                var line = string.Join(',', new[] {
                    Escape(s.StudentId), Escape(s.FullName), Escape(s.Program), Escape(s.Email), Escape(dob)
                });
                lines.Add(line);
            }
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        public static void SaveCourses(IEnumerable<SIMS_WEB.Models.Course> courses)
        {
            EnsureDataDir();
            var path = Path.Combine(DataDir, "courses.csv");
            var lines = new List<string>();
            lines.Add("Code,Title,Capacity,EnrolledCount,Instructor");
            foreach (var c in courses)
            {
                var line = string.Join(',', new[] {
                    Escape(c.Code), Escape(c.Title), c.Capacity.ToString(), c.EnrolledCount.ToString(), Escape(c.Instructor)
                });
                lines.Add(line);
            }
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        private static string Escape(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace('\r', ' ').Replace('\n', ' ').Replace(',', ';');
        }

        public static List<Student> LoadStudents()
        {
            EnsureDataDir();
            var path = Path.Combine(DataDir, "students.csv");
            var result = new List<Student>();
            if (!File.Exists(path)) return result;

            try
            {
                using var sr = new StreamReader(path, Encoding.UTF8);
                string? line;
                bool isHeader = true;
                while ((line = sr.ReadLine()) != null)
                {
                    if (isHeader) { isHeader = false; continue; }
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 1) continue;
                    var s = new Student();
                    s.StudentId = parts.Length > 0 ? parts[0].Trim() : "";
                    s.FullName = parts.Length > 1 ? parts[1].Trim().Replace(';', ',') : "";
                    s.Program = parts.Length > 2 ? parts[2].Trim().Replace(';', ',') : "";
                    s.Email = parts.Length > 3 ? parts[3].Trim().Replace(';', ',') : "";
                    if (parts.Length > 4 && DateTime.TryParse(parts[4].Trim(), out var dt)) s.DateOfBirth = dt;
                    result.Add(s);
                }
            }
            catch { }
            return result;
        }

        public static List<Course> LoadCourses()
        {
            EnsureDataDir();
            var path = Path.Combine(DataDir, "courses.csv");
            var result = new List<Course>();
            if (!File.Exists(path)) return result;

            try
            {
                using var sr = new StreamReader(path, Encoding.UTF8);
                string? line;
                bool isHeader = true;
                while ((line = sr.ReadLine()) != null)
                {
                    if (isHeader) { isHeader = false; continue; }
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length < 1) continue;
                    var c = new Course();
                    c.Code = parts.Length > 0 ? parts[0].Trim().Replace(';', ',') : "";
                    c.Title = parts.Length > 1 ? parts[1].Trim().Replace(';', ',') : "";
                    if (parts.Length > 2 && int.TryParse(parts[2].Trim(), out var cap)) c.Capacity = cap;
                    if (parts.Length > 3 && int.TryParse(parts[3].Trim(), out var en)) c.EnrolledCount = en;
                    c.Instructor = parts.Length > 4 ? parts[4].Trim().Replace(';', ',') : "";
                    result.Add(c);
                }
            }
            catch { }
            return result;
        }
    }
}
