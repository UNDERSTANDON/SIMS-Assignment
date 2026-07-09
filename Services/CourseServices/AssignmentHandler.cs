using SIMS_Assignment.Models.CourseRelatedModels;
using Microsoft.AspNetCore.Hosting;
using SIMS_Assignment.Storage;

namespace SIMS_Assignment.Services.CourseServices
{
    public class AssignmentHandler
    {
        // Basic CRUD for assignment
        protected readonly List<Assignment> _assignments;
        private readonly JsonStorage<Assignment> _storage;
        private readonly object _lock = new();

        public AssignmentHandler(IWebHostEnvironment env)
        {
            _storage = new JsonStorage<Assignment>(env, "assignments.json");
            _assignments = _storage.Load();
        }

        public void AddAssignment(Assignment assignment)
        {
            lock (_lock)
            {
                _assignments.Add(assignment);
                _storage.Save(_assignments);
            }
        }

        public void EditAssignment(Assignment assignment)
        {
            lock (_lock)
            {
                var assignmentToRemove = _assignments.FirstOrDefault(a => a.Id == assignment.Id);
                if (assignmentToRemove != null)
                {
                    _assignments.Remove(assignmentToRemove);
                }
                _assignments.Add(assignment);
                _storage.Save(_assignments);
            }
        }

        public void DeleteAssignment(string assignmentId)
        {
            lock (_lock)
            {
                var assignmentToRemove = _assignments.FirstOrDefault(a => a.Id == assignmentId);
                if (assignmentToRemove != null)
                {
                    _assignments.Remove(assignmentToRemove);
                    _storage.Save(_assignments);
                }
            }
        }

        // Read access for controllers/views
        public List<Assignment> GetAll()
        {
            lock (_lock)
            {
                return _assignments.ToList();
            }
        }
    }
}