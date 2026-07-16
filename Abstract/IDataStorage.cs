using SIMS_Assignment.Models;
using SIMS_Assignment.Models.CourseRelatedModels;
using SIMS_Assignment.Services.CourseServices;

namespace SIMS_Assignment.Abstract
{
    public interface IDataStorage
    {
        Task<bool> SaveUserAsync(User user);
        Task<User> GetUserByNameAsync(string name);
        Task<bool> SaveCourseAsync(Course course);
        Task<bool> DeleteCourseAsync(string courseId);
        Task<List<Course>> GetAllCoursesAsync();
        Task<List<User>> GetAllUsersAsync();
        Task<bool> DeleteUserByNameAsync(string name);
        
        // Materials management
        Task<List<Material>> GetAllMaterialsAsync();
        Task<bool> SaveMaterialAsync(Material material);
        Task<bool> DeleteMaterialAsync(string id);
        Task<List<FileMapping>> GetFileMappingsAsync();
        Task<bool> SaveFileMappingAsync(FileMapping mapping);
    }
}
