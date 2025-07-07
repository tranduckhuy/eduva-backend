using AutoMapper;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Queries.GetStudentClasses;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Queries.GetStudentClasses
{
    [TestFixture]
    public class GetStudentClassesHandlerTest
    {
        #region Setup
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepoMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetStudentClassesHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _studentClassRepoMock = new Mock<IStudentClassRepository>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();

            // Setup repositories
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);


            // Create handler with mocked dependencies
            _handler = new GetStudentClassesHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region Tests
        [Test]
        public async Task Handle_ShouldReturnStudentClasses_WithCorrectStudentId()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, studentId);

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ClassId = Guid.NewGuid(),
                    EnrolledAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            var paginatedResult = new Pagination<StudentClass>(1, 10, 1, studentClasses);

            // Setup specification verification
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.Is<StudentClassSpecification>(
                s => s.Criteria.Compile().Invoke(new StudentClass { StudentId = studentId }))))
                .ReturnsAsync(paginatedResult);

            // Setup mapping directly
            var studentClassResponses = studentClasses.Select(sc => new StudentClassResponse
            {
                Id = sc.Id,
                StudentId = sc.StudentId,
                ClassId = sc.ClassId,
                CountLessonMaterial = 0
            }).ToList();

            var paginatedResponses = new Pagination<StudentClassResponse>(1, 10, 1, studentClassResponses);

            // Use auto mapper directly
            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<StudentClassResponse>>(It.IsAny<Pagination<StudentClass>>()))
                .Returns(paginatedResponses);

            // Apply AutoMapper mock using reflection for this test only
            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);

                // Setup empty folders
                _folderRepoMock.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Folder>());

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.Multiple(() =>
                {
                    Assert.That(result.Data.First().StudentId, Is.EqualTo(studentId));

                    // Verify StudentId was set in the spec params
                    Assert.That(query.StudentClassSpecParam.StudentId, Is.EqualTo(studentId));
                });

                // Verify StudentClassRepository was called with the right spec
                _studentClassRepoMock.Verify(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>()), Times.Once);
            }
            finally
            {
                // Restore original mapper field
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldCalculateLessonMaterialCounts_ForClassFolders()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId1 = Guid.NewGuid();
            var classId2 = Guid.NewGuid();
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, studentId);

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ClassId = classId1,
                    EnrolledAt = DateTime.UtcNow.AddDays(-10)
                },
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ClassId = classId2,
                    EnrolledAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            var paginatedResult = new Pagination<StudentClass>(1, 10, 2, studentClasses);

            // Setup repository
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>()))
                .ReturnsAsync(paginatedResult);

            // Setup mapping
            var studentClassResponses = studentClasses.Select(sc => new StudentClassResponse
            {
                Id = sc.Id,
                StudentId = sc.StudentId,
                ClassId = sc.ClassId,
                CountLessonMaterial = 0
            }).ToList();

            var paginatedResponses = new Pagination<StudentClassResponse>(1, 10, 2, studentClassResponses);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<StudentClassResponse>>(It.IsAny<Pagination<StudentClass>>()))
                .Returns(paginatedResponses);

            // Apply AutoMapper mock using reflection for this test only
            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Setup folders
                var folderId1 = Guid.NewGuid();
                var folderId2 = Guid.NewGuid();
                var folderId3 = Guid.NewGuid();

                var folders = new List<Folder>
                {
                    new Folder { Id = folderId1, ClassId = classId1, OwnerType = OwnerType.Class },
                    new Folder { Id = folderId2, ClassId = classId1, OwnerType = OwnerType.Class }, // Second folder for class 1
                    new Folder { Id = folderId3, ClassId = classId2, OwnerType = OwnerType.Class },
                    new Folder { Id = Guid.NewGuid(), ClassId = null, OwnerType = OwnerType.Personal } // This should be ignored
                };

                _folderRepoMock.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(folders);

                // Setup lesson material counts
                var countsByFolder = new Dictionary<Guid, int>
                {
                    { folderId1, 3 },
                    { folderId2, 2 }, // Total for class1: 5
                    { folderId3, 4 }  // Total for class2: 4
                };

                _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(countsByFolder);

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(2));

                // Verify counts were calculated correctly
                var class1Response = result.Data.First(r => r.ClassId == classId1);
                var class2Response = result.Data.First(r => r.ClassId == classId2);

                Assert.Multiple(() =>
                {
                    Assert.That(class1Response.CountLessonMaterial, Is.EqualTo(5));
                    Assert.That(class2Response.CountLessonMaterial, Is.EqualTo(4));
                });

                // Verify material count API was called with the right folder IDs
                _lessonMaterialRepoMock.Verify(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.Is<List<Guid>>(ids =>
                        ids.Contains(folderId1) &&
                        ids.Contains(folderId2) &&
                        ids.Contains(folderId3) &&
                        ids.Count == 3),
                    It.IsAny<CancellationToken>()));
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldHandleEmptyClassList()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, studentId);

            var emptyList = new List<StudentClass>();
            var paginatedResult = new Pagination<StudentClass>(1, 10, 0, emptyList);

            // Setup repository
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>()))
                .ReturnsAsync(paginatedResult);

            // Setup mapping
            var emptyResponses = new List<StudentClassResponse>();
            var paginatedResponses = new Pagination<StudentClassResponse>(1, 10, 0, emptyResponses);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<StudentClassResponse>>(It.IsAny<Pagination<StudentClass>>()))
                .Returns(paginatedResponses);

            // Apply AutoMapper mock using reflection for this test only
            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(0));
                Assert.That(result.Data, Is.Empty);

                // Verify folder repository was not called for an empty list
                _folderRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
                _lessonMaterialRepoMock.Verify(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            }
            finally
            {
                // Restore original mapper field
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldHandleNoFoldersForClass()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, studentId);

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ClassId = classId,
                    EnrolledAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            var paginatedResult = new Pagination<StudentClass>(1, 10, 1, studentClasses);

            // Setup repository
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>()))
                .ReturnsAsync(paginatedResult);

            // Setup mapping
            var studentClassResponses = studentClasses.Select(sc => new StudentClassResponse
            {
                Id = sc.Id,
                StudentId = sc.StudentId,
                ClassId = sc.ClassId,
                CountLessonMaterial = 0
            }).ToList();

            var paginatedResponses = new Pagination<StudentClassResponse>(1, 10, 1, studentClassResponses);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<StudentClassResponse>>(It.IsAny<Pagination<StudentClass>>()))
                .Returns(paginatedResponses);

            // Apply AutoMapper mock using reflection for this test only
            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Setup an empty folder list or folders for other classes
                var folders = new List<Folder>
                {
                    new Folder { Id = Guid.NewGuid(), ClassId = Guid.NewGuid(), OwnerType = OwnerType.Class }, // Different class
                    new Folder { Id = Guid.NewGuid(), ClassId = null, OwnerType = OwnerType.Personal }
                };

                _folderRepoMock.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(folders);

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Data.First().CountLessonMaterial, Is.EqualTo(0));

                // Verify material count API was not called since there are no matching folders
                _lessonMaterialRepoMock.Verify(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            }
            finally
            {
                // Restore original mapper field
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public async Task Handle_ShouldHandleNoApprovedMaterials()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, studentId);

            var studentClasses = new List<StudentClass>
            {
                new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ClassId = classId,
                    EnrolledAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            var paginatedResult = new Pagination<StudentClass>(1, 10, 1, studentClasses);

            // Setup repository
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>()))
                .ReturnsAsync(paginatedResult);

            // Setup mapping
            var studentClassResponses = studentClasses.Select(sc => new StudentClassResponse
            {
                Id = sc.Id,
                StudentId = sc.StudentId,
                ClassId = sc.ClassId,
                CountLessonMaterial = 0
            }).ToList();

            var paginatedResponses = new Pagination<StudentClassResponse>(1, 10, 1, studentClassResponses);

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Pagination<StudentClassResponse>>(It.IsAny<Pagination<StudentClass>>()))
                .Returns(paginatedResponses);

            // Apply AutoMapper mock using reflection for this test only
            var appMapperType = typeof(Eduva.Application.Common.Mappings.AppMapper<AppMappingProfile>);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Setup folders
                var folderId = Guid.NewGuid();
                var folders = new List<Folder>
                {
                    new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class }
                };

                _folderRepoMock.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(folders);

                // Setup empty lesson material counts
                _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                    It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Dictionary<Guid, int>());

                // Act
                var result = await _handler.Handle(query, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.Data.First().CountLessonMaterial, Is.EqualTo(0));
            }
            finally
            {
                // Restore original mapper field
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }
        #endregion
    }
}