using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Commands.ArchiveSchool;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Commands.ArchiveSchool
{
    [TestFixture]
    public class ArchiveSchoolCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ISchoolRepository> _schoolRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IAuthService> _authServiceMock = null!;
        private ArchiveSchoolCommandHandler _handler = null!;

        #region ArchiveSchoolCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<ISchoolRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _authServiceMock = new Mock<IAuthService>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
                .Returns(_schoolRepoMock.Object);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);

            _handler = new ArchiveSchoolCommandHandler(_unitOfWorkMock.Object, _authServiceMock.Object);
        }

        #endregion

        #region ArchiveSchoolCommandHandler Tests

        [Test]
        public async Task Handle_ShouldArchiveSchoolAndUsers_WhenSchoolIsActive()
        {
            // Arrange
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active
            };

            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = Guid.NewGuid(), Status = EntityStatus.Active },
                new ApplicationUser { Id = Guid.NewGuid(), Status = EntityStatus.Active }
            };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(school.Id))
                .ReturnsAsync(school);

            _userRepoMock.Setup(r => r.GetUsersBySchoolIdAsync(school.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            _unitOfWorkMock.Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Setup IAuthService for each user
            foreach (var user in users)
            {
                _authServiceMock.Setup(x => x.InvalidateAllUserTokensAsync(user.Id.ToString()))
                    .Returns(Task.CompletedTask);
            }

            var command = new ArchiveSchoolCommand(school.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.EqualTo(Unit.Value));
                Assert.That(school.Status, Is.EqualTo(EntityStatus.Archived));
                Assert.That(school.LastModifiedAt, Is.Not.EqualTo(default));
            });

            foreach (var user in users)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(user.Status, Is.EqualTo(EntityStatus.Inactive));
                    Assert.That(user.LockoutEnabled, Is.True);
                    Assert.That(user.LockoutEnd, Is.EqualTo(DateTimeOffset.MaxValue));
                });
            }

            _schoolRepoMock.Verify(r => r.Update(It.IsAny<School>()), Times.Once);
            _userRepoMock.Verify(r => r.Update(It.IsAny<ApplicationUser>()), Times.Exactly(users.Count));

            // Verify that InvalidateAllUserTokensAsync was called for each user
            foreach (var user in users)
            {
                _authServiceMock.Verify(x => x.InvalidateAllUserTokensAsync(user.Id.ToString()), Times.Once);
            }

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrow_WhenSchoolNotFound()
        {
            // Arrange
            _schoolRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((School?)null);

            var command = new ArchiveSchoolCommand(99);

            // Act & Assert
            Assert.ThrowsAsync<SchoolNotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public void Handle_ShouldThrow_WhenSchoolAlreadyArchived()
        {
            // Arrange
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Archived
            };

            _schoolRepoMock.Setup(r => r.GetByIdAsync(school.Id))
                .ReturnsAsync(school);

            var command = new ArchiveSchoolCommand(school.Id);

            // Act & Assert
            Assert.ThrowsAsync<SchoolAlreadyArchivedException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        #endregion

    }
}