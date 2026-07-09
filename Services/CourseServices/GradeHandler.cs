using Microsoft.AspNetCore.Hosting;
using SIMS_Assignment.Storage;
using SIMS_WEB.Models;

namespace SIMS_Assignment.Services.CourseServices
{
    public class GradeHandler
    {
        private readonly List<GradeRecord> _grades;
        private readonly JsonStorage<GradeRecord> _storage;
        private readonly object _lock = new();

        public GradeHandler(IWebHostEnvironment env)
        {
            _storage = new JsonStorage<GradeRecord>(env, "grades.json");
            _grades = _storage.Load();
        }

        public List<GradeRecord> GetAll()
        {
            lock (_lock)
            {
                return _grades.ToList();
            }
        }

        public List<GradeRecord> GetByCourse(string courseCode)
        {
            lock (_lock)
            {
                return _grades.Where(g => g.CourseCode == courseCode).ToList();
            }
        }

        public GradeRecord SaveGrade(string studentId, string courseCode, double score)
        {
            lock (_lock)
            {
                var existing = _grades.FirstOrDefault(
                    g => g.StudentId == studentId && g.CourseCode == courseCode);
                if (existing != null)
                {
                    existing.Score = score;
                    existing.UpdatedAt = DateTime.Now;
                    _storage.Save(_grades);
                    return existing;
                }

                var record = new GradeRecord
                {
                    StudentId = studentId,
                    CourseCode = courseCode,
                    Score = score,
                    UpdatedAt = DateTime.Now
                };
                _grades.Add(record);
                _storage.Save(_grades);
                return record;
            }
        }

        public void RemoveByStudent(string studentId)
        {
            lock (_lock)
            {
                var removed = _grades.RemoveAll(g => g.StudentId == studentId);
                if (removed > 0) _storage.Save(_grades);
            }
        }

        public void RemoveByCourse(string courseCode)
        {
            lock (_lock)
            {
                var removed = _grades.RemoveAll(g => g.CourseCode == courseCode);
                if (removed > 0) _storage.Save(_grades);
            }
        }

        public void SyncToStore(SIMS_WEB.Models.SimsDataStore store)
        {
            lock (_lock)
            {
                store.Grades.Clear();
                store.Grades.AddRange(_grades);
            }
        }
    }
}
