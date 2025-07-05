using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Queries.GetClasses;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Queries.GetClasses
{
    [TestFixture]
    public class GetClassesHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetClassesHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();

            // Setup repositories
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            // Create handler with mocked dependencies
            _handler = new GetClassesHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_ShouldReturnAllClasses_WhenUserIsSystemAdmin()
        {
            var adminId = Guid.NewGuid();
            var admin = new ApplicationUser { Id = adminId, FullName = "System Admin" };
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetClassesQuery(classSpecParam, adminId);

            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            var classrooms = new List<Classroom>
            {
                new Classroom { Id = Guid.NewGuid(), Name = "Class 1", SchoolId = 1 },
                new Classroom { Id = Guid.NewGuid(), Name = "Class 2", SchoolId = 2 }
            };

            var paginatedClassrooms = new Pagination<Classroom>(1, 10, 2, classrooms);

            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedClassrooms);

            var folders = new List<Folder>
            {
                new Folder { Id = Guid.NewGuid(), ClassId = classrooms[0].Id, OwnerType = OwnerType.Class },
                new Folder { Id = Guid.NewGuid(), ClassId = classrooms[1].Id, OwnerType = OwnerType.Class }
            };

            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);

            var countsByFolder = new Dictionary<Guid, int>
            {
                { folders[0].Id, 3 },
                { folders[1].Id, 2 }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(countsByFolder);

            // Setup mock for AppMapper
            var mockMapper = new Mock<AutoMapper.IMapper>();
            var classResponses = classrooms.Select(c => new ClassResponse
            {
                Id = c.Id,
                Name = c.Name,
                CountLessonMaterial = 0
            }).ToList();
            var paginatedResponses = new Pagination<ClassResponse>(1, 10, 2, classResponses);
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponses);

            // Replace AppMapper.Mapper with our mock using reflection
            var appMapperType = typeof(AppMapper);
            var mapperField = appMapperType.GetField("_mapper",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            if (mapperField != null)
                mapperField.SetValue(null, mockMapper.Object);

            try
            {
                var result = await _handler.Handle(query, CancellationToken.None);

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.Count, Is.EqualTo(2));
                    Assert.That(result.Data, Has.Count.EqualTo(2));
                });
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldFilterBySchool_WhenUserIsSchoolAdmin()
        {
            var schoolId = 1;
            var adminId = Guid.NewGuid();
            var admin = new ApplicationUser { Id = adminId, FullName = "School Admin", SchoolId = schoolId };
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetClassesQuery(classSpecParam, adminId);

            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            var classrooms = new List<Classroom>
            {
                new Classroom { Id = Guid.NewGuid(), Name = "Class 1", SchoolId = schoolId },
                new Classroom { Id = Guid.NewGuid(), Name = "Class 2", SchoolId = schoolId }
            };

            var paginatedClassrooms = new Pagination<Classroom>(1, 10, 2, classrooms);

            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedClassrooms);

            // Setup mock for AppMapper
            var mockMapper = new Mock<AutoMapper.IMapper>();
            var classResponses = classrooms.Select(c => new ClassResponse
            {
                Id = c.Id,
                Name = c.Name,
                CountLessonMaterial = 0
            }).ToList();
            var paginatedResponses = new Pagination<ClassResponse>(1, 10, 2, classResponses);
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponses);

            // Replace AppMapper.Mapper with our mock using reflection
            var appMapperType = typeof(AppMapper);
            var mapperField = appMapperType.GetField("_mapper",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var originalMapper = mapperField?.GetValue(null);
            if (mapperField != null)
                mapperField.SetValue(null, mockMapper.Object);

            try
            {
                _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Folder>());

                var result = await _handler.Handle(query, CancellationToken.None);

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.Count, Is.EqualTo(2));
                    Assert.That(query.ClassSpecParam.SchoolId, Is.EqualTo(schoolId));
                });
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public void Handle_ShouldThrowException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var query = new GetClassesQuery(new ClassSpecParam(), userId);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_ShouldThrowException_WhenUserIsNotAdmin()
        {
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, FullName = "Regular User" };
            var query = new GetClassesQuery(new ClassSpecParam(), userId);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.NotAdminForClassList));
        }

        [Test]
        public void Handle_ShouldThrowException_WhenSchoolAdminHasNoSchool()
        {
            var adminId = Guid.NewGuid();
            var admin = new ApplicationUser { Id = adminId, FullName = "School Admin", SchoolId = null };
            var query = new GetClassesQuery(new ClassSpecParam(), adminId);

            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.SchoolNotFound));
        }

        [Test]
        public async Task Handle_ShouldCalculateTotalLessonMaterials_ForEachClass()
        {
            var adminId = Guid.NewGuid();
            var admin = new ApplicationUser { Id = adminId, FullName = "System Admin" };
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetClassesQuery(classSpecParam, adminId);

            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            var classId1 = Guid.NewGuid();
            var classId2 = Guid.NewGuid();

            var classrooms = new List<Classroom>
            {
                new Classroom { Id = classId1, Name = "Class 1", SchoolId = 1 },
                new Classroom { Id = classId2, Name = "Class 2", SchoolId = 2 }
            };

            var paginatedClassrooms = new Pagination<Classroom>(1, 10, 2, classrooms);

            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedClassrooms);

            var folderId1 = Guid.NewGuid();
            var folderId2 = Guid.NewGuid();
            var folderId3 = Guid.NewGuid();

            var folders = new List<Folder>
            {
                new Folder { Id = folderId1, ClassId = classId1, OwnerType = OwnerType.Class },
                new Folder { Id = folderId2, ClassId = classId1, OwnerType = OwnerType.Class },
                new Folder { Id = folderId3, ClassId = classId2, OwnerType = OwnerType.Class }
            };

            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);

            var countsByFolder = new Dictionary<Guid, int>
            {
                { folderId1, 3 },
                { folderId2, 2 },
                { folderId3, 4 }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(countsByFolder);

            // Setup mock for AppMapper
            var mockMapper = new Mock<AutoMapper.IMapper>();
            var classResponses = classrooms.Select(c => new ClassResponse
            {
                Id = c.Id,
                Name = c.Name,
                CountLessonMaterial = 0 // Will be updated by handler
            }).ToList();
            var paginatedResponses = new Pagination<ClassResponse>(1, 10, 2, classResponses);
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponses);

            var appMapperType = typeof(AppMapper);
            var mapperField = appMapperType.GetField("_mapper",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);
            if (mapperField != null)
                mapperField.SetValue(null, mockMapper.Object);

            try
            {
                var result = await _handler.Handle(query, CancellationToken.None);

                Assert.That(result, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Data.First(c => c.Id == classId1).CountLessonMaterial, Is.EqualTo(5));
                    Assert.That(result.Data.First(c => c.Id == classId2).CountLessonMaterial, Is.EqualTo(4));
                });
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }
    }
}
