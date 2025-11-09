using System.Net;
using FluentAssertions;
using JwtAuthApi.DTOs;
using JwtAuthApi.Tests.Infrastructure;
using Xunit;

namespace JwtAuthApi.Tests.Integration;

public class AuthorizationTests : IClassFixture<WebApplicationFactoryFixture>
{
  private readonly HttpClient _client;

  public AuthorizationTests(WebApplicationFactoryFixture factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task AccessProtectedEndpoint_WithoutToken_Returns401()
  {
    // Act
    var response = await _client.GetAsync("/api/v1/weatherforecast");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task AccessProtectedEndpoint_WithValidToken_Returns200()
  {
    // Arrange - 先登入取得 token
    var loginRequest = TestDataBuilder.CreateLoginRequest("user", "User@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act
    var response = await _client.GetWithTokenAsync("/api/v1/weatherforecast", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task AccessAdminEndpoint_WithUserRole_Returns403()
  {
    // Arrange - 使用 user 角色登入
    var loginRequest = TestDataBuilder.CreateLoginRequest("user", "User@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act
    var response = await _client.GetWithTokenAsync("/api/v1/admin/users", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task AccessAdminEndpoint_WithAdminRole_Returns200()
  {
    // Arrange - 使用 admin 角色登入
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act
    var response = await _client.GetWithTokenAsync("/api/v1/admin/users", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task AccessUserProfile_WithValidToken_ReturnsUserInfo()
  {
    // Arrange
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act
    var response = await _client.GetWithTokenAsync("/api/v1/user/profile", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("admin");
    content.Should().Contain("Admin");
  }
}
