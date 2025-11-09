using JwtAuthApi.Models;

namespace JwtAuthApi.Services;

/// <summary>
/// 使用者服務介面
/// </summary>
public interface IUserService
{
  /// <summary>
  /// 驗證使用者憑證
  /// </summary>
  /// <param name="username">使用者名稱</param>
  /// <param name="password">密碼</param>
  /// <returns>驗證成功則回傳使用者物件，否則回傳 null</returns>
  Task<User?> ValidateCredentialsAsync(string username, string password);

  /// <summary>
  /// 根據 ID 取得使用者
  /// </summary>
  /// <param name="id">使用者 ID</param>
  /// <returns>使用者物件或 null</returns>
  Task<User?> GetUserByIdAsync(string id);

  /// <summary>
  /// 根據使用者名稱取得使用者
  /// </summary>
  /// <param name="username">使用者名稱</param>
  /// <returns>使用者物件或 null</returns>
  Task<User?> GetUserByUsernameAsync(string username);
}
