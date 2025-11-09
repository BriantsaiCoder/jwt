using JwtAuthApi.DTOs;
using JwtAuthApi.Models;

namespace JwtAuthApi.Tests.Infrastructure;

public static class TestDataBuilder
{
  public static User CreateUser(
      string id = "1",
      string username = "testuser",
      string passwordHash = "hashedpassword",
      string[]? roles = null)
  {
    return new User
    {
      Id = id,
      Username = username,
      PasswordHash = passwordHash,
      Roles = roles ?? new[] { "User" },
      CreatedAt = DateTime.UtcNow
    };
  }

  public static LoginRequest CreateLoginRequest(
      string username = "admin",
      string password = "Admin@123")
  {
    return new LoginRequest
    {
      Username = username,
      Password = password
    };
  }

  public static TokenResponse CreateTokenResponse(
      string accessToken = "test.access.token",
      string refreshToken = "testrefreshtoken",
      int expiresIn = 900)
  {
    return new TokenResponse
    {
      AccessToken = accessToken,
      RefreshToken = refreshToken,
      ExpiresIn = expiresIn,
      TokenType = "Bearer"
    };
  }

  public static RefreshRequest CreateRefreshRequest(string refreshToken = "testrefreshtoken")
  {
    return new RefreshRequest
    {
      RefreshToken = refreshToken
    };
  }
}
