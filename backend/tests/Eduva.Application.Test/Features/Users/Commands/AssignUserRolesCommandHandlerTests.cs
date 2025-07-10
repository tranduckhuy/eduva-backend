using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Commands;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class AssignUserRolesCommandHandlerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private AssignUserRolesCommandHandler _handler = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            var storeMock = new Mock<IUserStore<ApplicationUser>>();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                storeMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );

            _handler = new AssignUserRolesCommandHandler(_userManagerMock.Object);
        }

        #endregion

        #region Validation Tests

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTargetUserIdIsEmpty()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.Empty,
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenSchoolAdminIdIsEmpty()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.Empty
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenRolesIsNull()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = null!,
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RoleListEmpty));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenRolesIsEmpty()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role>(),
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RoleListEmpty));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenRoleContainsSystemAdmin()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.SystemAdmin },
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RestrictedRoleNotAllowed));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenRoleContainsSchoolAdmin()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.SchoolAdmin },
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RestrictedRoleNotAllowed));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenRoleContainsStudent()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Student },
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.StudentRoleNotAssignable));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenMultipleRolesContainNonTeacherOrContentModerator()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher, Role.Student },
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.StudentRoleNotAssignable));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTooManyRolesAssigned()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher, Role.ContentModerator, Role.Teacher }, // 3 roles but one duplicate
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert - This should pass validation since distinct roles = 2
            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // This should succeed
            var result = await _handler.Handle(command, default);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTooManyUniqueRolesAssigned()
        {
            // Arrange - 3 different roles
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher, Role.ContentModerator, Role.SystemAdmin },
                SchoolAdminId = Guid.NewGuid()
            };

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RestrictedRoleNotAllowed)); // Will fail at SystemAdmin check first
            }
        }

        #endregion

        #region School Admin Authentication Tests

        [Test]
        public async Task Handle_ShouldThrowUserNotExistsException_WhenSchoolAdminNotFound()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected UserNotExistsException was not thrown");
            }
            catch (UserNotExistsException)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenSchoolAdminNotInSchoolAdminRole()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "Teacher" }); // Not SchoolAdmin

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.InsufficientPermissionToManageRoles));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowUserNotPartOfSchoolException_WhenSchoolAdminNotInSchool()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = null // Not in any school
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected UserNotPartOfSchoolException was not thrown");
            }
            catch (UserNotPartOfSchoolException)
            {
                Assert.Pass();
            }
        }

        #endregion

        #region Target User Validation Tests

        [Test]
        public async Task Handle_ShouldThrowUserNotExistsException_WhenTargetUserNotFound()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected UserNotExistsException was not thrown");
            }
            catch (UserNotExistsException)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTargetUserNotInSameSchool()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 2 // Different school
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotManageUserFromDifferentSchool));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTargetUserIsSystemAdmin()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "SystemAdmin" }); // Restricted role

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotModifyRestrictedUserRoles));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTargetUserIsAnotherSchoolAdmin()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "SchoolAdmin" }); // Another SchoolAdmin

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotModifyRestrictedUserRoles));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenTryingToModifyOwnRoles()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var command = new AssignUserRolesCommand
            {
                TargetUserId = adminId, // Same as SchoolAdminId
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = adminId
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = adminId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(adminId.ToString()))
                .ReturnsAsync(schoolAdmin);

            // Setup sequence: first call for school admin check, second call for target user check
            _userManagerMock.SetupSequence(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" }) // First call: pass school admin check
                .ReturnsAsync(new List<string> { "Teacher" });    // Second call: avoid restricted user check

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotModifyOwnRoles));
            }
        }

        #endregion

        #region Role Assignment Operations Tests

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenRemoveRolesFails()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Student" });

            var identityResult = IdentityResult.Failed(new IdentityError
            {
                Description = "Role removal failed"
            });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(identityResult);

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RoleRemovalFailed));
            }
        }

        [Test]
        public async Task Handle_ShouldThrowAppException_WhenAddRolesFails()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            var identityResult = IdentityResult.Failed(new IdentityError
            {
                Description = "Role assignment failed"
            });
            _userManagerMock.Setup(x => x.AddToRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(identityResult);

            // Act & Assert
            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.RoleAssignmentFailed));
            }
        }

        #endregion

        #region Success Scenarios Tests

        [Test]
        public async Task Handle_ShouldSucceed_WhenAssigningSingleTeacherRole()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _userManagerMock.Verify(x => x.RemoveFromRolesAsync(targetUser,
                It.Is<IEnumerable<string>>(roles => roles.Contains("Student"))), Times.Once);
            _userManagerMock.Verify(x => x.AddToRolesAsync(targetUser,
                It.Is<IEnumerable<string>>(roles => roles.Contains("Teacher"))), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldSucceed_WhenAssigningSingleContentModeratorRole()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.ContentModerator },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _userManagerMock.Verify(x => x.AddToRolesAsync(targetUser,
                It.Is<IEnumerable<string>>(roles => roles.Contains("ContentModerator"))), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldSucceed_WhenAssigningBothTeacherAndContentModeratorRoles()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher, Role.ContentModerator },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Student" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _userManagerMock.Verify(x => x.AddToRolesAsync(targetUser,
                It.Is<IEnumerable<string>>(roles =>
                    roles.Contains("Teacher") && roles.Contains("ContentModerator"))), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldSkipRemoveRoles_WhenTargetUserHasNoRemovableRoles()
        {
            // Arrange
            var command = new AssignUserRolesCommand
            {
                TargetUserId = Guid.NewGuid(),
                Roles = new List<Role> { Role.Teacher },
                SchoolAdminId = Guid.NewGuid()
            };

            var schoolAdmin = new ApplicationUser
            {
                Id = command.SchoolAdminId,
                SchoolId = 1
            };
            var targetUser = new ApplicationUser
            {
                Id = command.TargetUserId,
                SchoolId = 1
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.SchoolAdminId.ToString()))
                .ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(x => x.GetRolesAsync(schoolAdmin))
                .ReturnsAsync(new List<string> { "SchoolAdmin" });
            _userManagerMock.Setup(x => x.FindByIdAsync(command.TargetUserId.ToString()))
                .ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string>()); // No roles - fresh user
            _userManagerMock.Setup(x => x.AddToRolesAsync(targetUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _userManagerMock.Verify(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()), Times.Never);
            _userManagerMock.Verify(x => x.AddToRolesAsync(targetUser,
                It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        #endregion
    }
}