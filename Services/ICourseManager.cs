using SIMS_WEB.Models;

namespace SIMS_Assignment.Services
{
    public interface ICourseManager
    {
        Task<List<Course>> GetAllAsync();
        Task<Course?> GetByCodeAsync(string code);
        Task<bool> CreateAsync(Course course);
        Task<bool> UpdateAsync(Course course);
        Task<bool> DeleteAsync(string code);
    }
}
