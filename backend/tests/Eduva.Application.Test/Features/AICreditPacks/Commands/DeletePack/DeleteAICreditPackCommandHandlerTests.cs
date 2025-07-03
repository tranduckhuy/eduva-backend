using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Features.AICreditPacks.Commands.DeleteCreditPacks;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.AICreditPacks.Commands.DeletePack;

[TestFixture]
public class DeleteAICreditPackCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<AICreditPack, int>> _creditPackRepoMock = default!;
    private DeleteAICreditPackCommandHandler _handler = default!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _creditPackRepoMock = new Mock<IGenericRepository<AICreditPack, int>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<AICreditPack, int>())
            .Returns(_creditPackRepoMock.Object);

        _handler = new DeleteAICreditPackCommandHandler(_unitOfWorkMock.Object);
    }

    [Test]
    public async Task Handle_ShouldDelete_WhenPackIsArchived()
    {
        // Arrange
        var pack = new AICreditPack
        {
            Id = 1,
            Status = EntityStatus.Archived
        };

        _creditPackRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(pack);

        var command = new DeleteAICreditPackCommand(1);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        _creditPackRepoMock.Verify(r => r.Remove(pack), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        Assert.That(result, Is.EqualTo(Unit.Value));
    }

    [Test]
    public void Handle_ShouldThrowNotFound_WhenPackDoesNotExist()
    {
        // Arrange
        _creditPackRepoMock.Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync((AICreditPack?)null);

        var command = new DeleteAICreditPackCommand(99);

        // Act & Assert
        Assert.ThrowsAsync<AICreditPackNotFoundException>(() =>
            _handler.Handle(command, default));
    }

    [Test]
    public void Handle_ShouldThrow_WhenPackIsNotArchived()
    {
        // Arrange
        var pack = new AICreditPack
        {
            Id = 2,
            Status = EntityStatus.Active
        };

        _creditPackRepoMock.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(pack);

        var command = new DeleteAICreditPackCommand(2);

        // Act & Assert
        Assert.ThrowsAsync<AICreditPackMustBeArchivedException>(() =>
            _handler.Handle(command, default));
    }
}