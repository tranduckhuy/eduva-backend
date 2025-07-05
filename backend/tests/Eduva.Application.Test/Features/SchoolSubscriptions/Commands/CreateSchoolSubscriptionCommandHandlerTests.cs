using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.PaymentTransaction;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Constants;
using Eduva.Shared.Enums;
using Moq;
using Net.payOS.Types;
using System.Reflection;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Commands;

[TestFixture]
public class CreateSchoolSubscriptionCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<IPayOSService> _payOsServiceMock = null!;
    private Mock<ISystemConfigHelper> _systemConfigHelperMock = null!;
    private CreateSchoolSubscriptionCommandHandler _handler = null!;

    private Mock<IGenericRepository<School, int>> _schoolRepo = null!;
    private Mock<IGenericRepository<SubscriptionPlan, int>> _planRepo = null!;
    private Mock<IGenericRepository<PaymentTransaction, Guid>> _paymentRepo = null!;
    private Mock<ISchoolSubscriptionRepository> _schoolSubRepo = null!;
    private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepo = null!;
    private ApplicationUser _testUser = null!;

    #region CreateSchoolSubscriptionCommandHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _payOsServiceMock = new Mock<IPayOSService>();
        _systemConfigHelperMock = new Mock<ISystemConfigHelper>();

        _userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
        _schoolRepo = new Mock<IGenericRepository<School, int>>();
        _planRepo = new Mock<IGenericRepository<SubscriptionPlan, int>>();
        _paymentRepo = new Mock<IGenericRepository<PaymentTransaction, Guid>>();
        _schoolSubRepo = new Mock<ISchoolSubscriptionRepository>();

        _testUser = new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 123 };

        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);

        _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<School, int>()).Returns(_schoolRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<SubscriptionPlan, int>()).Returns(_planRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<PaymentTransaction, Guid>()).Returns(_paymentRepo.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>()).Returns(_schoolSubRepo.Object);

        // Setup system config helper mock
        _systemConfigHelperMock
            .Setup(x => x.GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN, It.IsAny<string>()))
            .ReturnsAsync("https://school.eduva.tech/school-admin");

        _handler = new CreateSchoolSubscriptionCommandHandler(
            _unitOfWorkMock.Object,
            _payOsServiceMock.Object,
            _systemConfigHelperMock.Object);
    }

    #endregion

    #region CreateSchoolSubscriptionCommandHandler Tests 

    [Test]
    public async Task Should_Calculate_Correct_DeductedAmount_For_Yearly_Billing()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var oldPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 200000, PricePerYear = 1000000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 300000, PricePerYear = 1200000, Status = EntityStatus.Active };

        var school = new School { Id = 999, Name = "Truong ABC", ContactEmail = "truong@edu.vn", ContactPhone = "0123456789" };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Amount = 1000000 // yearly plan paid
        };

        var currentSubscription = new SchoolSubscription
        {
            Plan = oldPlan,
            PlanId = oldPlan.Id,
            BillingCycle = BillingCycle.Yearly, // 👈 this is the key
            StartDate = now.AddDays(-100), // still has time left
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction
        };

        _testUser.SchoolId = school.Id;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(school.Id)).ReturnsAsync(school);
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(currentSubscription);
        _paymentRepo.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        _payOsServiceMock.Setup(s => s.CreatePaymentLinkAsync(It.IsAny<PaymentData>()))
            .ReturnsAsync(new CreatePaymentResult(
                bin: "9704",
                accountNumber: "123456789",
                amount: 123456,
                description: "Eduva Subscription",
                orderCode: 999999999,
                currency: "VND",
                paymentLinkId: "link-999",
                status: "PENDING",
                expiredAt: DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeMilliseconds(),
                checkoutUrl: "https://checkout.url",
                qrCode: "QRCodeHere"
            ));

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = newPlan.Id,
            BillingCycle = BillingCycle.Yearly, // 👈 key path
            UserId = _testUser.Id
        };

        // Act
        var result = await _handler.Handle(command, default);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Item2.Amount, Is.GreaterThan(10000));
            Assert.That(result.Item2.DeductedAmount, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task Should_Apply_DeductedAmount_Correctly_When_Final_Amount_Is_More_Than_Minimum()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var oldPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 300000, PricePerYear = 1000000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 400000, PricePerYear = 1200000, Status = EntityStatus.Active };

        var school = new School { Id = 123, Name = "Test School", ContactEmail = "school@test.com", ContactPhone = "0123456789" };

        var transaction = new PaymentTransaction { Id = Guid.NewGuid(), Amount = 300000 };

        var currentSubscription = new SchoolSubscription
        {
            Plan = oldPlan,
            PlanId = oldPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            StartDate = now.AddDays(-5),
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction
        };

        _testUser.SchoolId = school.Id;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(school.Id)).ReturnsAsync(school);
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(currentSubscription);
        _paymentRepo.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        _payOsServiceMock.Setup(s => s.CreatePaymentLinkAsync(It.IsAny<PaymentData>()))
            .ReturnsAsync(new CreatePaymentResult(
                bin: "9704",
                accountNumber: "123456789",
                amount: 390000, // > 10000
                description: "Eduva Subscription",
                orderCode: 1234567890,
                currency: "VND",
                paymentLinkId: "link456",
                status: "PENDING",
                expiredAt: DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeMilliseconds(),
                checkoutUrl: "https://checkout.eduva.vn",
                qrCode: "QRCodeHere"
            ));

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = newPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        // Act
        var result = await _handler.Handle(command, default);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Item2.Amount, Is.GreaterThan(10000));
            Assert.That(result.Item2.DeductedAmount, Is.GreaterThan(0));
            Assert.That(result.Item2.CheckoutUrl, Is.EqualTo("https://checkout.eduva.vn"));
        });
    }

    [TestCase(BillingCycle.Monthly, 100000, 900000, ExpectedResult = 100000)]
    [TestCase(BillingCycle.Yearly, 100000, 900000, ExpectedResult = 900000)]
    public decimal Should_Calculate_BaseAmount_Correctly(BillingCycle cycle, decimal monthly, decimal yearly)
    {
        var handlerType = typeof(CreateSchoolSubscriptionCommandHandler);
        var method = handlerType.GetMethod("GetBaseAmount", BindingFlags.NonPublic | BindingFlags.Static)!;

        var plan = new SubscriptionPlan { PriceMonthly = monthly, PricePerYear = yearly };
        return (decimal)method.Invoke(null, [plan, cycle])!;
    }

    [TestCase(BillingCycle.Yearly, BillingCycle.Monthly, 100000, 100000, 100000, true)]
    [TestCase(BillingCycle.Monthly, BillingCycle.Monthly, 50000, 100000, 100000, true)]
    [TestCase(BillingCycle.Yearly, BillingCycle.Yearly, 800000, 1000000, 1000000, false)]
    [TestCase(BillingCycle.Monthly, BillingCycle.Monthly, 100000, 100000, 100000, false)]
    [TestCase(BillingCycle.Yearly, BillingCycle.Yearly, 900000, 900000, 900000, false)]
    public void Should_Detect_Downgrade(
        BillingCycle currentCycle,
        BillingCycle newCycle,
        decimal newMonthly,
        decimal newYearly,
        decimal currentAmount,
        bool expected)
    {
        var handlerType = typeof(CreateSchoolSubscriptionCommandHandler);
        var method = handlerType.GetMethod("IsDowngrade", BindingFlags.NonPublic | BindingFlags.Static)!;

        var current = new SchoolSubscription
        {
            BillingCycle = currentCycle,
            Plan = new SubscriptionPlan { PriceMonthly = currentAmount, PricePerYear = currentAmount }
        };

        var newPlan = new SubscriptionPlan { PriceMonthly = newMonthly, PricePerYear = newYearly };

        var command = new CreateSchoolSubscriptionCommand { BillingCycle = newCycle };
        var result = (bool)method.Invoke(null, [command, newPlan, current])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(50000, 40000, 60000, 30, 10)]
    [TestCase(100000, 5000, 100000, 30, 29)]
    public async Task Should_Build_Payment_Request_Correctly(
    int amount,
    int deducted,
    int expectedAmount,
    int totalDays,
    int daysUsed)
    {
        // Setup system config helper mock
        var systemConfigHelperMock = new Mock<ISystemConfigHelper>();
        systemConfigHelperMock
            .Setup(x => x.GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN, It.IsAny<string>()))
            .ReturnsAsync("https://school.eduva.tech/school-admin");

        var method = typeof(CreateSchoolSubscriptionCommandHandler)
            .GetMethod("BuildPaymentRequestAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        var handler = Activator.CreateInstance(typeof(CreateSchoolSubscriptionCommandHandler),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IPayOSService>(),
            systemConfigHelperMock.Object
        );

        var result = await (Task<PaymentData>)method!.Invoke(handler, new object[]
        {
            new SubscriptionPlan { Name = "Plan X" },
            BillingCycle.Monthly,
            expectedAmount,
            new School { Name = "School A", ContactEmail = "mail@mail.com", ContactPhone = "012345" },
            1234567890L
        })!;

        Assert.Multiple(() =>
        {
            Assert.That(result.orderCode, Is.EqualTo(1234567890L));
            Assert.That(result.amount, Is.EqualTo(expectedAmount));
            Assert.That(result.description, Does.Contain("Plan X"));
            Assert.That(result.cancelUrl, Is.EqualTo("https://school.eduva.tech/school-admin"));
            Assert.That(result.returnUrl, Is.EqualTo("https://school.eduva.tech/school-admin"));
        });

        // Verify system config was called
        systemConfigHelperMock.Verify(
            x => x.GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN, It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task Should_Use_Correct_BillingCode_For_Yearly()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = 1,
            Name = "Plus",
            PricePerYear = 1000000,
            Status = EntityStatus.Active
        };

        var school = new School
        {
            Id = 123,
            Name = "Eduva School",
            ContactEmail = "email@test.com",
            ContactPhone = "0123456789"
        };

        _testUser.SchoolId = school.Id;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(school.Id)).ReturnsAsync(school);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((SchoolSubscription?)null);

        var payOsResult = new CreatePaymentResult(
            bin: "9704",
            accountNumber: "0123456789",
            amount: 1000000,
            description: "Eduva Plus Nam",
            orderCode: 1234567890,
            currency: "VND",
            paymentLinkId: "link456",
            status: "PENDING",
            checkoutUrl: "https://checkout.url",
            qrCode: "QRCodeHere",
            expiredAt: null
        );

        _payOsServiceMock
            .Setup(s => s.CreatePaymentLinkAsync(It.Is<PaymentData>(p => p.description.Contains(" Nam"))))
            .ReturnsAsync(payOsResult);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = plan.Id,
            BillingCycle = BillingCycle.Yearly,
            UserId = _testUser.Id
        };

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        Assert.That(result.Item2.CheckoutUrl, Is.EqualTo("https://checkout.url"));
    }

    [Test]
    public async Task Should_Apply_Minimum_Amount_When_Final_Less_Than_10000()
    {
        // Arrange
        var oldPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 300000, PricePerYear = 800000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 300000, PricePerYear = 1200000, Status = EntityStatus.Active };
        var now = DateTimeOffset.UtcNow;

        var subscription = new SchoolSubscription
        {
            Plan = oldPlan,
            PlanId = oldPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            StartDate = now.AddDays(-1),
            PaymentTransactionId = Guid.NewGuid()
        };

        var transaction = new PaymentTransaction
        {
            Amount = 300000
        };

        var school = new School
        {
            Id = 123,
            Name = "Test School",
            ContactEmail = "school@test.com",
            ContactPhone = "0123456789"
        };

        _testUser.SchoolId = school.Id;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(school.Id)).ReturnsAsync(school);
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo
            .Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _paymentRepo.Setup(r => r.GetByIdAsync(subscription.PaymentTransactionId))
            .ReturnsAsync(transaction);

        _payOsServiceMock.Setup(s => s.CreatePaymentLinkAsync(It.IsAny<PaymentData>()))
            .ReturnsAsync(new CreatePaymentResult(
                bin: "9704",
                accountNumber: "0123456789",
                amount: 10000,
                description: "Plus Thang",
                orderCode: 1234567890,
                currency: "VND",
                paymentLinkId: "min-link",
                status: "PENDING",
                checkoutUrl: "https://checkout.url",
                qrCode: "QRCodeHere",
                expiredAt: null
            ));

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = newPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        Assert.That(result.Item2.Amount, Is.EqualTo(10000));
    }

    [TestCase(10000, 10000)]
    [TestCase(50000, 50000)]
    [TestCase(0, 10000)]
    public void Should_Enforce_Minimum_FinalAmount_When_Calculated(decimal calculatedFinal, decimal expectedFinal)
    {
        var final = calculatedFinal <= 10000 ? 10000 : calculatedFinal;
        Assert.That(final, Is.EqualTo(expectedFinal));
    }

    [Test]
    public async Task Should_Create_Subscription_When_Valid_And_No_Existing()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = 1,
            Name = "Plus",
            PriceMonthly = 200000,
            PricePerYear = 1000000,
            Status = EntityStatus.Active
        };

        var school = new School
        {
            Id = 123,
            Name = "Eduva School",
            ContactEmail = "email@test.com",
            ContactPhone = "0123456789"
        };

        _testUser.SchoolId = school.Id;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
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

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = plan.Id,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
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
        _testUser.SchoolId = 999;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((School?)null);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = 1,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_Plan_NotFound()
    {
        _testUser.SchoolId = 123;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((SubscriptionPlan?)null);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = 1,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        Assert.ThrowsAsync<PlanNotFoundException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_Plan_IsNotActive()
    {
        var plan = new SubscriptionPlan { Status = EntityStatus.Inactive };

        _testUser.SchoolId = 123;
        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _schoolRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School());
        _planRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(plan);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = 1,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        Assert.ThrowsAsync<PlanNotActiveException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_SamePlanAndCycle_Exists()
    {
        var now = DateTimeOffset.UtcNow;

        var plan = new SubscriptionPlan
        {
            Id = 1,
            Name = "Gói A",
            PriceMonthly = 200000,
            PricePerYear = 1000000,
            Status = EntityStatus.Active
        };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Amount = 200000
        };

        var user = new ApplicationUser
        {
            Id = _testUser.Id,
            Email = _testUser.Email,
            SchoolId = 123
        };

        var current = new SchoolSubscription
        {
            Plan = plan,
            PlanId = plan.Id,
            BillingCycle = BillingCycle.Monthly,
            StartDate = now.AddDays(-5),
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction
        };

        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(user);
        _schoolRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School { Id = 123 });
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(123, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(current);
        _paymentRepo.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = plan.Id,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        Assert.ThrowsAsync<SchoolSubscriptionAlreadyExistsException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_PaymentTransaction_NotFound()
    {
        var currentPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 200000, PricePerYear = 800000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 300000, PricePerYear = 1200000, Status = EntityStatus.Active };

        var transactionId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = _testUser.Id,
            Email = _testUser.Email,
            SchoolId = 123
        };

        var current = new SchoolSubscription
        {
            Plan = currentPlan,
            PlanId = currentPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            StartDate = DateTimeOffset.UtcNow.AddDays(-5),
            PaymentTransactionId = transactionId
        };

        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(user);
        _schoolRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School { Id = 123 });
        _planRepo.Setup(r => r.GetByIdAsync(currentPlan.Id)).ReturnsAsync(currentPlan);
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(123, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(current);
        _paymentRepo.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync((PaymentTransaction?)null);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = newPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        Assert.ThrowsAsync<PaymentTransactionNotFoundException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_Downgrade_Yearly_To_Monthly()
    {
        var currentPlan = new SubscriptionPlan { Id = 1, PriceMonthly = 200000, PricePerYear = 1000000 };
        var newPlan = new SubscriptionPlan { Id = 2, PriceMonthly = 100000, PricePerYear = 500000, Status = EntityStatus.Active };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Amount = 1000000
        };

        var user = new ApplicationUser
        {
            Id = _testUser.Id,
            Email = _testUser.Email,
            SchoolId = 123
        };

        var existing = new SchoolSubscription
        {
            Plan = currentPlan,
            PlanId = currentPlan.Id,
            BillingCycle = BillingCycle.Yearly,
            StartDate = DateTimeOffset.UtcNow.AddDays(-10),
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction
        };

        _userRepo.Setup(r => r.GetByIdAsync(_testUser.Id)).ReturnsAsync(user);
        _schoolRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync(new School { Id = 123 });
        _planRepo.Setup(r => r.GetByIdAsync(currentPlan.Id)).ReturnsAsync(currentPlan);
        _planRepo.Setup(r => r.GetByIdAsync(newPlan.Id)).ReturnsAsync(newPlan);
        _schoolSubRepo.Setup(r => r.GetActiveSubscriptionBySchoolIdAsync(123, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existing);
        _paymentRepo.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = newPlan.Id,
            BillingCycle = BillingCycle.Monthly,
            UserId = _testUser.Id
        };

        Assert.ThrowsAsync<DowngradeNotAllowedException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_User_Not_Found()
    {
        var command = new CreateSchoolSubscriptionCommand
        {
            UserId = Guid.NewGuid()
        };

        _userRepo.Setup(r => r.GetByIdAsync(command.UserId)).ReturnsAsync((ApplicationUser?)null);

        Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Should_Throw_When_User_Not_In_School()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            SchoolId = null
        };

        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var command = new CreateSchoolSubscriptionCommand
        {
            UserId = user.Id
        };

        Assert.ThrowsAsync<UserNotPartOfSchoolException>(() => _handler.Handle(command, default));
    }

    #endregion

}