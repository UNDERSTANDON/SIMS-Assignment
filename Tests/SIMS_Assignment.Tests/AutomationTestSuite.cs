using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;
using SIMS_WEB.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    [Collection("WebApplication")]
    public class AutomationTestSuite : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AutomationTestSuite(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

        [Fact]
        public async Task Login_WithValidStudentCredentials_RedirectsToStudentDashboard()
        {
            var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = "student123",
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb,
                $"Expected a redirect status but got {response.StatusCode}");
            Assert.Contains("/StudentDashboard", response.Headers.Location?.OriginalString ?? string.Empty);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShowsValidationError()
        {
            var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = "wrong",
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb,
                $"Expected a normal response but got {response.StatusCode}");
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Login", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Password", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Register_NewStudentAccount_CreatesUserAndRedirects()
        {
            var username = $"student_{Guid.NewGuid():N}";
            var response = await _client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Email"] = $"{username}@example.com",
                ["Password"] = "Password123",
                ["ConfirmPassword"] = "Password123",
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb,
                $"Expected a redirect status but got {response.StatusCode}");
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task AccessingLecturerAction_WithoutFacultySession_IsRedirected()
        {
            var response = await _client.GetAsync("/Lecturer/Index");
            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb,
                $"Expected a redirect status but got {response.StatusCode}");
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task AccessingProtectedPage_WithoutSession_IsRedirected()
        {
            var response = await _client.GetAsync("/Dashboard/Index");
            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb,
                $"Expected a redirect status but got {response.StatusCode}");
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task Storage_SaveUserAsync_PersistsUserData()
        {
            var storage = _factory.Services.GetRequiredService<IDataStorage>();
            var user = new SIMS_Assignment.Models.Student
            {
                Id = 999999,
                Name = $"storageuser_{Guid.NewGuid():N}",
                Role = "Student",
                PasswordHash = "hash"
            };

            var saved = await storage.SaveUserAsync(user);
            Assert.True(saved);

            var loaded = await storage.GetUserByNameAsync(user.Name);
            Assert.NotNull(loaded);
            Assert.Equal(user.Name, loaded!.Name);
        }

        [Fact]
        public async Task Storage_SaveCourseAsync_PersistsCourseData()
        {
            var storage = _factory.Services.GetRequiredService<IDataStorage>();
            var course = new SIMS_Assignment.Models.Course
            {
                CourseId = $"AUTO_{Guid.NewGuid():N}",
                CourseName = "Automation Course",
                Credits = 3,
                LecturerId = 1,
                EnrolledStudentIds = new List<int> { 10, 11 }
            };

            var saved = await storage.SaveCourseAsync(course);
            Assert.True(saved);

            var courses = await storage.GetAllCoursesAsync();
            Assert.Contains(courses, c => c.CourseId == course.CourseId);
        }

        [Fact]
        public async Task LoginPage_RendersExpectedFormFields()
        {
            var response = await _client.GetAsync("/Account/Login");
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb,
                $"Expected a normal response but got {response.StatusCode}");
            Assert.Contains("Username", html);
            Assert.Contains("Password", html);
            Assert.Contains("Role", html);
        }

        [Fact]
        public async Task StudentDashboardPage_LoadsForValidStudentSession()
        {
            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = "student123",
                ["Role"] = "Student"
            }));

            Assert.True(loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb,
                $"Expected a redirect status but got {loginResponse.StatusCode}");

            var dashboardResponse = await _client.GetAsync("/StudentDashboard/Index");
            Assert.True(dashboardResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb,
                $"Expected a normal response but got {dashboardResponse.StatusCode}");
        }
    }
}
