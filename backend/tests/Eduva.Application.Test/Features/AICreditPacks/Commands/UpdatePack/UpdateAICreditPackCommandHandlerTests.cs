using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.AICreditPacks.Commands;

[TestFixture]
public class UpdateAICreditPackCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<AICreditPack, int>> _creditPackRepoMock = default!;
    private UpdateAICreditPackCommandHandler _handler = default!;

    #region Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _creditPackRepoMock = new Mock<IGenericRepository<AICreditPack, int>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<AICreditPack, int>())
            .Returns(_creditPackRepoMock.Object);

        _handler = new UpdateAICreditPackCommandHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldUpdateCreditPack_AndCommit()
    {
        // Arrange
        var command = new UpdateAICreditPackCommand
        {
            Id = 1,
            Name = "Updated Pack",
            Price = 30000,
            Credits = 150,
            BonusCredits = 20
        };

        var existingPack = new AICreditPack
        {
            Id = 1,
            Name = "Old Pack",
            Price = 10000,
            Credits = 100,
            BonusCredits = 5
        };

        _creditPackRepoMock
            .Setup(r => r.GetByIdAsync(command.Id))
            .ReturnsAsync(existingPack);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            Assert.That(existingPack.Name, Is.EqualTo(command.Name));
            Assert.That(existingPack.Price, Is.EqualTo(command.Price));
            Assert.That(existingPack.Credits, Is.EqualTo(command.Credits));
            Assert.That(existingPack.BonusCredits, Is.EqualTo(command.BonusCredits));
            Assert.That(existingPack.LastModifiedAt, Is.Not.Null);
        });

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public void Handle_ShouldThrow_WhenPackNotFound()
    {
        // Arrange
        var command = new UpdateAICreditPackCommand { Id = 99 };

        _creditPackRepoMock
            .Setup(r => r.GetByIdAsync(command.Id))
            .ReturnsAsync((AICreditPack?)null);

        // Act & Assert
        Assert.ThrowsAsync<AICreditPackNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    #endregion

}