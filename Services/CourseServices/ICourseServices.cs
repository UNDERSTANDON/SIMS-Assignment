using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public interface ICourseServices
    {
        // Basic CRUD operations for Course
        // I may transform these to async methods in the future
        // Materials
        void AddMaterials(Material material);
        void EditMaterials(Material material);
        void DeleteMaterials(int materialId);

        // Assignments
        void AddAssignment(Assignment assignment);
        void EditAssignment(Assignment assignment);
        void DeleteAssignment(int assignmentId);

        // Submissions
        void AddSubmission(Submission submission);
        void EditSubmission(Submission submission);
        void DeleteSubmission(int submissionId);
    }
}
