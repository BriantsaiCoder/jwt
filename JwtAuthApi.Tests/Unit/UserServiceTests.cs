using FluentAssertions;
using JwtAuthApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JwtAuthApi.Tests.Unit;

public class UserServiceTests
{
  private readonly InMemoryUserService _userService;

  public UserServiceTests()
  {
    var passwordHasher = new PasswordHasher<Models.User>();
    var logger = new LoggerFactory().CreateLogger<InMemoryUserService>();
    _userService = new InMemoryUserService(passwordHasher, logger);
  }

  [Fact]
  public async Task ValidateCredentials_WithCorrectPassword_ReturnsUser()
  {
    // Act
    var user = await _userService.ValidateCredentialsAsync("admin", "Admin@123");

    // Assert
    user.Should().NotBeNull();
    user!.Username.Should().Be("admin");
    user.Roles.Should().Contain("Admin");
  }

  [Fact]
  public async Task ValidateCredentials_WithWrongPassword_ReturnsNull()
  {
    // Act
    var user = await _userService.ValidateCredentialsAsync("admin", "WrongPassword");

    // Assert
    user.Should().BeNull();
  }

  [Fact]
  public async Task ValidateCredentials_WithNonExistentUser_ReturnsNull()
  {
    // Act
    var user = await _userService.ValidateCredentialsAsync("nonexistent", "Password");

    // Assert
    user.Should().BeNull();
  }

  [Fact]
  public async Task GetUserById_WithValidId_ReturnsUser()
  {
    // Act
    var user = await _userService.GetUserByIdAsync("1");

    // Assert
    user.Should().NotBeNull();
    user!.Id.Should().Be("1");
  }

  [Fact]
  public async Task GetUserById_WithInvalidId_ReturnsNull()
  {
    // Act
    var user = await _userService.GetUserByIdAsync("999");

    // Assert
    user.Should().BeNull();
  }

  [Fact]
  public async Task GetUserByUsername_WithValidUsername_ReturnsUser()
  {
    // Act
    var user = await _userService.GetUserByUsernameAsync("user");

    // Assert
    user.Should().NotBeNull();
    user!.Username.Should().Be("user");
    user.Roles.Should().Contain("User");
  }

  [Theory]
  [InlineData("admin", "Admin@123")]
  [InlineData("user", "User@123")]
  [InlineData("guest", "Guest@123")]
  public async Task AllDefaultUsers_CanLogin(string username, string password)
  {
    // Act
    var user = await _userService.ValidateCredentialsAsync(username, password);

    // Assert
    user.Should().NotBeNull();
    user!.Username.Should().Be(username);
  }
}
