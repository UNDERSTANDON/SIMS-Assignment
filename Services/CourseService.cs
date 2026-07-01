namespace SIMS_Assignment.Services
{
    public abstract class CourseService
    {
        // Course service has an association with only Course model, so it can manage courses directly.
        // Of course, if the course service grew too large, we could consider splitting it into multiple services or using a repository pattern.
        public CourseService() { }

        // We will have a service class that handle material
        // And another to handle assignments and grading.
        // Only admin can create courses, but lecturers have all rights to update the course content and manage assignments.

    }
}
