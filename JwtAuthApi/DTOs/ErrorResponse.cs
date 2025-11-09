namespace JwtAuthApi.DTOs;

/// <summary>
/// 錯誤回應 DTO
/// </summary>
public class ErrorResponse
{
  /// <summary>
  /// HTTP 狀態碼
  /// </summary>
  public int StatusCode { get; set; }

  /// <summary>
  /// 錯誤訊息
  /// </summary>
  public string Message { get; set; } = string.Empty;

  /// <summary>
  /// 追蹤 ID
  /// </summary>
  public string TraceId { get; set; } = string.Empty;

  /// <summary>
  /// 時間戳記
  /// </summary>
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// 詳細錯誤資訊（選用）
  /// </summary>
  public Dictionary<string, string[]>? Errors { get; set; }
}
