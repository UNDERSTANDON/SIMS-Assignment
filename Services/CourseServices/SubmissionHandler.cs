using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class SubmissionHandler
    {
        protected readonly List<Submission> _submissions;
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
    }
}
