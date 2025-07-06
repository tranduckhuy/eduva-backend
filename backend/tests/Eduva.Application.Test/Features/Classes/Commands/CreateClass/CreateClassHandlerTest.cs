using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Classes.Commands.CreateClass
{
    [TestFixture]
    public class CreateClassHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private CreateClassHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();
            _folderRepoMock = new Mock<IFolderRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>())
                .Returns(_schoolRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>())
                .Returns(_folderRepoMock.Object);

            _handler = new CreateClassHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_ShouldCreateClass_WhenValid()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var schoolId = 1;
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = schoolId,
                BackgroundImageUrl = "http://img"
            };

            var school = new School { Id = schoolId, Status = EntityStatus.Active };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(false);
            _classroomRepoMock.Setup(r => r.AddAsync(It.IsAny<Classroom>())).Returns(Task.CompletedTask);
            _folderRepoMock.Setup(r => r.AddAsync(It.IsAny<Folder>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Mock AppMapper.Mapper
            var classroom = new Classroom { Id = Guid.NewGuid(), Name = command.Name, TeacherId = teacherId, SchoolId = schoolId };
            var classResponse = new ClassResponse { Id = classroom.Id, Name = classroom.Name, TeacherId = teacherId, SchoolId = schoolId };

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Classroom>(It.IsAny<CreateClassCommand>())).Returns(classroom);
            mockMapper.Setup(m => m.Map<ClassResponse>(It.IsAny<Classroom>())).Returns(classResponse);

            var appMapperType = typeof(AppMapper);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                // Act
                var result = await _handler.Handle(command, CancellationToken.None);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Name, Is.EqualTo(command.Name));
                Assert.That(result.TeacherId, Is.EqualTo(command.TeacherId));
                Assert.That(result.SchoolId, Is.EqualTo(command.SchoolId));
                _classroomRepoMock.Verify(r => r.AddAsync(It.IsAny<Classroom>()), Times.Once);
                _folderRepoMock.Verify(r => r.AddAsync(It.IsAny<Folder>()), Times.Once);
                _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public void Handle_ShouldThrow_WhenSchoolNotFound()
        {
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = Guid.NewGuid(),
                SchoolId = 123,
                BackgroundImageUrl = "http://img"
            };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.SchoolId)).ReturnsAsync((School?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.SchoolNotFound));
        }

        [Test]
        public void Handle_ShouldThrow_WhenSchoolInactive()
        {
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = Guid.NewGuid(),
                SchoolId = 123,
                BackgroundImageUrl = "http://img"
            };
            var school = new School { Id = 123, Status = EntityStatus.Inactive };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.SchoolId)).ReturnsAsync(school);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.CannotCreateClassForInactiveSchool));
        }

        [Test]
        public void Handle_ShouldThrow_WhenClassNameAlreadyExistsForTeacher()
        {
            var teacherId = Guid.NewGuid();
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = 1,
                BackgroundImageUrl = "http://img"
            };
            var school = new School { Id = 1, Status = EntityStatus.Active };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.SchoolId)).ReturnsAsync(school);
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNameAlreadyExistsForTeacher));
        }

        [Test]
        public void Handle_ShouldThrow_WhenBackgroundImageUrl_IsEmpty()
        {
            var teacherId = Guid.NewGuid();
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = 1,
                BackgroundImageUrl = ""
            };
            var school = new School { Id = 1, Status = EntityStatus.Active };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.SchoolId)).ReturnsAsync(school);
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(false);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
        }

        [Test]
        public void Handle_ShouldThrow_WhenClassCodeDuplicate()
        {
            var teacherId = Guid.NewGuid();
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = 1,
                BackgroundImageUrl = "http://img"
            };
            var school = new School { Id = 1, Status = EntityStatus.Active };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(command.SchoolId)).ReturnsAsync(school);
            _classroomRepoMock.SetupSequence(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>()))
                .ReturnsAsync(false) // classExistsForTeacher
                .ReturnsAsync(true)  // codeExists (1)
                .ReturnsAsync(true)  // codeExists (2)
                .ReturnsAsync(true)  // codeExists (3)
                .ReturnsAsync(true)  // codeExists (4)
                .ReturnsAsync(true); // codeExists (5)

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassCodeDuplicate));
        }

        [Test]
        public void Handle_ShouldThrow_WhenCommitFails()
        {
            var teacherId = Guid.NewGuid();
            var schoolId = 1;
            var command = new CreateClassCommand
            {
                Name = "Math",
                TeacherId = teacherId,
                SchoolId = schoolId,
                BackgroundImageUrl = "http://img"
            };

            var school = new School { Id = schoolId, Status = EntityStatus.Active };
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(false);
            _classroomRepoMock.Setup(r => r.AddAsync(It.IsAny<Classroom>())).Returns(Task.CompletedTask);
            _folderRepoMock.Setup(r => r.AddAsync(It.IsAny<Folder>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));

            // Mock AppMapper.Mapper
            var classroom = new Classroom { Id = Guid.NewGuid(), Name = command.Name, TeacherId = teacherId, SchoolId = schoolId };
            var classResponse = new ClassResponse { Id = classroom.Id, Name = classroom.Name, TeacherId = teacherId, SchoolId = schoolId };

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<Classroom>(It.IsAny<CreateClassCommand>())).Returns(classroom);
            mockMapper.Setup(m => m.Map<ClassResponse>(It.IsAny<Classroom>())).Returns(classResponse);

            var appMapperType = typeof(AppMapper);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
                Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassCreateFailed));
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }
    }
}