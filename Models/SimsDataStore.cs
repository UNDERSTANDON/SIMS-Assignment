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
            // Try to load students, courses, enrollments, and grades from the CSV-backed DataStorage on startup.
            try
            {
                SIMS_WEB.Storage.ModelFilePersistence.EnsureDataDir();
                var loadedStudents = SIMS_WEB.Storage.ModelFilePersistence.LoadStudents();
                if (loadedStudents != null && loadedStudents.Any())
                {
                    Students.AddRange(loadedStudents);
                }
                var loadedCourses = SIMS_WEB.Storage.ModelFilePersistence.LoadCourses();
                if (loadedCourses != null && loadedCourses.Any())
                {
                    Courses.AddRange(loadedCourses);
                }
                var loadedEnrollments = SIMS_WEB.Storage.ModelFilePersistence.LoadEnrollments();
                if (loadedEnrollments != null && loadedEnrollments.Any())
                {
                    Enrollments.AddRange(loadedEnrollments);
                }
                var loadedGrades = SIMS_WEB.Storage.ModelFilePersistence.LoadGrades();
                if (loadedGrades != null && loadedGrades.Any())
                {
                    Grades.AddRange(loadedGrades);
                }
            }
            catch { }

            // If CSVs were absent or empty, fall back to embedded demo data
            if (!Students.Any())
            {
                Students.AddRange(new[]
                {
                    new Student { StudentId = "SV2024001", FullName = "Nguyễn Văn An",    Program = "Công nghệ Thông tin",  Email = "an.nv@univ.edu" },
                    new Student { StudentId = "SV2024002", FullName = "Trần Thị Bình",   Program = "Kỹ thuật Phần mềm",   Email = "binh.tt@univ.edu" },
                    new Student { StudentId = "SV2024003", FullName = "Lê Hoàng Cường",  Program = "Khoa học Máy tính",    Email = "cuong.lh@univ.edu" },
                    new Student { StudentId = "SV2024004", FullName = "Phạm Thị Dung",   Program = "Quản trị Kinh doanh",  Email = "dung.pt@univ.edu" },
                });
            }

            if (!Courses.Any())
            {
                Courses.AddRange(new[]
                {
                    new Course { Code = "CS101", Title = "Lập trình Căn bản",     Capacity = 40, EnrolledCount = 12, Instructor = "GV. Nguyễn" },
                    new Course { Code = "CS202", Title = "Cấu trúc Dữ liệu",     Capacity = 35, EnrolledCount = 28, Instructor = "GV. Trần" },
                    new Course { Code = "BA301", Title = "Quản trị Kinh doanh",   Capacity = 50, EnrolledCount = 45, Instructor = "GV. Lê" },
                    new Course { Code = "SE401", Title = "Kỹ nghệ Phần mềm",     Capacity = 30, EnrolledCount = 5,  Instructor = "GV. Phạm" },
                });
            }
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
            try
            {
                SIMS_WEB.Storage.ModelFilePersistence.SaveGrades(Grades);
            }
            catch { }
        }

        // ============ Removal helpers (clean-up related data) ============
        public bool RemoveStudent(string studentId)
        {
            var student = Students.FirstOrDefault(s => s.StudentId == studentId);
            if (student == null) return false;

            // Remove student record
            Students.Remove(student);

            // Remove enrollments and decrement course counts
            var enrolls = Enrollments.Where(e => e.StudentId == studentId).ToList();
            foreach (var e in enrolls)
            {
                var course = Courses.FirstOrDefault(c => c.Code == e.CourseCode);
                if (course != null && course.EnrolledCount > 0)
                    course.EnrolledCount--;
                Enrollments.Remove(e);
            }

            // Remove grades for the student
            Grades.RemoveAll(g => g.StudentId == studentId);
            try
            {
                SIMS_WEB.Storage.ModelFilePersistence.SaveGrades(Grades);
            }
            catch { }
 
            return true;
        }

        public bool RemoveCourse(string courseCode)
        {
            var course = Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null) return false;

            // Remove enrollments for the course
            var enrolls = Enrollments.Where(e => e.CourseCode == courseCode).ToList();
            foreach (var e in enrolls)
            {
                Enrollments.Remove(e);
            }

            // Remove grades associated with the course
            Grades.RemoveAll(g => g.CourseCode == courseCode);
            try
            {
                SIMS_WEB.Storage.ModelFilePersistence.SaveGrades(Grades);
            }
            catch { }
 
            // Finally remove the course
            Courses.Remove(course);
            return true;
        }
    }
}
