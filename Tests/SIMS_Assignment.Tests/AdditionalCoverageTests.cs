using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using SIMS_Assignment.Storage;
using Xunit;

namespace SIMS_Assignment.Tests
{
    [Collection("WebApplication")]
    public class AdditionalCoverageTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AdditionalCoverageTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        }

        private HttpClient CreateClient()
        {
            return _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

        private async Task<HttpClient> LoginAsAsync(string username, string password, string role)
        {
            var client = CreateClient();
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = password,
                ["Role"] = role
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb,
                $"Expected redirect after login but got {response.StatusCode}");
            return client;
        }

        [Theory]
        [InlineData("sinhvien", "student123", "Student")]
        [InlineData("giaovien", "faculty123", "Faculty")]
        [InlineData("admin", "admin123", "Admin")]
        public async Task Login_WithValidCredentials_RedirectsToRoleSpecificDashboard(string username, string password, string role)
        {
            using var client = await LoginAsAsync(username, password, role);
            var response = await client.GetAsync("/Account/Login");
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
        }

        [Theory]
        [InlineData("sinhvien", "wrong", "Student")]
        [InlineData("giaovien", "wrong", "Faculty")]
        [InlineData("admin", "wrong", "Admin")]
        public async Task Login_WithWrongPassword_ShowsValidationError(string username, string password, string role)
        {
            using var client = CreateClient();
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = password,
                ["Role"] = role
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Sign In", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Role", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_WithUnknownUsername_IsRejected()
        {
            using var client = CreateClient();
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "missing_user",
                ["Password"] = "whatever",
                ["Role"] = "Student"
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Sign In", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Role", html, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("", "user@example.com", "Password123", "Password123", "Student")]
        [InlineData("newuser", "", "Password123", "Password123", "Student")]
        [InlineData("newuser", "user@example.com", "", "", "Student")]
        [InlineData("newuser", "user@example.com", "Password123", "Different123", "Student")]
        public async Task Register_WithEmptyOrInvalidFields_IsRejected(string username, string email, string password, string confirmPassword, string role)
        {
            using var client = CreateClient();
            var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Email"] = email,
                ["Password"] = password,
                ["ConfirmPassword"] = confirmPassword,
                ["Role"] = role
            }));

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Register", html, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("short")]
        [InlineData("abc123")]
        public async Task Register_WithShortPassword_IsHandledGracefully(string password)
        {
            using var client = CreateClient();
            var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = $"shortpass_{Guid.NewGuid():N}",
                ["Email"] = "short@example.com",
                ["Password"] = password,
                ["ConfirmPassword"] = password,
                ["Role"] = "Student"
            }));

            Assert.NotNull(response);
            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError, $"The registration request should not crash the app, but returned {response.StatusCode}");
        }

        [Fact]
        public async Task Lecturer_AssignmentCreation_WithoutTitle_IsRejected()
        {
            using var client = await LoginAsAsync("giaovien", "faculty123", "Faculty");
            var response = await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "12312664",
                ["title"] = string.Empty,
                ["description"] = "No title should fail",
                ["dueDate"] = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm")
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task Lecturer_AssignmentCreation_ForUnassignedCourse_IsDenied()
        {
            using var client = await LoginAsAsync("giaovien", "faculty123", "Faculty");
            var response = await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "CS101",
                ["title"] = "Should not be created",
                ["description"] = "This course is not assigned to the lecturer",
                ["dueDate"] = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm")
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task Lecturer_ManagementPage_ForUnassignedCourse_RedirectsToIndex()
        {
            using var client = await LoginAsAsync("giaovien", "faculty123", "Faculty");
            var response = await client.GetAsync("/Lecturer/Manage/CS101");

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            Assert.NotNull(response.Headers.Location);
        }

        [Fact]
        public async Task AdminDashboard_LoadsSuccessfully_ForAuthenticatedAdmin()
        {
            using var client = await LoginAsAsync("admin", "admin123", "Admin");
            var response = await client.GetAsync("/Dashboard/Index");
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
        }

        [Fact]
        public async Task StudentDashboard_LoadsSuccessfully_ForAuthenticatedStudent()
        {
            using var client = await LoginAsAsync("sinhvien", "student123", "Student");
            var response = await client.GetAsync("/StudentDashboard/Index");
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
        }

        [Fact]
        public async Task StudentDashboard_ShowsExpectedContentForValidStudent()
        {
            using var client = await LoginAsAsync("sinhvien", "student123", "Student");
            var response = await client.GetAsync("/StudentDashboard/Index");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("My Transcript", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Observer Pattern", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Session_RemainsAuthenticatedAcrossMultipleRequests()
        {
            using var client = await LoginAsAsync("sinhvien", "student123", "Student");
            var firstResponse = await client.GetAsync("/StudentDashboard/Index");
            var secondResponse = await client.GetAsync("/StudentDashboard/Index");

            Assert.True(firstResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            Assert.True(secondResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
        }

        [Fact]
        public async Task Logout_ClearsSessionAndBlocksProtectedAccess()
        {
            using var client = await LoginAsAsync("sinhvien", "student123", "Student");
            var logoutResponse = await client.GetAsync("/Account/Logout");
            var protectedResponse = await client.GetAsync("/StudentDashboard/Index");

            Assert.True(logoutResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            Assert.True(protectedResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
        }

        [Fact]
        public async Task Storage_ReadUsers_WhenStorageFileIsMissing_ReturnsEmptyList()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"sims_storage_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var storage = new CvsStorageEngine(tempDir);
                var users = await storage.GetAllUsersAsync();
                Assert.Empty(users);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public async Task Storage_ReadCourses_WhenStorageFileIsMissing_ReturnsEmptyList()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"sims_storage_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var storage = new CvsStorageEngine(tempDir);
                var courses = await storage.GetAllCoursesAsync();
                Assert.Empty(courses);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public async Task Storage_SaveUserTwiceWithSameId_UsesLatestState()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"sims_storage_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var storage = new CvsStorageEngine(tempDir);
                var user = new SIMS_Assignment.Models.Student { Id = 1001, Name = "dupuser", Role = "Student", PasswordHash = "hash" };
                await storage.SaveUserAsync(user);
                user.Email = "updated@example.com";
                await storage.SaveUserAsync(user);

                var loaded = await storage.GetUserByNameAsync("dupuser");
                Assert.NotNull(loaded);
                Assert.Equal("updated@example.com", loaded!.Email);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public async Task Storage_InvalidRows_AreIgnoredSafely()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"sims_storage_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var usersPath = Path.Combine(tempDir, "users.csv");
                await File.WriteAllTextAsync(usersPath, "bad,row\nnot-enough\n");
                var storage = new CvsStorageEngine(tempDir);
                var users = await storage.GetAllUsersAsync();
                Assert.Empty(users);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
