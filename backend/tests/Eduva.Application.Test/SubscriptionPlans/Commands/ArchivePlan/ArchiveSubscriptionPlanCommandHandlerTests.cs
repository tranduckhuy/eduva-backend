using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.ArchivePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.SubscriptionPlans.Commands.ArchivePlan
{
    [TestFixture]
    public class ArchiveSubscriptionPlanCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _planRepoMock = default!;
        private Mock<IGenericRepository<SchoolSubscription, int>> _schoolSubRepoMock = default!;
        private ArchiveSubscriptionPlanCommandHandler _handler = default!;

        #region ArchiveSubscriptionPlanCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _planRepoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();
            _schoolSubRepoMock = new Mock<IGenericRepository<SchoolSubscription, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(u => u.GetRepository<SubscriptionPlan, int>())
                .Returns(_planRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<SchoolSubscription, int>())
                .Returns(_schoolSubRepoMock.Object);

            _handler = new ArchiveSubscriptionPlanCommandHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region ArchiveSubscriptionPlanCommandHandler Tests

        [Test]
        public async Task Handle_ShouldArchivePlan_WhenValid()
        {
            // Arrange
            var command = new ArchiveSubscriptionPlanCommand(1);
            var plan = new SubscriptionPlan
            {
                Id = 1,
                Status = EntityStatus.Active
            };

            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id))
                .ReturnsAsync(plan);
            _schoolSubRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SchoolSubscription, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.EqualTo(Unit.Value));
                Assert.That(plan.Status, Is.EqualTo(EntityStatus.Archived));
            });
        }

        [Test]
        public void Handle_ShouldThrow_PlanNotFound()
        {
            var command = new ArchiveSubscriptionPlanCommand(99);
            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id))
                .ReturnsAsync((SubscriptionPlan?)null);

            Assert.ThrowsAsync<PlanNotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrow_AlreadyArchived()
        {
            var command = new ArchiveSubscriptionPlanCommand(1);
            var plan = new SubscriptionPlan { Id = 1, Status = EntityStatus.Archived };

            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync(plan);

            Assert.ThrowsAsync<PlanAlreadyArchivedException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrow_InUse()
        {
            var command = new ArchiveSubscriptionPlanCommand(1);
            var plan = new SubscriptionPlan { Id = 1, Status = EntityStatus.Active };

            _planRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync(plan);
            _schoolSubRepoMock.Setup(r =>
                r.ExistsAsync(It.IsAny<Expression<Func<SchoolSubscription, bool>>>())).ReturnsAsync(true);

            Assert.ThrowsAsync<PlanInUseException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        #endregion

    }
}