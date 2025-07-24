using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialsByFolder;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialsByFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetLessonMaterialsByFolderHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new GetLessonMaterialsByFolderHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_Should_Return_AllMaterials_For_SystemAdmin()
        {
            var folderId = Guid.NewGuid();
            var schoolId = 1;
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "SystemAdmin" };

            var materials = new List<LessonMaterial>
            {
                new LessonMaterial { Id = Guid.NewGuid(), Title = "Material 1" },
                new LessonMaterial { Id = Guid.NewGuid(), Title = "Material 2" }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetLessonMaterialsByFolderAsync(
                folderId, schoolId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(materials);

            var query = new GetLessonMaterialsByFolderQuery(folderId, userId, schoolId, userRoles);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Title, Is.EqualTo("Material 1"));
                Assert.That(result[1].Title, Is.EqualTo("Material 2"));
            });
        }

        [Test]
        public async Task Handle_Should_Return_AllMaterials_For_SchoolAdmin()
        {
            var folderId = Guid.NewGuid();
            var schoolId = 1;
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "SchoolAdmin" };

            var materials = new List<LessonMaterial>
            {
                new LessonMaterial { Id = Guid.NewGuid(), Title = "Material 1" }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetLessonMaterialsByFolderAsync(
                folderId, schoolId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(materials);

            var query = new GetLessonMaterialsByFolderQuery(folderId, userId, schoolId, userRoles);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Material 1"));
        }

        [Test]
        public async Task Handle_Should_Return_AllMaterials_For_ContentModerator()
        {
            var folderId = Guid.NewGuid();
            var schoolId = 1;
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "ContentModerator" };

            var materials = new List<LessonMaterial>
            {
                new LessonMaterial { Id = Guid.NewGuid(), Title = "Material 1" }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetLessonMaterialsByFolderAsync(
                folderId, schoolId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(materials);

            var query = new GetLessonMaterialsByFolderQuery(folderId, userId, schoolId, userRoles);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Material 1"));
        }

        [Test]
        public async Task Handle_Should_Return_TeacherMaterials_For_Teacher()
        {
            var folderId = Guid.NewGuid();
            var schoolId = 1;
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "Teacher" };

            var materials = new List<LessonMaterial>
            {
                new LessonMaterial { Id = Guid.NewGuid(), Title = "Teacher Material" }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetLessonMaterialsByFolderForTeacherAsync(
                folderId, userId, schoolId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(materials);

            var query = new GetLessonMaterialsByFolderQuery(folderId, userId, schoolId, userRoles);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Teacher Material"));
        }

        [Test]
        public async Task Handle_Should_Return_StudentMaterials_For_Student()
        {
            var folderId = Guid.NewGuid();
            var schoolId = 1;
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "Student" };

            var materials = new List<LessonMaterial>
            {
                new LessonMaterial { Id = Guid.NewGuid(), Title = "Student Material" }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetLessonMaterialsByFolderForStudentAsync(
                folderId, userId, schoolId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(materials);

            var query = new GetLessonMaterialsByFolderQuery(folderId, userId, schoolId, userRoles);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Student Material"));
        }

        [Test]
        public async Task Handle_Should_Return_EmptyList_For_UnknownRole()
        {
            var folderId = Guid.NewGuid();
            var schoolId = 1;
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "UnknownRole" };

            var query = new GetLessonMaterialsByFolderQuery(folderId, userId, schoolId, userRoles);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Empty);
        }
    }
}