using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.DeletePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.SubscriptionPlans.Commands.DeletePlan
{
    [TestFixture]
    public class DeleteSubscriptionPlanCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _planRepoMock = default!;
        private DeleteSubscriptionPlanCommandHandler _handler = default!;

        #region DeleteSubscriptionPlanCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _planRepoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<SubscriptionPlan, int>())
                .Returns(_planRepoMock.Object);

            _handler = new DeleteSubscriptionPlanCommandHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region DeleteSubscriptionPlanCommandHandler Tests

        [Test]
        public async Task Handle_ShouldDeletePlan_WhenPlanIsArchived()
        {
            // Arrange
            var command = new DeleteSubscriptionPlanCommand(1);
            var plan = new SubscriptionPlan
            {
                Id = 1,
                Status = EntityStatus.Archived
            };

            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync(plan);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _planRepoMock.Verify(r => r.Remove(plan), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrow_PlanNotFoundException_WhenPlanDoesNotExist()
        {
            // Arrange
            var command = new DeleteSubscriptionPlanCommand(123);
            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync((SubscriptionPlan?)null);

            // Act & Assert
            Assert.ThrowsAsync<PlanNotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrow_SubscriptionPlanMustBeArchivedException_WhenPlanIsNotArchived()
        {
            // Arrange
            var command = new DeleteSubscriptionPlanCommand(1);
            var plan = new SubscriptionPlan
            {
                Id = 1,
                Status = EntityStatus.Active // Not archived
            };

            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync(plan);

            // Act & Assert
            Assert.ThrowsAsync<SubscriptionPlanMustBeArchivedException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        #endregion

    }
}