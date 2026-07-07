namespace SIMS_WEB.Models
{
    /// <summary>
    /// Singleton in-memory data store — equivalent to Java SimsDataStore
    /// </summary>
    public class SimsDataStore
    {
        private static SimsDataStore? _instance;
        public static SimsDataStore Instance => _instance ??= new SimsDataStore();

        public List<Student> Students { get; } = new();
        public List<Course> Courses { get; } = new();
        public List<Enrollment> Enrollments { get; } = new();
        public List<GradeRecord> Grades { get; } = new();

        // Login attempt tracking
        public Dictionary<string, int> FailedAttempts { get; } = new();
        public Dictionary<string, DateTime> LockUntil { get; } = new();

        // Observer Pattern — grade update events
        public event Action<GradeRecord>? GradeUpdated;
        public void NotifyGradeUpdated(GradeRecord record) => GradeUpdated?.Invoke(record);

        private SimsDataStore()
        {
            // Seed demo students
            Students.AddRange(new[]
            {
                new Student { StudentId = "SV2024001", FullName = "Nguyễn Văn An",    Program = "Công nghệ Thông tin",  Email = "an.nv@univ.edu" },
                new Student { StudentId = "SV2024002", FullName = "Trần Thị Bình",   Program = "Kỹ thuật Phần mềm",   Email = "binh.tt@univ.edu" },
                new Student { StudentId = "SV2024003", FullName = "Lê Hoàng Cường",  Program = "Khoa học Máy tính",    Email = "cuong.lh@univ.edu" },
                new Student { StudentId = "SV2024004", FullName = "Phạm Thị Dung",   Program = "Quản trị Kinh doanh",  Email = "dung.pt@univ.edu" },
            });

            // Seed demo courses
            Courses.AddRange(new[]
            {
                new Course { Code = "CS101", Title = "Lập trình Căn bản",     Capacity = 40, EnrolledCount = 12, Instructor = "GV. Nguyễn" },
                new Course { Code = "CS202", Title = "Cấu trúc Dữ liệu",     Capacity = 35, EnrolledCount = 28, Instructor = "GV. Trần" },
                new Course { Code = "BA301", Title = "Quản trị Kinh doanh",   Capacity = 50, EnrolledCount = 45, Instructor = "GV. Lê" },
                new Course { Code = "SE401", Title = "Kỹ nghệ Phần mềm",     Capacity = 30, EnrolledCount = 5,  Instructor = "GV. Phạm" },
            });

            Enrollments.AddRange(new[]
            {
                new Enrollment { StudentId = "SV2024001", CourseCode = "CS101" },
                new Enrollment { StudentId = "SV2024001", CourseCode = "CS202" },
                new Enrollment { StudentId = "SV2024002", CourseCode = "CS101" },
            });

            Grades.AddRange(new[]
            {
                new GradeRecord { StudentId = "SV2024001", CourseCode = "CS101", Score = 85 },
                new GradeRecord { StudentId = "SV2024001", CourseCode = "CS202", Score = 78 },
                new GradeRecord { StudentId = "SV2024002", CourseCode = "CS101", Score = 92 },
            });
        }

        // ============ Facade Pattern helpers ============
        public (bool success, string message) Enroll(string studentId, string courseCode)
        {
            var course = Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null) return (false, "Khóa học không tồn tại");

            bool duplicate = Enrollments.Any(e => e.StudentId == studentId && e.CourseCode == courseCode);
            if (duplicate) return (false, "Sinh viên đã đăng ký khóa học này rồi");

            if (course.IsFull) return (false, $"Khóa học đã đủ sĩ số ({course.Capacity}/{course.Capacity})");

            Enrollments.Add(new Enrollment { StudentId = studentId, CourseCode = courseCode });
            course.EnrolledCount++;
            return (true, "Ghi danh thành công");
        }

        // ============ Grade helpers ============
        public void SaveGrade(string studentId, string courseCode, double score)
        {
            var existing = Grades.FirstOrDefault(g => g.StudentId == studentId && g.CourseCode == courseCode);
            if (existing != null)
            {
                existing.Score = score;
                existing.UpdatedAt = DateTime.Now;
                NotifyGradeUpdated(existing);
            }
            else
            {
                var record = new GradeRecord { StudentId = studentId, CourseCode = courseCode, Score = score };
                Grades.Add(record);
                NotifyGradeUpdated(record);
            }
        }
    }
}
