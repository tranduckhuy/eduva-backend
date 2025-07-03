using Eduva.Application.Common.Models;
using Eduva.Application.Features.CreditTransactions.Queries;
using Eduva.Application.Features.CreditTransactions.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.CreditTransactions.Queries
{
    [TestFixture]
    public class GetCreditTransactionQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IGenericRepository<UserCreditTransaction, Unit>> _repoMock;
        private GetCreditTransactionQueryHandler _handler;

        #region GetCreditTransactionQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IGenericRepository<UserCreditTransaction, Unit>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(u => u.GetRepository<UserCreditTransaction, Unit>())
                .Returns(_repoMock.Object);

            _handler = new GetCreditTransactionQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetCreditTransactionQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnPaginatedCreditTransactionResponse()
        {
            // Arrange
            var param = new CreditTransactionSpecParam
            {
                PageIndex = 1,
                PageSize = 2,
                UserId = Guid.NewGuid()
            };

            var query = new GetCreditTransactionQuery(param);

            var entityList = new List<UserCreditTransaction>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = param.UserId,
                    Credits = 200,
                    CreatedAt = DateTimeOffset.UtcNow,
                    User = new ApplicationUser
                    {
                        Id = param.UserId,
                        FullName = "Sang",
                        Email = "sang@eduva.vn"
                    },
                    AICreditPack = new AICreditPack
                    {
                        Id = 1,
                        Name = "Basic Pack",
                        Credits = 200,
                        BonusCredits = 20,
                        Price = 9.99m
                    },
                    PaymentTransaction = new PaymentTransaction
                    {
                        Id = Guid.NewGuid(),
                        PaymentMethod = Domain.Enums.PaymentMethod.PayOS
                    }
                }
            };

            var pagedResult = new Pagination<UserCreditTransaction>
            {
                PageIndex = 1,
                PageSize = 2,
                Count = 1,
                Data = entityList
            };

            _repoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<CreditTransactionSpecification>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(1));
                Assert.That(result.Count, Is.EqualTo(1));
                var first = result.Data.First();

                Assert.Multiple(() =>
                {
                    Assert.That(first.Credits, Is.EqualTo(200));
                    Assert.That(first.User.Email, Is.EqualTo("sang@eduva.vn"));
                    Assert.That(first.AICreditPack.Name, Is.EqualTo("Basic Pack"));
                });
            });
        }

        #endregion

    }
}