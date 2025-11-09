using System.Text;
using Asp.Versioning;
using JwtAuthApi.Middleware;
using JwtAuthApi.Models;
using JwtAuthApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("啟動應用程式");

    var builder = WebApplication.CreateBuilder(args);

    // 使用 Serilog
    builder.Host.UseSerilog();

    // 載入 JWT 設定
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
        ?? throw new InvalidOperationException("JWT 設定無法載入");

    // 驗證密鑰
    if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    {
        throw new InvalidOperationException("JWT SecretKey 未設定。請執行密鑰產生器或設定 User Secrets。");
    }

    // 註冊服務
    builder.Services.AddMemoryCache();
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

    // 註冊密碼雜湊器，使用 100,000 次疊代
    builder.Services.AddSingleton<IPasswordHasher<User>>(sp =>
    {
        var hasher = new PasswordHasher<User>(
            optionsAccessor: Microsoft.Extensions.Options.Options.Create(
                new PasswordHasherOptions
                {
                    IterationCount = 100_000 // 符合 2024-2025 安全標準
                }));
        return hasher;
    });

    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
    builder.Services.AddSingleton<IUserService, InMemoryUserService>();

    // 配置 CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // 配置 JWT 驗證
    var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // 移除預設 5 分鐘時鐘偏移
        };

        // 驗證 token 是否被加入黑名單
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenService = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>();

                // 從 Claims 中取得 JTI (JWT ID)
                var jti = context.Principal?.FindFirst("jti")?.Value
                    ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(jti) && await tokenService.IsTokenBlacklistedAsync(jti))
                {
                    context.Fail("此 token 已被撤銷");
                    Log.Warning("使用已撤銷的 token 嘗試存取: JTI={Jti}", jti);
                }
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception != null)
                {
                    Log.Warning("JWT 驗證失敗: {Message}", context.Exception.Message);
                }
                return Task.CompletedTask;
            }
        };

        options.SaveToken = true;
    });

    builder.Services.AddAuthorization();

    // 配置 API 版本控制
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // 配置 Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "JWT Authentication API",
            Version = "v1",
            Description = "ASP.NET Core 8 JWT 驗證 API with Refresh Token Rotation",
            Contact = new OpenApiContact
            {
                Name = "Development Team",
                Email = "dev@example.com"
            }
        });

        // 加入 JWT Bearer 驗證至 Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                          "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                          "Example: \"Bearer 12345abcdef\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // 包含 XML 註解檔
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // 註冊全域例外處理器
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // 配置 HTTP 請求管線
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "JWT Auth API v1");
            c.RoutePrefix = string.Empty; // Swagger UI 在根路徑
        });
    }

    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("應用程式啟動成功，監聽: {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "應用程式啟動失敗");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// 讓測試專案可以存取 Program 類別
public partial class Program { }
