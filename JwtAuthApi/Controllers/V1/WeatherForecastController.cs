using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers.V1;

/// <summary>
/// 天氣預報 API - 需要驗證的範例端點
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class WeatherForecastController : ControllerBase
{
  private static readonly string[] Summaries = new[]
  {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

  private readonly ILogger<WeatherForecastController> _logger;

  public WeatherForecastController(ILogger<WeatherForecastController> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// 取得天氣預報資料
  /// </summary>
  /// <returns>未來5天的天氣預報</returns>
  /// <response code="200">成功取得天氣預報</response>
  /// <response code="401">未驗證或 token 無效</response>
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public IActionResult Get()
  {
    var username = User.Identity?.Name ?? "Unknown";
    _logger.LogInformation("使用者 {Username} 請求天氣預報資料", username);

    var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
    {
      Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
      TemperatureC = Random.Shared.Next(-20, 55),
      Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    })
    .ToArray();

    return Ok(forecasts);
  }

  /// <summary>
  /// 取得特定日期的天氣預報
  /// </summary>
  /// <param name="days">未來天數 (1-30)</param>
  /// <returns>指定天數後的天氣預報</returns>
  /// <response code="200">成功取得天氣預報</response>
  /// <response code="400">天數參數無效</response>
  /// <response code="401">未驗證或 token 無效</response>
  [HttpGet("{days:int}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public IActionResult GetByDays(int days)
  {
    if (days < 1 || days > 30)
    {
      return BadRequest(new { Message = "天數必須介於 1 到 30 之間" });
    }

    var forecast = new WeatherForecast
    {
      Date = DateOnly.FromDateTime(DateTime.Now.AddDays(days)),
      TemperatureC = Random.Shared.Next(-20, 55),
      Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    };

    return Ok(forecast);
  }
}

/// <summary>
/// 天氣預報資料模型
/// </summary>
public class WeatherForecast
{
  /// <summary>
  /// 日期
  /// </summary>
  public DateOnly Date { get; set; }

  /// <summary>
  /// 攝氏溫度
  /// </summary>
  public int TemperatureC { get; set; }

  /// <summary>
  /// 華氏溫度
  /// </summary>
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

  /// <summary>
  /// 天氣摘要
  /// </summary>
  public string? Summary { get; set; }
}
