using SIMS_Assignment.Models.CourseRelatedModels;
using Microsoft.AspNetCore.Hosting;

namespace SIMS_Assignment.Services.CourseServices
{
    public class CourseServiceFacade : ICourseServices
    {
        private readonly MaterialHandler _materialHandler;
        private readonly AssignmentHandler _assignmentHandler;
        private readonly SubmissionHandler _submissionHandler;
        public CourseServiceFacade(IWebHostEnvironment env)
        {
            _materialHandler = new MaterialHandler(env);
            _assignmentHandler = new AssignmentHandler(env);
            _submissionHandler = new SubmissionHandler(env);
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

        // Enrollment
        public (bool success, string message) EnrollStudent(string studentId, string courseCode)
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            return store.Enroll(studentId, courseCode);
        }

        public bool UnenrollStudent(string studentId, string courseCode)
        {
            var store = SIMS_WEB.Models.SimsDataStore.Instance;
            var enrollment = store.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode);
            if (enrollment == null) return false;
            store.Enrollments.Remove(enrollment);
            var course = store.Courses.FirstOrDefault(c => c.Code == courseCode);
            if (course != null && course.EnrolledCount > 0) course.EnrolledCount--;
            return true;
        }
    }
}
