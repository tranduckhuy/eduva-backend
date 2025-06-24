using Eduva.Application.Features.Users.Specifications;
using Eduva.Domain.Entities;

namespace Eduva.Application.Test.Features.Users.Specifications;

[TestFixture]
public class UserSpecificationTests
{

    #region UserSpecification Tests

    [Test]
    public void Should_Filter_By_SchoolId_And_SearchTerm()
    {
        var param = new UserSpecParam
        {
            SchoolId = 1,
            SearchTerm = "john",
            PageIndex = 2,
            PageSize = 5
        };

        var spec = new UserSpecification(param);

        Assert.Multiple(() =>
        {
            Assert.That(spec.Criteria, Is.Not.Null);
            Assert.That(spec.Skip, Is.EqualTo(5));
            Assert.That(spec.Take, Is.EqualTo(5));
            Assert.That(spec.Includes.Count, Is.EqualTo(1));
        });
        Assert.That(spec.Includes[0].Body.ToString(), Does.Contain("u.School"));
    }

    [Test]
    public void Should_Filter_Without_SchoolId_And_Empty_SearchTerm()
    {
        var param = new UserSpecParam
        {
            SchoolId = null,
            SearchTerm = "",
            PageIndex = 1,
            PageSize = 10
        };

        var spec = new UserSpecification(param);
        var user = new ApplicationUser { FullName = "Any", Email = "test@example.com", SchoolId = 5 };

        var compiled = spec.Criteria.Compile();
        var result = compiled(user); // Always true when no filters

        Assert.That(result, Is.True);
    }

    [Test]
    public void Should_Order_By_FullName_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "FullName",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[] {
            new ApplicationUser { FullName = "B" },
            new ApplicationUser { FullName = "A" }
        }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].FullName, Is.EqualTo("A"));
    }

    [Test]
    public void Should_Order_By_FullName_Desc()
    {
        var param = new UserSpecParam
        {
            SortBy = "FullName",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[] {
            new ApplicationUser { FullName = "A" },
            new ApplicationUser { FullName = "B" }
        }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].FullName, Is.EqualTo("B"));
    }

    [Test]
    public void Should_Order_By_Email_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "Email",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[] {
            new ApplicationUser { Email = "z@example.com" },
            new ApplicationUser { Email = "a@example.com" }
        }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].Email, Is.EqualTo("a@example.com"));
    }

    [Test]
    public void Should_Order_By_Email_Desc()
    {
        var param = new UserSpecParam
        {
            SortBy = "email",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[] {
            new ApplicationUser { Email = "a@example.com" },
            new ApplicationUser { Email = "z@example.com" }
        }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].Email, Is.EqualTo("z@example.com"));
    }

    [Test]
    public void Should_Fallback_To_Default_Order_When_SortBy_Invalid()
    {
        var param = new UserSpecParam
        {
            SortBy = "invalid",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[] {
            new ApplicationUser { FullName = "A" },
            new ApplicationUser { FullName = "Z" }
        }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].FullName, Is.EqualTo("Z"));
    }

    [Test]
    public void Should_Have_Null_OrderBy_When_SortBy_Is_Null()
    {
        var param = new UserSpecParam
        {
            SortBy = null,
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);

        Assert.That(spec.OrderBy, Is.Null);
    }

    #endregion

}