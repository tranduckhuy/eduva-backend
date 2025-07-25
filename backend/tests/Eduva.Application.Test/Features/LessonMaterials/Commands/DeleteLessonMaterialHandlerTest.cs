using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class DeleteLessonMaterialHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private Mock<IStorageService> _storageServiceMock = null!;
        private Mock<ILogger<DeleteLessonMaterialHandler>> _loggerMock = null!;
        private DeleteLessonMaterialHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _storageServiceMock = new Mock<IStorageService>();
            _loggerMock = new Mock<ILogger<DeleteLessonMaterialHandler>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new DeleteLessonMaterialHandler(_unitOfWorkMock.Object, _loggerMock.Object, _storageServiceMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_LessonMaterialNotFoundException_If_NotFound()
        {
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial>());

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = Guid.NewGuid(),
                SchoolId = 1,
                Permanent = false
            };

            Assert.ThrowsAsync<LessonMaterialNotFoundException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_ForbiddenException_If_SchoolId_Not_Match()
        {
            var userId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var material = new LessonMaterial { Id = ids[0], SchoolId = 2, CreatedByUserId = userId };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial> { material });

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = Guid.NewGuid(),
                SchoolId = 1,
                Permanent = false
            };

            Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_ForbiddenException_If_Not_Owner()
        {
            var ids = new List<Guid> { Guid.NewGuid() };
            var material = new LessonMaterial { Id = ids[0], SchoolId = 1, CreatedByUserId = Guid.NewGuid() };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial> { material });

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = Guid.NewGuid(),
                SchoolId = 1,
                Permanent = false
            };

            Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Remove_If_Permanent_And_Status_Deleted_And_Not_AIContent()
        {
            var userId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var material = new LessonMaterial
            {
                Id = ids[0],
                SchoolId = 1,
                CreatedByUserId = userId,
                Status = EntityStatus.Deleted,
                IsAIContent = false,
                SourceUrl = "file-url"
            };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial> { material });

            _lessonMaterialRepoMock.Setup(r => r.Remove(material));
            _storageServiceMock.Setup(s => s.DeleteFileAsync(It.Is<string>(url => url == material.SourceUrl), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = userId,
                SchoolId = 1,
                Permanent = true
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Remove(material), Times.Once);
            _storageServiceMock.Verify(s => s.DeleteRangeFileAsync(It.Is<List<string>>(urls => urls.Count == 1 && urls.Contains(material.SourceUrl)),
    true), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public async Task Handle_Should_Remove_If_Permanent_And_Status_Deleted_And_AIContent()
        {
            var userId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var material = new LessonMaterial
            {
                Id = ids[0],
                SchoolId = 1,
                CreatedByUserId = userId,
                Status = EntityStatus.Deleted,
                IsAIContent = true,
                SourceUrl = "file-url"
            };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial> { material });

            _lessonMaterialRepoMock.Setup(r => r.Remove(material));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = userId,
                SchoolId = 1,
                Permanent = true
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Remove(material), Times.Once);
            _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), false), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public async Task Handle_Should_Update_Status_If_Not_Permanent()
        {
            var userId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var material = new LessonMaterial
            {
                Id = ids[0],
                SchoolId = 1,
                CreatedByUserId = userId,
                Status = EntityStatus.Active
            };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial> { material });

            _lessonMaterialRepoMock.Setup(r => r.Update(material));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = userId,
                SchoolId = 1,
                Permanent = false
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Update(material), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(material.Status, Is.EqualTo(EntityStatus.Deleted));
                Assert.That(result, Is.EqualTo(Unit.Value));
            });
        }

        [Test]
        public async Task Handle_Should_SoftDelete_If_Permanent_But_Status_Not_Deleted()
        {
            var userId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var material = new LessonMaterial
            {
                Id = ids[0],
                SchoolId = 1,
                CreatedByUserId = userId,
                Status = EntityStatus.Active,
                IsAIContent = false,
                SourceUrl = "file-url"
            };

            _lessonMaterialRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LessonMaterial, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LessonMaterial> { material });

            _lessonMaterialRepoMock.Setup(r => r.Remove(material));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var cmd = new DeleteLessonMaterialCommand
            {
                Ids = ids,
                UserId = userId,
                SchoolId = 1,
                Permanent = true
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Remove(material), Times.Once);
            _lessonMaterialRepoMock.Verify(r => r.Update(It.IsAny<LessonMaterial>()), Times.Never);
            _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>(), false), Times.Never);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }
    }
}