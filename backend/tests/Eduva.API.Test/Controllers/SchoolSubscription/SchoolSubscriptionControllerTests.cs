using Eduva.API.Controllers.SchoolSubscriptions;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Features.SchoolSubscriptions.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.SchoolSubscription;

[TestFixture]
public class SchoolSubscriptionControllerTests
{
    private SchoolSubscriptionController _controller = default!;
    private Mock<IMediator> _mediatorMock = default!;
    private Mock<ILogger<SchoolSubscriptionController>> _loggerMock = default!;

    #region SchoolSubscriptionControllerTests Setup

    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SchoolSubscriptionController>>();
        _controller = new SchoolSubscriptionController(_mediatorMock.Object, _loggerMock.Object);
    }

    private void SetupUser(string? userId, string role = "SchoolAdmin")
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        claims.Add(new Claim(ClaimTypes.Role, $"{nameof(Role)}.{role}"));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #endregion

    #region CreatePaymentLink Tests

    [Test]
    public async Task CreatePaymentLink_ShouldReturnModelInvalid_WhenModelIsInvalid()
    {
        SetupUser(Guid.NewGuid().ToString());
        _controller.ModelState.AddModelError("PlanId", "Required");

        var result = await _controller.CreatePaymentLink(new CreateSchoolSubscriptionCommand());

        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);

        var response = objectResult!.Value as ApiResponse<object>;
        Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
    }

    [Test]
    public async Task CreatePaymentLink_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
    {
        SetupUser("invalid-guid");

        var command = new CreateSchoolSubscriptionCommand();
        var result = await _controller.CreatePaymentLink(command);

        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);

        var response = objectResult!.Value as ApiResponse<object>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
    }

    [Test]
    public async Task CreatePaymentLink_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());

        var expectedResponse = new CreatePaymentLinkResponse
        {
            CheckoutUrl = "https://pay.url",
            PaymentLinkId = "mock-id",
            Amount = 500000
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateSchoolSubscriptionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomCode.Success, expectedResponse));

        var result = await _controller.CreatePaymentLink(new CreateSchoolSubscriptionCommand());

        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null, "objectResult is null");

        var response = objectResult!.Value as ApiResponse<object>;
        Assert.That(response, Is.Not.Null, "response is null");

        var actualData = response!.Data as CreatePaymentLinkResponse;
        Assert.That(actualData, Is.Not.Null, "actualData is null");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)CustomCode.Success));
            Assert.That(actualData!.CheckoutUrl, Is.EqualTo(expectedResponse.CheckoutUrl));
        });
    }

    #endregion

    #region GetCurrentSubscription Tests

    [Test]
    public async Task GetCurrentSubscription_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
    {
        SetupUser("invalid-guid");

        var result = await _controller.GetCurrentSubscription();

        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);

        var response = objectResult!.Value as ApiResponse<object>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
    }

    [Test]
    public async Task GetCurrentSubscription_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId.ToString());

        var expectedResponse = new MySchoolSubscriptionResponse
        {
            PlanName = "Plus",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(30),
            BillingCycle = BillingCycle.Monthly,
            SubscriptionStatus = SubscriptionStatus.Active,
            MaxUsers = 50,
            StorageLimitGB = 10,
            AmountPaid = 500000
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMySchoolSubscriptionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetCurrentSubscription();

        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null, "objectResult is null");

        var response = objectResult!.Value as ApiResponse<object>;
        Assert.That(response, Is.Not.Null, "response is null");

        var actualData = response!.Data as MySchoolSubscriptionResponse;
        Assert.That(actualData, Is.Not.Null, "actualData is null");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo((int)CustomCode.Success));
            Assert.That(actualData!.PlanName, Is.EqualTo(expectedResponse.PlanName));
        });
    }

    #endregion

    #region GetSchoolSubscriptions

    [Test]
    public async Task GetSchoolSubscriptions_ShouldReturnSuccess_WhenValidRequest()
    {
        var specParam = new SchoolSubscriptionSpecParam
        {
            PageIndex = 1,
            PageSize = 10
        };

        var expected = new Pagination<SchoolSubscriptionResponse>
        {
            PageIndex = 1,
            PageSize = 10,
            Count = 1,
            Data = new List<SchoolSubscriptionResponse>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SubscriptionStatus = SubscriptionStatus.Active,
                    BillingCycle = BillingCycle.Monthly,
                    School = new(),
                    Plan = new(),
                    PaymentTransaction = new(),
                    User = new()
                }
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetSchoolSubscriptionQuery>(q => q.Param == specParam), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetSchoolSubscriptions(specParam);

        var objectResult = result as ObjectResult;
        var response = objectResult?.Value as ApiResponse<object>;
        var data = response?.Data as Pagination<SchoolSubscriptionResponse>;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
            Assert.That(data!.Data, Has.Count.EqualTo(1));
        });
    }

    #endregion

    #region GetSchoolSubscriptionById Tests

    [Test]
    public async Task GetSchoolSubscriptionById_ShouldReturnSuccess_WhenValidId()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        SetupUser(Guid.NewGuid().ToString());

        var expectedResponse = new SchoolSubscriptionResponse { Id = subscriptionId };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetSchoolSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSchoolSubscriptionById(subscriptionId);

        // Assert
        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

        var response = objectResult.Value as ApiResponse<object>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
    }

    [Test]
    public async Task GetSchoolSubscriptionById_ShouldReturnUserIdNotFound_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // no user
        };

        var subscriptionId = Guid.NewGuid();

        // Act
        var result = await _controller.GetSchoolSubscriptionById(subscriptionId);

        // Assert
        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(401));

        var response = objectResult.Value as ApiResponse<object>;
        Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
    }

    [Test]
    public async Task GetSchoolSubscriptionById_ShouldPassSystemAdminFlag_WhenUserIsSystemAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, nameof(Role.SystemAdmin))
            };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var subscriptionId = Guid.NewGuid();
        var expectedResponse = new SchoolSubscriptionResponse { Id = subscriptionId };

        _mediatorMock
             .Setup(m => m.Send(It.IsAny<GetSchoolSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSchoolSubscriptionById(subscriptionId);

        // Assert
        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

        // Verify that the query was called with SystemAdmin = true
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetSchoolSubscriptionByIdQuery>(q =>
                q.Id == subscriptionId &&
                q.IsSystemAdmin == true &&
                q.UserId == Guid.Parse(userId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

}