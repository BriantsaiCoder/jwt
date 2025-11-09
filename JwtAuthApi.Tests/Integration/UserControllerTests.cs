using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JwtAuthApi.Controllers.V1;
using JwtAuthApi.DTOs;
using JwtAuthApi.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace JwtAuthApi.Tests.Integration;

[Collection("Sequential")]
public class UserControllerTests : IClassFixture<WebApplicationFactoryFixture>
{
  private readonly HttpClient _client;
  private readonly ITestOutputHelper _output;

  public UserControllerTests(WebApplicationFactoryFixture factory, ITestOutputHelper output)
  {
    _client = factory.CreateClient();
    _output = output;
  }

  [Fact]
  public async Task GetProfile_WithValidToken_ReturnsUserProfile()
  {
    // Arrange - 登入取得 token
    var loginRequest = TestDataBuilder.CreateLoginRequest("user", "User@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Act - 使用 token 存取個人資料
    var response = await _client.GetWithTokenAsync("/api/v1/user/profile", tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("user");
    content.Should().Contain("\"id\"");  // JSON 中的 id 欄位
    content.Should().Contain("claims");
  }

  [Fact]
  public async Task GetProfile_WithoutToken_Returns401()
  {
    // Act - 未提供 token
    var response = await _client.GetAsync("/api/v1/user/profile");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task GetProfile_WithInvalidToken_Returns401()
  {
    // Act - 使用無效的 token
    var response = await _client.GetWithTokenAsync("/api/v1/user/profile", "invalid.token.here");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task UpdateProfile_WithValidData_ReturnsSuccess()
  {
    // Arrange - 登入取得 token
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    var updateRequest = new UpdateProfileRequest
    {
      Email = "newemail@test.com"
    };

    // Act - 更新個人資料
    var response = await _client.PutWithTokenAsync("/api/v1/user/profile", updateRequest, tokens!.AccessToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("個人資料更新成功");  // 修正為實際的中文訊息
  }

  [Fact]
  public async Task UpdateProfile_WithoutToken_Returns401()
  {
    // Arrange
    var updateRequest = new UpdateProfileRequest
    {
      Email = "newemail@test.com"
    };

    // Act - 未提供 token
    var response = await _client.PutAsJsonAsync("/api/v1/user/profile", updateRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
