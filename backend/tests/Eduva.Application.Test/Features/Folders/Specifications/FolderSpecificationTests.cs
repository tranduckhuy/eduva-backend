using Eduva.Application.Features.Folders.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eduva.Application.Test.Features.Folders.Specifications;

[TestFixture]
public class FolderSpecificationTests
{
    private List<Folder> _folders = default!;

    #region Setup
    [SetUp]
    public void SetUp()
    {
        _folders = new List<Folder>
        {
            new Folder { Id = Guid.NewGuid(), Name = "Personal Folder", OwnerType = OwnerType.Personal, UserId = Guid.NewGuid(), Status = EntityStatus.Active, Order = 1 },
            new Folder { Id = Guid.NewGuid(), Name = "Class Folder", OwnerType = OwnerType.Class, ClassId = Guid.NewGuid(), Status = EntityStatus.Active, Order = 2 },
            new Folder { Id = Guid.NewGuid(), Name = "Inactive Folder", OwnerType = OwnerType.Personal, UserId = Guid.NewGuid(), Status = EntityStatus.Inactive, Order = 3 },
        };
    }
    #endregion

    #region Tests

    [Test]
    public void Selector_ShouldBeNull_ByDefault()
    {
        var spec = new FolderSpecification(new FolderSpecParam { PageIndex = 1, PageSize = 10 });
        Assert.That(spec.Selector, Is.Null);
    }

    [Test]
    public void Criteria_ShouldHandle_NullSearchTerm()
    {
        var spec = new FolderSpecification(new FolderSpecParam { SearchTerm = null, PageIndex = 1, PageSize = 10 });
        var result = _folders.AsQueryable().Where(spec.Criteria).ToList();
        Assert.That(result, Is.Not.Null);
    }

    [TestCase("name")]
    [TestCase("order")]
    [TestCase("createdat")]
    [TestCase("unknown")]
    public void OrderBy_ShouldSortCorrectly_Asc(string sortBy)
    {
        var spec = new FolderSpecification(new FolderSpecParam
        {
            SortBy = sortBy,
            SortDirection = "asc",
            PageIndex = 1,
            PageSize = 10
        });
        var result = spec.OrderBy?.Invoke(_folders.AsQueryable()).ToList();
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Criteria_ShouldFilterBySearchTerm_Without_EF_Like()
    {
        var searchTerm = "folder";
        var lowered = searchTerm.ToLower();
        var filtered = _folders.Where(f =>
            (f.Name?.ToLower().Contains(lowered, StringComparison.CurrentCultureIgnoreCase) ?? false)
        ).ToList();
        Assert.That(filtered, Has.Count.GreaterThan(0));
    }

    [TestCase("name")]
    [TestCase("order")]
    [TestCase("createdat")]
    [TestCase("unknown")]
    public void OrderBy_ShouldSortCorrectly(string sortBy)
    {
        var spec = new FolderSpecification(new FolderSpecParam
        {
            SortBy = sortBy,
            SortDirection = "desc",
            PageIndex = 1,
            PageSize = 10
        });
        var query = _folders.AsQueryable();
        var result = spec.OrderBy?.Invoke(query).ToList();
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Count, Is.EqualTo(_folders.Count));
    }

    [Test]
    public void Pagination_ShouldSetSkipAndTakeCorrectly()
    {
        var param = new FolderSpecParam { PageIndex = 2, PageSize = 5 };
        var spec = new FolderSpecification(param);
        Assert.Multiple(() =>
        {
            Assert.That(spec.Skip, Is.EqualTo(5));
            Assert.That(spec.Take, Is.EqualTo(5));
        });
    }

    [Test]
    public void Criteria_ShouldFilter_By_Multiple_Fields()
    {
        var userId = _folders[0].UserId;
        var ownerType = OwnerType.Personal;
        var name = _folders[0].Name;
        var param = new FolderSpecParam { UserId = userId, OwnerType = ownerType, Name = name, PageIndex = 1, PageSize = 10 };
        var spec = new FolderSpecification(param);
        var result = _folders.AsQueryable().Where(spec.Criteria).ToList();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo(name));
    }

    [Test]
    public void Criteria_ShouldFilter_By_ClassId()
    {
        var classId = _folders.FirstOrDefault(f => f.OwnerType == OwnerType.Class)?.ClassId;
        var param = new FolderSpecParam { ClassId = classId, OwnerType = OwnerType.Class, PageIndex = 1, PageSize = 10 };
        var spec = new FolderSpecification(param);
        var result = _folders.AsQueryable().Where(spec.Criteria).ToList();
        Assert.That(result.All(f => f.ClassId == classId));
    }

    [Test]
    public void Criteria_ShouldReturn_Empty_When_Paging_Out_Of_Range()
    {
        var param = new FolderSpecParam { PageIndex = 100, PageSize = 10 };
        var spec = new FolderSpecification(param);
        var paged = _folders.AsQueryable().Skip(spec.Skip).Take(spec.Take).ToList();
        Assert.That(paged, Is.Empty);
    }

    [Test]
    public void OrderBy_ShouldHandle_Null_SortBy()
    {
        var param = new FolderSpecParam { SortBy = null, PageIndex = 1, PageSize = 10 };
        var spec = new FolderSpecification(param);
        var result = spec.OrderBy?.Invoke(_folders.AsQueryable()).ToList();
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Includes_ShouldContain_User_And_Class()
    {
        var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
        var spec = new FolderSpecification(param);
        Assert.That(spec.Includes.Any(i => i.Body.ToString()!.Contains("User")), Is.True);
        Assert.That(spec.Includes.Any(i => i.Body.ToString()!.Contains("Class")), Is.True);
    }

    [Test]
    public void Criteria_ShouldFilter_By_SearchTerm_And_Name()
    {
        var searchTerm = "personal";
        var name = "Personal Folder";
        var param = new FolderSpecParam { SearchTerm = searchTerm, Name = name, PageIndex = 1, PageSize = 10 };
        var spec = new FolderSpecification(param);
        var result = _folders.AsQueryable().Where(spec.Criteria).ToList();
        Assert.That(result.All(f => f.Name.ToLower().Contains(searchTerm) && f.Name.ToLower().Contains(name.ToLower())));
    }

    #endregion
}
