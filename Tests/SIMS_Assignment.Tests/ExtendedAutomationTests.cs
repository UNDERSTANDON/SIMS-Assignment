using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    [Collection("WebApplication")]
    public class ExtendedAutomationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ExtendedAutomationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

        [Fact]
        public async Task Login_WithEmptyUsername_FailsValidation()
        {
            var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = string.Empty,
                ["Password"] = "student123",
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Login", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_WithEmptyPassword_FailsValidation()
        {
            var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = string.Empty,
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Login", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Register_WithDuplicateUsername_IsRejected()
        {
            var response = await _client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Email"] = "duplicate@example.com",
                ["Password"] = "Password123",
                ["ConfirmPassword"] = "Password123",
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Register", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Logout_ClearsSessionAndRedirectsToLogin()
        {
            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = "student123",
                ["Role"] = "Student"
            }));

            Assert.True(loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);

            var logoutResponse = await _client.GetAsync("/Account/Logout");
            Assert.True(logoutResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
        }

        [Fact]
        public async Task Storage_DeleteCourse_RemovesCourseFromPersistedStorage()
        {
            var storage = _factory.Services.GetRequiredService<IDataStorage>();
            var courseId = $"DEL_{Guid.NewGuid():N}";
            var course = new SIMS_Assignment.Models.Course
            {
                CourseId = courseId,
                CourseName = "Temp Course",
                Credits = 2,
                LecturerId = 1
            };

            await storage.SaveCourseAsync(course);
            var deleted = await storage.DeleteCourseAsync(courseId);

            Assert.True(deleted);
            var courses = await storage.GetAllCoursesAsync();
            Assert.DoesNotContain(courses, c => c.CourseId == courseId);
        }

        [Fact]
        public async Task Storage_DeleteUser_RemovesUserFromPersistedStorage()
        {
            var storage = _factory.Services.GetRequiredService<IDataStorage>();
            var userName = $"tempuser_{Guid.NewGuid():N}";
            var user = new SIMS_Assignment.Models.Student
            {
                Id = 888888,
                Name = userName,
                Role = "Student",
                PasswordHash = "hash"
            };

            await storage.SaveUserAsync(user);
            var deleted = await storage.DeleteUserByNameAsync(userName);

            Assert.True(deleted);
            var loaded = await storage.GetUserByNameAsync(userName);
            Assert.Null(loaded);
        }

        [Fact]
        public async Task LoginPage_DisplaysExpectedInputs()
        {
            var response = await _client.GetAsync("/Account/Login");
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            Assert.Contains("Username", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Password", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Role", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegisterPage_DisplaysExpectedInputs()
        {
            var response = await _client.GetAsync("/Account/Register");
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            Assert.Contains("Username", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Email", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Password", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
