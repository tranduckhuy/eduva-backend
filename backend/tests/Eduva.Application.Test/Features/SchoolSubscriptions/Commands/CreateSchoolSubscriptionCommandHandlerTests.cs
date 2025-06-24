using Eduva.Application.Exceptions.PaymentTransaction;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.Payments.Configurations;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Net.payOS.Types;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Commands;

[TestFixture]
public class CreateSchoolSubscriptionCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<IPayOSService> _payOsServiceMock = null!;
    private IOptions<PayOSConfig> _payOsOptions = null!;
    private CreateSchoolSubscriptionCommandHandler _handler = null!;

    private Mock<IGenericRepository<School, int>> _schoolRepo = null!;
    private Mock<IGenericRepository<SubscriptionPlan, int>> _planRepo = null!;
    private Mock<IGenericRepository<PaymentTransaction, Guid>> _paymentRepo = null!;
    private Mock<ISchoolSubscriptionRepository> _schoolSubRepo = null!;

    #region CreateSchoolSubscriptionCommandHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _payOsServiceMock = new Mock<IPayOSService>();
        _payOsOptions = Options.Create(new PayOSConfig { CancelUrl = "cancel", ReturnUrl = "return" });

        _schoolRepo = new Mock<IGenericRepository<School, int>>();
        _planRepo = new Mock<IGenericRepository<SubscriptionPlan, int>>();
        _paymentRepo = new Mock<IGenericRepository<PaymentTransaction, Guid>>();
        _schoolSubRepo = new Mock<ISchoolSubscriptionRepository>();

        _unitOfWorkMock.Setup(u => u.GetRepository<School, int>()).Returns(_schoolRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<SubscriptionPlan, int>()).Returns(_planRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<PaymentTransaction, Guid>()).Returns(_paymentRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>()).Returns(_schoolSubRepo.Object);

        _handler = new CreateSchoolSubscriptionCommandHandler(_unitOfWorkMock.Object, _payOsServiceMock.Object, _payOsOptions);
    }

    #endregion

    #region CreateSchoolSubscriptionCommandHandler Tests 

    [Test]
    public async Task Should_Create_Subscription_When_Valid_And_No_Existing()
    {
        var plan = new SubscriptionPlan { Id = 1, Name = "Plus", PriceMonthly = 200000, PricePerYear = 1000000, Status = EntityStatus.Active };
        var school = new School { Id = 123, Name = "Eduva School", ContactEmail = "email@test.com", ContactPhone = "0123456789" };
        var command = new CreateSchoolSubscriptionCommand { SchoolId = school.Id, PlanId = plan.Id, BillingCycle = BillingCycle.Monthly, UserId = Guid.NewGuid() };

        _schoolRepo.Setup(r => r.GetByIdAsync(school.Id)).ReturnsAsync(school);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchoolSubscription?)null);

        _payOsServiceMock.Setup(s => s.CreatePaymentLinkAsync(It.IsAny<PaymentData>()))
            .ReturnsAsync(new CreatePaymentResult(
                bin: "9704",
                accountNumber: "123456789",
                amount: 200000,
                description: "Eduva Subscription",
                orderCode: 1234567890,
                currency: "VND",
                paymentLinkId: "link123",
                status: "PENDING",
                expiredAt: DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeMilliseconds(),
                checkoutUrl: "https://checkout.url",
                qrCode: "QRCodeHere"
            ));


        var result = await _handler.Handle(command, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1, Is.EqualTo(CustomCode.Success));
            Assert.That(result.Item2.CheckoutUrl, Is.EqualTo("https://checkout.url"));
            Assert.That(result.Item2.PaymentLinkId, Is.EqualTo("link123"));
        });
    }

    [Test]
    public void Should_Throw_When_School_NotFound()
    {
        _schoolRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((School?)null);
        var command = new CreateSchoolSubscriptionCommand { SchoolId = 123 };
        Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_Plan_NotFound()
    {
        _schoolRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SubscriptionPlan?)null);
        var command = new CreateSchoolSubscriptionCommand { SchoolId = 123, PlanId = 1 };
        Assert.ThrowsAsync<PlanNotFoundException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_Plan_IsNotActive()
    {
        var plan = new SubscriptionPlan { Status = EntityStatus.Inactive };
        _schoolRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(plan);
        var command = new CreateSchoolSubscriptionCommand { SchoolId = 123, PlanId = 1 };
        Assert.ThrowsAsync<PlanNotActiveException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_SamePlanAndCycle_Exists()
    {
        var now = DateTimeOffset.UtcNow;
        var plan = new SubscriptionPlan { Id = 1, PriceMonthly = 200000, PricePerYear = 1000000, Status = EntityStatus.Active };
        var current = new SchoolSubscription { PlanId = 1, BillingCycle = BillingCycle.Monthly, StartDate = now.AddDays(-5) };

        _schoolRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(plan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(current);

        var command = new CreateSchoolSubscriptionCommand { PlanId = 1, BillingCycle = BillingCycle.Monthly };
        Assert.ThrowsAsync<SchoolSubscriptionAlreadyExistsException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_Downgrade_Yearly_To_Monthly()
    {
        var currentPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 200000, PricePerYear = 1000000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 100000, PricePerYear = 500000, Status = EntityStatus.Active };

        var existing = new SchoolSubscription
        {
            Plan = currentPlan,
            PlanId = currentPlan.Id,
            BillingCycle = BillingCycle.Yearly,
            StartDate = DateTimeOffset.UtcNow.AddDays(-10),
            PaymentTransactionId = Guid.NewGuid()
        };

        _schoolRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var command = new CreateSchoolSubscriptionCommand { PlanId = newPlan.Id, BillingCycle = BillingCycle.Monthly };
        Assert.ThrowsAsync<DowngradeNotAllowedException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_PaymentTransaction_NotFound()
    {
        var currentPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 200000, PricePerYear = 800000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 300000, PricePerYear = 1200000, Status = EntityStatus.Active };

        var current = new SchoolSubscription
        {
            Plan = currentPlan,
            PlanId = currentPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            StartDate = DateTimeOffset.UtcNow.AddDays(-5),
            PaymentTransactionId = Guid.NewGuid()
        };

        _schoolRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo
            .Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        _paymentRepo.Setup(r => r.GetByIdAsync(current.PaymentTransactionId)).ReturnsAsync((PaymentTransaction?)null);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = newPlan.Id,
            BillingCycle = BillingCycle.Monthly
        };

        Assert.ThrowsAsync<PaymentTransactionNotFoundException>(() =>
            _handler.Handle(command, default));
    }

    #endregion

}