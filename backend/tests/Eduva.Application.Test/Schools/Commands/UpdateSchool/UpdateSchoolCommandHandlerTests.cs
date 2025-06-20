using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Commands.UpdateSchool;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Schools.Commands.UpdateSchool
{
    [TestFixture]
    public class UpdateSchoolCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;
        private UpdateSchoolCommandHandler _handler = null!;

        #region UpdateSchoolCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>())
                .Returns(_schoolRepoMock.Object);

            _handler = new UpdateSchoolCommandHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region UpdateSchoolCommandHandler Tests

        [Test]
        public async Task Handle_ShouldUpdateSchoolAndReturnUnit_WhenSchoolExists()
        {
            // Arrange
            var command = new UpdateSchoolCommand
            {
                Id = 1,
                Name = "Updated School",
                ContactEmail = "email@eduva.vn",
                ContactPhone = "0123456789",
                Address = "New Address",
                WebsiteUrl = "https://eduva.vn"
            };

            var existingSchool = new School { Id = command.Id };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.Id))
                .ReturnsAsync(existingSchool);

            _unitOfWorkMock.Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _schoolRepoMock.Verify(r => r.Update(It.Is<School>(s =>
                s.Name == command.Name &&
                s.ContactEmail == command.ContactEmail &&
                s.ContactPhone == command.ContactPhone &&
                s.Address == command.Address &&
                s.WebsiteUrl == command.WebsiteUrl
            )), Times.Once);

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowSchoolNotFoundException_WhenSchoolDoesNotExist()
        {
            // Arrange
            var command = new UpdateSchoolCommand { Id = 99 };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.Id))
                .ReturnsAsync((School?)null);

            // Act & Assert
            Assert.ThrowsAsync<SchoolNotFoundException>(async () =>
            {
                await _handler.Handle(command, CancellationToken.None);
            });

            _schoolRepoMock.Verify(r => r.Update(It.IsAny<School>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        #endregion

    }
}