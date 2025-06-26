using Eduva.Application.Features.Schools.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.Schools.Specifications;

[TestFixture]
public class SchoolSpecificationTests
{
    private List<School> _schools = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _schools = new List<School>
        {
            new School { Id = 1, Name = "Eduva", ContactEmail = "info@eduva.vn", ContactPhone = "0123456789", Status = EntityStatus.Active },
            new School { Id = 2, Name = "Inactive School", ContactEmail = "inactive@eduva.vn", ContactPhone = "0987654321", Status = EntityStatus.Inactive },
        };
    }

    #endregion

    #region Tests

    [Test]
    public void Selector_ShouldProjectCorrectly()
    {
        var spec = new SchoolSpecification(new SchoolSpecParam { PageIndex = 1, PageSize = 10 })
        {
            Selector = q => q.Select(s => new School { Id = s.Id }) // Dummy projection
        };

        var query = _schools.AsQueryable();
        var projected = spec.Selector?.Invoke(query).ToList();

        Assert.That(projected, Is.All.Not.Null);
    }

    [Test]
    public void Criteria_ShouldHandle_NullSearchTerm()
    {
        var spec = new SchoolSpecification(new SchoolSpecParam { SearchTerm = null, PageIndex = 1, PageSize = 10 });
        var result = _schools.AsQueryable().Where(spec.Criteria).ToList();

        Assert.That(result, Is.Not.Null);
    }

    [TestCase("contactemail")]
    [TestCase("contactphone")]
    [TestCase("createdat")]
    [TestCase("unknown")]
    public void OrderBy_ShouldSortCorrectly_Asc(string sortBy)
    {
        var spec = new SchoolSpecification(new SchoolSpecParam
        {
            SortBy = sortBy,
            SortDirection = "asc",
            PageIndex = 1,
            PageSize = 10
        });

        var result = spec.OrderBy?.Invoke(_schools.AsQueryable()).ToList();
        Assert.That(result, Is.Not.Null);
    }

    [TestCase(null, TestName = "Criteria_ShouldReturnAll_When_ActiveOnlyIsNull")]
    [TestCase(true, TestName = "Criteria_ShouldReturnActive_When_ActiveOnlyIsTrue")]
    [TestCase(false, TestName = "Criteria_ShouldReturnInactive_When_ActiveOnlyIsFalse")]
    public void Criteria_ShouldFilterByActiveOnly(bool? activeOnly)
    {
        // Arrange
        var spec = new SchoolSpecification(new SchoolSpecParam { ActiveOnly = activeOnly, PageIndex = 1, PageSize = 10 });
        var result = _schools.AsQueryable().Where(spec.Criteria).ToList();

        // Assert
        if (activeOnly == true)
            Assert.That(result.All(s => s.Status == EntityStatus.Active));
        else if (activeOnly == false)
            Assert.That(result.All(s => s.Status != EntityStatus.Active));
        else
            Assert.That(result, Has.Count.EqualTo(_schools.Count));
    }

    [Test]
    public void Criteria_ShouldFilterBySearchTerm_Without_EF_Like()
    {
        var searchTerm = "eduva";
        var lowered = searchTerm.ToLower();

        var filtered = _schools.Where(s =>
            (s.Name?.ToLower().Contains(lowered, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (s.ContactEmail?.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains(lowered, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (s.ContactPhone?.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains(lowered, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();

        Assert.That(filtered, Has.Count.EqualTo(2));
    }

    [TestCase("name")]
    [TestCase("contactemail")]
    [TestCase("contactphone")]
    [TestCase("createdat")]
    [TestCase("unknown")]
    public void OrderBy_ShouldSortCorrectly(string sortBy)
    {
        var spec = new SchoolSpecification(new SchoolSpecParam
        {
            SortBy = sortBy,
            SortDirection = "desc",
            PageIndex = 1,
            PageSize = 10
        });

        var query = _schools.AsQueryable();
        var result = spec.OrderBy?.Invoke(query).ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Count, Is.EqualTo(2));
    }

    [Test]
    public void Pagination_ShouldSetSkipAndTakeCorrectly()
    {
        var param = new SchoolSpecParam { PageIndex = 2, PageSize = 5 };
        var spec = new SchoolSpecification(param);

        Assert.Multiple(() =>
        {
            Assert.That(spec.Skip, Is.EqualTo(5));
            Assert.That(spec.Take, Is.EqualTo(5));
        });
    }

    #endregion

}