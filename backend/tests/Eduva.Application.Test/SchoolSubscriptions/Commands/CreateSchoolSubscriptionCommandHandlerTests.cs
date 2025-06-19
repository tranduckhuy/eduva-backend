using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Application.Features.SchoolSubscriptions.Configurations;
using Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOS;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Net.payOS.Types;

namespace Eduva.Application.Test.SchoolSubscriptions.Commands
{
    [TestFixture]
    public class CreateSchoolSubscriptionCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _planRepoMock = null!;
        private Mock<ISchoolSubscriptionRepository> _subRepoMock = null!;
        private Mock<IPayOSService> _payOSServiceMock = null!;
        private CreateSchoolSubscriptionCommandHandler _handler = null!;

        #region CreateSchoolSubscriptionCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();
            _planRepoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();
            _subRepoMock = new Mock<ISchoolSubscriptionRepository>();
            _payOSServiceMock = new Mock<IPayOSService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<School, int>()).Returns(_schoolRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<SubscriptionPlan, int>()).Returns(_planRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<ISchoolSubscriptionRepository>()).Returns(_subRepoMock.Object);

            var config = Options.Create(new PayOSConfig
            {
                CancelUrl = "https://cancel",
                ReturnUrl = "https://return",
                PAYOS_CLIENT_ID = "client",
                PAYOS_API_KEY = "key",
                PAYOS_CHECKSUM_KEY = "checksum"
            });

            _handler = new CreateSchoolSubscriptionCommandHandler(_unitOfWorkMock.Object, _payOSServiceMock.Object, config);
        }

        #endregion

        #region CreateSchoolSubscriptionCommandHandler Tests

        [Test]
        public void Should_Throw_SchoolNotFound_When_SchoolMissing()
        {
            var command = new CreateSchoolSubscriptionCommand { SchoolId = 1, PlanId = 2, BillingCycle = BillingCycle.Monthly };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((School?)null);

            Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Should_Throw_PlanNotFound_When_Missing()
        {
            var command = new CreateSchoolSubscriptionCommand { SchoolId = 1, PlanId = 2, BillingCycle = BillingCycle.Monthly };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new School());
            _planRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((SubscriptionPlan?)null);

            Assert.ThrowsAsync<PlanNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Should_Throw_PlanNotActive_When_StatusInvalid()
        {
            var command = new CreateSchoolSubscriptionCommand { SchoolId = 1, PlanId = 2, BillingCycle = BillingCycle.Monthly };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new School());
            _planRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new SubscriptionPlan { Status = EntityStatus.Inactive });

            Assert.ThrowsAsync<PlanNotActiveException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Should_Throw_AlreadyExists_When_SamePlan_And_Cycle()
        {
            var school = new School { Id = 1, Name = "ABC School", ContactEmail = "abc@email.com", ContactPhone = "123" };
            var plan = new SubscriptionPlan { Id = 2, Status = EntityStatus.Active, PriceMonthly = 50000 };
            var existing = new SchoolSubscription { PlanId = 2, BillingCycle = BillingCycle.Monthly };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(school);
            _planRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(plan);
            _subRepoMock.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(1)).ReturnsAsync(existing);

            var command = new CreateSchoolSubscriptionCommand { SchoolId = 1, PlanId = 2, BillingCycle = BillingCycle.Monthly };

            Assert.ThrowsAsync<SchoolSubscriptionAlreadyExistsException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Should_Throw_DowngradeNotAllowed()
        {
            var plan = new SubscriptionPlan { Id = 2, Status = EntityStatus.Active, PriceMonthly = 10000, PricePerYear = 100000 };
            var existing = new SchoolSubscription
            {
                PlanId = 1,
                BillingCycle = BillingCycle.Yearly,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                AmountPaid = 100000,
                Plan = new SubscriptionPlan { PriceMonthly = 99999, PricePerYear = 999999 }
            };
            var school = new School { Id = 1, Name = "ABC", ContactEmail = "a", ContactPhone = "b" };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(school);
            _planRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(plan);
            _subRepoMock.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(1)).ReturnsAsync(existing);

            var command = new CreateSchoolSubscriptionCommand { SchoolId = 1, PlanId = 2, BillingCycle = BillingCycle.Monthly };

            Assert.ThrowsAsync<DowngradeNotAllowedException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public async Task Should_Return_Success_When_ValidRequest_And_NoPreviousSubscription()
        {
            // Arrange
            var school = new School
            {
                Id = 1,
                Name = "Test School",
                ContactEmail = "test@email.com",
                ContactPhone = "123456789"
            };

            var plan = new SubscriptionPlan
            {
                Id = 2,
                Name = "Plus",
                Status = EntityStatus.Active,
                PriceMonthly = 120000
            };

            var command = new CreateSchoolSubscriptionCommand
            {
                SchoolId = 1,
                PlanId = 2,
                BillingCycle = BillingCycle.Monthly
            };

            var paymentResult = new CreatePaymentResult(
                bin: "970436",
                accountNumber: "123456789",
                amount: 120000,
                description: "PlusM",
                orderCode: 12345678,
                currency: "VND",
                paymentLinkId: "fake-link-id",
                status: "PENDING",
                expiredAt: DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds(),
                checkoutUrl: "https://checkout.fake",
                qrCode: "fake-qr"
            );

            _schoolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(school);
            _planRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(plan);
            _subRepoMock.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(1)).ReturnsAsync((SchoolSubscription?)null);
            _unitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            _payOSServiceMock.Setup(p => p.CreatePaymentLinkAsync(It.IsAny<PaymentData>()))
                .ReturnsAsync(paymentResult);

            // Act
            var (code, response) = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(response.CheckoutUrl, Is.EqualTo("https://checkout.fake"));
                Assert.That(response.PaymentLinkId, Is.EqualTo("fake-link-id"));
                Assert.That(response.Amount, Is.EqualTo(120000));
                Assert.That(response.TransactionId, Is.Not.Null.And.Not.Empty);
            });

            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _payOSServiceMock.Verify(p => p.CreatePaymentLinkAsync(It.IsAny<PaymentData>()), Times.Once);
        }

        #endregion

    }
}