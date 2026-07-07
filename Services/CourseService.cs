using SIMS_Assignment.Models;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Services.CourseServices;
using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services
{
    // Using facade
    public class CourseService
    {
        private readonly CourseServiceFacade _CSF;
        private readonly IDataStorage _storage;
        private Material _mat;
        private Assignment _assignment;
        private Submission _submission;

        public CourseService(IDataStorage storage, CourseServiceFacade facade,
        Material mat,
        Assignment assignment,
        Submission submission)
        {
            _storage = storage;
            _CSF = facade;
            _mat = mat;
            _assignment = assignment;
            _submission = submission;
        }

        // The course services will only call from the facade like a lib
        public void AddMaterials(Material material)
        {
            _CSF.AddMaterials(material);
        }

        public void EditMaterials(Material material)
        {
            _CSF.EditMaterials(material);
        }

        public void DeleteMaterials(string materialId)
        {
            _CSF.DeleteMaterials(materialId);
        }

        public void AddAssignment(Assignment assignment)
        {
            _CSF.AddAssignment(assignment);
        }

        public void EditAssignment(Assignment assignment)
        {
            _CSF.EditAssignment(assignment);
        }

        public void DeleteAssignment(string assignmentId)
        {
            _CSF.DeleteAssignment(assignmentId);
        }

        public void AddSubmission(Submission submission)
        {
            _CSF.AddSubmission(submission);
        }

        public void EditSubmission(Submission submission)
        {
            _CSF.EditSubmission(submission);
        }

        public void DeleteSubmission(string studentId, string assignmentTitle)
        {
            _CSF.DeleteSubmission(studentId, assignmentTitle);
        }
    }
}
