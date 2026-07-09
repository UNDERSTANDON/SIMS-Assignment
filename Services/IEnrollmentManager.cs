using SIMS_WEB.Models;

namespace SIMS_Assignment.Services
{
    public interface IEnrollmentManager
    {
        Task<(bool success, string message)> EnrollAsync(string studentId, string courseCode);
        Task<bool> UnenrollAsync(string studentId, string courseCode);
        Task<List<Enrollment>> GetEnrollmentsAsync();
        Task<List<Enrollment>> GetEnrollmentsByCourseAsync(string courseCode);
        Task<List<Student>> GetEnrolledStudentsByCourseAsync(string courseCode);
        Task<int> GetEnrollmentCountAsync(string courseCode);
        Task<bool> IsStudentEnrolledAsync(string studentId, string courseCode);
    }
}
