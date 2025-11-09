using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JwtAuthApi.DTOs;
using JwtAuthApi.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace JwtAuthApi.Tests.Integration;

[Collection("Sequential")]
public class AdminControllerTests : IClassFixture<WebApplicationFactoryFixture>
{
  private readonly HttpClient _client;
  private readonly ITestOutputHelper _output;

  public AdminControllerTests(WebApplicationFactoryFixture factory, ITestOutputHelper output)
  {
    _client = factory.CreateClient();
    _output = output;
  }

  [Fact]
  public async Task GetAllUsers_WithAdminToken_ReturnsUserList()
  {
    // Arrange - Admin 登入
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act
    var response = await _client.GetWithTokenAsync("/api/v1/admin/users", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("admin");
    content.Should().Contain("user");
    content.Should().Contain("guest");
  }

  [Fact]
  public async Task GetAllUsers_WithUserToken_Returns403()
  {
    // Arrange - 普通使用者登入
    var loginRequest = TestDataBuilder.CreateLoginRequest("user", "User@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act
    var response = await _client.GetWithTokenAsync("/api/v1/admin/users", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task GetAllUsers_WithoutToken_Returns401()
  {
    // Act
    var response = await _client.GetAsync("/api/v1/admin/users");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
