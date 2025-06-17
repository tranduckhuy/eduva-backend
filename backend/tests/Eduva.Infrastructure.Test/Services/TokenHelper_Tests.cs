using Eduva.Domain.Entities;
using Eduva.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eduva.Infrastructure.Test.Services;

[TestFixture]
public class TokenHelper_Tests
{
    [Test]
    public void GetClaims_ShouldReturnStandardAndRoleClaims()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com"
        };

        var roles = new List<string> { "Admin", "User" };
        var userClaims = new List<Claim> { new(ClaimTypes.Country, "VN") };

        var claims = TokenHelper.GetClaims(user, roles, userClaims);

        Assert.Multiple(() =>
        {
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Name && c.Value == "testuser"), Is.True);
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com"), Is.True);
            Assert.That(claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString()), Is.True);
            Assert.That(claims.Count(c => c.Type == ClaimTypes.Role), Is.EqualTo(2));
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Country && c.Value == "VN"), Is.True);
        });
    }

    [Test]
    public void GenerateRefreshToken_ShouldReturnBase64StringOfExpectedLength()
    {
        var token = TokenHelper.GenerateRefreshToken();
        var tokenBytes = Convert.FromBase64String(token);

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.Not.Null.And.Not.Empty);
            Assert.That(tokenBytes.Length, Is.EqualTo(64));
        });
    }

    [Test]
    public void GenerateRefreshToken_ShouldReturnDifferentValueEachTime()
    {
        var token1 = TokenHelper.GenerateRefreshToken();
        var token2 = TokenHelper.GenerateRefreshToken();

        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    [Test]
    public void GetTokenExpiry_ShouldReturnValidExpiry()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("test-key-should-be-at-least-32-characters");
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = creds
        });

        var tokenString = tokenHandler.WriteToken(token);

        var expiry = TokenHelper.GetTokenExpiry(tokenString);

        Assert.That(expiry, Is.EqualTo(token.ValidTo).Within(TimeSpan.FromSeconds(1)));
    }
}