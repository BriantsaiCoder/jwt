using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JwtAuthApi.Models;
using Microsoft.AspNetCore.Identity;

namespace JwtAuthApi.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class PasswordHashingBenchmarks
{
  private PasswordHasher<User> _hasher10k = null!;
  private PasswordHasher<User> _hasher100k = null!;
  private PasswordHasher<User> _hasher500k = null!;
  private User _user = null!;
  private const string TestPassword = "TestPassword@123";

  [GlobalSetup]
  public void Setup()
  {
    _user = new User
    {
      Id = "1",
      Username = "testuser",
      Roles = new[] { "User" },
      CreatedAt = DateTime.UtcNow
    };

    _hasher10k = new PasswordHasher<User>(
        new OptionsWrapper<PasswordHasherOptions>(
            new PasswordHasherOptions { IterationCount = 10000 }
        )
    );

    _hasher100k = new PasswordHasher<User>(
        new OptionsWrapper<PasswordHasherOptions>(
            new PasswordHasherOptions { IterationCount = 100000 }
        )
    );

    _hasher500k = new PasswordHasher<User>(
        new OptionsWrapper<PasswordHasherOptions>(
            new PasswordHasherOptions { IterationCount = 500000 }
        )
    );
  }

  [Benchmark(Baseline = true)]
  public string HashPassword_10k_Iterations()
  {
    return _hasher10k.HashPassword(_user, TestPassword);
  }

  [Benchmark]
  public string HashPassword_100k_Iterations()
  {
    return _hasher100k.HashPassword(_user, TestPassword);
  }

  [Benchmark]
  public string HashPassword_500k_Iterations()
  {
    return _hasher500k.HashPassword(_user, TestPassword);
  }

  [Benchmark]
  public PasswordVerificationResult VerifyPassword_100k_Iterations()
  {
    var hash = _hasher100k.HashPassword(_user, TestPassword);
    return _hasher100k.VerifyHashedPassword(_user, hash, TestPassword);
  }
}

// Helper class for OptionsWrapper
public class OptionsWrapper<T> : Microsoft.Extensions.Options.IOptions<T> where T : class
{
  public OptionsWrapper(T value)
  {
    Value = value;
  }

  public T Value { get; }
}
