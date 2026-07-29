using System.Linq;
using SIMS_WEB.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    public class StudentManagerTests
    {
        [Fact]
        public void Enroll_ValidStudentAndOpenCourse_Succeeds()
        {
            // Arrange
            var store = SimsDataStore.Instance;
            string studentId = "SV9999001";
            string courseCode = "TEST_CS101";

            // Cleanup if exists
            var existingCourse = store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (existingCourse == null)
            {
                store.Courses.Add(new Course { Code = courseCode, Title = "Test Course", Capacity = 30, EnrolledCount = 0 });
            }

            // Act
            var (success, message) = store.Enroll(studentId, courseCode);

            // Assert
            Assert.True(success, $"Expected enrollment to succeed but got message: {message}");
            Assert.Contains(store.Enrollments, e => e.StudentId == studentId && e.CourseCode == courseCode);
        }

        [Fact]
        public void Enroll_DuplicateEnrollment_ReturnsError()
        {
            // Arrange
            var store = SimsDataStore.Instance;
            string studentId = "SV9999002";
            string courseCode = "TEST_CS102";

            var course = store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null)
            {
                store.Courses.Add(new Course { Code = courseCode, Title = "Test Course 2", Capacity = 30, EnrolledCount = 0 });
            }

            store.Enroll(studentId, courseCode); // First enrollment

            // Act
            var (success, message) = store.Enroll(studentId, courseCode); // Duplicate attempt

            // Assert
            Assert.False(success);
            Assert.Equal("Sinh viên đã đăng ký khóa học này rồi", message);
        }

        [Fact]
        public void Enroll_FullCourse_ReturnsCapacityError()
        {
            // Arrange
            var store = SimsDataStore.Instance;
            string studentId = "SV9999003";
            string courseCode = "TEST_FULL101";

            var course = store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course == null)
            {
                course = new Course { Code = courseCode, Title = "Full Course", Capacity = 5, EnrolledCount = 5 };
                store.Courses.Add(course);
            }
            else
            {
                course.Capacity = 5;
                course.EnrolledCount = 5;
            }

            // Act
            var (success, message) = store.Enroll(studentId, courseCode);

            // Assert
            Assert.False(success);
            Assert.Contains("đã đủ sĩ số", message);
        }

        [Fact]
        public void SaveGrade_And_RemoveStudent_CleansUpRelatedRecords()
        {
            // Arrange
            var store = SimsDataStore.Instance;
            string studentId = "SV9999004";
            string courseCode = "TEST_GRADE101";

            store.Students.Add(new Student { StudentId = studentId, FullName = "Test Grade Student", Email = "test@univ.edu" });
            store.SaveGrade(studentId, courseCode, 8.5);

            // Act
            bool removed = store.RemoveStudent(studentId);

            // Assert
            Assert.True(removed);
            Assert.DoesNotContain(store.Students, s => s.StudentId == studentId);
            Assert.DoesNotContain(store.Grades, g => g.StudentId == studentId && g.CourseCode == courseCode);
        }
    }
}
