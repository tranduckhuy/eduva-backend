using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.UpdateLessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class UpdateLessonMaterialHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private UpdateLessonMaterialHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new UpdateLessonMaterialHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_LessonMaterialNotFoundException_If_NotFound()
        {
            var cmd = new UpdateLessonMaterialCommand
            {
                Id = Guid.NewGuid(),
                CreatorId = Guid.NewGuid()
            };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(cmd.Id)).ReturnsAsync((LessonMaterial?)null);

            Assert.ThrowsAsync<LessonMaterialNotFoundException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_ForbiddenException_If_Not_Owner()
        {
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = Guid.NewGuid()
            };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lessonMaterial);

            var cmd = new UpdateLessonMaterialCommand
            {
                Id = lessonId,
                CreatorId = creatorId
            };

            Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Update_LessonMaterial_Properties()
        {
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                Title = "Old Title",
                Description = "Old Description",
                Duration = 10,
                Visibility = LessonMaterialVisibility.Private
            };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lessonMaterial);
            _lessonMaterialRepoMock.Setup(r => r.Update(lessonMaterial));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new UpdateLessonMaterialCommand
            {
                Id = lessonId,
                CreatorId = creatorId,
                Title = "New Title",
                Description = "New Description",
                Duration = 20,
                Visibility = LessonMaterialVisibility.School
            };

            await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Update(lessonMaterial), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(lessonMaterial.Title, Is.EqualTo("New Title"));
                Assert.That(lessonMaterial.Description, Is.EqualTo("New Description"));
                Assert.That(lessonMaterial.Duration, Is.EqualTo(20));
                Assert.That(lessonMaterial.Visibility, Is.EqualTo(LessonMaterialVisibility.School));
            });
        }

        [Test]
        public async Task Handle_Should_Not_Overwrite_Properties_If_Null()
        {
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                Title = "Old Title",
                Description = "Old Description",
                Duration = 10,
                Visibility = LessonMaterialVisibility.Private
            };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lessonMaterial);
            _lessonMaterialRepoMock.Setup(r => r.Update(lessonMaterial));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new UpdateLessonMaterialCommand
            {
                Id = lessonId,
                CreatorId = creatorId,
                Title = null,
                Description = null,
                Duration = null,
                Visibility = null
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Update(lessonMaterial), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(lessonMaterial.Title, Is.EqualTo("Old Title"));
                Assert.That(lessonMaterial.Description, Is.EqualTo("Old Description"));
                Assert.That(lessonMaterial.Duration, Is.EqualTo(10));
                Assert.That(lessonMaterial.Visibility, Is.EqualTo(LessonMaterialVisibility.Private));
                Assert.That(result, Is.EqualTo(Unit.Value));
            });
        }

        [Test]
        public async Task Handle_Should_Update_Only_NonNull_Properties()
        {
            var lessonId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = lessonId,
                CreatedByUserId = creatorId,
                Title = "Old Title",
                Description = "Old Description",
                Duration = 10,
                Visibility = LessonMaterialVisibility.Private
            };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lessonMaterial);
            _lessonMaterialRepoMock.Setup(r => r.Update(lessonMaterial));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new UpdateLessonMaterialCommand
            {
                Id = lessonId,
                CreatorId = creatorId,
                Title = "New Title",
                Description = null,
                Duration = 30,
                Visibility = null
            };

            await _handler.Handle(cmd, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(lessonMaterial.Title, Is.EqualTo("New Title"));
                Assert.That(lessonMaterial.Description, Is.EqualTo("Old Description"));
                Assert.That(lessonMaterial.Duration, Is.EqualTo(30));
                Assert.That(lessonMaterial.Visibility, Is.EqualTo(LessonMaterialVisibility.Private));
            });
        }
    }
}