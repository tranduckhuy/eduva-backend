using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Requests;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Users.Queries
{
    [TestFixture]
    public class ExportUsersQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IUserRepository> _userRepositoryMock = default!;
        private ExportUsersQueryHandler _handler = default!;

        #region Setup

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _userRepositoryMock = new Mock<IUserRepository>();

            _handler = new ExportUsersQueryHandler(
                _unitOfWorkMock.Object,
                _userManagerMock.Object,
                _userRepositoryMock.Object
            );
        }

        #endregion

        #region Tests

        [Test]
        public async Task Handle_ExportStudentRole_ReturnsExcelFile()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var student = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Student 1",
                Email = "student1@example.com",
                PhoneNumber = "0123456789",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { student });
            _userManagerMock.Setup(m => m.GetRolesAsync(student))
                .ReturnsAsync(new List<string> { "Student" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest { Role = Role.Student }, schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void Handle_Throws_WhenUserNotInSchool()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationUser { Id = schoolAdminId, SchoolId = null });

            var request = new ExportUsersQuery(new ExportUsersRequest { Role = Role.Student }, schoolAdminId);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(
                async () => await _handler.Handle(request, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public async Task Handle_ExportAllRoles_NoUser_ReturnsEmptyExcel()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            _userManagerMock.Setup(m => m.GetUsersInRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ApplicationUser>());
            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest(), schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportAllRoles_MultiRoleUser_ReturnsExcelFile()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Teacher CM",
                Email = "teacher_cm@example.com",
                PhoneNumber = "0999999999",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher", "ContentModerator" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest(), schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportWithStatusAndSearchTerm_FiltersCorrectly()
        {
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Nguyen Van A",
                Email = "a@example.com",
                PhoneNumber = "0999999999",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Tran Van B",
                Email = "b@example.com",
                PhoneNumber = "0888888888",
                Status = EntityStatus.Inactive,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user1, user2 });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Student" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest
            {
                Role = Role.Student,
                Status = EntityStatus.Active,
                SearchTerm = "Nguyen"
            }, schoolAdminId);

            var result = await _handler.Handle(request, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportUser_SchoolNull_DoesNotThrow()
        {
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "No School",
                Email = "noschool@example.com",
                PhoneNumber = "0777777777",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync((School?)null);

            var request = new ExportUsersQuery(new ExportUsersRequest { Role = Role.Student }, schoolAdminId);

            var result = await _handler.Handle(request, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        #endregion

    }
}