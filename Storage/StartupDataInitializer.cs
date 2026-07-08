using SIMS_WEB.Models;

namespace SIMS_WEB.Storage
{
    public class StartupDataInitializer
    {
        private readonly string _dataDir = Path.Combine(AppContext.BaseDirectory, "DataStorage");
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
        }
    }
}
