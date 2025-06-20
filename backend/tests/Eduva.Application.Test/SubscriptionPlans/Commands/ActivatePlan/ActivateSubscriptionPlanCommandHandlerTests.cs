using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.ActivatePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.SubscriptionPlans.Commands.ActivatePlan
{
    [TestFixture]
    public class ActivateSubscriptionPlanCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _repoMock = default!;
        private ActivateSubscriptionPlanCommandHandler _handler = default!;

        #region ActivateSubscriptionPlanCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<SubscriptionPlan, int>())
                .Returns(_repoMock.Object);

            _handler = new ActivateSubscriptionPlanCommandHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region ActivateSubscriptionPlanCommandHandler Tests

        [Test]
        public async Task Handle_ShouldActivatePlan_WhenPlanIsArchived()
        {
            // Arrange
            var plan = new SubscriptionPlan
            {
                Id = 1,
                Status = EntityStatus.Archived
            };

            _repoMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var command = new ActivateSubscriptionPlanCommand(plan.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(plan.Status, Is.EqualTo(EntityStatus.Active));
                Assert.That(result, Is.EqualTo(Unit.Value));
            });
            _repoMock.Verify(r => r.Update(plan), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowPlanNotFoundException_WhenPlanDoesNotExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SubscriptionPlan?)null);

            var command = new ActivateSubscriptionPlanCommand(123);

            // Act & Assert
            Assert.ThrowsAsync<PlanNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowPlanAlreadyActiveException_WhenPlanIsAlreadyActive()
        {
            // Arrange
            var plan = new SubscriptionPlan
            {
                Id = 2,
                Status = EntityStatus.Active
            };

            _repoMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

            var command = new ActivateSubscriptionPlanCommand(plan.Id);

            // Act & Assert
            Assert.ThrowsAsync<PlanAlreadyActiveException>(() => _handler.Handle(command, CancellationToken.None));
        }

        #endregion

    }
}