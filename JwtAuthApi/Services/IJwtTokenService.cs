using JwtAuthApi.DTOs;
using JwtAuthApi.Models;

namespace JwtAuthApi.Services;

/// <summary>
/// JWT Token 服務介面
/// </summary>
public interface IJwtTokenService
{
  /// <summary>
  /// 產生存取令牌
  /// </summary>
  /// <param name="user">使用者物件</param>
  /// <returns>JWT 存取令牌</returns>
  string GenerateAccessToken(User user);

  /// <summary>
  /// 產生重新整理令牌
  /// </summary>
  /// <returns>隨機重新整理令牌</returns>
  string GenerateRefreshToken();

  /// <summary>
  /// 建立 Token Family
  /// </summary>
  /// <param name="userId">使用者 ID</param>
  /// <param name="refreshToken">重新整理令牌</param>
  /// <returns>Token Family ID</returns>
  Task<string> CreateTokenFamilyAsync(string userId, string refreshToken);

  /// <summary>
  /// 輪換重新整理令牌
  /// </summary>
  /// <param name="refreshToken">當前重新整理令牌</param>
  /// <param name="userId">使用者 ID</param>
  /// <returns>新的 Token 回應</returns>
  Task<TokenResponse> RotateRefreshTokenAsync(string refreshToken, string userId);

  /// <summary>
  /// 偵測令牌重用並撤銷整個 Family
  /// </summary>
  /// <param name="familyId">Family ID</param>
  /// <param name="reason">撤銷原因</param>
  Task DetectReuseAndRevokeAsync(string familyId, string reason);

  /// <summary>
  /// 撤銷 Token Family
  /// </summary>
  /// <param name="familyId">Family ID</param>
  /// <param name="reason">撤銷原因</param>
  Task RevokeTokenFamilyAsync(string familyId, string reason);

  /// <summary>
  /// 檢查令牌是否在黑名單中
  /// </summary>
  /// <param name="jti">JWT ID</param>
  /// <returns>是否在黑名單中</returns>
  Task<bool> IsTokenBlacklistedAsync(string jti);

  /// <summary>
  /// 將令牌加入黑名單
  /// </summary>
  /// <param name="jti">JWT ID</param>
  /// <param name="expiresAt">過期時間</param>
  Task AddToBlacklistAsync(string jti, DateTime expiresAt);

  /// <summary>
  /// 根據 Refresh Token 取得 Family ID
  /// </summary>
  /// <param name="refreshToken">重新整理令牌</param>
  /// <returns>FamilyId，如果找不到則回傳 null</returns>
  Task<string?> GetFamilyIdByRefreshTokenAsync(string refreshToken);

  /// <summary>
  /// 根據 Refresh Token 取得關聯的 User ID
  /// </summary>
  /// <param name="refreshToken">Refresh token</param>
  /// <returns>UserId，如果找不到則回傳 null</returns>
  Task<string?> GetUserIdByRefreshTokenAsync(string refreshToken);
}
