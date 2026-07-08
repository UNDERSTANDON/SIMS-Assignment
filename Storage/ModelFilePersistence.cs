using System.Text;
using SIMS_WEB.Models;

namespace SIMS_WEB.Storage
{
    public static class ModelFilePersistence
    {
        private static string DataDir => Path.Combine(AppContext.BaseDirectory, "DataStorage");

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
    }
}
