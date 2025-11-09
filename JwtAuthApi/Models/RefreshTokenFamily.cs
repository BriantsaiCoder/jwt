namespace JwtAuthApi.Models;

/// <summary>
/// Refresh Token Family 模型，用於追蹤令牌輪換與重用偵測
/// </summary>
public class RefreshTokenFamily
{
  /// <summary>
  /// Token Family 唯一識別碼
  /// </summary>
  public string FamilyId { get; set; } = string.Empty;

  /// <summary>
  /// 當前有效的 Refresh Token
  /// </summary>
  public string CurrentToken { get; set; } = string.Empty;

  /// <summary>
  /// 父 Token（用於寬限期檢查）
  /// </summary>
  public string? ParentToken { get; set; }

  /// <summary>
  /// 父 Token 是否已被使用過（防止重複使用）
  /// </summary>
  public bool ParentTokenUsed { get; set; }

  /// <summary>
  /// 所屬使用者 ID
  /// </summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// Token 核發時間
  /// </summary>
  public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Token 過期時間
  /// </summary>
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// 是否已撤銷
  /// </summary>
  public bool IsRevoked { get; set; }

  /// <summary>
  /// 撤銷時間
  /// </summary>
  public DateTime? RevokedAt { get; set; }

  /// <summary>
  /// 撤銷原因
  /// </summary>
  public string? RevokedReason { get; set; }
}
