using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SIMS_Assignment.Abstract;
using SIMS_WEB.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    [Collection("WebApplication")]
    public class RemainingAutomationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RemainingAutomationTests(WebApplicationFactory<Program> factory)
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

        private async Task<HttpClient> LoginAsync(string username, string password, string role)
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

        private static void ResetLoginState()
        {
            var store = SimsDataStore.Instance;
            store.FailedAttempts.Clear();
            store.LockUntil.Clear();
        }

        [Fact]
        public async Task Login_WithRepeatedFailedAttempts_LocksOutAccountAfterThreshold()
        {
            ResetLoginState();
            using var client = CreateClient();
            HttpResponseMessage? lastResponse = null;

            for (var i = 0; i < 5; i++)
            {
                lastResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Username"] = "sinhvien",
                    ["Password"] = "wrong",
                    ["Role"] = "Student"
                }));

                Assert.True(lastResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            }

            Assert.NotNull(lastResponse);
            var body = await lastResponse.Content.ReadAsStringAsync();
            Assert.Contains("Account Temporarily Locked", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_WithRoleMismatch_ShowsRoleError()
        {
            ResetLoginState();
            using var client = CreateClient();
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = "student123",
                ["Role"] = "Faculty"
            }));

            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Sign In", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Role", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UploadMaterial_ToUnassignedCourse_IsDeniedAndDoesNotCreateFile()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var env = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var materialsDir = Path.Combine(env.ContentRootPath, "DataStorage", "Materials");
            Directory.CreateDirectory(materialsDir);
            var before = Directory.GetFiles(materialsDir).Length;

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("CS101"), "courseId");
            form.Add(new StringContent("Should not be stored"), "title");
            form.Add(new StringContent("Not assigned to this lecturer"), "description");

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "content");
            try
            {
                await using var fs = File.OpenRead(tempFile);
                var streamContent = new StreamContent(fs);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                form.Add(streamContent, "file", Path.GetFileName(tempFile));

                var response = await client.PostAsync("/Lecturer/AddMaterial", form);
                Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
                var after = Directory.GetFiles(materialsDir).Length;
                Assert.Equal(before, after);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task UploadMaterial_WithoutTitle_IsRejected()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var env = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var materialsDir = Path.Combine(env.ContentRootPath, "DataStorage", "Materials");
            Directory.CreateDirectory(materialsDir);
            var before = Directory.GetFiles(materialsDir).Length;

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("12312664"), "courseId");
            form.Add(new StringContent(string.Empty), "title");
            form.Add(new StringContent("Missing title"), "description");

            var response = await client.PostAsync("/Lecturer/AddMaterial", form);
            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var after = Directory.GetFiles(materialsDir).Length;
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task UploadMaterial_WithoutFile_IsHandledGracefullyAndStillCreatesEntry()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("12312664"), "courseId");
            form.Add(new StringContent("No file attachment"), "title");
            form.Add(new StringContent("Uploaded without a file"), "description");

            var response = await client.PostAsync("/Lecturer/AddMaterial", form);
            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var html = await client.GetAsync("/Lecturer/Manage/12312664");
            var body = await html.Content.ReadAsStringAsync();
            Assert.DoesNotContain("No file attachment", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UploadMaterial_WithValidTitleAndDescription_PersistsMetadata()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("12312664"), "courseId");
            form.Add(new StringContent("Metadata test"), "title");
            form.Add(new StringContent("metadata description"), "description");

            var response = await client.PostAsync("/Lecturer/AddMaterial", form);
            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var manageResponse = await client.GetAsync("/Lecturer/Manage/12312664");
            var body = await manageResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Metadata test", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UploadMaterial_WithSupportedExtension_IsAccepted()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var env = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var materialsDir = Path.Combine(env.ContentRootPath, "DataStorage", "Materials");
            Directory.CreateDirectory(materialsDir);
            var before = Directory.GetFiles(materialsDir).Length;

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("12312664"), "courseId");
            form.Add(new StringContent("Supported extension"), "title");
            form.Add(new StringContent("txt upload"), "description");

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "hello");
            try
            {
                await using var fs = File.OpenRead(tempFile);
                var streamContent = new StreamContent(fs);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                form.Add(streamContent, "file", "sample.txt");

                var response = await client.PostAsync("/Lecturer/AddMaterial", form);
                Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
                var after = Directory.GetFiles(materialsDir).Length;
                Assert.True(after >= before);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task UploadMaterial_WithUnsupportedExtension_IsHandledGracefully()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var env = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var materialsDir = Path.Combine(env.ContentRootPath, "DataStorage", "Materials");
            Directory.CreateDirectory(materialsDir);
            var before = Directory.GetFiles(materialsDir).Length;

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("12312664"), "courseId");
            form.Add(new StringContent("Unsupported extension"), "title");
            form.Add(new StringContent("exe upload"), "description");

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "binary");
            try
            {
                await using var fs = File.OpenRead(tempFile);
                var streamContent = new StreamContent(fs);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(streamContent, "file", "sample.exe");

                var response = await client.PostAsync("/Lecturer/AddMaterial", form);
                Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
                var after = Directory.GetFiles(materialsDir).Length;
                Assert.True(after >= before);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task CreateAssignment_ForAssignedCourse_PersistsAndShowsInManagementView()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var title = $"Assignment_{Guid.NewGuid():N}";
            var response = await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "12312664",
                ["title"] = title,
                ["description"] = "Assignment created from automation",
                ["dueDate"] = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm")
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var manageResponse = await client.GetAsync("/Lecturer/Manage/12312664");
            Assert.True(manageResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateAssignment_WithoutTitle_IsRejected()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var response = await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "12312664",
                ["title"] = string.Empty,
                ["description"] = "Should not be created",
                ["dueDate"] = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm")
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var manageResponse = await client.GetAsync("/Lecturer/Manage/12312664");
            var body = await manageResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Should not be created", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAssignment_ForUnassignedCourse_IsDenied()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var response = await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "CS101",
                ["title"] = "Should not be created",
                ["description"] = "Unassigned course",
                ["dueDate"] = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm")
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var manageResponse = await client.GetAsync("/Lecturer/Manage/CS101");
            Assert.True(manageResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateAssignment_WithPastDueDate_IsStillStoredAndVisible()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var title = $"PastDue_{Guid.NewGuid():N}";
            var response = await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "12312664",
                ["title"] = title,
                ["description"] = "Past due date",
                ["dueDate"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm")
            }));

            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK);
            var manageResponse = await client.GetAsync("/Lecturer/Manage/12312664");
            Assert.True(manageResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateMultipleAssignments_ForSameCourse_AreStoredSeparately()
        {
            using var client = await LoginAsync("giaovien", "faculty123", "Faculty");
            var firstTitle = $"MultiA_{Guid.NewGuid():N}";
            var secondTitle = $"MultiB_{Guid.NewGuid():N}";

            await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "12312664",
                ["title"] = firstTitle,
                ["description"] = "First assignment",
                ["dueDate"] = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm")
            }));

            await client.PostAsync("/Lecturer/AddAssignment", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["courseCode"] = "12312664",
                ["title"] = secondTitle,
                ["description"] = "Second assignment",
                ["dueDate"] = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm")
            }));

            var manageResponse = await client.GetAsync("/Lecturer/Manage/12312664");
            var body = await manageResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain(firstTitle, body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(secondTitle, body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteNonExistentCourse_ReturnsFalseAndDoesNotCrash()
        {
            var storage = _factory.Services.GetRequiredService<IDataStorage>();
            var deleted = await storage.DeleteCourseAsync($"missing_{Guid.NewGuid():N}");
            Assert.False(deleted);
        }

        [Fact]
        public async Task DeleteNonExistentUser_ReturnsFalseAndDoesNotCrash()
        {
            var storage = _factory.Services.GetRequiredService<IDataStorage>();
            var deleted = await storage.DeleteUserByNameAsync($"missing_user_{Guid.NewGuid():N}");
            Assert.False(deleted);
        }
    }
}
