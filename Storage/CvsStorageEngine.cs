using System.Runtime.CompilerServices;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Storage
{
    public class CvsStorageEngine : IDataStorage
    {
        private string _directory;
        private string _content;

        // Implement later, currently mapping out the structure
        public Task<User> GetUserByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveCourseAsync(Course course)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveUserAsync(User user)
        {
            throw new NotImplementedException();
        }
    }
}
