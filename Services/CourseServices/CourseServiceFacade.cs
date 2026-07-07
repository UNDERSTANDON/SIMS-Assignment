using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class CourseServiceFacade : ICourseServices
    {
        private readonly MaterialHandler _materialHandler;
        private readonly AssignmentHandler _assignmentHandler;
        private readonly SubmissionHandler _submissionHandler;
        public CourseServiceFacade()
        {
            _materialHandler = new MaterialHandler();
            _assignmentHandler = new AssignmentHandler();
            _submissionHandler = new SubmissionHandler();
        }
        public void AddMaterials(Material material)
        {
            _materialHandler.AddMaterial(material);
        }

        public void EditMaterials(Material material)
        {
            _materialHandler.EditMaterial(material);
        }

        public void DeleteMaterials(string materialId)
        {
            _materialHandler.DeleteMaterial(materialId);
        }

        public void AddAssignment(Assignment assignment)
        {
            _assignmentHandler.AddAssignment(assignment);
        }

        public void EditAssignment(Assignment assignment)
        {
            _assignmentHandler.EditAssignment(assignment);
        }

        public void DeleteAssignment(string assignmentId)
        {
            _assignmentHandler.DeleteAssignment(assignmentId);
        }

        public void AddSubmission(Submission submission)
        {
            _submissionHandler.AddSubmission(submission);
        }

        public void EditSubmission(Submission submission)
        {
            _submissionHandler.EditSubmission(submission);
        }

        public void DeleteSubmission(string studentId, string assignmentTitle)
        {
            _submissionHandler.DeleteSubmission(studentId, assignmentTitle);
        }
    }
}
