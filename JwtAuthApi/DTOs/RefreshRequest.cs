using System.ComponentModel.DataAnnotations;

namespace JwtAuthApi.DTOs;

/// <summary>
/// 重新整理令牌請求 DTO
/// </summary>
public class RefreshRequest
{
  /// <summary>
  /// 重新整理令牌
  /// </summary>
  [Required(ErrorMessage = "重新整理令牌為必填")]
  public string RefreshToken { get; set; } = string.Empty;
}
