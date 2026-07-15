using SIMS_WEB.Models;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Models;

namespace SIMS_WEB.Storage
{
    public class StartupDataInitializer
    {
        private readonly IDataStorage _storage;
        private readonly IPasswordHasher _hasher;
        private string _dataDir => SIMS_WEB.Storage.ModelFilePersistence.DataDir;

        public StartupDataInitializer(IDataStorage storage, IPasswordHasher hasher)
        {
            _storage = storage;
            _hasher = hasher;
        }

        public void Initialize()
        {
            ModelFilePersistence.EnsureDataDir();

            var studentsPath = Path.Combine(_dataDir, "students.csv");
            var coursesPath = Path.Combine(_dataDir, "courses.csv");
            var usersPath = Path.Combine(_dataDir, "users.csv");

            var store = SimsDataStore.Instance;

            if (!File.Exists(studentsPath))
            {
                try { ModelFilePersistence.SaveStudents(store.Students); } catch { }
            }

            if (!File.Exists(coursesPath))
            {
                try { ModelFilePersistence.SaveCourses(store.Courses); } catch { }
            }

            if (!File.Exists(usersPath))
            {
                // create an empty users.csv to ensure CvsStorageEngine can read/write later
                try { File.WriteAllText(usersPath, "Id,Name,Role,PasswordHash\n"); } catch { }
            }

            // Seed default users if users.csv has no users
            try
            {
                var users = _storage.GetAllUsersAsync().GetAwaiter().GetResult();
                if (users == null || !users.Any())
                {
                    var admin = new SIMS_Assignment.Models.Admin { Name = "admin", Role = "Admin", PasswordHash = _hasher.Hash("admin123") };
                    var faculty = new SIMS_Assignment.Models.Lecturer { Name = "giaovien", Role = "Faculty", PasswordHash = _hasher.Hash("faculty123") };
                    var student = new SIMS_Assignment.Models.Student { Name = "sinhvien", Role = "Student", PasswordHash = _hasher.Hash("student123") };

                    _storage.SaveUserAsync(admin).GetAwaiter().GetResult();
                    _storage.SaveUserAsync(faculty).GetAwaiter().GetResult();
                    _storage.SaveUserAsync(student).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding users: {ex}");
            }
        }
    }
}
