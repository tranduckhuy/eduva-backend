using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetAllFoldersByClassIdHandlerTest
    {
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepoMock = null!;
        private GetAllFoldersByClassIdHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _folderRepoMock = new Mock<IFolderRepository>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _studentClassRepoMock = new Mock<IStudentClassRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepoMock.Object);

            _handler = new GetAllFoldersByClassIdHandler(
                _folderRepoMock.Object,
                _mapperMock.Object,
                _unitOfWorkMock.Object,
                _userManagerMock.Object
            );
        }

        [Test]
        public async Task Handle_Should_Return_Folders_For_Teacher()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };

            var folders = new List<Folder>
            {
                new() { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId },
                new() { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId }
            };

            var counts = folders.ToDictionary(f => f.Id, f => 5);

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);
            _mapperMock.Setup(m => m.Map<IEnumerable<FolderResponse>>(It.IsAny<IEnumerable<Folder>>()))
                .Returns((IEnumerable<Folder> fs) => fs.Select(f => new FolderResponse { Id = f.Id }).ToList());
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.All(r => r.CountLessonMaterial == 5));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Exists()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Exists()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public async Task Handle_Should_Return_Folders_For_SchoolAdmin()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };

            var folders = new List<Folder>
            {
                new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId }
            };

            var counts = folders.ToDictionary(f => f.Id, f => 2);

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);
            _mapperMock.Setup(m => m.Map<IEnumerable<FolderResponse>>(It.IsAny<IEnumerable<Folder>>()))
                .Returns((IEnumerable<Folder> fs) => fs.Select(f => new FolderResponse { Id = f.Id }).ToList());
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterial, Is.EqualTo(2));
        }

        [Test]
        public async Task Handle_Should_Return_Folders_For_Student_Enrolled()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };

            var folders = new List<Folder>
            {
                new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId }
            };

            var counts = folders.ToDictionary(f => f.Id, f => 1);

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(userId, classId)).ReturnsAsync(true);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);
            _mapperMock.Setup(m => m.Map<IEnumerable<FolderResponse>>(It.IsAny<IEnumerable<Folder>>()))
                .Returns((IEnumerable<Folder> fs) => fs.Select(f => new FolderResponse { Id = f.Id }).ToList());
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterial, Is.EqualTo(1));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_If_User_Has_No_Access()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(userId, classId)).ReturnsAsync(false);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_Should_Return_Folders_For_SystemAdmin()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            var folders = new List<Folder>
            {
                new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId }
            };

            var counts = folders.ToDictionary(f => f.Id, f => 7);

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);
            _mapperMock.Setup(m => m.Map<IEnumerable<FolderResponse>>(It.IsAny<IEnumerable<Folder>>()))
                .Returns((IEnumerable<Folder> fs) => fs.Select(f => new FolderResponse { Id = f.Id }).ToList());
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterial, Is.EqualTo(7));
        }

        [Test]
        public async Task Handle_Should_Set_CountLessonMaterial_To_Zero_If_NotInCounts()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };

            var folders = new List<Folder>
            {
                new() { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId }
            };

            var counts = new Dictionary<Guid, int>();

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);
            _mapperMock.Setup(m => m.Map<IEnumerable<FolderResponse>>(It.IsAny<IEnumerable<Folder>>()))
                .Returns((IEnumerable<Folder> fs) => fs.Select(f => new FolderResponse { Id = f.Id }).ToList());
            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var query = new GetAllFoldersByClassIdQuery(classId, userId);
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterial, Is.EqualTo(0));
        }
    }
}