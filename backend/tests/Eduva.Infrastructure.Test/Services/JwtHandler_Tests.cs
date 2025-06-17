using Eduva.Application.Exceptions.Auth;
using Eduva.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eduva.Infrastructure.Test.Services;

[TestFixture]
public class JwtHandler_Tests
{
    private JwtHandler _jwtHandler;
    private Mock<IConfiguration> _configurationMock;
    private Mock<ILogger<JwtHandler>> _loggerMock;
    private IConfigurationSection _jwtSection;
    private const string ValidLongKey = "supersecretkey1234567890supersecretkey1234567890supersecretkey123456";

    [SetUp]
    public void SetUp()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<JwtHandler>>();

        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["SecretKey"]).Returns(ValidLongKey);
        sectionMock.Setup(s => s["ValidIssuer"]).Returns("TestIssuer");
        sectionMock.Setup(s => s["ValidAudience"]).Returns("TestAudience");
        sectionMock.Setup(s => s["ExpiryInSecond"]).Returns("3600");

        _jwtSection = sectionMock.Object;

        _configurationMock.Setup(c => c.GetSection("JwtSettings"))
                          .Returns(_jwtSection);

        _jwtHandler = new JwtHandler(_configurationMock.Object, _loggerMock.Object);
    }

    [Test]
    public void GetSigningCredentials_ShouldReturnValidCredentials()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        Assert.That(creds, Is.Not.Null);
        Assert.That(creds.Algorithm, Is.EqualTo(SecurityAlgorithms.HmacSha256));
    }

    [Test]
    public void GetExpiryInSecond_ShouldReturn3600()
    {
        var expiry = _jwtHandler.GetExpiryInSecond();
        Assert.That(expiry, Is.EqualTo(3600));
    }

    [Test]
    public void GenerateTokenOptions_ShouldIncludeClaims()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "123") };

        var token = _jwtHandler.GenerateTokenOptions(creds, claims);

        Assert.That(token, Is.Not.Null);
        Assert.That(token.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Iat), Is.True);
        Assert.That(token.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123"), Is.True);
    }

    [Test]
    public void GetPrincipalFromExpiredToken_ShouldThrow_OnInvalidAlgorithm()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidLongKey)),
            SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        Assert.Throws<InvalidTokenException>(() => _jwtHandler.GetPrincipalFromExpiredToken(jwt));
    }

    [Test]
    public void GetPrincipalFromExpiredToken_ShouldReturnPrincipal()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        var token = _jwtHandler.GenerateTokenOptions(creds, new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") });
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var principal = _jwtHandler.GetPrincipalFromExpiredToken(jwt);

        Assert.That(principal, Is.Not.Null);
        Assert.That(principal.Identity, Is.Not.Null);
        Assert.That(principal.Identity!.IsAuthenticated, Is.True);
        Assert.That(principal.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1"), Is.True);
    }
}