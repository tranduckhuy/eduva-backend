using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetFolderByIdHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IClassroomRepository> _classRepoMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetFolderByIdHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IFolderRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _classRepoMock = new Mock<IClassroomRepository>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new GetFolderByIdHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Found()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync((Folder?)null);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Personal_Folder_Not_Owner()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Personal,
                UserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Folder_ClassId_Null()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = null
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Classroom_Not_Found()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = classId
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Found_For_Class_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = classId
            };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Authorized_For_Class_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = classId
            };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 2 };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_Should_Throw_When_OwnerType_Invalid()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = (OwnerType)999 // invalid
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);

            var query = new GetFolderByIdQuery(folderId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_Should_Return_Response_For_Personal_Owner()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Personal,
                UserId = userId
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int> { { folderId, 3 } });

            var query = new GetFolderByIdQuery(folderId, userId);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(folderId));
                Assert.That(result.CountLessonMaterial, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task Handle_Should_Return_Response_For_Class_Teacher()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = classId
            };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int> { { folderId, 2 } });

            var query = new GetFolderByIdQuery(folderId, userId);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(folderId));
                Assert.That(result.CountLessonMaterial, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task Handle_Should_Return_Response_For_Class_Same_School()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = classId
            };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int> { { folderId, 1 } });

            var query = new GetFolderByIdQuery(folderId, userId);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(folderId));
                Assert.That(result.CountLessonMaterial, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task Handle_Should_Return_Response_For_Class_User_SchoolId_Null()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                OwnerType = OwnerType.Class,
                ClassId = classId
            };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = null };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());

            var query = new GetFolderByIdQuery(folderId, userId);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(folderId));
                Assert.That(result.CountLessonMaterial, Is.EqualTo(0));
            });
        }
    }
}