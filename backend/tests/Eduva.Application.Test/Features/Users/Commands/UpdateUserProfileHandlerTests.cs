using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class UpdateUserProfileHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IUserRepository> _userRepositoryMock = default!;
        private UpdateUserProfileHandler _handler = default!;

        #region UpdateUserProfileHandler Setup

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock
                .Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepositoryMock.Object);

            _handler = new UpdateUserProfileHandler(_unitOfWorkMock.Object);

            // Initialize AutoMapper
            _ = AppMapper<AppMappingProfile>.Mapper;
        }

        #endregion

        #region UpdateUserProfileHandler Tests

        [Test]
        public async Task Handle_ShouldUpdateUserAndReturnResponse_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateUserProfileCommand
            {
                UserId = userId,
                FullName = "Updated Name",
                PhoneNumber = "9876543210",
                AvatarUrl = "https://example.com/new-avatar.jpg"
            };

            var existingUser = new ApplicationUser
            {
                Id = userId,
                FullName = "Original Name",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/old-avatar.jpg",
                SchoolId = 1,
                School = new School
                {
                    Id = 1,
                    Name = "Test School",
                    ContactEmail = "contact@school.edu",
                    ContactPhone = "123456789",
                    WebsiteUrl = "https://school.edu"
                }
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _unitOfWorkMock
                .Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(userId));
                Assert.That(result.FullName, Is.EqualTo(command.FullName));
                Assert.That(result.PhoneNumber, Is.EqualTo(command.PhoneNumber));
                Assert.That(result.AvatarUrl, Is.EqualTo(command.AvatarUrl));
                Assert.That(result.Email, Is.EqualTo(existingUser.Email));

                Assert.That(result.School, Is.Not.Null);
                Assert.That(result.School!.Id, Is.EqualTo(existingUser.SchoolId));
            });

            // Verify the user properties were updated
            Assert.Multiple(() =>
            {
                Assert.That(existingUser.FullName, Is.EqualTo(command.FullName));
                Assert.That(existingUser.PhoneNumber, Is.EqualTo(command.PhoneNumber));
                Assert.That(existingUser.AvatarUrl, Is.EqualTo(command.AvatarUrl));
            });

            _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _userRepositoryMock.Verify(r => r.Update(existingUser), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldUpdateOnlyProvidedFields_WhenSomeFieldsAreEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateUserProfileCommand
            {
                UserId = userId,
                FullName = "Updated Name",
                PhoneNumber = string.Empty, // This should not update since it's empty
                AvatarUrl = string.Empty    // This should not update since it's empty
            };

            var existingUser = new ApplicationUser
            {
                Id = userId,
                FullName = "Original Name",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                SchoolId = 1
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _unitOfWorkMock
                .Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.FullName, Is.EqualTo(command.FullName));
                Assert.That(result.PhoneNumber, Is.EqualTo("1234567890")); // Should remain original
                Assert.That(result.AvatarUrl, Is.EqualTo("https://example.com/avatar.jpg")); // Should remain original
            });            // Verify only FullName was updated
            Assert.Multiple(() =>
            {
                Assert.That(existingUser.FullName, Is.EqualTo(command.FullName));
                Assert.That(existingUser.PhoneNumber, Is.EqualTo("1234567890"));
                Assert.That(existingUser.AvatarUrl, Is.EqualTo("https://example.com/avatar.jpg"));
            });
        }

        [Test]
        public void Handle_ShouldThrowUserNotExistsException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateUserProfileCommand
            {
                UserId = userId,
                FullName = "Updated Name",
                PhoneNumber = "9876543210"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UserNotExistsException>(
                () => _handler.Handle(command, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _userRepositoryMock.Verify(r => r.Update(It.IsAny<ApplicationUser>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task Handle_ShouldNotUpdateAnyFields_WhenAllFieldsAreEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateUserProfileCommand
            {
                UserId = userId,
                FullName = null,
                PhoneNumber = string.Empty,
                AvatarUrl = string.Empty
            };

            var existingUser = new ApplicationUser
            {
                Id = userId,
                FullName = "Original Name",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                SchoolId = 1
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _unitOfWorkMock
                .Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.FullName, Is.EqualTo("Original Name"));
                Assert.That(result.PhoneNumber, Is.EqualTo("1234567890"));
                Assert.That(result.AvatarUrl, Is.EqualTo("https://example.com/avatar.jpg"));
            });

            // Verify no fields were changed
            Assert.Multiple(() =>
            {
                Assert.That(existingUser.FullName, Is.EqualTo("Original Name"));
                Assert.That(existingUser.PhoneNumber, Is.EqualTo("1234567890"));
                Assert.That(existingUser.AvatarUrl, Is.EqualTo("https://example.com/avatar.jpg"));
            });
        }

        #endregion
    }
}
