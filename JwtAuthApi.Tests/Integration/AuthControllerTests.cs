using System.Net;
using FluentAssertions;
using JwtAuthApi.DTOs;
using JwtAuthApi.Tests.Infrastructure;
using Xunit;

namespace JwtAuthApi.Tests.Integration;

public class AuthControllerTests : IClassFixture<WebApplicationFactoryFixture>
{
  private readonly HttpClient _client;

  public AuthControllerTests(WebApplicationFactoryFixture factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task Login_WithValidCredentials_ReturnsTokens()
  {
    // Arrange
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var tokenResponse = await response.Content.ReadAsJsonAsync<TokenResponse>();
    tokenResponse.Should().NotBeNull();
    tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
    tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
    tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
    tokenResponse.TokenType.Should().Be("Bearer");
  }

  [Fact]
  public async Task Login_WithInvalidPassword_Returns401()
  {
    // Arrange
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "WrongPassword");

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Login_WithInvalidUsername_Returns401()
  {
    // Arrange
    var loginRequest = TestDataBuilder.CreateLoginRequest("nonexistent", "Password123");

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Refresh_WithValidToken_ReturnsNewTokens()
  {
    // Arrange - 先登入取得 token
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var loginTokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    var refreshRequest = new RefreshRequest { RefreshToken = loginTokens!.RefreshToken };

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var newTokens = await response.Content.ReadAsJsonAsync<TokenResponse>();
    newTokens.Should().NotBeNull();
    newTokens!.AccessToken.Should().NotBeNullOrEmpty();
    newTokens.RefreshToken.Should().NotBeNullOrEmpty();
    newTokens.RefreshToken.Should().NotBe(loginTokens.RefreshToken); // 應該是新的 token
  }

  [Fact]
  public async Task Refresh_WithReusedToken_RevokesFamily_Returns401()
  {
    // Arrange - 先登入取得 token
    var loginRequest = TestDataBuilder.CreateLoginRequest("user", "User@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens1 = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // 第一次 refresh - 取得新 token
    var refreshRequest1 = new RefreshRequest { RefreshToken = tokens1!.RefreshToken };
    var refreshResponse1 = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest1);
    var tokens2 = await refreshResponse1.Content.ReadAsJsonAsync<TokenResponse>();

    // Act 1 - 第二次使用舊 token (在寬限期內,第一次使用父令牌) - 應該成功
    var refreshRequest2 = new RefreshRequest { RefreshToken = tokens1.RefreshToken };
    var refreshResponse2 = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest2);
    var tokens3 = await refreshResponse2.Content.ReadAsJsonAsync<TokenResponse>();
    refreshResponse2.StatusCode.Should().Be(HttpStatusCode.OK, "父令牌在寬限期內第一次使用應該成功");

    // Act 2 - **第三次使用舊 token (父令牌已被使用過) - 應該被偵測為攻擊並撤銷 family**
    var refreshRequest3 = new RefreshRequest { RefreshToken = tokens1.RefreshToken };
    var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest3);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "重複使用父令牌應該被檢測為攻擊");

    // 驗證整個 family 都被撤銷 - 所有相關 token 都無法使用
    var refreshRequest4 = new RefreshRequest { RefreshToken = tokens2!.RefreshToken };
    var response2 = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest4);
    response2.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "family 被撤銷後,tokens2 應該無法使用");

    var refreshRequest5 = new RefreshRequest { RefreshToken = tokens3!.RefreshToken };
    var response3 = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest5);
    response3.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "family 被撤銷後,tokens3 應該無法使用");
  }

  [Fact]
  public async Task Logout_RevokesTokenFamily()
  {
    // Arrange - 先登入取得 token
    var loginRequest = TestDataBuilder.CreateLoginRequest("admin", "Admin@123");
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    var logoutRequest = new RefreshRequest { RefreshToken = tokens!.RefreshToken };

    // Act - 登出
    var logoutResponse = await _client.PostWithTokenAsync("/api/v1/auth/logout", logoutRequest, tokens.AccessToken);

    // Assert
    logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "登出成功應該返回 204 NoContent");

    // 驗證登出後無法使用 refresh token
    var refreshRequest = new RefreshRequest { RefreshToken = tokens.RefreshToken };
    var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
    refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Refresh_WithInvalidToken_Returns401()
  {
    // Arrange
    var refreshRequest = new RefreshRequest { RefreshToken = "invalid.refresh.token" };

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
