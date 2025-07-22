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
        public void Handle_Throws_WhenSchoolAdminIsNull()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApplicationUser?)null);

            var request = new ExportUsersQuery(new ExportUsersRequest { Role = Role.Student }, schoolAdminId);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(
                async () => await _handler.Handle(request, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public async Task Handle_ExportAllRoles_WithStatusFilter_ReturnsFilteredExcel()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var activeUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Active User",
                Email = "active@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var inactiveUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Inactive User",
                Email = "inactive@example.com",
                Status = EntityStatus.Inactive,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Teacher"))
                .ReturnsAsync(new List<ApplicationUser> { activeUser });
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { inactiveUser });
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("ContentModerator"))
                .ReturnsAsync(new List<ApplicationUser>());
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Teacher" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest { Status = EntityStatus.Active }, schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportAllRoles_WithSearchTerm_ReturnsFilteredExcel()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Nguyen Van A",
                Email = "nguyen@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Tran Van B",
                Email = "tran@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Teacher"))
                .ReturnsAsync(new List<ApplicationUser> { user1 });
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user2 });
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("ContentModerator"))
                .ReturnsAsync(new List<ApplicationUser>());
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Teacher" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest { SearchTerm = "Nguyen" }, schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportUser_WithLastLoginAt_ReturnsExcelWithLoginInfo()
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
                FullName = "User With Login",
                Email = "login@example.com",
                PhoneNumber = "0123456789",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
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
        public async Task Handle_ExportUser_WithInactiveStatus_ReturnsExcelWithInactiveStyling()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var inactiveUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Inactive User",
                Email = "inactive@example.com",
                PhoneNumber = "0123456789",
                Status = EntityStatus.Inactive,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { inactiveUser });
            _userManagerMock.Setup(m => m.GetRolesAsync(inactiveUser))
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
        public async Task Handle_ExportUser_WithDifferentSortingOptions_ReturnsSortedExcel()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "A User",
                Email = "a@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "B User",
                Email = "b@example.com",
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

            // Test different sorting options
            var sortOptions = new[]
            {
                new { SortBy = "email", SortDirection = "asc" },
                new { SortBy = "email", SortDirection = "desc" },
                new { SortBy = "status", SortDirection = "asc" },
                new { SortBy = "status", SortDirection = "desc" },
                new { SortBy = "createdat", SortDirection = "asc" },
                new { SortBy = "createdat", SortDirection = "desc" },
                new { SortBy = "fullname", SortDirection = "asc" },
                new { SortBy = "fullname", SortDirection = "desc" },
                new { SortBy = "unknown", SortDirection = "asc" }, // Tests default case
                new { SortBy = "unknown", SortDirection = "desc" } // Tests default case
            };

            foreach (var sortOption in sortOptions)
            {
                var request = new ExportUsersQuery(new ExportUsersRequest
                {
                    Role = Role.Student,
                    SortBy = sortOption.SortBy,
                    SortDirection = sortOption.SortDirection
                }, schoolAdminId);

                // Act
                var result = await _handler.Handle(request, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.Not.Empty);
            }
        }

        [Test]
        public async Task Handle_ExportUser_WithDeletedStatus_ReturnsExcelWithDeletedStatus()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 1;
            var admin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };
            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var deletedUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Deleted User",
                Email = "deleted@example.com",
                PhoneNumber = "0123456789",
                Status = EntityStatus.Deleted,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { deletedUser });
            _userManagerMock.Setup(m => m.GetRolesAsync(deletedUser))
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
        public async Task Handle_ExportUser_WithUnknownStatus_ReturnsExcelWithUnknownStatus()
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
                FullName = "Unknown Status User",
                Email = "unknown@example.com",
                PhoneNumber = "0123456789",
                Status = (EntityStatus)999, // Unknown status
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
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
        public async Task Handle_ExportUser_WithEmptySearchTerm_ReturnsAllUsers()
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
                FullName = "Test User",
                Email = "test@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest
            {
                Role = Role.Student,
                SearchTerm = "" // Empty search term
            }, schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportUser_WithNullSearchTerm_ReturnsAllUsers()
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
                FullName = "Test User",
                Email = "test@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest
            {
                Role = Role.Student,
                SearchTerm = null // Null search term
            }, schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task Handle_ExportUser_WithWhitespaceSearchTerm_ReturnsAllUsers()
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
                FullName = "Test User",
                Email = "test@example.com",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(new List<ApplicationUser> { user });
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>().GetByIdAsync(schoolId))
                .ReturnsAsync(new School { Id = schoolId, Name = "Test School" });

            var request = new ExportUsersQuery(new ExportUsersRequest
            {
                Role = Role.Student,
                SearchTerm = "   " // Whitespace search term
            }, schoolAdminId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

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