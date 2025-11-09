using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers.V1;

/// <summary>
/// 管理員專用 API - 需要 Admin 角色
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
  private readonly ILogger<AdminController> _logger;

  public AdminController(ILogger<AdminController> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// 取得所有使用者清單（僅限管理員）
  /// </summary>
  /// <returns>使用者清單</returns>
  /// <response code="200">成功取得使用者清單</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足（非管理員）</response>
  [HttpGet("users")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public IActionResult GetAllUsers()
  {
    var adminUsername = User.Identity?.Name ?? "Unknown";
    _logger.LogInformation("管理員 {AdminUsername} 請求使用者清單", adminUsername);

    var users = new[]
    {
            new { Id = "1", Username = "admin", Roles = new[] { "Admin", "User" }, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new { Id = "2", Username = "user", Roles = new[] { "User" }, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new { Id = "3", Username = "guest", Roles = new[] { "Guest" }, CreatedAt = DateTime.UtcNow.AddDays(-7) }
        };

    return Ok(new { TotalCount = users.Length, Users = users });
  }

  /// <summary>
  /// 取得系統統計資訊（僅限管理員）
  /// </summary>
  /// <returns>系統統計資訊</returns>
  /// <response code="200">成功取得統計資訊</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足（非管理員）</response>
  [HttpGet("stats")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public IActionResult GetSystemStats()
  {
    var adminUsername = User.Identity?.Name ?? "Unknown";
    _logger.LogInformation("管理員 {AdminUsername} 請求系統統計資訊", adminUsername);

    var stats = new
    {
      TotalUsers = 3,
      ActiveSessions = Random.Shared.Next(1, 10),
      TotalApiCalls = Random.Shared.Next(100, 10000),
      SystemUptime = TimeSpan.FromHours(Random.Shared.Next(1, 720)),
      LastUpdated = DateTime.UtcNow
    };

    return Ok(stats);
  }

  /// <summary>
  /// 清除快取（僅限管理員）
  /// </summary>
  /// <returns>操作結果</returns>
  /// <response code="200">快取清除成功</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足（非管理員）</response>
  [HttpPost("clear-cache")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public IActionResult ClearCache()
  {
    var adminUsername = User.Identity?.Name ?? "Unknown";
    _logger.LogInformation("管理員 {AdminUsername} 執行清除快取操作", adminUsername);

    // 實際應用中會清除快取，這裡只是示範
    return Ok(new
    {
      Message = "快取已成功清除",
      ClearedAt = DateTime.UtcNow,
      ClearedBy = adminUsername
    });
  }

  /// <summary>
  /// 撤銷使用者的所有 token（僅限管理員）
  /// </summary>
  /// <param name="userId">要撤銷的使用者 ID</param>
  /// <returns>操作結果</returns>
  /// <response code="200">Token 撤銷成功</response>
  /// <response code="400">使用者 ID 無效</response>
  /// <response code="401">未驗證</response>
  /// <response code="403">權限不足（非管理員）</response>
  /// <response code="404">找不到使用者</response>
  [HttpPost("revoke-user-tokens/{userId}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public IActionResult RevokeUserTokens(string userId)
  {
    if (string.IsNullOrWhiteSpace(userId))
    {
      return BadRequest(new { Message = "使用者 ID 不可為空" });
    }

    var adminUsername = User.Identity?.Name ?? "Unknown";
    _logger.LogWarning("管理員 {AdminUsername} 撤銷使用者 {UserId} 的所有 token", adminUsername, userId);

    // 實際應用中會撤銷該使用者的所有 token family
    return Ok(new
    {
      Message = $"已撤銷使用者 {userId} 的所有 token",
      RevokedAt = DateTime.UtcNow,
      RevokedBy = adminUsername
    });
  }
}
