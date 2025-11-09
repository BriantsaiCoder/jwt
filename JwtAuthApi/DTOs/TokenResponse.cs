namespace JwtAuthApi.DTOs;

/// <summary>
/// Token 回應 DTO
/// </summary>
public class TokenResponse
{
  /// <summary>
  /// 存取令牌
  /// </summary>
  public string AccessToken { get; set; } = string.Empty;

  /// <summary>
  /// 重新整理令牌
  /// </summary>
  public string RefreshToken { get; set; } = string.Empty;

  /// <summary>
  /// 存取令牌過期時間（秒）
  /// </summary>
  public int ExpiresIn { get; set; }

  /// <summary>
  /// Token 類型
  /// </summary>
  public string TokenType { get; set; } = "Bearer";
}
