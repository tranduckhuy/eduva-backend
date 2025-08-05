using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class DeleteFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _folderLessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterialQuestion, int>> _lessonMaterialQuestionsRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterialApproval, int>> _lessonMaterialsApproveRepoMock = null!;
        private DeleteFolderHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _folderRepoMock = new Mock<IFolderRepository>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _lessonMaterialQuestionsRepoMock = new Mock<IGenericRepository<LessonMaterialQuestion, int>>();
            _lessonMaterialsApproveRepoMock = new Mock<IGenericRepository<LessonMaterialApproval, int>>();


            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>())
                .Returns(_folderLessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialQuestion, int>())
                .Returns(_lessonMaterialQuestionsRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialApproval, int>())
                .Returns(_lessonMaterialsApproveRepoMock.Object);

            _handler = new DeleteFolderHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Found()
        {
            var command = new DeleteFolderCommand { Id = Guid.NewGuid(), CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(command.Id)).ReturnsAsync((Folder?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Has_No_Permission()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = Guid.NewGuid() };
            var command = new DeleteFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);

            // Mock HasPermissionToUpdateFolder to return false by making user null
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_Should_Return_True_When_Folder_Already_Deleted()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Deleted };
            var command = new DeleteFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);

            // Mock permission
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Archived()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active };
            var command = new DeleteFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);

            // Mock permission
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderShouldBeArchivedBeforeDelete));
        }

        [Test]
        public async Task Handle_Should_Delete_Class_Folder_And_Links()
        {
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Archived,
                OwnerType = OwnerType.Class,
                ClassId = classId,
                FolderLessonMaterials = new List<FolderLessonMaterial>
        {
            new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = Guid.NewGuid() },
            new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = Guid.NewGuid() }
        }
            };

            var classroom = new Classroom
            {
                Id = classId,
                Status = EntityStatus.Active,
                SchoolId = 1
            };

            var command = new DeleteFolderCommand { Id = folderId, CurrentUserId = Guid.NewGuid() };

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folderId)).ReturnsAsync(folder);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.Remove(It.IsAny<FolderLessonMaterial>()));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            _folderLessonMaterialRepoMock.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()), Times.Once);
            _folderRepoMock.Verify(r => r.Remove(folder), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_Should_Delete_Personal_Folder_And_LessonMaterials_If_Only_Used_Here()
        {
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = Guid.NewGuid() };
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Archived,
                OwnerType = OwnerType.Personal,
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = lessonMaterialId, LessonMaterial = lessonMaterial }
                }
            };
            var command = new DeleteFolderCommand { Id = folderId, CurrentUserId = Guid.NewGuid() };

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.Remove(It.IsAny<FolderLessonMaterial>()));
            _folderLessonMaterialRepoMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
            _lessonMaterialRepoMock.Setup(r => r.Remove(lessonMaterial));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _lessonMaterialsApproveRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialApproval>());
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            _folderLessonMaterialRepoMock.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()), Times.Once);
            _lessonMaterialRepoMock.Verify(r => r.Update(lessonMaterial), Times.Once);
            _folderRepoMock.Verify(r => r.Remove(folder), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_Should_Delete_Personal_Folder_But_Not_LessonMaterial_If_Used_Elsewhere()
        {
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = Guid.NewGuid() };
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Archived,
                OwnerType = OwnerType.Personal,
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = lessonMaterialId, LessonMaterial = lessonMaterial }
                }
            };
            var command = new DeleteFolderCommand { Id = folderId, CurrentUserId = Guid.NewGuid() };

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.Remove(It.IsAny<FolderLessonMaterial>()));
            _folderLessonMaterialRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _folderRepoMock.Setup(r => r.Remove(folder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            _folderLessonMaterialRepoMock.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()), Times.Once);
            _lessonMaterialRepoMock.Verify(r => r.Remove(It.IsAny<LessonMaterial>()), Times.Never);
            _folderRepoMock.Verify(r => r.Remove(folder), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.True);
        }

        [Test]
        public void Handle_Should_Throw_When_Exception_Occurs()
        {
            var folderId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Archived,
                OwnerType = OwnerType.Personal,
                UserId = Guid.NewGuid(),
                FolderLessonMaterials = new List<FolderLessonMaterial>
        {
            new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = Guid.NewGuid() }
        }
            };
            var command = new DeleteFolderCommand { Id = folderId, CurrentUserId = Guid.NewGuid() };

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>())).Throws(new Exception("DB error"));

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderDeleteFailed));
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnTrue_ForPersonalFolderOwner()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Personal, UserId = userId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { Role.Teacher.ToString() });

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.True);
        }


        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnFalse_ForPersonalFolderNotOwner()
        {
            var ownerId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Personal, UserId = ownerId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { Role.Teacher.ToString() });

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnTrue_ForSystemAdmin()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Personal, UserId = Guid.NewGuid() };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { Role.SystemAdmin.ToString() });

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.True);
        }


        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnTrue_ForClassTeacher()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Class, ClassId = classId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { Role.Teacher.ToString() });

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 });

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnTrue_ForSchoolAdminOfClassSchool()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Class, ClassId = classId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId, SchoolId = 10 });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { Role.SchoolAdmin.ToString() });

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 10 });

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnFalse_ForClassFolder_UserNotTeacherOrSchoolAdmin()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Class, ClassId = classId };

            var user = new ApplicationUser { Id = userId, SchoolId = 2 };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { Role.Teacher.ToString() });

            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_ShouldReturnFalse_ForClassFolder_ClassroomNotFound()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { OwnerType = OwnerType.Class, ClassId = classId };

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { Role.SchoolAdmin.ToString() });

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var result = await _handler.HasPermissionToUpdateFolder(folder, userId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task Handle_Should_Not_Update_LessonMaterial_If_Already_Deleted()
        {
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Deleted };
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Archived,
                OwnerType = OwnerType.Personal,
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new() { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = lessonMaterialId, LessonMaterial = lessonMaterial }
                }
            };
            var command = new DeleteFolderCommand { Id = folderId, CurrentUserId = Guid.NewGuid() };

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.Remove(It.IsAny<FolderLessonMaterial>()));
            _folderLessonMaterialRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
            _lessonMaterialRepoMock.Setup(r => r.Remove(lessonMaterial));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _lessonMaterialsApproveRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialApproval>());
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Update(It.IsAny<LessonMaterial>()), Times.Never);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_Should_Remove_Questions_And_Approvals_Of_OnlyUsedLessonMaterial()
        {
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active };
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Archived,
                OwnerType = OwnerType.Personal,
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new() { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = lessonMaterialId, LessonMaterial = lessonMaterial }
                }
            };
            var command = new DeleteFolderCommand { Id = folderId, CurrentUserId = Guid.NewGuid() };

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(new ApplicationUser { Id = command.CurrentUserId });
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.Remove(It.IsAny<FolderLessonMaterial>()));
            _folderLessonMaterialRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
            _lessonMaterialRepoMock.Setup(r => r.Remove(lessonMaterial));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var questions = new List<LessonMaterialQuestion>
            {
                new() { Id = Guid.NewGuid(), LessonMaterialId = lessonMaterialId },
                new() { Id = Guid.NewGuid(), LessonMaterialId = Guid.NewGuid() }
            };
            var approvals = new List<LessonMaterialApproval>
            {
                new() { Id = Guid.NewGuid(), LessonMaterialId = lessonMaterialId },
                new() { Id = Guid.NewGuid(), LessonMaterialId = Guid.NewGuid() }
            };
            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(questions);
            _lessonMaterialsApproveRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(approvals);

            _lessonMaterialQuestionsRepoMock.Setup(r => r.RemoveRange(It.Is<IEnumerable<LessonMaterialQuestion>>(qs => qs.All(q => q.LessonMaterialId == lessonMaterialId))));
            _lessonMaterialsApproveRepoMock.Setup(r => r.RemoveRange(It.Is<IEnumerable<LessonMaterialApproval>>(asv => asv.All(a => a.LessonMaterialId == lessonMaterialId))));

            var result = await _handler.Handle(command, CancellationToken.None);

            _lessonMaterialQuestionsRepoMock.Verify(r => r.RemoveRange(It.Is<IEnumerable<LessonMaterialQuestion>>(qs => qs.All(q => q.LessonMaterialId == lessonMaterialId))), Times.Once);
            _lessonMaterialsApproveRepoMock.Verify(r => r.RemoveRange(It.Is<IEnumerable<LessonMaterialApproval>>(asv => asv.All(a => a.LessonMaterialId == lessonMaterialId))), Times.Once);
            Assert.That(result, Is.True);
        }
    }
}