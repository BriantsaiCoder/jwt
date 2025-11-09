using FluentAssertions;
using JwtAuthApi.Models;
using JwtAuthApi.Services;
using JwtAuthApi.Tests.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JwtAuthApi.Tests.Unit;

public class JwtTokenServiceTests
{
  private readonly JwtTokenService _tokenService;
  private readonly IMemoryCache _cache;

  public JwtTokenServiceTests()
  {
    var settings = new JwtSettings
    {
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      SecretKey = "VGhpc0lzQVZlcnlTZWN1cmVTZWNyZXRLZXlGb3JUZXN0aW5nUHVycG9zZXNPbmx5MTIzNDU2Nzg5MA==",
      AccessTokenExpiryMinutes = 15,
      RefreshTokenExpiryDays = 14,
      GracePeriodSeconds = 30
    };

    _cache = new MemoryCache(new MemoryCacheOptions());
    var logger = new LoggerFactory().CreateLogger<JwtTokenService>();
    var userServiceLogger = new LoggerFactory().CreateLogger<InMemoryUserService>();
    var userService = new InMemoryUserService(new Microsoft.AspNetCore.Identity.PasswordHasher<User>(), userServiceLogger);

    _tokenService = new JwtTokenService(
        Options.Create(settings),
        _cache,
        logger,
        userService
    );
  }

  [Fact]
  public void GenerateAccessToken_ContainsCorrectClaims()
  {
    // Arrange
    var user = TestDataBuilder.CreateUser("1", "testuser", "hash", new[] { "User", "Admin" });

    // Act
    var token = _tokenService.GenerateAccessToken(user);

    // Assert
    token.Should().NotBeNullOrEmpty();
    token.Should().Contain(".");
    var parts = token.Split('.');
    parts.Should().HaveCount(3); // JWT 格式：header.payload.signature
  }

  [Fact]
  public void GenerateRefreshToken_ReturnsBase64String()
  {
    // Act
    var token = _tokenService.GenerateRefreshToken();

    // Assert
    token.Should().NotBeNullOrEmpty();
    token.Length.Should().BeGreaterThan(40); // Base64 編碼的 64 bytes

    // 驗證是否為有效的 Base64
    var bytes = Convert.FromBase64String(token);
    bytes.Should().NotBeEmpty();
  }

  [Fact]
  public async Task CreateTokenFamily_StoresInCache()
  {
    // Arrange
    var userId = "1";
    var refreshToken = _tokenService.GenerateRefreshToken();

    // Act
    var familyId = await _tokenService.CreateTokenFamilyAsync(userId, refreshToken);

    // Assert
    familyId.Should().NotBeNullOrEmpty();

    // 驗證可以透過 refresh token 找到 family
    var foundFamilyId = await _tokenService.GetFamilyIdByRefreshTokenAsync(refreshToken);
    foundFamilyId.Should().Be(familyId);
  }

  [Fact]
  public async Task IsTokenBlacklisted_ReturnsFalse_WhenNotBlacklisted()
  {
    // Arrange
    var jti = Guid.NewGuid().ToString();

    // Act
    var result = await _tokenService.IsTokenBlacklistedAsync(jti);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public async Task AddToBlacklist_MakesTokenBlacklisted()
  {
    // Arrange
    var jti = Guid.NewGuid().ToString();
    var expiresAt = DateTime.UtcNow.AddHours(1);

    // Act
    await _tokenService.AddToBlacklistAsync(jti, expiresAt);
    var result = await _tokenService.IsTokenBlacklistedAsync(jti);

    // Assert
    result.Should().BeTrue();
  }
}
