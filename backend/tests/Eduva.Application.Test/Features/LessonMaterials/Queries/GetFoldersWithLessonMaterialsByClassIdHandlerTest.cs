using Eduva.Application.Features.LessonMaterials.Queries.GetFoldersWithLessonMaterialsByClassId;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetFoldersWithLessonMaterialsByClassIdHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IClassroomRepository> _classRepoMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepoMock = null!;
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private Mock<ICacheService> _cacheServiceMock = null!;
        private GetFoldersWithLessonMaterialsByClassIdHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classRepoMock = new Mock<IClassroomRepository>();
            _studentClassRepoMock = new Mock<IStudentClassRepository>();
            _folderRepoMock = new Mock<IFolderRepository>();
            _cacheServiceMock = new Mock<ICacheService>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>())
                .Returns(_folderRepoMock.Object);

            _handler = new GetFoldersWithLessonMaterialsByClassIdHandler(_unitOfWorkMock.Object, _cacheServiceMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_If_SchoolId_Not_Match()
        {
            var classId = Guid.NewGuid();
            var classEntity = new Classroom { Id = classId, SchoolId = 123 }; // khác schoolId

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classEntity);

            var query = new GetFoldersWithLessonMaterialsByClassIdQuery(
                classId,
                0,
                Guid.Empty,
                new List<string> { "Teacher" },
                null,
                null
            );

            Assert.ThrowsAsync<Eduva.Application.Exceptions.Auth.ForbiddenException>(
                () => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_If_ClassEntity_Is_Null()
        {
            var classId = Guid.NewGuid();

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var query = new GetFoldersWithLessonMaterialsByClassIdQuery(
                classId,
                123,
                Guid.NewGuid(),
                new List<string> { "Teacher" },
                null,
                null
            );

            Assert.ThrowsAsync<Eduva.Application.Exceptions.Auth.ForbiddenException>(
                () => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Return_Empty_If_Student_Not_Enrolled()
        {
            var classId = Guid.NewGuid();
            var schoolId = 123;
            var classEntity = new Classroom { Id = classId, SchoolId = schoolId };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classEntity);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(It.IsAny<Guid>(), classId)).ReturnsAsync(false);

            var query = new GetFoldersWithLessonMaterialsByClassIdQuery(
                classId,
                schoolId,
                Guid.NewGuid(),
                new List<string> { "Student" },
                null,
                null
            );

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_Should_Filter_LessonMaterials_For_Student()
        {
            var classId = Guid.NewGuid();
            var schoolId = 123;
            var userId = Guid.NewGuid();
            var classEntity = new Classroom { Id = classId, SchoolId = schoolId };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classEntity);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(userId, classId)).ReturnsAsync(true);

            var lessonMaterialActiveApproved = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var lessonMaterialInactive = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Inactive,
                LessonStatus = LessonMaterialStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = "Folder 1",
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new() { LessonMaterial = lessonMaterialActiveApproved, LessonMaterialId = lessonMaterialActiveApproved.Id },
                    new() { LessonMaterial = lessonMaterialInactive, LessonMaterialId = lessonMaterialInactive.Id }
                }
            };

            _folderRepoMock.Setup(r => r.GetFoldersWithLessonMaterialsByClassIdAsync(classId))
                .ReturnsAsync(new List<Folder> { folder });

            var query = new GetFoldersWithLessonMaterialsByClassIdQuery(
                classId,
                schoolId,
                userId,
                new List<string> { "Student" },
                null,
                null
            );

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].CountLessonMaterials, Is.EqualTo(1));
                Assert.That(result[0].LessonMaterials.All(lm => lm.Status == EntityStatus.Active && lm.LessonStatus == LessonMaterialStatus.Approved));
            });
        }

        [Test]
        public async Task Handle_Should_Filter_LessonMaterials_For_Teacher_By_Status()
        {
            var classId = Guid.NewGuid();
            var schoolId = 1234; // Use int instead of Guid
            var userId = Guid.NewGuid();
            var classEntity = new Classroom { Id = classId, SchoolId = 1234 };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classEntity);

            var lessonMaterialActiveApproved = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var lessonMaterialInactive = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Inactive,
                LessonStatus = LessonMaterialStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = "Folder 1",
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new() { LessonMaterial = lessonMaterialActiveApproved, LessonMaterialId = lessonMaterialActiveApproved.Id },
                    new() { LessonMaterial = lessonMaterialInactive, LessonMaterialId = lessonMaterialInactive.Id }
                }
            };

            _folderRepoMock.Setup(r => r.GetFoldersWithLessonMaterialsByClassIdAsync(classId))
                .ReturnsAsync(new List<Folder> { folder });

            var query = new GetFoldersWithLessonMaterialsByClassIdQuery(
                classId,
                schoolId,
                userId,
                new List<string> { "Teacher" },
                LessonMaterialStatus.Approved,
                EntityStatus.Active
            );

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].CountLessonMaterials, Is.EqualTo(1));
                Assert.That(result[0].LessonMaterials.All(lm => lm.Status == EntityStatus.Active && lm.LessonStatus == LessonMaterialStatus.Approved));
            });
        }

        [Test]
        public async Task Handle_Should_Return_All_LessonMaterials_If_No_Status_Filter()
        {
            var classId = Guid.NewGuid();
            var schoolId = 12345;
            var userId = Guid.NewGuid();
            var classEntity = new Classroom { Id = classId, SchoolId = 12345 };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classEntity);

            var lessonMaterial1 = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var lessonMaterial2 = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Inactive,
                LessonStatus = LessonMaterialStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = "Folder 1",
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new() { LessonMaterial = lessonMaterial1, LessonMaterialId = lessonMaterial1.Id },
                    new() { LessonMaterial = lessonMaterial2, LessonMaterialId = lessonMaterial2.Id }
                }
            };

            _folderRepoMock.Setup(r => r.GetFoldersWithLessonMaterialsByClassIdAsync(classId))
                .ReturnsAsync(new List<Folder> { folder });

            var query = new GetFoldersWithLessonMaterialsByClassIdQuery(
                classId,
                schoolId,
                userId,
                new List<string> { "Teacher" },
                null,
                null
            );

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterials, Is.EqualTo(2));
        }
    }
}