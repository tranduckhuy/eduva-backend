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
public class JwtHandlerTests
{
    private JwtHandler _jwtHandler;
    private Mock<IConfiguration> _configurationMock;
    private Mock<ILogger<JwtHandler>> _loggerMock;
    private IConfigurationSection _jwtSection;
    private const string ValidLongKey = "supersecretkey1234567890supersecretkey1234567890supersecretkey123456";

    #region JwtHandler Setup

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

    #endregion

    #region JwtHandler Tests

    // Verifies that the JwtHandler is initialized correctly with the configuration.
    [Test]
    public void GetSigningCredentials_ShouldReturnValidCredentials()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        Assert.That(creds, Is.Not.Null);
        Assert.That(creds.Algorithm, Is.EqualTo(SecurityAlgorithms.HmacSha256));
    }

    // Verifies that the JwtHandler throws an exception when the SecretKey is missing.
    [Test]
    public void GetSigningCredentials_ShouldThrow_WhenSecretKeyMissing()
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s["SecretKey"]).Returns((string?)null);
        mockSection.Setup(s => s["ValidIssuer"]).Returns("issuer");
        mockSection.Setup(s => s["ValidAudience"]).Returns("audience");
        mockSection.Setup(s => s["ExpiryInSecond"]).Returns("3600");

        _configurationMock.Setup(c => c.GetSection("JwtSettings")).Returns(mockSection.Object);
        var handler = new JwtHandler(_configurationMock.Object, _loggerMock.Object);

        Assert.Throws<InvalidDataException>(() => handler.GetSigningCredentials());
    }

    // Verifies that the token expiry duration is correctly set to 3600 seconds.
    [Test]
    public void GetExpiryInSecond_ShouldReturn3600()
    {
        var expiry = _jwtHandler.GetExpiryInSecond();
        Assert.That(expiry, Is.EqualTo(3600));
    }

    // Verifies that the GenerateTokenOptions method creates a valid JWT token with the expected claims.
    [Test]
    public void GenerateTokenOptions_ShouldIncludeClaims()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "123") };

        var token = _jwtHandler.GenerateTokenOptions(creds, claims);

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.Not.Null);
            Assert.That(token.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Iat), Is.True);
            Assert.That(token.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123"), Is.True);
        });
    }

    // Verifies that the GenerateTokenOptions method sets the correct issuer and audience.
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

    // Verifies that the GetPrincipalFromExpiredToken method throws an exception when the token is not a valid JWT.
    [Test]
    public void GetPrincipalFromExpiredToken_ShouldThrow_WhenTokenIsNotJwtSecurityToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidLongKey)), SecurityAlgorithms.HmacSha256);

        // create token manually and tamper type
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds
        );

        var tokenString = handler.WriteToken(token);

        // simulate tamper by modifying payload so it becomes uncastable
        var parts = tokenString.Split('.');
        parts[1] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"typ\":\"non-jwt\"}"));
        var tampered = string.Join('.', parts);

        Assert.Throws<InvalidTokenException>(() => _jwtHandler.GetPrincipalFromExpiredToken(tampered));
    }

    // Verifies that the GetPrincipalFromExpiredToken method returns a ClaimsPrincipal with the expected claims.
    [Test]
    public void GetPrincipalFromExpiredToken_ShouldReturnPrincipal()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        var token = _jwtHandler.GenerateTokenOptions(creds, new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") });
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var principal = _jwtHandler.GetPrincipalFromExpiredToken(jwt);

        Assert.Multiple(() =>
        {
            Assert.That(principal, Is.Not.Null);
            Assert.That(principal.Identity, Is.Not.Null);
            Assert.That(principal.Identity!.IsAuthenticated, Is.True);
            Assert.That(principal.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1"), Is.True);
        });
    }

    // Verifies that the GetPrincipalFromExpiredToken method throws an exception when the algorithm is null.
    [Test]
    public void GetPrincipalFromExpiredToken_ShouldThrow_WhenAlgorithmIsNull()
    {
        var creds = _jwtHandler.GetSigningCredentials();
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds
        );

        // generate token and decode to modify header manually
        var handler = new JwtSecurityTokenHandler();
        var tokenStr = handler.WriteToken(token);
        var parts = tokenStr.Split('.');

        // decode header, remove alg
        var jsonHeader = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[0])));
        var headerObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonHeader)!;
        headerObj.Remove("alg");

        var newHeaderJson = System.Text.Json.JsonSerializer.Serialize(headerObj);
        parts[0] = Convert.ToBase64String(Encoding.UTF8.GetBytes(newHeaderJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var tampered = string.Join('.', parts);

        Assert.Throws<InvalidTokenException>(() => _jwtHandler.GetPrincipalFromExpiredToken(tampered));
    }

    #endregion

    #region Helper Methods

    // Helper to pad base64 for decoding
    private static string PadBase64(string input)
    {
        switch (input.Length % 4)
        {
            case 2: return input + "==";
            case 3: return input + "=";
            default: return input;
        }
    }

    #endregion

}