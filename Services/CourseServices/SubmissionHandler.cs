using System.Text.Json;
using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class SubmissionHandler
    {
        // Basic CRUD for submission
        private readonly List<Submission> _submissions = new();
        private readonly string _storagePath;
        private readonly object _fileLock = new();

        public SubmissionHandler()
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "DataStorage");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            _storagePath = Path.Combine(dataDir, "submissions.json");
            LoadFromDisk();
        }

        public void AddSubmission(Submission submission)
        {
            if (string.IsNullOrEmpty(submission.Id)) submission.Id = Guid.NewGuid().ToString("N");
            _submissions.Add(submission);
            SaveToDisk();
        }

        public void EditSubmission(Submission submission)
        {
            var existing = _submissions.FirstOrDefault(s => s.Id == submission.Id);
            if (existing != null)
            {
                _submissions.Remove(existing);
            }
            else
            {
                // fallback to remove by student/assignment
                var bySa = _submissions.FirstOrDefault(s => s.StudentId == submission.StudentId && s.AssignmentTitle == submission.AssignmentTitle);
                if (bySa != null) _submissions.Remove(bySa);
            }
            if (string.IsNullOrEmpty(submission.Id)) submission.Id = Guid.NewGuid().ToString("N");
            _submissions.Add(submission);
            SaveToDisk();
        }

        public void DeleteSubmission(string submissionId)
        {
            var submissionToRemove = _submissions.FirstOrDefault(s => s.Id == submissionId);
            if (submissionToRemove != null)
            {
                _submissions.Remove(submissionToRemove);
                SaveToDisk();
            }
        }

        public void DeleteSubmission(string studentId, string assignmentTitle)
        {
            var submissionToRemove = _submissions.FirstOrDefault(s => s.StudentId == studentId && s.AssignmentTitle == assignmentTitle);
            if (submissionToRemove != null)
            {
                _submissions.Remove(submissionToRemove);
                SaveToDisk();
            }
        }

        // Read access
        public List<Submission> GetAll() => _submissions;
        public List<Submission> GetByAssignment(string assignmentTitle) => _submissions.Where(s => s.AssignmentTitle == assignmentTitle).ToList();
        public Submission? GetById(string id) => _submissions.FirstOrDefault(s => s.Id == id);

        private void SaveToDisk()
        {
            try
            {
                lock (_fileLock)
                {
                    var opts = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(_submissions, opts);
                    File.WriteAllText(_storagePath, json);
                }
            }
            catch { }
        }

        private void LoadFromDisk()
        {
            try
            {
                lock (_fileLock)
                {
                    if (!File.Exists(_storagePath)) return;
                    var json = File.ReadAllText(_storagePath);
                    var list = JsonSerializer.Deserialize<List<Submission>>(json);
                    if (list != null)
                    {
                        _submissions.Clear();
                        _submissions.AddRange(list);
                    }
                }
            }
            catch { }
        }
    }
}
