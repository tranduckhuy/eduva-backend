using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Queries
{
    [TestFixture]
    public class GetMySchoolSubscriptionQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<ISchoolSubscriptionRepository> _subRepoMock = null!;
        private GetMySchoolSubscriptionQueryHandler _handler = null!;

        #region GetMySchoolSubscriptionQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepoMock = new Mock<IUserRepository>();
            _subRepoMock = new Mock<ISchoolSubscriptionRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>())
                .Returns(_subRepoMock.Object);

            _handler = new GetMySchoolSubscriptionQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetMySchoolSubscriptionQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnResponse_WhenValidUserAndSubscriptionExist()
        {
            var userId = Guid.NewGuid();
            var schoolId = 123;

            var user = new ApplicationUser { Id = userId, SchoolId = schoolId };
            var plan = new SubscriptionPlan
            {
                Name = "Plus",
                Description = "Advanced plan",
                MaxUsers = 50,
                StorageLimitGB = 100,
                MaxMinutesPerMonth = 2000
            };
            var subscription = new SchoolSubscription
            {
                Plan = plan,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                EndDate = DateTimeOffset.UtcNow.AddDays(20),
                BillingCycle = BillingCycle.Monthly,
                SubscriptionStatus = SubscriptionStatus.Active,
                AmountPaid = 500000
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _subRepoMock.Setup(r => r.GetLatestPaidBySchoolIdAsync(schoolId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var result = await _handler.Handle(new GetMySchoolSubscriptionQuery(userId), default);

            Assert.Multiple(() =>
            {
                Assert.That(result.PlanName, Is.EqualTo(plan.Name));
                Assert.That(result.Description, Is.EqualTo(plan.Description));
                Assert.That(result.MaxUsers, Is.EqualTo(plan.MaxUsers));
                Assert.That(result.SubscriptionStatus, Is.EqualTo(subscription.SubscriptionStatus));
                Assert.That(result.BillingCycle, Is.EqualTo(subscription.BillingCycle));
                Assert.That(result.AmountPaid, Is.EqualTo(subscription.AmountPaid));
            });
        }

        [Test]
        public void Handle_ShouldThrowUserNotExistsException_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            Assert.ThrowsAsync<UserNotExistsException>(() =>
                _handler.Handle(new GetMySchoolSubscriptionQuery(userId), default));
        }

        [Test]
        public void Handle_ShouldThrowSchoolNotFoundException_WhenSchoolIdIsNull()
        {
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = null };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            Assert.ThrowsAsync<SchoolNotFoundException>(() =>
                _handler.Handle(new GetMySchoolSubscriptionQuery(userId), default));
        }

        [Test]
        public void Handle_ShouldThrowSchoolSubscriptionNotFoundException_WhenNoSubscription()
        {
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 123 };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _subRepoMock.Setup(r => r.GetLatestPaidBySchoolIdAsync(user.SchoolId.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SchoolSubscription?)null);

            Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() =>
                _handler.Handle(new GetMySchoolSubscriptionQuery(userId), default));
        }

        #endregion

    }
}