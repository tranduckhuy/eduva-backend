using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Interfaces.Repositories;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Queries
{
    [TestFixture]
    public class GetSchoolUserLimitQueryHandlerTests
    {
        private Mock<ISchoolRepository> _schoolRepoMock = default!;
        private GetSchoolUserLimitQueryHandler _handler = default!;

        #region Setup

        [SetUp]
        public void SetUp()
        {
            _schoolRepoMock = new Mock<ISchoolRepository>();
            _handler = new GetSchoolUserLimitQueryHandler(_schoolRepoMock.Object);
        }

        #endregion

        #region Tests

        [Test]
        public async Task Handle_Should_Return_CorrectUserLimitInfo()
        {
            // Arrange
            var executorId = Guid.NewGuid();
            var expectedCurrent = 15;
            var expectedMax = 120;

            _schoolRepoMock
                .Setup(x => x.GetUserLimitInfoByUserIdAsync(executorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedCurrent, expectedMax));

            var query = new GetSchoolUserLimitQuery(executorId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.CurrentUserCount, Is.EqualTo(expectedCurrent));
                Assert.That(result.MaxUsers, Is.EqualTo(expectedMax));
            });

            _schoolRepoMock.Verify(x => x.GetUserLimitInfoByUserIdAsync(executorId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}