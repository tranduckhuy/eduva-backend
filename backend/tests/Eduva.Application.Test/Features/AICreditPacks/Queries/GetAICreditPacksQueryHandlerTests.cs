using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Queries;
using Eduva.Application.Features.AICreditPacks.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.AICreditPacks.Queries;

[TestFixture]
public class GetAICreditPacksQueryHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<AICreditPack, int>> _repoMock = default!;
    private GetAICreditPacksQueryHandler _handler = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _repoMock = new Mock<IGenericRepository<AICreditPack, int>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<AICreditPack, int>())
            .Returns(_repoMock.Object);

        _handler = new GetAICreditPacksQueryHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldReturnMappedPaginationResult()
    {
        // Arrange
        var param = new AICreditPackSpecParam { PageIndex = 1, PageSize = 10 };
        var query = new GetAICreditPacksQuery(param);

        var packs = new List<AICreditPack>
        {
            new AICreditPack
            {
                Id = 1,
                Name = "Pro",
                Price = 50000,
                Credits = 300,
                BonusCredits = 50,
                Status = EntityStatus.Active
            }
        };

        var paginated = new Pagination<AICreditPack>
        {
            Data = packs,
            PageIndex = 1,
            PageSize = 10,
            Count = 1
        };

        _repoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<AICreditPackSpecification>()))
                 .ReturnsAsync(paginated);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Has.Count.EqualTo(1));

        var item = result.Data.First();

        Assert.Multiple(() =>
        {
            Assert.That(item.Name, Is.EqualTo("Pro"));
            Assert.That(item.Credits, Is.EqualTo(300));
            Assert.That(item.BonusCredits, Is.EqualTo(50));
            Assert.That(item.Status, Is.EqualTo(EntityStatus.Active));
        });

        _repoMock.Verify(r => r.GetWithSpecAsync(It.IsAny<AICreditPackSpecification>()), Times.Once);
    }

    #endregion

}