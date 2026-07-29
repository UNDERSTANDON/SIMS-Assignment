using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Models;
using SIMS_WEB.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    public class AddMaterialTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AddMaterialTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AddMaterial_WithFile_UploadsSuccessfully()
        {
            // Seed the test user and course assignment dynamically
            using (var setupScope = _factory.Services.CreateScope())
            {
                var storage = setupScope.ServiceProvider.GetRequiredService<IDataStorage>();
                var hasher = setupScope.ServiceProvider.GetRequiredService<IPasswordHasher>();

                // Ensure the user "giaovien" exists with the correct password
                var existingUser = await storage.GetUserByNameAsync("giaovien");
                if (existingUser == null)
                {
                    var facultyUser = new Lecturer
                    {
                        Name = "giaovien",
                        FullName = "giaovien",
                        Role = "Faculty",
                        PasswordHash = hasher.Hash("faculty123"),
                        Email = "giaovien@test.com"
                    };
                    await storage.SaveUserAsync(facultyUser);
                }

                // Ensure "giaovien" is assigned to "CS101" in the in-memory store
                var store = SimsDataStore.Instance;
                var course = store.Courses.FirstOrDefault(c => c.Code == "CS101");
                if (course == null)
                {
                    course = new SIMS_WEB.Models.Course { Code = "CS101", Title = "Lập trình Căn bản", Capacity = 40, Instructor = "giaovien" };
                    store.Courses.Add(course);
                }
                else
                {
                    course.Instructor = "giaovien";
                }
                // Write back to courses.csv to keep it in sync
                SIMS_WEB.Storage.ModelFilePersistence.SaveCourses(store.Courses);
            }

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
                BaseAddress = new System.Uri("https://localhost")
            });

            // Login as faculty to establish session
            var loginData = new[]
            {
                new KeyValuePair<string, string>("Username", "giaovien"),
                new KeyValuePair<string, string>("Password", "faculty123"),
                new KeyValuePair<string, string>("Role", "Faculty"),
            };

            var loginResp = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginData));
            var loginContent = await loginResp.Content.ReadAsStringAsync();
            var cookies = loginResp.Headers.Contains("Set-Cookie") ? string.Join(", ", loginResp.Headers.GetValues("Set-Cookie")) : "None";
            Assert.True(loginResp.IsSuccessStatusCode, $"Login request failed with status {loginResp.StatusCode}. Content: {loginContent}. Cookies: {cookies}");
            if (loginContent.Contains("Tên đăng nhập hoặc mật khẩu không hợp lệ"))
            {
                throw new System.Exception("Login failed: Invalid credentials/role. Response content: " + loginContent);
            }

            // Prepare the test file at C:\Test.txt as requested; fall back to temp file if not writable
            var filePath = @"C:\Test.txt";
            try
            {
                File.WriteAllText(filePath, "Automated test file content");
            }
            catch
            {
                filePath = Path.GetTempFileName();
                File.WriteAllText(filePath, "Automated test file content");
            }

            System.Console.WriteLine($"DEBUG: File path - {filePath}");
            System.Console.WriteLine("File accessed successfully.\n");

            // Determine the application's content root so we can inspect DataStorage/Materials
            using var scope = _factory.Services.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var materialsDir = Path.Combine(env.ContentRootPath, "DataStorage", "Materials");
            if (!Directory.Exists(materialsDir)) Directory.CreateDirectory(materialsDir);
            var before = Directory.GetFiles(materialsDir).Length;

            using var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent("CS101"), "courseId");
            multipart.Add(new StringContent("Test Material"), "title");
            multipart.Add(new StringContent("Uploaded from automated test"), "description");

            await using var fs = File.OpenRead(filePath);
            var streamContent = new StreamContent(fs);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipart.Add(streamContent, "file", Path.GetFileName(filePath));
            var resp = await client.PostAsync("/Lecturer/AddMaterial", multipart);
            var responseContent = await resp.Content.ReadAsStringAsync();
            Assert.True(resp.StatusCode == HttpStatusCode.Redirect || resp.IsSuccessStatusCode, $"Expected redirect or success status code from AddMaterial, but got {resp.StatusCode}. Content: {responseContent}");
            var after = Directory.GetFiles(materialsDir).Length;
            Assert.True(after > before, $"Expected a new file in {materialsDir}");
        }
    }
}
