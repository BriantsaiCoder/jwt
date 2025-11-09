using Asp.Versioning;
using JwtAuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers.V1;

/// <summary>
/// 使用者 API - 需要 User 或更高權限
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "User,Admin")]
[Produces("application/json")]
public class UserController : ControllerBase
{
  private readonly IUserService _userService;
  private readonly ILogger<UserController> _logger;

  public UserController(IUserService userService, ILogger<UserController> logger)
  {
    _userService = userService;
    _logger = logger;
  }

  /// <summary>
  /// 取得當前使用者的個人資料
  /// </summary>
  /// <returns>使用者個人資料</returns>
  /// <response code="200">成功取得個人資料</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足</response>
  /// <response code="404">找不到使用者</response>
  [HttpGet("profile")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetProfile()
  {
    // 從 JWT Claims 中取得使用者 ID
    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
    {
      return Unauthorized(new { Message = "無法識別使用者" });
    }

    _logger.LogInformation("使用者 {UserId} 請求個人資料", userId);

    var user = await _userService.GetUserByIdAsync(userId);
    if (user == null)
    {
      return NotFound(new { Message = "找不到使用者" });
    }

    return Ok(new
    {
      Id = user.Id,
      Username = user.Username,
      Roles = user.Roles,
      CreatedAt = user.CreatedAt,
      Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
    });
  }

  /// <summary>
  /// 更新使用者個人資料（示範用）
  /// </summary>
  /// <param name="request">更新請求</param>
  /// <returns>更新結果</returns>
  /// <response code="200">更新成功</response>
  /// <response code="400">請求格式錯誤</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足</response>
  [HttpPut("profile")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
  {
    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var username = User.Identity?.Name ?? "Unknown";

    _logger.LogInformation("使用者 {Username} (ID: {UserId}) 更新個人資料", username, userId);

    // 實際應用中會更新資料庫
    return Ok(new
    {
      Message = "個人資料更新成功",
      UpdatedAt = DateTime.UtcNow,
      Data = request
    });
  }

  /// <summary>
  /// 取得使用者活動記錄（示範用）
  /// </summary>
  /// <param name="limit">限制筆數</param>
  /// <returns>活動記錄清單</returns>
  /// <response code="200">成功取得活動記錄</response>
  /// <response code="400">參數無效</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足</response>
  [HttpGet("activities")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public IActionResult GetActivities([FromQuery] int limit = 10)
  {
    if (limit < 1 || limit > 100)
    {
      return BadRequest(new { Message = "limit 必須介於 1 到 100 之間" });
    }

    var username = User.Identity?.Name ?? "Unknown";
    _logger.LogInformation("使用者 {Username} 請求活動記錄，限制 {Limit} 筆", username, limit);

    var activities = Enumerable.Range(1, limit).Select(i => new
    {
      Id = i,
      Action = i % 3 == 0 ? "登入" : (i % 2 == 0 ? "更新資料" : "查詢資料"),
      Timestamp = DateTime.UtcNow.AddMinutes(-i * 5),
      IpAddress = $"192.168.1.{Random.Shared.Next(1, 255)}",
      UserAgent = "Mozilla/5.0"
    }).ToArray();

    return Ok(new
    {
      TotalCount = activities.Length,
      Activities = activities
    });
  }
}

/// <summary>
/// 更新個人資料請求
/// </summary>
public class UpdateProfileRequest
{
  /// <summary>
  /// 電子郵件
  /// </summary>
  public string? Email { get; set; }

  /// <summary>
  /// 顯示名稱
  /// </summary>
  public string? DisplayName { get; set; }

  /// <summary>
  /// 個人簡介
  /// </summary>
  public string? Bio { get; set; }
}
