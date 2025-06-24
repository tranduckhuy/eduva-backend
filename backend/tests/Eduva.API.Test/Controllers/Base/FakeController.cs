using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Shared.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.API.Test.Controllers.Base
{

    #region FakeController 

    public class FakeController : BaseController<FakeController>
    {
        public FakeController(ILogger<FakeController> logger) : base(logger) { }

        public IActionResult CallRespond(CustomCode code, object? data = null, IEnumerable<string>? errors = null)
            => Respond(code, data, errors);

        public Task<IActionResult> CallHandlePaginatedRequestAsync<T>(Func<Task<(CustomCode, Pagination<T>)>> func)
            where T : class
            => HandlePaginatedRequestAsync(func);

        public Task<IActionResult> CallHandleRequestAsync<T>(Func<Task<(CustomCode, T)>> func)
            where T : class
            => HandleRequestAsync(func);

        public Task<IActionResult> CallHandleRequestAsync(Func<Task> func)
            => HandleRequestAsync(func);
    }

    #endregion

    [TestFixture]
    public class BaseControllerTests
    {
        private FakeController _controller = default!;
        private Mock<ILogger<FakeController>> _loggerMock = default!;

        #region BaseControllerTests Setup

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<FakeController>>();
            _controller = new FakeController(_loggerMock.Object);
        }

        #endregion

        #region BaseController Tests

        [Test]
        public void Respond_ShouldReturn500_WhenCodeNotInDictionary()
        {
            var result = _controller.CallRespond((CustomCode)999, null, new[] { "Error" });

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(500);

            var response = objectResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.Message.Should().Be("Unknown error");
            response.Errors.Should().Contain("Error");
        }

        [Test]
        public void Respond_ShouldReturnExpectedMessage_WhenCodeExists()
        {
            var result = _controller.CallRespond(CustomCode.Success, "data", null);

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(200);

            var response = objectResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.StatusCode.Should().Be((int)CustomCode.Success);
            response.Message.Should().Be("Success");
            response.Data.Should().Be("data");
        }

        [Test]
        public async Task HandlePaginatedRequestAsync_ShouldReturnPaginationResult()
        {
            var pagination = new Pagination<string>(1, 10, 2, new List<string> { "A", "B" });

            var result = await _controller.CallHandlePaginatedRequestAsync(() =>
                Task.FromResult((CustomCode.Success, pagination)));

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(200);

            var response = objectResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.Data.Should().BeEquivalentTo(pagination);
        }

        [Test]
        public async Task HandleRequestAsync_WithResult_ShouldReturnOk()
        {
            var result = await _controller.CallHandleRequestAsync(() =>
                Task.FromResult((CustomCode.Success, "OK")));

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(200);

            var response = objectResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.Data.Should().Be("OK");
        }

        [Test]
        public async Task HandleRequestAsync_Void_ShouldReturnSuccess()
        {
            var result = await _controller.CallHandleRequestAsync(() => Task.CompletedTask);

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(200);

            var response = objectResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.Data.Should().BeNull();
        }

        #endregion

    }
}