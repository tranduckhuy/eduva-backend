using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Features.AICreditPacks.Commands.ArchiveCreditPacks;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.AICreditPacks.Commands.ArchivePack;

[TestFixture]
public class ArchiveAICreditPackCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<AICreditPack, int>> _creditPackRepoMock = default!;
    private ArchiveAICreditPackCommandHandler _handler = default!;

    #region Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _creditPackRepoMock = new Mock<IGenericRepository<AICreditPack, int>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<AICreditPack, int>())
            .Returns(_creditPackRepoMock.Object);

        _handler = new ArchiveAICreditPackCommandHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldArchiveCreditPack_WhenStatusIsActive()
    {
        // Arrange
        var pack = new AICreditPack
        {
            Id = 1,
            Status = EntityStatus.Active
        };

        _creditPackRepoMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(pack);

        var command = new ArchiveAICreditPackCommand(1);

        // Act
        var result = await _handler.Handle(command, default);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(pack.Status, Is.EqualTo(EntityStatus.Archived));
            Assert.That(pack.LastModifiedAt, Is.Not.Null);
        });
        _creditPackRepoMock.Verify(r => r.Update(pack), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        Assert.That(result, Is.EqualTo(Unit.Value));
    }

    [Test]
    public void Handle_ShouldThrow_WhenCreditPackNotFound()
    {
        // Arrange
        _creditPackRepoMock
            .Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync((AICreditPack?)null);

        var command = new ArchiveAICreditPackCommand(99);

        // Act & Assert
        Assert.ThrowsAsync<AICreditPackNotFoundException>(() =>
            _handler.Handle(command, default));
    }

    [Test]
    public void Handle_ShouldThrow_WhenAlreadyArchived()
    {
        // Arrange
        var pack = new AICreditPack
        {
            Id = 2,
            Status = EntityStatus.Archived
        };

        _creditPackRepoMock
            .Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(pack);

        var command = new ArchiveAICreditPackCommand(2);

        // Act & Assert
        Assert.ThrowsAsync<AICreditPackAlreadyArchivedException>(() =>
            _handler.Handle(command, default));
    }

    #endregion

}