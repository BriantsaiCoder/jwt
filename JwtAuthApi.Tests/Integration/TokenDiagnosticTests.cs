using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using JwtAuthApi.DTOs;
using JwtAuthApi.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace JwtAuthApi.Tests.Integration;

public class TokenDiagnosticTests : IClassFixture<WebApplicationFactoryFixture>
{
  private readonly HttpClient _client;
  private readonly ITestOutputHelper _output;

  public TokenDiagnosticTests(WebApplicationFactoryFixture factory, ITestOutputHelper output)
  {
    _client = factory.CreateClient();
    _output = output;
  }

  [Fact]
  public async Task Login_GeneratesValidJwtToken()
  {
    // Arrange
    var loginRequest = TestDataBuilder.CreateLoginRequest("user", "User@123");

    // Act
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var tokens = await loginResponse.Content.ReadAsJsonAsync<TokenResponse>();

    // Assert
    tokens.Should().NotBeNull();
    tokens!.AccessToken.Should().NotBeNullOrEmpty();

    // 解析 token
    var handler = new JwtSecurityTokenHandler();
    var canRead = handler.CanReadToken(tokens.AccessToken);
    canRead.Should().BeTrue("token should be a valid JWT");

    var jwtToken = handler.ReadJwtToken(tokens.AccessToken);

    _output.WriteLine($"Token Issuer: {jwtToken.Issuer}");
    _output.WriteLine($"Token Audiences: {string.Join(", ", jwtToken.Audiences)}");
    _output.WriteLine($"Token Valid From: {jwtToken.ValidFrom}");
    _output.WriteLine($"Token Valid To: {jwtToken.ValidTo}");
    _output.WriteLine("Claims:");
    foreach (var claim in jwtToken.Claims)
    {
      _output.WriteLine($"  {claim.Type}: {claim.Value}");
    }

    jwtToken.Issuer.Should().Be("JwtAuthApi");
    jwtToken.Audiences.Should().Contain("JwtAuthApi");
  }
}
