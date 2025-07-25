using Eduva.Application.Features.LessonMaterials.Queries.GetAllLessonMaterials;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetAllLessonMaterialsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetAllLessonMaterialsHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new GetAllLessonMaterialsHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_Should_Return_Mapped_LessonMaterials()
        {
            var userId = Guid.NewGuid();
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var isStudent = true;

            var lessonMaterials = new List<LessonMaterial>
            {
                new() { Id = Guid.NewGuid(), Title = "Material 1" },
                new() { Id = Guid.NewGuid(), Title = "Material 2" }
            };

            _lessonMaterialRepoMock.Setup(r => r.GetAllBySchoolAsync(
                userId, isStudent, schoolId, classId, folderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lessonMaterials);

            var query = new GetAllLessonMaterialsQuery(
                userId,
                isStudent,
                schoolId,
                classId,
                folderId
            );

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Title, Is.EqualTo("Material 1"));
                Assert.That(result[1].Title, Is.EqualTo("Material 2"));
            });
        }

        [Test]
        public async Task Handle_Should_Return_EmptyList_If_NoMaterials()
        {
            var userId = Guid.NewGuid();
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var folderId = Guid.NewGuid();

            _lessonMaterialRepoMock.Setup(r => r.GetAllBySchoolAsync(
                userId, false, schoolId, classId, folderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial>());

            var query = new GetAllLessonMaterialsQuery(
                userId,
                false,
                schoolId,
                classId,
                folderId
            );

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Handle_Should_ThrowException_If_RepositoryThrows()
        {
            var query = new GetAllLessonMaterialsQuery(
                Guid.NewGuid(), true, 1, Guid.NewGuid(), Guid.NewGuid()
            );

            _lessonMaterialRepoMock.Setup(r => r.GetAllBySchoolAsync(
                It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()
            )).ThrowsAsync(new Exception("DB error"));

            Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(query, CancellationToken.None));
        }
    }
}