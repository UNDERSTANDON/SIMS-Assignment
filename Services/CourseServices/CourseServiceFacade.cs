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
        // Implement methods from ICourseServices interface and delegate to the appropriate handlers
        public void AddMaterials(Material material)
        {
            _materialHandler.AddMaterials(material);
        }

        public void EditMaterials(Material material, int i)
        {
            _materialHandler.EditMaterials(material, i);
        }

        public void DeleteMaterials(int materialId, int i)
        {
            _materialHandler.DeleteMaterials(materialId, i);
        }

        public void AddAssignment(Assignment assignment)
        {
            _assignmentHandler.AddAssignment(assignment);
        }

        public void EditAssignment(Assignment assignment)
        {
            _assignmentHandler.EditAssignment(assignment);
        }

        public void DeleteAssignment(int assignmentId)
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

        public void DeleteSubmission(int submissionId)
        {
            _submissionHandler.DeleteSubmission(submissionId);
        }
    }
}
