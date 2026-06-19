# Name: Student Information Management System (SIMS)
System: Students information management system

# Functional requirements
1. Student Registration:
+ System should allow for the efficient registration of new students.
+ Capture and store essential student information, including personal details and academic records.
2. Course Management:
+ Provide functionality for administrators to manage courses offered by the university.
+ Assign students to courses based on their academic program.
3. User Authentication and Authorization:
+ Ensure secure user authentication for students, faculty, and administrators.
+ Implement role-based access control to restrict system functionalities based on user roles.

# Non-Functional requirements
1. Scalability:
+ The system should be scalable to accommodate a growing number of students and courses over time.
2. Performance:
+ Ensure that the system responds to user requests within acceptable time frames, even during peak usage.
3. Security:
+ Implement robust security measures to protect sensitive student information and ensure data integrity.
4. Usability:
+ Design a user-friendly interface that accommodates users with varying levels of technical expertise.
5. Accessibility:
+ Ensure the system is accessible to users with disabilities, complying with accessibility standards.
6. Reliability:
+ The system should be reliable, with minimal downtime for maintenance or unexpected issues.

# Architecture and Design Requirements
1. Design Principles & Architecture:
+ Adhere to SOLID principles (SRP, OCP, LSP, ISP, DIP) across the system design.
+ Implement clean coding techniques (meaningful naming, modularity, comments) to manage data structures and algorithms effectively.
+ Utilize multiple design patterns (creational, structural, and behavioural).
2. Documentation & Modeling:
+ Create a Use Case Diagram, Class Diagram, and Package Diagram.
3. Quality Assurance:
+ Design a suitable testing regime for the application, including provisions for automated testing.

# Extra UI/UX
1. Login/Register
2. Dash Board
3. Profile page
4. Course page

# Tools used
1. Programming Languages & Frameworks
	+ C#
	+ ASP .NET Core 
2. Storage
	+ CSV (Primary storage format to handle large datasets).
	+ Storage Abstraction Layer (Allows dynamic switching between CSV, MySQL, and Plain Text to satisfy the Dependency Inversion Principle).

# Detailed Blueprint
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

// ==========================================
// --- Entities (Data Models) ---
// ==========================================

public abstract class User 
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; } 
}

public class Student : User 
{
    public string CurrentProgram { get; set; }
    public Dictionary<string, bool> AcademicRecords { get; set; }
}

public class Admin : User 
{
    public string Department { get; set; }
}

public class Lecturer : User 
{
    public string Specialization { get; set; }
    public List<string> AssignedCourses { get; set; } = new List<string>();
}

public class Course 
{
    public string CourseId { get; set; }
    public string CourseName { get; set; }
    public int Credits { get; set; }
    public int LecturerId { get; set; } // Links to the Lecturer teaching the course
    public List<int> EnrolledStudentIds { get; set; } = new List<int>();
}

// ==========================================
// --- Storage Abstraction Layer (DIP & OCP) ---
// ==========================================

public interface IDataStorage 
{
    Task<bool> SaveUserAsync(User user);
    Task<User> GetUserByNameAsync(string name);
    Task<bool> SaveCourseAsync(Course course);
    // Password storage methods...
}

// Implementations (CsvStorage, MySqlStorage, PlainTextStorage) remain the same...

// ==========================================
// --- Services (SRP & Core Logic) ---
// ==========================================

public class AuthenticationService 
{
    private readonly IDataStorage _storage;

    public AuthenticationService(IDataStorage storage) 
    {
        _storage = storage;
    }

    public async Task<bool> AuthenticateAsync(string name, string password) 
    {
        // Hash password and compare with storage
        return true; 
    }
}

// New Abstract UserService handling common actor logic and authentication
public abstract class UserService 
{
    protected readonly AuthenticationService _authService;
    protected readonly IDataStorage _storage;

    // AuthenticationService now resides here and is passed down to all actors
    protected UserService(AuthenticationService authService, IDataStorage storage) 
    {
        _authService = authService;
        _storage = storage;
    }

    public async Task<bool> LoginAsync(string username, string password) 
    {
        return await _authService.AuthenticateAsync(username, password);
    }

    public abstract void ViewDashboard(); // To be implemented by specific actors
}

// Concrete Actor Services inheriting from UserService

public class StudentService : UserService 
{
    public StudentService(AuthenticationService authService, IDataStorage storage) 
        : base(authService, storage) { }

    public async Task RegisterForCourseAsync(Student student, Course course) 
    {
        // Logic to add student to course and update storage
    }

    public override void ViewDashboard() 
    {
        // Display Student Profile and Course pages
    }
}

public class AdminService : UserService 
{
    public AdminService(AuthenticationService authService, IDataStorage storage) 
        : base(authService, storage) { }

    public async Task CreateCourseAsync(Course newCourse) 
    {
        // Logic to create a new course offering
    }

    public async Task AssignLecturerAsync(Course course, Lecturer lecturer)
    {
        // Logic to assign a course to a lecturer
    }

    public override void ViewDashboard() 
    {
        // Display Admin management dashboard
    }
}

public class LecturerService : UserService 
{
    public LecturerService(AuthenticationService authService, IDataStorage storage) 
        : base(authService, storage) { }

    public async Task GradeStudentAsync(Student student, Course course, bool isPassed) 
    {
        // Logic to update student's academic records
    }

    public override void ViewDashboard() 
    {
        // Display Lecturer dashboard with assigned courses
    }
}
```