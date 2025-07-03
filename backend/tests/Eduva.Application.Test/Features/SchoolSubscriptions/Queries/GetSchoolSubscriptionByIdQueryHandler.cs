using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Queries
{
    [TestFixture]
    public class GetSchoolSubscriptionByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<ISchoolSubscriptionRepository> _subscriptionRepoMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
        private GetSchoolSubscriptionByIdQueryHandler _handler = default!;

        #region GetSchoolSubscriptionByIdQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _subscriptionRepoMock = new Mock<ISchoolSubscriptionRepository>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            _unitOfWorkMock
                .Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>())
                .Returns(_subscriptionRepoMock.Object);

            _unitOfWorkMock
                .Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _handler = new GetSchoolSubscriptionByIdQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetSchoolSubscriptionByIdQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedResponse_WhenSystemAdminAndSubscriptionExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var subscription = new SchoolSubscription
            {
                Id = id,
                SchoolId = 1,
                PaymentTransaction = new PaymentTransaction
                {
                    UserId = Guid.NewGuid() // Different from requester 
                }
            };

            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var query = new GetSchoolSubscriptionByIdQuery(id, userId, isSystemAdmin: true);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(id));
        }

        [Test]
        public async Task Handle_ShouldReturnMappedResponse_WhenNonAdminFromSameSchool()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var schoolId = 1;

            var user = new ApplicationUser { Id = userId, SchoolId = schoolId };
            var subscription = new SchoolSubscription
            {
                Id = id,
                SchoolId = schoolId,
                PaymentTransaction = new PaymentTransaction
                {
                    UserId = Guid.NewGuid()
                }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var query = new GetSchoolSubscriptionByIdQuery(id, userId, isSystemAdmin: false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(id));
        }

        [Test]
        public async Task Handle_ShouldReturnMappedResponse_WhenNonAdminWhoMadePayment()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var schoolId = 1;

            var user = new ApplicationUser { Id = userId, SchoolId = 2 }; // Different school
            var subscription = new SchoolSubscription
            {
                Id = id,
                SchoolId = schoolId,
                PaymentTransaction = new PaymentTransaction
                {
                    UserId = userId // Same user who made payment
                }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var query = new GetSchoolSubscriptionByIdQuery(id, userId, isSystemAdmin: false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(id));
        }

        [Test]
        public void Handle_ShouldThrow_WhenSubscriptionNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SchoolSubscription?)null);

            var query = new GetSchoolSubscriptionByIdQuery(id, userId, isSystemAdmin: false);

            // Act & Assert
            Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() =>
                _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrow_WhenNonAdminTriesToAccessUnrelatedSubscription()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var schoolId = 1;

            var user = new ApplicationUser { Id = userId, SchoolId = 2 }; // Different school
            var subscription = new SchoolSubscription
            {
                Id = id,
                SchoolId = schoolId,
                PaymentTransaction = new PaymentTransaction
                {
                    UserId = Guid.NewGuid() // Different user
                }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var query = new GetSchoolSubscriptionByIdQuery(id, userId, isSystemAdmin: false);

            // Act & Assert
            Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() =>
                _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var subscription = new SchoolSubscription
            {
                Id = id,
                SchoolId = 1,
                PaymentTransaction = new PaymentTransaction
                {
                    UserId = Guid.NewGuid()
                }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var query = new GetSchoolSubscriptionByIdQuery(id, userId, isSystemAdmin: false);

            // Act & Assert
            Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() =>
                _handler.Handle(query, CancellationToken.None));
        }

        #endregion
    }
}