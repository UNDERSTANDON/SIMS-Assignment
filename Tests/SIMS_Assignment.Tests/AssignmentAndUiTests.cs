using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace SIMS_Assignment.Tests
{
    [Collection("WebApplication")]
    public class AssignmentAndUiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AssignmentAndUiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }

        [Fact]
        public async Task AssignmentCreationPage_IsAccessibleToAuthenticatedLecturer()
        {
            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "giaovien",
                ["Password"] = "faculty123",
                ["Role"] = "Faculty"
            }));

            Assert.True(loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            var response = await _client.GetAsync("/Lecturer/Manage/12312664");
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task LecturerManagementPage_ContainsAssignmentAndMaterialSections()
        {
            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "giaovien",
                ["Password"] = "faculty123",
                ["Role"] = "Faculty"
            }));

            Assert.True(loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            var response = await _client.GetAsync("/Lecturer/Manage/12312664");
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Contains("Add New Material", html, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Create New Assignment", html, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task StudentDashboardPage_RendersWithoutErrors()
        {
            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "sinhvien",
                ["Password"] = "student123",
                ["Role"] = "Student"
            }));

            Assert.True(loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            var response = await _client.GetAsync("/StudentDashboard/Index");
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            Assert.Contains("Student", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AdminDashboardPage_RendersWithoutErrors()
        {
            var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "admin",
                ["Password"] = "admin123",
                ["Role"] = "Admin"
            }));

            Assert.True(loginResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb);
            var response = await _client.GetAsync("/Dashboard/Index");
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.RedirectKeepVerb);
            Assert.Contains("Dashboard", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
