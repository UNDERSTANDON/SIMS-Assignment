using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class AssignmentHandler
    {
        // Basic CRUD for assignment
        protected readonly List<Assignment> _assignments = new();

        public void AddAssignment(Assignment assignment)
        {
            _assignments.Add(assignment);
        }

        public void EditAssignment(Assignment assignment)
        {
            DeleteAssignment(assignment.Id);
            _assignments.Add(assignment);
        }

        public void DeleteAssignment(string assignmentId)
        {
            var assignmentToRemove = _assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignmentToRemove != null)
            {
                _assignments.Remove(assignmentToRemove);
            }
        }
    }
}