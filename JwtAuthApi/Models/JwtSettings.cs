namespace JwtAuthApi.Models;

/// <summary>
/// JWT 組態設定
/// </summary>
public class JwtSettings
{
  /// <summary>
  /// Token 發行者
  /// </summary>
  public string Issuer { get; set; } = string.Empty;

  /// <summary>
  /// Token 接收者
  /// </summary>
  public string Audience { get; set; } = string.Empty;

  /// <summary>
  /// 密鑰（從 User Secrets 或環境變數讀取）
  /// </summary>
  public string SecretKey { get; set; } = string.Empty;

  /// <summary>
  /// Access Token 過期時間（分鐘）
  /// </summary>
  public int AccessTokenExpiryMinutes { get; set; } = 15;

  /// <summary>
  /// Refresh Token 過期時間（天）
  /// </summary>
  public int RefreshTokenExpiryDays { get; set; } = 14;

  /// <summary>
  /// 寬限期（秒）- 允許父 Token 在此期間內使用一次
  /// </summary>
  public int GracePeriodSeconds { get; set; } = 30;
}
