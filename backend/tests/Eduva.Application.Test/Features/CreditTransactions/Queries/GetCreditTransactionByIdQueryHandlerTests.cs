using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.CreditTransactions.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Features.CreditTransactions.Queries
{
    [TestFixture]
    public class GetCreditTransactionByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ICreditTransactionRepository> _repoMock;
        private GetCreditTransactionByIdQueryHandler _handler;

        #region GetCreditTransactionByIdQueryHandlerTests Setup

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<ICreditTransactionRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ICreditTransactionRepository>())
                .Returns(_repoMock.Object);

            _handler = new GetCreditTransactionByIdQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetCreditTransactionByIdQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedResponse_WhenTransactionExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var transaction = new UserCreditTransaction
            {
                Id = id,
                UserId = userId,
                Credits = 300,
                CreatedAt = DateTimeOffset.UtcNow,
                PaymentTransactionId = paymentId,
                AICreditPackId = 1,
                User = new ApplicationUser
                {
                    Id = userId,
                    FullName = "Alice Smith",
                    Email = "alice@eduva.vn",
                    PhoneNumber = "0123456789"
                },
                AICreditPack = new AICreditPack
                {
                    Id = 1,
                    Name = "Premium Pack",
                    Credits = 300,
                    BonusCredits = 50,
                    Price = 99.9m
                },
                PaymentTransaction = new PaymentTransaction
                {
                    Id = paymentId,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentMethod = PaymentMethod.PayOS
                }
            };

            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transaction);

            var query = new GetCreditTransactionByIdQuery(id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Id, Is.EqualTo(id));
                Assert.That(result.Credits, Is.EqualTo(300));
                Assert.That(result.User, Is.Not.Null);
                Assert.That(result.User.Email, Is.EqualTo("alice@eduva.vn"));
                Assert.That(result.AICreditPack, Is.Not.Null);
                Assert.That(result.AICreditPack.Name, Is.EqualTo("Premium Pack"));
                Assert.That(result.PaymentTransactionId, Is.EqualTo(paymentId));
            });
        }

        [Test]
        public void Handle_ShouldThrowAppException_WhenTransactionNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCreditTransaction?)null);

            var query = new GetCreditTransactionByIdQuery(id);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.CreditTransactionNotFound));
        }

        #endregion

    }
}