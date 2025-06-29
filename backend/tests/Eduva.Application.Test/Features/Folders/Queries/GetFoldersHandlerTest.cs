using AutoMapper;
using Eduva.Application.Common.Models;
using Eduva.Application.Common.Specifications;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetFoldersHandlerTest
    {
        #region Setup
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IFolderRepository> _folderRepoMock;
        private Mock<IMapper> _mapperMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private GetFoldersHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IFolderRepository>();
            _mapperMock = new Mock<IMapper>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!
            );
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>()).Returns(_folderRepoMock.Object);
            _handler = new GetFoldersHandler(_folderRepoMock.Object, _mapperMock.Object, _unitOfWorkMock.Object, _userManagerMock.Object);
        }
        #endregion

        #region Tests
        [Test]
        public async Task Handle_Returns_Folders()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "Test Folder" } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(1));
                Assert.That(result.Data.First().Name, Is.EqualTo("Test Folder"));
            });
        }

        [Test]
        public async Task Handle_Returns_Empty_When_NoFolders()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder>();
            var pagination = new Pagination<Folder>(1, 10, 0, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(new List<FolderResponse>());
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
        }

        private static readonly string[] ExpectedNames = { "A", "B" };

        [Test]
        public async Task Handle_Returns_MultipleFolders_WithPagination()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 2 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "A" }, new Folder { Name = "B" } };
            var pagination = new Pagination<Folder>(1, 2, 2, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "A" }, new FolderResponse { Name = "B" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(result.Data, Has.Count.EqualTo(2));
                Assert.That(result.Data.Select(f => f.Name), Is.EquivalentTo(ExpectedNames));
            });
        }

        [Test]
        public void Handle_Throws_When_MapperFails()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 1 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "X" } };
            var pagination = new Pagination<Folder>(1, 1, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Throws(new Exception("Mapping failed"));
            Assert.That(async () => await _handler.Handle(query, CancellationToken.None), Throws.TypeOf<Exception>());
        }

        [Test]
        public void Handle_Throws_When_RepositoryThrows()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 1 };
            var query = new GetFoldersQuery(param);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ThrowsAsync(new Exception("Repo error"));
            Assert.That(async () => await _handler.Handle(query, CancellationToken.None), Throws.TypeOf<Exception>());
        }

        [Test]
        public async Task Handle_Returns_Empty_When_PaginationIsEmpty()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            var emptyPagination = new Pagination<Folder>(1, 10, 0, new List<Folder>());
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(emptyPagination);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data == null || result.Data.Count == 0);
        }

        [Test]
        public async Task Handle_Returns_Empty_When_PaginationIsNull()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync((Pagination<Folder>)null!);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
        }

        [Test]
        public async Task Handle_Returns_ClassFolders_When_OwnerType_Is_Class()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "Class Folder", OwnerType = Eduva.Domain.Enums.OwnerType.Class } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Class Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(1));
                Assert.That(result.Data.First().Name, Is.EqualTo("Class Folder"));
            });
        }

        [Test]
        public async Task Handle_Calls_CheckClassFolderAccess_When_OwnerType_Class_And_UserId_And_ClassId_Provided()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var folders = new List<Folder> { new Folder { Name = "Class Folder", OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Class Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(folderResponses);

            // Setup user repo to return a valid user
            var user = new ApplicationUser { Id = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            // Setup user manager to return SystemAdmin role
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.SystemAdmin) });
            // Setup class repo (not needed for SystemAdmin, but safe to mock)
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);

            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(1));
            Assert.That(result.Data.First().Name, Is.EqualTo("Class Folder"));
        }

        [Test]
        public void Handle_Throws_When_User_Not_Found_For_ClassFolderAccess()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            // User not found
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            // The rest of the dependencies are not needed as it should throw before using them
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(async () => await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Grants_Access_When_User_Is_SystemAdmin()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.SystemAdmin) });
            var folders = new List<Folder> { new Folder { Name = "Class Folder", OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>())).ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Class Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders)).Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(1));
        }

        [Test]
        public void Handle_Throws_When_Class_Not_Found_For_ClassFolderAccess()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.Teacher) });
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom)null!);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(async () => await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Grants_Access_When_User_Is_SchoolAdmin_And_School_Matches()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, SchoolId = 1 };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.SchoolAdmin) });
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);
            var folders = new List<Folder> { new Folder { Name = "Class Folder", OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>())).ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Class Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders)).Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Handle_Grants_Access_When_User_Is_Teacher_Of_Class()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var classroom = new Classroom { Id = classId, TeacherId = userId };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.Teacher) });
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);
            var folders = new List<Folder> { new Folder { Name = "Class Folder", OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>())).ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Class Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders)).Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Handle_Grants_Access_When_User_Is_Student_And_Enrolled()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var classroom = new Classroom { Id = classId };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.Student) });
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);
            var studentClassRepo = new Mock<IStudentClassRepository>();
            studentClassRepo.Setup(r => r.IsStudentEnrolledInClassAsync(userId, classId)).ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>()).Returns(studentClassRepo.Object);
            var folders = new List<Folder> { new Folder { Name = "Class Folder", OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>())).ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Class Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders)).Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(1));
        }

        [Test]
        public void Handle_Throws_When_Student_Not_Enrolled()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var classroom = new Classroom { Id = classId };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Eduva.Domain.Enums.Role.Student) });
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);
            var studentClassRepo = new Mock<IStudentClassRepository>();
            studentClassRepo.Setup(r => r.IsStudentEnrolledInClassAsync(userId, classId)).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>()).Returns(studentClassRepo.Object);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(async () => await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_User_Has_No_Access_Role()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId };
            var classroom = new Classroom { Id = classId };
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10, OwnerType = Eduva.Domain.Enums.OwnerType.Class, ClassId = classId };
            var query = new GetFoldersQuery(param) { UserId = userId };
            var userRepo = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepo.Object);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "OtherRole" });
            var classRepo = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepo.Object);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(async () => await _handler.Handle(query, CancellationToken.None));
        }
        #endregion
    }
}