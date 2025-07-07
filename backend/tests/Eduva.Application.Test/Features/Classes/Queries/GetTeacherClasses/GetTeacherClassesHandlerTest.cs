using AutoMapper;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Queries.GetTeacherClasses;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Queries.GetTeacherClasses
{
    [TestFixture]
    public class GetTeacherClassesHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetTeacherClassesHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new GetTeacherClassesHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_ShouldReturnMappedClasses_WithLessonMaterialCount()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetTeacherClassesQuery(classSpecParam, teacherId);

            var classrooms = new List<Classroom>
            {
                new Classroom { Id = classId, TeacherId = teacherId, Name = "Math" }
            };
            var paginatedDomain = new Pagination<Classroom>(1, 10, 1, classrooms);

            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedDomain);

            var classResponses = classrooms.Select(c => new ClassResponse
            {
                Id = c.Id,
                TeacherId = c.TeacherId,
                Name = c.Name,
                CountLessonMaterial = 0
            }).ToList();
            var paginatedResponse = new Pagination<ClassResponse>(1, 10, 1, classResponses);

            // Mock AppMapper<AppMappingProfile>.Mapper
            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponse);

            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Folders for the class
                var folderId = Guid.NewGuid();
                var folders = new List<Folder>
                {
                    new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class }
                };
                _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);

                // Lesson material counts
                var countsByFolder = new Dictionary<Guid, int> { { folderId, 5 } };
                _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(countsByFolder);

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Data.First().CountLessonMaterial, Is.EqualTo(5));
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldReturnEmpty_WhenNoClasses()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetTeacherClassesQuery(classSpecParam, teacherId);

            var paginatedDomain = new Pagination<Classroom>(1, 10, 0, new List<Classroom>());
            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedDomain);

            var paginatedResponse = new Pagination<ClassResponse>(1, 10, 0, new List<ClassResponse>());
            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponse);

            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(0));
                Assert.That(result.Data, Is.Empty);
                _folderRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldSetCountLessonMaterial_Zero_WhenNoFolders()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetTeacherClassesQuery(classSpecParam, teacherId);

            var classrooms = new List<Classroom>
            {
                new Classroom { Id = classId, TeacherId = teacherId, Name = "Math" }
            };
            var paginatedDomain = new Pagination<Classroom>(1, 10, 1, classrooms);

            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedDomain);

            var classResponses = classrooms.Select(c => new ClassResponse
            {
                Id = c.Id,
                TeacherId = c.TeacherId,
                Name = c.Name,
                CountLessonMaterial = 0
            }).ToList();
            var paginatedResponse = new Pagination<ClassResponse>(1, 10, 1, classResponses);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponse);

            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // No folders for this class
                _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Folder>());

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Data.First().CountLessonMaterial, Is.EqualTo(0));
                _lessonMaterialRepoMock.Verify(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldSetCountLessonMaterial_Zero_WhenNoApprovedMaterials()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var classSpecParam = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetTeacherClassesQuery(classSpecParam, teacherId);

            var classrooms = new List<Classroom>
            {
                new Classroom { Id = classId, TeacherId = teacherId, Name = "Math" }
            };
            var paginatedDomain = new Pagination<Classroom>(1, 10, 1, classrooms);

            _classroomRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ClassSpecification>()))
                .ReturnsAsync(paginatedDomain);

            var classResponses = classrooms.Select(c => new ClassResponse
            {
                Id = c.Id,
                TeacherId = c.TeacherId,
                Name = c.Name,
                CountLessonMaterial = 0
            }).ToList();
            var paginatedResponse = new Pagination<ClassResponse>(1, 10, 1, classResponses);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<ClassResponse>>(It.IsAny<Pagination<Classroom>>()))
                .Returns(paginatedResponse);

            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Folders for the class
                var folderId = Guid.NewGuid();
                var folders = new List<Folder>
                {
                    new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class }
                };
                _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(folders);

                // No approved materials
                _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Data.First().CountLessonMaterial, Is.EqualTo(0));
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }
    }
}