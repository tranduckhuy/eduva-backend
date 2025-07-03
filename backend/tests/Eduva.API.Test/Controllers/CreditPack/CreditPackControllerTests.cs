using Eduva.API.Controllers.CreditPacks;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Commands.ActivateCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.ArchiveCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.DeleteCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks;
using Eduva.Application.Features.AICreditPacks.Queries;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Features.AICreditPacks.Specifications;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.API.Test.Controllers.CreditPack
{
    [TestFixture]
    public class CreditPackControllerTests
    {
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<CreditPackController>> _loggerMock = default!;
        private CreditPackController _controller = default!;

        #region CreditPackControllerTest Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<CreditPackController>>();
            _controller = new CreditPackController(_mediatorMock.Object, _loggerMock.Object);
        }

        #endregion

        #region GetAICreditPacks Test

        [Test]
        public async Task GetAICreditPacks_ShouldReturnOk()
        {
            var param = new AICreditPackSpecParam();
            var result = new Pagination<AICreditPackResponse>(1, 10, 1, new List<AICreditPackResponse>());
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAICreditPacksQuery>(), default))
                .ReturnsAsync(result);

            var response = await _controller.GetAICreditPacks(param);

            var ok = response as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task GetAICreditPacks_ShouldReturnError_WhenExceptionThrown()
        {
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetAICreditPacksQuery>(), default))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetAICreditPacks(new AICreditPackSpecParam());

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region CreateAICreditPack Test

        [Test]
        public async Task CreateAICreditPack_ShouldReturnOk()
        {
            var command = new CreateAICreditPackCommand();
            _mediatorMock.Setup(m => m.Send(command, default))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.CreateAICreditPack(command);

            var ok = result as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task CreateAICreditPack_ShouldReturnError_WhenExceptionThrown()
        {
            var command = new CreateAICreditPackCommand();
            _mediatorMock.Setup(x => x.Send(command, default)).ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.CreateAICreditPack(command);

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region UpdateAICreditPack Test

        [Test]
        public async Task UpdateAICreditPack_ShouldReturnOk()
        {
            var command = new UpdateAICreditPackCommand { Id = 5 };
            _mediatorMock.Setup(m => m.Send(command, default))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.UpdateAICreditPack(5, command);

            var ok = result as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task UpdateAICreditPack_ShouldReturnSystemError_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateAICreditPackCommand>(), default))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.UpdateAICreditPack(1, new UpdateAICreditPackCommand());

            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region ArchiveAICreditPack Test

        [Test]
        public async Task ArchiveAICreditPack_ShouldReturnOk()
        {
            var command = new ArchiveAICreditPackCommand(5);
            _mediatorMock.Setup(m => m.Send(command, default))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ArchiveAICreditPack(5);

            var ok = result as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task ArchiveAICreditPack_ShouldReturnSystemError_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ArchiveAICreditPackCommand>(), default))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.ArchiveAICreditPack(1);

            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region ActivateAICreditPack Test

        [Test]
        public async Task ActivateAICreditPack_ShouldReturnOk()
        {
            var command = new ActivateAICreditPackCommand(5);
            _mediatorMock.Setup(m => m.Send(command, default))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ActivateAICreditPack(5);

            var ok = result as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task ActivateAICreditPack_ShouldReturnSystemError_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ActivateAICreditPackCommand>(), default))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.ActivateAICreditPack(1);

            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region DeleteAICreditPack Test

        [Test]
        public async Task DeleteAICreditPack_ShouldReturnOk()
        {
            var command = new DeleteAICreditPackCommand(5);
            _mediatorMock.Setup(m => m.Send(command, default))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.DeleteAICreditPack(5);

            var ok = result as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }


        [Test]
        public async Task DeleteAICreditPack_ShouldReturnSystemError_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<DeleteAICreditPackCommand>(), default))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.DeleteAICreditPack(1);

            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region GetAICreditPackById Test

        [Test]
        public async Task GetAICreditPackById_ShouldReturnOk()
        {
            var response = new AICreditPackResponse { Id = 1, Name = "Pack 1" };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAICreditPackByIdQuery>(), default))
                .ReturnsAsync(response);

            var result = await _controller.GetAICreditPackById(1);

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Test]
        public async Task GetAICreditPackById_ShouldReturnSystemError_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAICreditPackByIdQuery>(), default))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.GetAICreditPackById(1);

            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        #endregion

    }
}