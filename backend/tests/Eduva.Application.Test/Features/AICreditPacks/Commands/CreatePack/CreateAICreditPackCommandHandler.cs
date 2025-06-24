using Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.AICreditPacks.Commands.CreatePack;

[TestFixture]
public class CreateAICreditPackCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<AICreditPack, int>> _creditPackRepoMock = default!;
    private CreateAICreditPackCommandHandler _handler = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _creditPackRepoMock = new Mock<IGenericRepository<AICreditPack, int>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<AICreditPack, int>())
            .Returns(_creditPackRepoMock.Object);

        _handler = new CreateAICreditPackCommandHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldCreateCreditPackAndCommit()
    {
        // Arrange
        var command = new CreateAICreditPackCommand
        {
            Name = "Starter Pack",
            Price = 20000,
            Credits = 100,
            BonusCredits = 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _creditPackRepoMock.Verify(r => r.AddAsync(It.Is<AICreditPack>(p =>
            p.Name == "Starter Pack" &&
            p.Price == 20000 &&
            p.Credits == 100 &&
            p.BonusCredits == 10 &&
            p.Status == EntityStatus.Active &&
            p.CreatedAt <= DateTime.UtcNow
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        Assert.That(result, Is.EqualTo(Unit.Value));
    }

    #endregion

}