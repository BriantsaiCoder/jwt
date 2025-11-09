using FluentAssertions;
using JwtAuthApi.Models;
using JwtAuthApi.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace JwtAuthApi.Tests.Integration;

public class ConfigurationTests : IClassFixture<WebApplicationFactoryFixture>
{
  private readonly WebApplicationFactoryFixture _factory;

  public ConfigurationTests(WebApplicationFactoryFixture factory)
  {
    _factory = factory;
  }

  [Fact]
  public void JwtSettings_ShouldBeConfiguredCorrectly()
  {
    // Arrange & Act
    var jwtSettings = _factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

    // Assert
    jwtSettings.Should().NotBeNull();
    jwtSettings.Issuer.Should().Be("JwtAuthApi");
    jwtSettings.Audience.Should().Be("JwtAuthApi");
    jwtSettings.SecretKey.Should().NotBeNullOrWhiteSpace();
    jwtSettings.AccessTokenExpiryMinutes.Should().Be(15);
    jwtSettings.RefreshTokenExpiryDays.Should().Be(14);
    jwtSettings.GracePeriodSeconds.Should().Be(30);
  }
}
