using JwtAuthApi.Models;
using Microsoft.AspNetCore.Identity;

namespace JwtAuthApi.Services;

/// <summary>
/// 記憶體內存使用者服務（用於展示）
/// </summary>
public class InMemoryUserService : IUserService
{
  private readonly IPasswordHasher<User> _passwordHasher;
  private readonly ILogger<InMemoryUserService> _logger;
  private readonly Dictionary<string, User> _users;

  public InMemoryUserService(
      IPasswordHasher<User> passwordHasher,
      ILogger<InMemoryUserService> logger)
  {
    _passwordHasher = passwordHasher;
    _logger = logger;
    _users = new Dictionary<string, User>();

    // 初始化測試使用者
    InitializeTestUsers();
  }

  private void InitializeTestUsers()
  {
    var users = new[]
    {
            new User
            {
                Id = "1",
                Username = "admin",
                Roles = new[] { "Admin", "User" },
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = "2",
                Username = "user",
                Roles = new[] { "User" },
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = "3",
                Username = "guest",
                Roles = new[] { "Guest" },
                CreatedAt = DateTime.UtcNow
            }
        };

    foreach (var user in users)
    {
      // 密碼格式: {Username}@123
      // admin -> Admin@123, user -> User@123, guest -> Guest@123
      var password = $"{char.ToUpper(user.Username[0])}{user.Username.Substring(1)}@123";
      user.PasswordHash = _passwordHasher.HashPassword(user, password);
      _users[user.Username.ToLower()] = user;
    }

    _logger.LogInformation("已初始化 {Count} 個測試使用者", _users.Count);
    _logger.LogInformation("測試帳號: admin/Admin@123 (Admin), user/User@123 (User), guest/Guest@123 (Guest)");
  }

  public async Task<User?> ValidateCredentialsAsync(string username, string password)
  {
    var user = await GetUserByUsernameAsync(username);
    if (user == null)
    {
      _logger.LogWarning("使用者不存在: {Username}", username);
      return null;
    }

    var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
    if (result == PasswordVerificationResult.Success ||
        result == PasswordVerificationResult.SuccessRehashNeeded)
    {
      _logger.LogInformation("使用者驗證成功: {Username}", username);
      return user;
    }

    _logger.LogWarning("使用者密碼驗證失敗: {Username}", username);
    return null;
  }

  public async Task<User?> GetUserByIdAsync(string id)
  {
    var user = _users.Values.FirstOrDefault(u => u.Id == id);
    return await Task.FromResult(user);
  }

  public async Task<User?> GetUserByUsernameAsync(string username)
  {
    _users.TryGetValue(username.ToLower(), out var user);
    return await Task.FromResult(user);
  }
}
