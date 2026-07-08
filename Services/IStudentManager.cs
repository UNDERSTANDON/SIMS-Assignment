using SIMS_WEB.Models;

namespace SIMS_Assignment.Services
{
    public interface IStudentManager
    {
        Task<List<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(string studentId);
        Task<bool> CreateAsync(Student student);
        Task<bool> UpdateAsync(Student student);
        Task<bool> DeleteAsync(string studentId);
        Task<int> ImportFromStreamAsync(System.IO.Stream csvStream);
    }
}
