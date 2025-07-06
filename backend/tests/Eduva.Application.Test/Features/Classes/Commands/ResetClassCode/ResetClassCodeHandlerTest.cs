using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Commands.ResetClassCode;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Classes.Commands.ResetClassCode
{
    [TestFixture]
    public class ResetClassCodeHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private ResetClassCodeHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _handler = new ResetClassCodeHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_Should_Reset_Class_Code_When_Teacher_Or_Admin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                ClassCode = "OLD123",
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "School Name" }
            };
            var teacher = new ApplicationUser { Id = teacherId };

            var command = new ResetClassCodeCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(false);
            _classroomRepoMock.Setup(r => r.Update(It.IsAny<Classroom>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Mock AppMapper.Mapper
            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<ClassResponse>(It.IsAny<Classroom>()))
                .Returns((Classroom c) => new ClassResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    TeacherId = c.TeacherId,
                    SchoolId = c.SchoolId,
                    ClassCode = c.ClassCode,
                    TeacherName = c.Teacher?.FullName ?? "",
                    SchoolName = c.School?.Name ?? ""
                });

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
                Assert.Multiple(() =>
                {
                    Assert.That(result.ClassCode, Is.Not.EqualTo("OLD123"));
                    Assert.That(result.TeacherName, Is.EqualTo("Teacher Name"));
                    Assert.That(result.SchoolName, Is.EqualTo("School Name"));
                });
                _classroomRepoMock.Verify(r => r.Update(It.IsAny<Classroom>()), Times.Once);
                _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found()
        {
            var command = new ResetClassCodeCommand { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid() };
            _classroomRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Found()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, TeacherId = teacherId };
            var command = new ResetClassCodeCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_Not_Teacher_Or_Admin()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid() };
            var user = new ApplicationUser { Id = teacherId };
            var command = new ResetClassCodeCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.NotTeacherOfClass));
        }

        [Test]
        public void Handle_Should_Throw_When_ClassCode_Duplicate()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, TeacherId = teacherId, ClassCode = "OLD123" };
            var teacher = new ApplicationUser { Id = teacherId };
            var command = new ResetClassCodeCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassCodeDuplicate));
        }

        [Test]
        public void Handle_Should_Throw_When_Commit_Fails()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                ClassCode = "OLD123",
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "School Name" }
            };
            var teacher = new ApplicationUser { Id = teacherId };
            var command = new ResetClassCodeCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Classroom, bool>>>())).ReturnsAsync(false);
            _classroomRepoMock.Setup(r => r.Update(It.IsAny<Classroom>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));

            // Mock AppMapper.Mapper
            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<ClassResponse>(It.IsAny<Classroom>()))
                .Returns((Classroom c) => new ClassResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    TeacherId = c.TeacherId,
                    SchoolId = c.SchoolId,
                    ClassCode = c.ClassCode,
                    TeacherName = c.Teacher?.FullName ?? "",
                    SchoolName = c.School?.Name ?? ""
                });

            var appMapperType = typeof(AppMapper);
            var mapperField = appMapperType.GetField("_mapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalMapper = mapperField?.GetValue(null);

            try
            {
                if (mapperField != null)
                    mapperField.SetValue(null, mockMapper.Object);

                var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
                Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassUpdateFailed));
            }
            finally
            {
                if (mapperField != null && originalMapper != null)
                    mapperField.SetValue(null, originalMapper);
            }
        }
    }
}