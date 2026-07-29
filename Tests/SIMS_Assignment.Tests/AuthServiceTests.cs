using System.Threading.Tasks;
using Moq;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Models;
using Xunit;

namespace SIMS_Assignment.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IDataStorage> _mockStorage;
        private readonly PasswordHasher _hasher;

        public AuthServiceTests()
        {
            _mockStorage = new Mock<IDataStorage>();
            _hasher = new PasswordHasher();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var testUser = new Admin
            {
                Id = 1,
                Name = "admin",
                Role = "Admin",
                PasswordHash = _hasher.Hash("admin123"),
                Email = "admin@sims.edu"
            };

            _mockStorage.Setup(s => s.GetUserByNameAsync("admin"))
                        .ReturnsAsync(testUser);

            var loginService = new LoginService(_mockStorage.Object, _hasher);

            // Act
            var result = await loginService.LoginAsync("admin", "admin123");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            var testUser = new Admin
            {
                Id = 1,
                Name = "admin",
                Role = "Admin",
                PasswordHash = _hasher.Hash("admin123"),
                Email = "admin@sims.edu"
            };

            _mockStorage.Setup(s => s.GetUserByNameAsync("admin"))
                        .ReturnsAsync(testUser);

            var loginService = new LoginService(_mockStorage.Object, _hasher);

            // Act
            var result = await loginService.LoginAsync("admin", "wrong_password");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LoginAsync_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            _mockStorage.Setup(s => s.GetUserByNameAsync("ghostuser"))
                        .ReturnsAsync((User?)null);

            var loginService = new LoginService(_mockStorage.Object, _hasher);

            // Act
            var result = await loginService.LoginAsync("ghostuser", "any_password");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PasswordHasher_HashAndVerify_MatchesCorrectly()
        {
            // Arrange
            string rawPassword = "SecurePassword2025!";

            // Act
            string hash = _hasher.Hash(rawPassword);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.True(_hasher.Verify(rawPassword, hash));
            Assert.False(_hasher.Verify("DifferentPassword", hash));
        }
    }
}
