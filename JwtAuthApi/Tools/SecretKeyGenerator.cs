using System.Diagnostics;
using System.Security.Cryptography;

namespace JwtAuthApi.Tools;

/// <summary>
/// JWT 密鑰生成器工具
/// </summary>
public static class SecretKeyGenerator
{
  /// <summary>
  /// 產生 64-byte (512-bit) 安全密鑰
  /// </summary>
  /// <returns>Base64 編碼的密鑰</returns>
  public static string GenerateSecureKey()
  {
    var key = new byte[64]; // 512 bits
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(key);
    }
    return Convert.ToBase64String(key);
  }

  /// <summary>
  /// 設定密鑰到 User Secrets
  /// </summary>
  /// <param name="key">要設定的密鑰</param>
  /// <param name="projectPath">專案路徑（選用）</param>
  public static void SetUserSecret(string key, string? projectPath = null)
  {
    try
    {
      var workingDirectory = projectPath ?? Directory.GetCurrentDirectory();

      var processStartInfo = new ProcessStartInfo
      {
        FileName = "dotnet",
        Arguments = $"user-secrets set \"Jwt:SecretKey\" \"{key}\"",
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(processStartInfo);
      if (process != null)
      {
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (process.ExitCode == 0)
        {
          Console.WriteLine("✓ 密鑰已成功設定到 User Secrets");
          Console.WriteLine(output);
        }
        else
        {
          Console.WriteLine("✗ 設定密鑰失敗:");
          Console.WriteLine(error);
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"✗ 執行 dotnet user-secrets 時發生錯誤: {ex.Message}");
    }
  }

  /// <summary>
  /// 一鍵產生並設定密鑰
  /// </summary>
  /// <param name="projectPath">專案路徑（選用）</param>
  /// <returns>產生的密鑰</returns>
  public static string GenerateAndSetKey(string? projectPath = null)
  {
    Console.WriteLine("正在產生安全密鑰...");
    var key = GenerateSecureKey();

    Console.WriteLine($"密鑰已產生: {key.Substring(0, 20)}... (共 {key.Length} 個字元)");
    Console.WriteLine();

    SetUserSecret(key, projectPath);

    return key;
  }
}
