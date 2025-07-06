using AutoMapper;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Queries;

[TestFixture]
public class GetMySchoolSubscriptionQueryHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<ISchoolSubscriptionRepository> _subRepoMock = null!;
    private IMapper _mapper = null!;
    private GetMySchoolSubscriptionQueryHandler _handler = null!;

    #region GetMySchoolSubscriptionQueryHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _subRepoMock = new Mock<ISchoolSubscriptionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
            .Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>())
            .Returns(_subRepoMock.Object);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AppMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetMySchoolSubscriptionQueryHandler(_unitOfWorkMock.Object, _mapper);
    }

    #endregion

    #region GetMySchoolSubscriptionQueryHandler Tests

    [Test]
    public async Task Handle_ShouldReturnResponse_WhenValidUserAndSubscriptionExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var schoolId = 123;

        var user = new ApplicationUser
        {
            Id = userId,
            SchoolId = schoolId
        };

        var subscription = new SchoolSubscription
        {
            Id = Guid.NewGuid(),
            Plan = new SubscriptionPlan
            {
                Name = "Plus",
                Description = "Premium access",
                MaxUsers = 100,
                StorageLimitGB = 500
            },
            PaymentTransaction = new PaymentTransaction
            {
                Amount = 1000000
            },
            StartDate = DateTimeOffset.UtcNow.AddDays(-10),
            EndDate = DateTimeOffset.UtcNow.AddDays(20),
            BillingCycle = BillingCycle.Monthly,
            SubscriptionStatus = SubscriptionStatus.Active,
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetLatestPaidBySchoolIdAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _handler.Handle(new GetMySchoolSubscriptionQuery(userId), default);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(subscription.Id));
            Assert.That(result.PlanName, Is.EqualTo(subscription.Plan.Name));
            Assert.That(result.Description, Is.EqualTo(subscription.Plan.Description));
            Assert.That(result.MaxUsers, Is.EqualTo(subscription.Plan.MaxUsers));
            Assert.That(result.StorageLimitGB, Is.EqualTo(subscription.Plan.StorageLimitGB));
            Assert.That(result.SubscriptionStatus, Is.EqualTo(subscription.SubscriptionStatus));
            Assert.That(result.BillingCycle, Is.EqualTo(subscription.BillingCycle));
            Assert.That(result.AmountPaid, Is.EqualTo(subscription.PaymentTransaction.Amount));
            Assert.That(result.PriceMonthly, Is.EqualTo(subscription.Plan.PriceMonthly));
            Assert.That(result.PricePerYear, Is.EqualTo(subscription.Plan.PricePerYear));
        });
    }

    [Test]
    public void Handle_ShouldThrowUserNotExistsException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();

        _userRepoMock.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

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
        var schoolId = 123;
        var user = new ApplicationUser { Id = userId, SchoolId = schoolId };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetLatestPaidBySchoolIdAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchoolSubscription?)null);

        Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() =>
            _handler.Handle(new GetMySchoolSubscriptionQuery(userId), default));
    }

    #endregion

}