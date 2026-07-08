using SIMS_WEB.Models;

namespace SIMS_Assignment.Services
{
    public interface IEnrollmentManager
    {
        Task<(bool success, string message)> EnrollAsync(string studentId, string courseCode);
        Task<bool> UnenrollAsync(string studentId, string courseCode);
        Task<List<Enrollment>> GetEnrollmentsAsync();
    }
}
