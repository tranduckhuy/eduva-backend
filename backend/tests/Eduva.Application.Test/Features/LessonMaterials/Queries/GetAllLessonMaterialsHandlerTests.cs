using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetAllLessonMaterialsHandlerTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILessonMaterialRepository> _mockRepository;
        private GetAllLessonMaterialsHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepository = new Mock<ILessonMaterialRepository>();

            _mockUnitOfWork.Setup(x => x.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_mockRepository.Object);

            _handler = new GetAllLessonMaterialsHandler(_mockUnitOfWork.Object);
        }

        [Test]
        public async Task Handle_ShouldReturnMappedLessonMaterials_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var schoolId = 1;
            var classId = Guid.NewGuid();
            var folderId = Guid.NewGuid();

            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: schoolId,
                ClassId: classId,
                FolderId: folderId
            );

            var lessonMaterials = new List<LessonMaterial>
            {
                new LessonMaterial
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Material 1",
                    Description = "Description 1",
                    ContentType = ContentType.PDF,
                    SchoolId = schoolId,
                    CreatedByUserId = userId,
                    LessonStatus = LessonMaterialStatus.Approved,
                    Visibility = LessonMaterialVisibility.School,
                    FileSize = 1024,
                    SourceUrl = "test1.pdf"
                },
                new LessonMaterial
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Material 2",
                    Description = "Description 2",
                    ContentType = ContentType.Video,
                    SchoolId = schoolId,
                    CreatedByUserId = userId,
                    LessonStatus = LessonMaterialStatus.Draft,
                    Visibility = LessonMaterialVisibility.Private,
                    FileSize = 2048,
                    SourceUrl = "test2.mp4"
                }
            };

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                userId, false, schoolId, classId, folderId, default))
                .ReturnsAsync(lessonMaterials);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Title, Is.EqualTo("Test Material 1"));
                Assert.That(result[result.Count - 1].Title, Is.EqualTo("Test Material 2"));
            });

            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, false, schoolId, classId, folderId, default), Times.Once);
        }

        [Test]
        public async Task Handle_WithStudentRole_ShouldPassIsStudentTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: true,
                SchoolId: 1
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, true, 1, null, null, default), Times.Once);
        }

        [Test]
        public async Task Handle_WithNullSchoolId_ShouldPassNullSchoolId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: null
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, false, null, null, null, default), Times.Once);
        }

        [Test]
        public async Task Handle_WithClassIdOnly_ShouldPassCorrectParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: 1,
                ClassId: classId
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, false, 1, classId, null, default), Times.Once);
        }

        [Test]
        public async Task Handle_WithFolderIdOnly_ShouldPassCorrectParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: 1,
                FolderId: folderId
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, false, 1, null, folderId, default), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldReturnEmptyList_WhenNoMaterialsFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: 1
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_ShouldHandleCancellationToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: 1
            );

            var cancellationToken = new CancellationToken();

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), cancellationToken))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, false, 1, null, null, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithAllParameters_ShouldPassAllParametersCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var schoolId = 1;
            var classId = Guid.NewGuid();
            var folderId = Guid.NewGuid();

            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: true,
                SchoolId: schoolId,
                ClassId: classId,
                FolderId: folderId
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ReturnsAsync(new List<LessonMaterial>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mockRepository.Verify(r => r.GetAllBySchoolAsync(
                userId, true, schoolId, classId, folderId, default), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetAllLessonMaterialsQuery(
                UserId: userId,
                IsStudent: false,
                SchoolId: 1
            );

            _mockRepository.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }
    }
}
