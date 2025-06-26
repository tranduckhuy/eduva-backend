using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Features.Schools.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Queries;

[TestFixture]
public class GetSchoolQueryHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<School, int>> _schoolRepoMock = default!;
    private GetSchoolQueryHandler _handler = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _schoolRepoMock = new Mock<IGenericRepository<School, int>>();

        _unitOfWorkMock
            .Setup(x => x.GetRepository<School, int>())
            .Returns(_schoolRepoMock.Object);

        _handler = new GetSchoolQueryHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldReturnPaginationOfSchoolResponses()
    {
        // Arrange
        var schools = new List<School>
        {
            new School { Id = 1, Name = "School A" },
            new School { Id = 2, Name = "School B" }
        };

        var pagedResult = new Pagination<School>
        {
            PageIndex = 1,
            PageSize = 10,
            Count = 2,
            Data = schools
        };

        _schoolRepoMock
            .Setup(r => r.GetWithSpecAsync(It.IsAny<SchoolSpecification>()))
            .ReturnsAsync(pagedResult);

        var query = new GetSchoolQuery(new SchoolSpecParam
        {
            PageIndex = 1,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = "asc"
        });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Data.First().Name, Is.EqualTo("School A"));
            Assert.That(result.Count, Is.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.PageIndex, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(10));
        });
    }

    #endregion

}