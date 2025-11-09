namespace JwtAuthApi.Models;

/// <summary>
/// 使用者領域模型
/// </summary>
public class User
{
  /// <summary>
  /// 使用者唯一識別碼
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// 使用者名稱
  /// </summary>
  public string Username { get; set; } = string.Empty;

  /// <summary>
  /// 密碼雜湊值
  /// </summary>
  public string PasswordHash { get; set; } = string.Empty;

  /// <summary>
  /// 使用者角色清單
  /// </summary>
  public string[] Roles { get; set; } = Array.Empty<string>();

  /// <summary>
  /// 建立時間
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
