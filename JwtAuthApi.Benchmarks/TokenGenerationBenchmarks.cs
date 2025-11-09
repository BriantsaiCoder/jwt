using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JwtAuthApi.Models;
using JwtAuthApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JwtAuthApi.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TokenGenerationBenchmarks
{
  private JwtTokenService _tokenService = null!;
  private User _testUser = null!;

  [GlobalSetup]
  public void Setup()
  {
    var settings = new JwtSettings
    {
      Issuer = "BenchmarkIssuer",
      Audience = "BenchmarkAudience",
      SecretKey = "VGhpc0lzQVZlcnlTZWN1cmVTZWNyZXRLZXlGb3JUZXN0aW5nUHVycG9zZXNPbmx5MTIzNDU2Nzg5MA==",
      AccessTokenExpiryMinutes = 15,
      RefreshTokenExpiryDays = 14,
      GracePeriodSeconds = 30
    };

    var cache = new MemoryCache(new MemoryCacheOptions());
    var logger = new LoggerFactory().CreateLogger<JwtTokenService>();
    var userServiceLogger = new LoggerFactory().CreateLogger<InMemoryUserService>();
    var userService = new InMemoryUserService(new Microsoft.AspNetCore.Identity.PasswordHasher<User>(), userServiceLogger);

    _tokenService = new JwtTokenService(
        Options.Create(settings),
        cache,
        logger,
        userService
    );

    _testUser = new User
    {
      Id = "1",
      Username = "benchmarkuser",
      PasswordHash = "hash",
      Roles = new[] { "User", "Admin" },
      CreatedAt = DateTime.UtcNow
    };
  }

  [Benchmark]
  public string GenerateSingleAccessToken()
  {
    return _tokenService.GenerateAccessToken(_testUser);
  }

  [Benchmark]
  public string GenerateSingleRefreshToken()
  {
    return _tokenService.GenerateRefreshToken();
  }

  [Benchmark]
  [Arguments(10)]
  [Arguments(100)]
  [Arguments(1000)]
  public List<string> GenerateMultipleAccessTokens(int count)
  {
    var tokens = new List<string>(count);
    for (int i = 0; i < count; i++)
    {
      tokens.Add(_tokenService.GenerateAccessToken(_testUser));
    }
    return tokens;
  }

  [Benchmark]
  [Arguments(10)]
  [Arguments(100)]
  [Arguments(1000)]
  public List<string> GenerateMultipleRefreshTokens(int count)
  {
    var tokens = new List<string>(count);
    for (int i = 0; i < count; i++)
    {
      tokens.Add(_tokenService.GenerateRefreshToken());
    }
    return tokens;
  }
}
