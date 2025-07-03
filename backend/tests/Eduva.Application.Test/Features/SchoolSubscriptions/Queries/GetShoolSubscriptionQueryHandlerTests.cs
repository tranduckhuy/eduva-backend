using Eduva.Application.Common.Models;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Features.SchoolSubscriptions.Specifications;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Queries
{
    [TestFixture]
    public class GetShoolSubscriptionQueryHandlerTests
    {
        private Mock<ISchoolSubscriptionRepository> _repositoryMock = default!;
        private GetShoolSubscriptionQueryHandler _handler = default!;

        #region GetShoolSubscriptionQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ISchoolSubscriptionRepository>();
            _handler = new GetShoolSubscriptionQueryHandler(_repositoryMock.Object);
        }

        #endregion

        #region GetShoolSubscriptionQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var param = new SchoolSubscriptionSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            var query = new GetSchoolSubscriptionQuery(param);

            var subscriptionList = new List<SchoolSubscription>
            {
                new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow },
                new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
            };

            var mockPagination = new Pagination<SchoolSubscription>(
                pageIndex: 1,
                pageSize: 10,
                count: 2,
                data: subscriptionList);

            _repositoryMock
                .Setup(r => r.GetWithSpecAsync(It.IsAny<SchoolSubscriptionSpecification>()))
                .ReturnsAsync(mockPagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(2));
                Assert.That(result.PageIndex, Is.EqualTo(1));
                Assert.That(result.PageSize, Is.EqualTo(10));
            });
        }

        [Test]
        public async Task Handle_ShouldReturnPaginatedResponse_WithFullPropertyCoverage()
        {
            // Arrange
            var param = new SchoolSubscriptionSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            var query = new GetSchoolSubscriptionQuery(param);

            var subscriptionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow;
            var startDate = DateTimeOffset.UtcNow.AddDays(-10);
            var endDate = DateTimeOffset.UtcNow.AddDays(20);

            var subscriptionList = new List<SchoolSubscription>
            {
                new()
                {
                    Id = subscriptionId,
                    CreatedAt = createdAt,
                    StartDate = startDate,
                    EndDate = endDate,
                    SubscriptionStatus = SubscriptionStatus.Active,
                    BillingCycle = BillingCycle.Yearly,
                    School = new()
                    {
                        Id = 1,
                        Name = "Eduva High School",
                        Address = "123 Main St",
                        ContactEmail = "contact@eduva.vn",
                        ContactPhone = "0123456789",
                        WebsiteUrl = "https://eduva.vn"
                    },
                    Plan = new()
                    {
                        Id = 2,
                        Name = "Premium Plan",
                        Description = "Access to all features",
                        MaxUsers = 100,
                        StorageLimitGB = 50,
                        PriceMonthly = 99.99M,
                        PricePerYear = 199.99M,
                        IsRecommended = true
                    },
                    PaymentTransaction = new()
                    {
                        UserId = userId,
                        PaymentPurpose = PaymentPurpose.SchoolSubscription,
                        PaymentItemId = 2,
                        RelatedId = "SUB123",
                        PaymentMethod = PaymentMethod.PayOS,
                        PaymentStatus = PaymentStatus.Paid,
                        TransactionCode = "TXN123",
                        Amount = 199.99M,
                        CreatedAt = createdAt,
                        User = new()
                        {
                            Id = userId,
                            FullName = "John Doe",
                            Email = "john@example.com",
                            PhoneNumber = "0123456789"
                        }
                    }
                }
            };

            var mockPagination = new Pagination<SchoolSubscription>(
                pageIndex: 1,
                pageSize: 10,
                count: 1,
                data: subscriptionList);

            _repositoryMock
                .Setup(r => r.GetWithSpecAsync(It.IsAny<SchoolSubscriptionSpecification>()))
                .ReturnsAsync(mockPagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);
            var response = result.Data.First();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(1));

                Assert.That(response.Id, Is.EqualTo(subscriptionId));
                Assert.That(response.CreatedAt, Is.EqualTo(createdAt));
                Assert.That(response.StartDate, Is.EqualTo(startDate));
                Assert.That(response.EndDate, Is.EqualTo(endDate));
                Assert.That(response.SubscriptionStatus, Is.EqualTo(SubscriptionStatus.Active));
                Assert.That(response.BillingCycle, Is.EqualTo(BillingCycle.Yearly));

                Assert.That(response.School, Is.Not.Null);
                Assert.That(response.School.Id, Is.EqualTo(1));
                Assert.That(response.School.Name, Is.EqualTo("Eduva High School"));
                Assert.That(response.School.Address, Is.EqualTo("123 Main St"));
                Assert.That(response.School.ContactEmail, Is.EqualTo("contact@eduva.vn"));
                Assert.That(response.School.ContactPhone, Is.EqualTo("0123456789"));
                Assert.That(response.School.WebsiteUrl, Is.EqualTo("https://eduva.vn"));

                Assert.That(response.Plan, Is.Not.Null);
                Assert.That(response.Plan.Id, Is.EqualTo(2));
                Assert.That(response.Plan.Name, Is.EqualTo("Premium Plan"));
                Assert.That(response.Plan.Description, Is.EqualTo("Access to all features"));
                Assert.That(response.Plan.MaxUsers, Is.EqualTo(100));
                Assert.That(response.Plan.StorageLimitGB, Is.EqualTo(50));
                Assert.That(response.Plan.Price, Is.EqualTo(199.99M)); // from PricePerYear
                Assert.That(response.Plan.IsRecommended, Is.True);

                Assert.That(response.PaymentTransaction, Is.Not.Null);
                Assert.That(response.PaymentTransaction.UserId, Is.EqualTo(userId));
                Assert.That(response.PaymentTransaction.PaymentPurpose, Is.EqualTo(PaymentPurpose.SchoolSubscription));
                Assert.That(response.PaymentTransaction.PaymentItemId, Is.EqualTo(2));
                Assert.That(response.PaymentTransaction.RelatedId, Is.EqualTo("SUB123"));
                Assert.That(response.PaymentTransaction.PaymentMethod, Is.EqualTo(PaymentMethod.PayOS));
                Assert.That(response.PaymentTransaction.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
                Assert.That(response.PaymentTransaction.TransactionCode, Is.EqualTo("TXN123"));
                Assert.That(response.PaymentTransaction.Amount, Is.EqualTo(199.99M));
                Assert.That(response.PaymentTransaction.CreatedAt, Is.EqualTo(createdAt));

                Assert.That(response.User, Is.Not.Null);
                Assert.That(response.User.Id, Is.EqualTo(userId));
                Assert.That(response.User.FullName, Is.EqualTo("John Doe"));
                Assert.That(response.User.Email, Is.EqualTo("john@example.com"));
                Assert.That(response.User.PhoneNumber, Is.EqualTo("0123456789"));
            });
        }

        #endregion 

    }
}