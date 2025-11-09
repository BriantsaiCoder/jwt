using System.Text;
using JwtAuthApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuthApi.Tests.Infrastructure;

public class WebApplicationFactoryFixture : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureAppConfiguration((context, config) =>
    {
      // 添加測試專用組態 (最後添加以覆蓋現有配置)
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Jwt:Issuer"] = "JwtAuthApi",
        ["Jwt:Audience"] = "JwtAuthApi",
        ["Jwt:SecretKey"] = "VGhpc0lzQVZlcnlTZWN1cmVTZWNyZXRLZXlGb3JUZXN0aW5nUHVycG9zZXNPbmx5MTIzNDU2Nzg5MA==",
        ["Jwt:AccessTokenExpiryMinutes"] = "15",
        ["Jwt:RefreshTokenExpiryDays"] = "14",
        ["Jwt:GracePeriodSeconds"] = "30",
        // 添加 Serilog 最低配置以避免啟動錯誤
        ["Serilog:Using:0"] = "Serilog.Sinks.Console",
        ["Serilog:MinimumLevel:Default"] = "Warning", //降低日誌等級
        ["Serilog:WriteTo:0:Name"] = "Console"
      });
    });

    builder.ConfigureTestServices(services =>
    {
      // 替換 IMemoryCache 為測試專用實例
      services.RemoveAll<IMemoryCache>();
      services.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));

      // 重新配置 JWT Bearer 選項,確保使用測試配置
      services.PostConfigureAll<JwtBearerOptions>(options =>
      {
        // 使用測試環境的硬編碼配置
        var secretKey = "VGhpc0lzQVZlcnlTZWN1cmVTZWNyZXRLZXlGb3JUZXN0aW5nUHVycG9zZXNPbmx5MTIzNDU2Nzg5MA==";
        var key = Encoding.UTF8.GetBytes(secretKey);

        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(key);
        options.TokenValidationParameters.ValidIssuer = "JwtAuthApi";
        options.TokenValidationParameters.ValidAudience = "JwtAuthApi";
      });
    });
  }
}
