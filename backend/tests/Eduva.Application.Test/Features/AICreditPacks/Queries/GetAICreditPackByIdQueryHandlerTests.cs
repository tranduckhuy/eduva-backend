using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.AICreditPacks.Queries;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Features.AICreditPacks.Queries
{
    [TestFixture]
    public class GetAICreditPackByIdQueryHandlerTests
    {
        private Mock<IGenericRepository<AICreditPack, int>> _repoMock;
        private Mock<IMapper> _mapperMock;
        private GetAICreditPackByIdQueryHandler _handler;

        #region GetAICreditPackByIdQueryHandlerTests Setup

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IGenericRepository<AICreditPack, int>>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetAICreditPackByIdQueryHandler(_repoMock.Object, _mapperMock.Object);
        }

        #endregion

        #region GetAICreditPackByIdQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedResponse_WhenPackExists()
        {
            // Arrange
            var query = new GetAICreditPackByIdQuery(1);
            var entity = new AICreditPack { Id = 1, Name = "Starter Pack", Credits = 500 };
            var expected = new AICreditPackResponse { Id = 1, Name = "Starter Pack", Credits = 500 };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<AICreditPackResponse>(entity)).Returns(expected);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Handle_ShouldThrowAppException_WhenPackNotFound()
        {
            // Arrange
            var query = new GetAICreditPackByIdQuery(99);
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((AICreditPack?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(exception!.StatusCode, Is.EqualTo(CustomCode.AICreditPackNotFound));
        }

        #endregion

    }
}