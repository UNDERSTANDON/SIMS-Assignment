using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SIMS_WEB.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    [Collection("WebApplication")]
    public class AddMaterialTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AddMaterialTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

        [Fact]
        public async Task AddMaterial_WithAssignedCourseAndValidFile_UploadsSuccessfully()
        {
            var courseCode = $"AUTO_{Guid.NewGuid():N}";
            SimsDataStore.Instance.Courses.Add(new SIMS_WEB.Models.Course
            {
                Code = courseCode,
                Title = "Automation Course",
                Capacity = 20,
                Instructor = "giaovien"
            });

            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "giaovien",
                ["Password"] = "faculty123",
                ["Role"] = "Faculty"
            }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            var env = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var materialsDir = Path.Combine(env.ContentRootPath, "DataStorage", "Materials");
            Directory.CreateDirectory(materialsDir);
            var before = Directory.GetFiles(materialsDir).Length;

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(courseCode), "courseId");
            form.Add(new StringContent("Automation Material"), "title");
            form.Add(new StringContent("Uploaded from automation test"), "description");

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "Automation test content");
            try
            {
                await using var fs = File.OpenRead(tempFile);
                var streamContent = new StreamContent(fs);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                form.Add(streamContent, "file", Path.GetFileName(tempFile));

                var response = await _client.PostAsync("/Lecturer/AddMaterial", form);
                Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.OK,
                    $"Expected redirect or success status code but got {response.StatusCode}");

                var after = Directory.GetFiles(materialsDir).Length;
                Assert.True(after > before, $"Expected a new file in {materialsDir}");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
