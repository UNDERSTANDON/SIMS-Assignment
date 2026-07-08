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

        // Enrollment (roll-call) — delegate to facade and persist change
        public (bool success, string message) EnrollStudent(string studentId, string courseCode)
        {
            var result = _CSF.EnrollStudent(studentId, courseCode);
            if (result.success)
            {
                // persist course counts to storage CSV if available
                try
                {
                    var storeCourses = SIMS_WEB.Models.SimsDataStore.Instance.Courses;
                    // save web-model CSV files
                    SIMS_WEB.Storage.ModelFilePersistence.SaveCourses(storeCourses);
                    // also persist each course into IDataStorage (CSV engine) by mapping models
                    foreach (var c in storeCourses)
                    {
                        var a = new SIMS_Assignment.Models.Course
                        {
                            CourseId = c.Code,
                            CourseName = c.Title,
                            Credits = c.Capacity,
                            LecturerId = 0,
                            EnrolledStudentIds = new List<int>()
                        };
                        _storage.SaveCourseAsync(a).GetAwaiter().GetResult();
                    }
                }
                catch { }
            }
            return result;
        }

        public bool UnenrollStudent(string studentId, string courseCode)
        {
            var ok = _CSF.UnenrollStudent(studentId, courseCode);
            if (ok)
            {
                try
                {
                    var storeCourses = SIMS_WEB.Models.SimsDataStore.Instance.Courses;
                    SIMS_WEB.Storage.ModelFilePersistence.SaveCourses(storeCourses);
                    foreach (var c in storeCourses)
                    {
                        var a = new SIMS_Assignment.Models.Course
                        {
                            CourseId = c.Code,
                            CourseName = c.Title,
                            Credits = c.Capacity,
                            LecturerId = 0,
                            EnrolledStudentIds = new List<int>()
                        };
                        _storage.SaveCourseAsync(a).GetAwaiter().GetResult();
                    }
                }
                catch { }
            }
            return ok;
        }
    }
}
