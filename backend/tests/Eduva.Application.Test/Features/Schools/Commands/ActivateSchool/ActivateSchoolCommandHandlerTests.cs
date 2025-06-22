using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Commands.ActivateSchool;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Commands.ActivateSchool
{
    [TestFixture]
    public class ActivateSchoolCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ISchoolRepository> _schoolRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private ActivateSchoolCommandHandler _handler = null!;

        #region ActivateSchoolCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<ISchoolRepository>();
            _userRepoMock = new Mock<IUserRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
                .Returns(_schoolRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);

            _handler = new ActivateSchoolCommandHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region ActivateSchoolCommandHandler Tests

        [Test]
        public async Task Handle_ShouldActivateSchoolAndUsers_WhenSchoolIsInactive()
        {
            // Arrange
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Inactive
            };

            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = Guid.NewGuid(), Status = EntityStatus.Inactive },
                new ApplicationUser { Id = Guid.NewGuid(), Status = EntityStatus.Inactive }
            };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(school.Id))
                .ReturnsAsync(school);

            _userRepoMock.Setup(r => r.GetUsersBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            _unitOfWorkMock.Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            var command = new ActivateSchoolCommand(school.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.EqualTo(Unit.Value));
                Assert.That(school.Status, Is.EqualTo(EntityStatus.Active));
                Assert.That(school.LastModifiedAt, Is.Not.EqualTo(default));
            });

            foreach (var user in users)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(user.Status, Is.EqualTo(EntityStatus.Active));
                    Assert.That(user.LockoutEnabled, Is.False);
                    Assert.That(user.LockoutEnd, Is.Null);
                });
            }

            _schoolRepoMock.Verify(r => r.Update(It.IsAny<School>()), Times.Once);
            _userRepoMock.Verify(r => r.Update(It.IsAny<ApplicationUser>()), Times.Exactly(users.Count));
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrow_WhenSchoolDoesNotExist()
        {
            // Arrange
            _schoolRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((School?)null);

            var command = new ActivateSchoolCommand(1);

            // Act & Assert
            Assert.ThrowsAsync<SchoolNotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public void Handle_ShouldThrow_WhenSchoolAlreadyActive()
        {
            // Arrange
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active
            };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(school.Id))
                .ReturnsAsync(school);

            var command = new ActivateSchoolCommand(school.Id);

            // Act & Assert
            Assert.ThrowsAsync<SchoolAlreadyActiveException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        #endregion

    }
}