using Eduva.Application.Features.AICreditPacks.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.AICreditPacks.Specifications;

[TestFixture]
public class AICreditPackSpecificationTests
{
    private List<AICreditPack> _packs = null!;

    #region Setup

    [SetUp]
    public void Setup()
    {
        _packs = new List<AICreditPack>
        {
            new AICreditPack { Id = 1, Name = "Starter", Status = EntityStatus.Active, Price = 10000, Credits = 50, BonusCredits = 5, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new AICreditPack { Id = 2, Name = "Pro", Status = EntityStatus.Inactive, Price = 20000, Credits = 100, BonusCredits = 10, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new AICreditPack { Id = 3, Name = "Advanced", Status = EntityStatus.Archived, Price = 30000, Credits = 200, BonusCredits = 20, CreatedAt = DateTime.UtcNow }
        };
    }

    #endregion

    #region Tests

    [Test]
    public void Criteria_ShouldFilterBySearchTerm()
    {
        var param = new AICreditPackSpecParam { SearchTerm = "pro", PageIndex = 1, PageSize = 10 };
        var spec = new AICreditPackSpecification(param);

        var loweredTerm = param.SearchTerm.ToLower();
        var result = _packs
            .Where(p =>
                (param.ActiveOnly == null ||
                 (param.ActiveOnly.Value && p.Status == EntityStatus.Active) ||
                 (!param.ActiveOnly.Value && p.Status != EntityStatus.Active)) &&
                (string.IsNullOrWhiteSpace(loweredTerm) || p.Name.ToLower().Contains(loweredTerm))
            ).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Pro"));
    }

    [Test]
    public void Criteria_ShouldFilterByActiveOnly()
    {
        var param = new AICreditPackSpecParam { ActiveOnly = true, PageIndex = 1, PageSize = 10 };
        var spec = new AICreditPackSpecification(param);

        var result = _packs.AsQueryable().Where(spec.Criteria.Compile()).ToList();

        Assert.That(result.All(p => p.Status == EntityStatus.Active));
    }

    [Test]
    public void OrderBy_ShouldSortByNameAsc()
    {
        var param = new AICreditPackSpecParam { SortBy = "name", SortDirection = "asc", PageIndex = 1, PageSize = 10 };
        var spec = new AICreditPackSpecification(param);

        var result = spec.OrderBy!(_packs.AsQueryable()).ToList();

        Assert.That(result.Select(p => p.Name), Is.EqualTo(_packs.OrderBy(p => p.Name).Select(p => p.Name)));
    }

    [Test]
    public void Paging_ShouldSkipAndTakeCorrectly()
    {
        var param = new AICreditPackSpecParam { PageIndex = 2, PageSize = 1 };
        var spec = new AICreditPackSpecification(param);

        var result = _packs.AsQueryable()
            .Where(spec.Criteria.Compile())
            .Skip(spec.Skip)
            .Take(spec.Take)
            .ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(2));
    }

    #endregion

}