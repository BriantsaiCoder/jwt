using System.ComponentModel.DataAnnotations;

namespace JwtAuthApi.DTOs;

/// <summary>
/// 登入請求 DTO
/// </summary>
public class LoginRequest
{
  /// <summary>
  /// 使用者名稱
  /// </summary>
  [Required(ErrorMessage = "使用者名稱為必填")]
  [StringLength(50, MinimumLength = 3, ErrorMessage = "使用者名稱長度必須在 3 到 50 個字元之間")]
  public string Username { get; set; } = string.Empty;

  /// <summary>
  /// 密碼
  /// </summary>
  [Required(ErrorMessage = "密碼為必填")]
  [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須在 6 到 100 個字元之間")]
  public string Password { get; set; } = string.Empty;
}
