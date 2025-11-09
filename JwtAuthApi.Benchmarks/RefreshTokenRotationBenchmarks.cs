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
public class RefreshTokenRotationBenchmarks
{
  private JwtTokenService _tokenService = null!;
  private string _userId = null!;
  private string _refreshToken = null!;
  private string _familyId = null!;

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

    _userId = "1";
  }

  [IterationSetup]
  public async Task IterationSetup()
  {
    // 每次迭代建立新的 token family
    _refreshToken = _tokenService.GenerateRefreshToken();
    _familyId = await _tokenService.CreateTokenFamilyAsync(_userId, _refreshToken);
  }

  [Benchmark]
  public async Task<string> CreateTokenFamily()
  {
    var refreshToken = _tokenService.GenerateRefreshToken();
    return await _tokenService.CreateTokenFamilyAsync(_userId, refreshToken);
  }

  [Benchmark]
  public async Task<DTOs.TokenResponse> RotateRefreshToken()
  {
    return await _tokenService.RotateRefreshTokenAsync(_refreshToken, _userId);
  }

  [Benchmark]
  public async Task RevokeTokenFamily()
  {
    await _tokenService.RevokeTokenFamilyAsync(_familyId, "Benchmark test");
  }
}
