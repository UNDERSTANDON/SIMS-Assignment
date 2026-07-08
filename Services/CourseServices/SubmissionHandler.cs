using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class SubmissionHandler
    {
        // Basic CRUD for submission
        private readonly List<Submission> _submissions = new();
        public void AddSubmission(Submission submission)
        {
            _submissions.Add(submission);
        }
        public void EditSubmission(Submission submission)
        {
            DeleteSubmission(submission.StudentId, submission.AssignmentTitle);
            _submissions.Add(submission);
        }
        public void DeleteSubmission(string studentId, string assignmentTitle)
        {
            var submissionToRemove = _submissions.FirstOrDefault(s => s.StudentId == studentId && s.AssignmentTitle == assignmentTitle);
            if (submissionToRemove != null)
            {
                _submissions.Remove(submissionToRemove);
            }
        }

        // Read access
        public List<Submission> GetAll() => _submissions;
        public List<Submission> GetByAssignment(string assignmentTitle) => _submissions.Where(s => s.AssignmentTitle == assignmentTitle).ToList();
    }
}
