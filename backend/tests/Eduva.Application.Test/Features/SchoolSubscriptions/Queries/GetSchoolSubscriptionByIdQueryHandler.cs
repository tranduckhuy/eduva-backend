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
        private GetSchoolSubscriptionByIdQueryHandler _handler = default!;

        #region GetSchoolSubscriptionByIdQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _subscriptionRepoMock = new Mock<ISchoolSubscriptionRepository>();

            _unitOfWorkMock
                .Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>())
                .Returns(_subscriptionRepoMock.Object);

            _handler = new GetSchoolSubscriptionByIdQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetSchoolSubscriptionByIdQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedResponse_WhenSubscriptionExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var subscription = new SchoolSubscription { Id = id };
            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            // Act
            var result = await _handler.Handle(new GetSchoolSubscriptionByIdQuery(id), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(id));
        }

        [Test]
        public void Handle_ShouldThrow_WhenSubscriptionNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _subscriptionRepoMock
                .Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SchoolSubscription?)null);

            // Act & Assert
            Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() =>
                _handler.Handle(new GetSchoolSubscriptionByIdQuery(id), CancellationToken.None));
        }

        #endregion

    }
}