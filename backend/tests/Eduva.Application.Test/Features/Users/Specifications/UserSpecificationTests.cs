using Eduva.Application.Features.Users.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.Users.Specifications;

[TestFixture]
public class UserSpecificationTests
{

    #region UserSpecification Tests

    [Test]
    public void Should_Order_By_LastLoginAt_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "lastloginin",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var now = DateTimeOffset.UtcNow;
        var queryable = new[] {
        new ApplicationUser { LastLoginAt = now.AddHours(-1) },
        new ApplicationUser { LastLoginAt = now.AddHours(-2) }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].LastLoginAt, Is.LessThan(result[1].LastLoginAt));
    }

    [Test]
    public void Should_Order_By_LastLoginAt_Desc()
    {
        var param = new UserSpecParam
        {
            SortBy = "lastloginin",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var now = DateTimeOffset.UtcNow;
        var queryable = new[] {
        new ApplicationUser { LastLoginAt = now.AddHours(-2) },
        new ApplicationUser { LastLoginAt = now.AddHours(-1) }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].LastLoginAt, Is.GreaterThan(result[1].LastLoginAt));
    }

    [Test]
    public void Should_Order_By_CreatedAt_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "createdat",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var now = DateTimeOffset.UtcNow;
        var queryable = new[] {
        new ApplicationUser { CreatedAt = now.AddDays(-1) },
        new ApplicationUser { CreatedAt = now.AddDays(-2) }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].CreatedAt, Is.LessThan(result[1].CreatedAt));
    }

    [Test]
    public void Should_Order_By_CreatedAt_Desc()
    {
        var param = new UserSpecParam
        {
            SortBy = "createdat",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var now = DateTimeOffset.UtcNow;
        var queryable = new[] {
        new ApplicationUser { CreatedAt = now.AddDays(-2) },
        new ApplicationUser { CreatedAt = now.AddDays(-1) }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].CreatedAt, Is.GreaterThan(result[1].CreatedAt));
    }

    [Test]
    public void Should_Order_By_LastModifiedAt_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "lastmodifiedat",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var now = DateTimeOffset.UtcNow;
        var queryable = new[] {
        new ApplicationUser { LastModifiedAt = now.AddHours(-1) },
        new ApplicationUser { LastModifiedAt = now.AddHours(-2) }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].LastModifiedAt, Is.LessThan(result[1].LastModifiedAt));
    }

    [Test]
    public void Should_Order_By_LastModifiedAt_Desc()
    {
        var param = new UserSpecParam
        {
            SortBy = "lastmodifiedat",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var now = DateTimeOffset.UtcNow;
        var queryable = new[] {
        new ApplicationUser { LastModifiedAt = now.AddHours(-2) },
        new ApplicationUser { LastModifiedAt = now.AddHours(-1) }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].LastModifiedAt, Is.GreaterThan(result[1].LastModifiedAt));
    }

    [Test]
    public void Should_Set_Selector_When_Manually_Assigned()
    {
        var param = new UserSpecParam();
        var spec = new UserSpecification(param)
        {
            Selector = q => q.Select(u => new ApplicationUser { Id = u.Id }) // dummy projection
        };

        var users = new List<ApplicationUser> {
        new() { Id = Guid.NewGuid() },
        new() { Id = Guid.NewGuid() }
    }.AsQueryable();

        var result = spec.Selector!(users).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Should_Fallback_To_Default_Order_When_SortBy_Invalid_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "unknown",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[] {
        new ApplicationUser { FullName = "Z" },
        new ApplicationUser { FullName = "A" }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].FullName, Is.EqualTo("A"));
    }

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
            Assert.That(spec.Includes, Has.Count.EqualTo(1));
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

    [Test]
    public void Should_Filter_By_Status_Active()
    {
        var param = new UserSpecParam
        {
            Status = EntityStatus.Active
        };

        var spec = new UserSpecification(param);
        var users = new[]
        {
        new ApplicationUser { FullName = "Active User", Status = EntityStatus.Active },
        new ApplicationUser { FullName = "Inactive User", Status = EntityStatus.Inactive }
    }.AsQueryable();

        var compiled = spec.Criteria.Compile();
        var result = users.Where(compiled).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Status, Is.EqualTo(EntityStatus.Active));
    }

    [Test]
    public void Should_Order_By_Status_Asc()
    {
        var param = new UserSpecParam
        {
            SortBy = "Status",
            SortDirection = "asc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[]
        {
        new ApplicationUser { FullName = "User1", Status = EntityStatus.Inactive },
        new ApplicationUser { FullName = "User2", Status = EntityStatus.Active }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].Status, Is.EqualTo(EntityStatus.Active));
    }

    [Test]
    public void Should_Order_By_TotalCredits_Desc()
    {
        var param = new UserSpecParam
        {
            SortBy = "TotalCredits",
            SortDirection = "desc"
        };

        var spec = new UserSpecification(param);
        var queryable = new[]
        {
        new ApplicationUser { FullName = "User1", TotalCredits = 100 },
        new ApplicationUser { FullName = "User2", TotalCredits = 200 }
    }.AsQueryable();

        var result = spec.OrderBy!(queryable).ToList();
        Assert.That(result[0].TotalCredits, Is.EqualTo(200));
    }

    #endregion

}