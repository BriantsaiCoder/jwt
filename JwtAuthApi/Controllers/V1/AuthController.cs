using Asp.Versioning;
using JwtAuthApi.DTOs;
using JwtAuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers.V1;

/// <summary>
/// 驗證 API - 處理使用者登入、token 刷新與登出
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
  private readonly IUserService _userService;
  private readonly IJwtTokenService _tokenService;
  private readonly ILogger<AuthController> _logger;

  public AuthController(
      IUserService userService,
      IJwtTokenService tokenService,
      ILogger<AuthController> logger)
  {
    _userService = userService;
    _tokenService = tokenService;
    _logger = logger;
  }

  /// <summary>
  /// 使用者登入
  /// </summary>
  /// <param name="request">登入請求，包含使用者名稱與密碼</param>
  /// <returns>存取 token 與刷新 token</returns>
  /// <response code="200">登入成功，回傳 JWT token</response>
  /// <response code="400">請求格式錯誤</response>
  /// <response code="401">帳號或密碼錯誤</response>
  [HttpPost("login")]
  [AllowAnonymous]
  [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Login([FromBody] LoginRequest request)
  {
    _logger.LogInformation("使用者嘗試登入: {Username}", request.Username);

    // 驗證使用者憑證
    var user = await _userService.ValidateCredentialsAsync(request.Username, request.Password);
    if (user == null)
    {
      _logger.LogWarning("登入失敗 - 無效的憑證: {Username}", request.Username);
      return Unauthorized(new ErrorResponse
      {
        StatusCode = 401,
        Message = "帳號或密碼錯誤",
        TraceId = HttpContext.TraceIdentifier,
        Timestamp = DateTime.UtcNow
      });
    }

    // 產生 access token
    var accessToken = _tokenService.GenerateAccessToken(user);

    // 產生 refresh token
    var refreshToken = _tokenService.GenerateRefreshToken();

    // 建立 token 家族
    await _tokenService.CreateTokenFamilyAsync(user.Id, refreshToken);

    _logger.LogInformation("使用者登入成功: {Username} (ID: {UserId})", user.Username, user.Id);

    return Ok(new TokenResponse
    {
      AccessToken = accessToken,
      RefreshToken = refreshToken,
      ExpiresIn = 15 * 60, // 15 分鐘
      TokenType = "Bearer"
    });
  }

  /// <summary>
  /// 刷新存取 token
  /// </summary>
  /// <param name="request">刷新請求，包含當前的 refresh token</param>
  /// <returns>新的存取 token 與刷新 token</returns>
  /// <response code="200">Token 刷新成功</response>
  /// <response code="400">請求格式錯誤</response>
  /// <response code="401">Token 無效、已過期或已被撤銷</response>
  [HttpPost("refresh")]
  [AllowAnonymous]
  [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
  {
    _logger.LogInformation("嘗試刷新 token");

    try
    {
      // 首先取得 user ID
      var userId = await _tokenService.GetUserIdByRefreshTokenAsync(request.RefreshToken);
      if (userId == null)
      {
        _logger.LogWarning("找不到對應的 token family");
        return Unauthorized(new ErrorResponse
        {
          StatusCode = 401,
          Message = "無效的 refresh token",
          TraceId = HttpContext.TraceIdentifier,
          Timestamp = DateTime.UtcNow
        });
      }

      // 旋轉 refresh token
      var tokenResponse = await _tokenService.RotateRefreshTokenAsync(request.RefreshToken, userId);

      _logger.LogInformation("Token 刷新成功: User {UserId}", userId);

      return Ok(tokenResponse);
    }
    catch (UnauthorizedAccessException ex)
    {
      _logger.LogWarning(ex, "Token 刷新失敗 - 未授權");
      return Unauthorized(new ErrorResponse
      {
        StatusCode = 401,
        Message = ex.Message,
        TraceId = HttpContext.TraceIdentifier,
        Timestamp = DateTime.UtcNow
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Token 刷新時發生錯誤");
      throw;
    }
  }

  /// <summary>
  /// 使用者登出
  /// </summary>
  /// <param name="request">登出請求，包含要撤銷的 refresh token</param>
  /// <returns>登出結果</returns>
  /// <response code="204">登出成功</response>
  /// <response code="400">請求格式錯誤</response>
  /// <response code="401">Token 無效</response>
  [HttpPost("logout")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
  {
    _logger.LogInformation("使用者嘗試登出");

    try
    {
      // 取得 family ID
      var familyId = await _tokenService.GetFamilyIdByRefreshTokenAsync(request.RefreshToken);

      if (familyId != null)
      {
        // 撤銷整個 token 家族
        await _tokenService.RevokeTokenFamilyAsync(familyId, "使用者登出");
      }

      // 將當前 access token 加入黑名單
      var accessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
      if (!string.IsNullOrWhiteSpace(accessToken))
      {
        // 從 token 中提取 JTI (需要解析 JWT)
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(accessToken);
        var jti = jwtToken.Id;
        var exp = jwtToken.ValidTo;

        await _tokenService.AddToBlacklistAsync(jti, exp);
      }

      _logger.LogInformation("使用者登出成功");
      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "登出時發生錯誤");
      throw;
    }
  }

  /// <summary>
  /// 取得當前使用者資訊
  /// </summary>
  /// <returns>使用者資訊</returns>
  /// <response code="200">成功取得使用者資訊</response>
  /// <response code="401">未驗證或 token 無效</response>
  [HttpGet("me")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
  public IActionResult GetCurrentUser()
  {
    var userId = User.FindFirst("sub")?.Value;
    var username = User.Identity?.Name;
    var roles = User.FindAll("role").Select(c => c.Value).ToArray();

    return Ok(new
    {
      UserId = userId,
      Username = username,
      Roles = roles,
      Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
    });
  }
}
