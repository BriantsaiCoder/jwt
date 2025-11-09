using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtAuthApi.DTOs;
using JwtAuthApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuthApi.Services;

/// <summary>
/// JWT Token 服務實作
/// </summary>
public class JwtTokenService : IJwtTokenService
{
  private readonly JwtSettings _jwtSettings;
  private readonly IMemoryCache _cache;
  private readonly ILogger<JwtTokenService> _logger;
  private readonly IUserService _userService;
  private const string FamilyKeyPrefix = "TokenFamily:";
  private const string BlacklistKeyPrefix = "Blacklist:";
  private const string RefreshTokenIndexPrefix = "RefreshTokenIndex:";

  public JwtTokenService(
      IOptions<JwtSettings> jwtSettings,
      IMemoryCache cache,
      ILogger<JwtTokenService> logger,
      IUserService userService)
  {
    _jwtSettings = jwtSettings.Value;
    _cache = cache;
    _logger = logger;
    _userService = userService;
  }

  public string GenerateAccessToken(User user)
  {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

    // 加入角色 Claims
    foreach (var role in user.Roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public string GenerateRefreshToken()
  {
    var randomNumber = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    return Convert.ToBase64String(randomNumber);
  }

  public async Task<string> CreateTokenFamilyAsync(string userId, string refreshToken)
  {
    var familyId = Guid.NewGuid().ToString();
    var family = new RefreshTokenFamily
    {
      FamilyId = familyId,
      CurrentToken = refreshToken,
      ParentToken = null,
      UserId = userId,
      IssuedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
      IsRevoked = false
    };

    var cacheKey = $"{FamilyKeyPrefix}{familyId}";
    var indexKey = $"{RefreshTokenIndexPrefix}{refreshToken}";

    var cacheOptions = new MemoryCacheEntryOptions
    {
      AbsoluteExpiration = family.ExpiresAt
    };

    _cache.Set(cacheKey, family, cacheOptions);
    _cache.Set(indexKey, familyId, cacheOptions);

    _logger.LogInformation("建立 Token Family: {FamilyId} for User: {UserId}", familyId, userId);

    return await Task.FromResult(familyId);
  }

  public async Task<TokenResponse> RotateRefreshTokenAsync(string refreshToken, string userId)
  {
    // 取得 Family ID
    var familyId = await GetFamilyIdByRefreshTokenAsync(refreshToken);
    if (string.IsNullOrEmpty(familyId))
    {
      throw new UnauthorizedAccessException("無效的重新整理令牌");
    }

    var cacheKey = $"{FamilyKeyPrefix}{familyId}";
    if (!_cache.TryGetValue<RefreshTokenFamily>(cacheKey, out var family) || family == null)
    {
      throw new UnauthorizedAccessException("Token Family 不存在或已過期");
    }

    // 檢查 Family 是否已撤銷
    if (family.IsRevoked)
    {
      _logger.LogWarning("嘗試使用已撤銷的 Token Family: {FamilyId}", familyId);
      throw new UnauthorizedAccessException("此令牌已被撤銷");
    }

    // 檢查 UserId 是否匹配
    if (family.UserId != userId)
    {
      _logger.LogWarning("User ID 不匹配。Family UserId: {FamilyUserId}, Provided UserId: {ProvidedUserId}",
          family.UserId, userId);
      await DetectReuseAndRevokeAsync(familyId, "User ID 不匹配");
      throw new UnauthorizedAccessException("令牌不屬於此使用者");
    }

    var now = DateTime.UtcNow;

    // 檢查是否為當前令牌
    if (family.CurrentToken == refreshToken)
    {
      // 正常輪換流程
      return await PerformTokenRotationAsync(family, userId);
    }

    // 檢查是否為父令牌且在寬限期內
    if (family.ParentToken == refreshToken)
    {
      var gracePeriodEnd = family.IssuedAt.AddSeconds(_jwtSettings.GracePeriodSeconds);
      if (now <= gracePeriodEnd)
      {
        // 檢查父令牌是否已被使用過
        if (family.ParentTokenUsed)
        {
          _logger.LogWarning("父令牌已被使用過，偵測到重用! Family: {FamilyId}", familyId);
          await DetectReuseAndRevokeAsync(familyId, "父令牌重複使用");
          throw new UnauthorizedAccessException("偵測到令牌重用，已撤銷所有相關令牌");
        }

        _logger.LogInformation("在寬限期內允許使用父令牌。Family: {FamilyId}", familyId);

        // 標記父令牌已被使用
        family.ParentTokenUsed = true;
        var familyCacheKey = $"{FamilyKeyPrefix}{familyId}";
        var familyCacheOptions = new MemoryCacheEntryOptions
        {
          AbsoluteExpiration = family.ExpiresAt
        };
        _cache.Set(familyCacheKey, family, familyCacheOptions);

        return await PerformTokenRotationAsync(family, userId);
      }
    }

    // 令牌重用偵測
    _logger.LogWarning("偵測到令牌重用! Family: {FamilyId}, UserId: {UserId}", familyId, userId);
    await DetectReuseAndRevokeAsync(familyId, "令牌重用偵測");
    throw new UnauthorizedAccessException("偵測到令牌重用，已撤銷所有相關令牌");
  }

  private async Task<TokenResponse> PerformTokenRotationAsync(RefreshTokenFamily family, string userId)
  {
    // 從 UserService 取得完整使用者資訊
    var user = await _userService.GetUserByIdAsync(userId);
    if (user == null)
    {
      throw new UnauthorizedAccessException("使用者不存在");
    }

    // 產生新的令牌
    var newRefreshToken = GenerateRefreshToken();
    var newAccessToken = GenerateAccessToken(user);

    // 更新 Family
    var oldRefreshToken = family.CurrentToken;
    family.ParentToken = family.CurrentToken;
    family.CurrentToken = newRefreshToken;
    family.IssuedAt = DateTime.UtcNow;
    family.ParentTokenUsed = false; // 重置父令牌使用標記

    var cacheKey = $"{FamilyKeyPrefix}{family.FamilyId}";
    var newIndexKey = $"{RefreshTokenIndexPrefix}{newRefreshToken}";
    var oldIndexKey = $"{RefreshTokenIndexPrefix}{oldRefreshToken}";

    // 計算寬限期過期時間（用於舊的父令牌索引）
    var gracePeriodExpiration = DateTime.UtcNow.AddSeconds(_jwtSettings.GracePeriodSeconds);

    var cacheOptions = new MemoryCacheEntryOptions
    {
      AbsoluteExpiration = family.ExpiresAt
    };

    // 父令牌索引保留到寬限期結束（所有之前的令牌都保留,以便檢測重用）
    var parentTokenCacheOptions = new MemoryCacheEntryOptions
    {
      AbsoluteExpiration = gracePeriodExpiration
    };

    _cache.Set(cacheKey, family, cacheOptions);
    _cache.Set(newIndexKey, family.FamilyId, cacheOptions);
    // 保留舊的當前令牌索引（現在變成父令牌），直到寬限期結束
    // 這樣可以檢測到重複使用
    _cache.Set(oldIndexKey, family.FamilyId, parentTokenCacheOptions);

    // 注意:不刪除更舊的令牌索引,讓它們在寬限期結束後自動過期
    // 這樣可以在寬限期內檢測到任何舊令牌的重複使用

    _logger.LogInformation("令牌輪換成功。Family: {FamilyId}, UserId: {UserId}", family.FamilyId, userId);

    return new TokenResponse
    {
      AccessToken = newAccessToken,
      RefreshToken = newRefreshToken,
      ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
      TokenType = "Bearer"
    };
  }

  public async Task DetectReuseAndRevokeAsync(string familyId, string reason)
  {
    await RevokeTokenFamilyAsync(familyId, reason);
    _logger.LogWarning("令牌重用偵測：已撤銷 Family {FamilyId}。原因: {Reason}", familyId, reason);
  }

  public async Task RevokeTokenFamilyAsync(string familyId, string reason)
  {
    var cacheKey = $"{FamilyKeyPrefix}{familyId}";
    if (_cache.TryGetValue<RefreshTokenFamily>(cacheKey, out var family) && family != null)
    {
      family.IsRevoked = true;
      family.RevokedAt = DateTime.UtcNow;
      family.RevokedReason = reason;

      _cache.Set(cacheKey, family, new MemoryCacheEntryOptions
      {
        AbsoluteExpiration = family.ExpiresAt
      });

      // 移除所有相關的 RefreshToken 索引
      var currentIndexKey = $"{RefreshTokenIndexPrefix}{family.CurrentToken}";
      _cache.Remove(currentIndexKey);

      if (!string.IsNullOrEmpty(family.ParentToken))
      {
        var parentIndexKey = $"{RefreshTokenIndexPrefix}{family.ParentToken}";
        _cache.Remove(parentIndexKey);
      }

      _logger.LogInformation("撤銷 Token Family: {FamilyId}。原因: {Reason}", familyId, reason);
    }

    await Task.CompletedTask;
  }

  public async Task<bool> IsTokenBlacklistedAsync(string jti)
  {
    var key = $"{BlacklistKeyPrefix}{jti}";
    var isBlacklisted = _cache.TryGetValue(key, out _);
    return await Task.FromResult(isBlacklisted);
  }

  public async Task AddToBlacklistAsync(string jti, DateTime expiresAt)
  {
    var key = $"{BlacklistKeyPrefix}{jti}";
    _cache.Set(key, true, new MemoryCacheEntryOptions
    {
      AbsoluteExpiration = expiresAt
    });

    _logger.LogInformation("令牌已加入黑名單: {Jti}", jti);
    await Task.CompletedTask;
  }

  public async Task<string?> GetFamilyIdByRefreshTokenAsync(string refreshToken)
  {
    var indexKey = $"{RefreshTokenIndexPrefix}{refreshToken}";
    if (_cache.TryGetValue<string>(indexKey, out var familyId))
    {
      return await Task.FromResult(familyId);
    }
    return await Task.FromResult<string?>(null);
  }

  public async Task<string?> GetUserIdByRefreshTokenAsync(string refreshToken)
  {
    // 先取得 FamilyId
    var familyId = await GetFamilyIdByRefreshTokenAsync(refreshToken);
    if (familyId == null)
    {
      return null;
    }

    // 從 Family 取得 UserId
    var cacheKey = $"{FamilyKeyPrefix}{familyId}";
    if (_cache.TryGetValue<RefreshTokenFamily>(cacheKey, out var family) && family != null)
    {
      return family.UserId;
    }

    return null;
  }
}
