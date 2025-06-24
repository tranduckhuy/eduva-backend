using Eduva.API.Controllers.SchoolSubscriptions;
using Eduva.API.Models;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.Payments.Queries;
using Eduva.Application.Features.Payments.Responses;
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

    private void SetupUser(string? userId)
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        claims.Add(new Claim(ClaimTypes.Role, nameof(Role.SchoolAdmin)));

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

}