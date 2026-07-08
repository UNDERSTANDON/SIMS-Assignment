using SIMS_Assignment.Models;

namespace SIMS_Assignment.Abstract
{
    public interface IDataStorage
    {
        Task<bool> SaveUserAsync(User user);
        Task<User> GetUserByNameAsync(string name);
        Task<bool> SaveCourseAsync(Course course);
        Task<List<Course>> GetAllCoursesAsync();
        Task<List<User>> GetAllUsersAsync();
    }
}
