using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.RemoveMaterialsFromFolder
{
    [TestFixture]
    public class RemoveMaterialsFromFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _folderLessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterialQuestion, int>> _lessonMaterialQuestionsRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterialApproval, int>> _lessonMaterialApprovalRepoMock = null!;

        private RemoveMaterialsFromFolderHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _lessonMaterialQuestionsRepoMock = new Mock<IGenericRepository<LessonMaterialQuestion, int>>();
            _lessonMaterialApprovalRepoMock = new Mock<IGenericRepository<LessonMaterialApproval, int>>();


            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>())
                .Returns(_folderLessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>()).Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialQuestion, int>()).Returns(_lessonMaterialQuestionsRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialApproval, int>()).Returns(_lessonMaterialApprovalRepoMock.Object);

            _handler = new RemoveMaterialsFromFolderHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_Should_Remove_Materials_When_Valid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SystemAdmin) };

            var folderMaterial = new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = materialId };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderMaterial });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _lessonMaterialApprovalRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialApproval>());


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _folderLessonMaterialRepoMock.Verify(r => r.Remove(It.Is<FolderLessonMaterial>(f => f.Id == folderMaterial.Id)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Found()
        {
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = Guid.NewGuid(),
                ClassId = Guid.NewGuid(),
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(command.FolderId)).ReturnsAsync((Folder?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Belongs_To_Class()
        {
            // Arrange a CLASS folder that the user does not own/teach/etc
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var wrongUserId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                ClassId = classId,
                OwnerType = OwnerType.Class
            };

            var user = new ApplicationUser { Id = wrongUserId, SchoolId = 1 };
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folder.Id,
                ClassId = classId,
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = wrongUserId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(command.CurrentUserId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            // Classroom exists but does NOT belong to user as teacher or school admin
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 2 };
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            // Act + Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Exists()
        {
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(command.CurrentUserId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_Material_Not_Found_In_Folder()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SystemAdmin) };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial>()); // Không có material
            // Không cần setup CommitAsync vì sẽ throw trước

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotFoundInFolder));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Authorized()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 2 }; // khác teacherId và schoolId
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial>());

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }


        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public async Task Handle_Should_Allow_When_Teacher_Of_Class()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };
            var folderMaterial = new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = materialId };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderMaterial });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _lessonMaterialApprovalRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialApproval>());
            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_Should_Allow_When_SchoolAdmin_Of_Same_School()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SchoolAdmin) };
            var folderMaterial = new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = materialId };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderMaterial });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _lessonMaterialApprovalRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialApproval>());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_Should_Only_Remove_Matching_Materials()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var otherFolderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId1 = Guid.NewGuid();
            var materialId2 = Guid.NewGuid();
            var materialId3 = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };

            var matchingMaterial = new FolderLessonMaterial
            {
                Id = Guid.NewGuid(),
                FolderId = folderId,
                LessonMaterialId = materialId1
            };

            var wrongFolderMaterial = new FolderLessonMaterial
            {
                Id = Guid.NewGuid(),
                FolderId = otherFolderId,
                LessonMaterialId = materialId1
            };

            var wrongMaterialIdMaterial = new FolderLessonMaterial
            {
                Id = Guid.NewGuid(),
                FolderId = folderId,
                LessonMaterialId = materialId3
            };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId1, materialId2 },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> {
            matchingMaterial,
            wrongFolderMaterial,
            wrongMaterialIdMaterial
                });

            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _lessonMaterialQuestionsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialQuestion>());
            _lessonMaterialApprovalRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterialApproval>());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);

            _folderLessonMaterialRepoMock.Verify(r => r.Remove(It.Is<FolderLessonMaterial>(f => f.Id == matchingMaterial.Id)), Times.Once);
            _folderLessonMaterialRepoMock.Verify(r => r.Remove(It.Is<FolderLessonMaterial>(f => f.Id == wrongFolderMaterial.Id)), Times.Never);
            _folderLessonMaterialRepoMock.Verify(r => r.Remove(It.Is<FolderLessonMaterial>(f => f.Id == wrongMaterialIdMaterial.Id)), Times.Never);

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}