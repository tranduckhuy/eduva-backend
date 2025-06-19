using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Schools
{
    [TestFixture]
    public class CreateSchoolCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
        private Mock<ISchoolRepository> _schoolRepoMock = default!;
        private CreateSchoolCommandHandler _handler = default!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _schoolRepoMock = new Mock<ISchoolRepository>();

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
                .Returns(_schoolRepoMock.Object);

            _handler = new CreateSchoolCommandHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_ShouldInitialize()
        {
            var handler = new CreateSchoolCommandHandler(_unitOfWorkMock.Object);
            Assert.That(handler, Is.Not.Null);
        }

        [Test]
        public async Task Handle_ShouldThrowUserIdNotFound_WhenUserNotExist()
        {
            var command = new CreateSchoolCommand { SchoolAdminId = Guid.NewGuid() };

            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            var ex = await TestDelegateWithException<AppException>(() => _handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task Handle_ShouldThrowUserAlreadyHasSchool_WhenSchoolIdExists()
        {
            var command = new CreateSchoolCommand { SchoolAdminId = Guid.NewGuid() };

            var user = new ApplicationUser { Id = command.SchoolAdminId, SchoolId = 1 };

            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);

            await TestDelegateWithException<UserAlreadyHasSchoolException>(() => _handler.Handle(command, default));
        }

        [Test]
        public async Task Handle_ShouldCreateSchoolAndReturnResponse_WhenValid()
        {
            var command = new CreateSchoolCommand
            {
                SchoolAdminId = Guid.NewGuid(),
                Name = "Eduva",
                ContactEmail = "eduva@email.com",
                ContactPhone = "0909123456",
                Address = "HCM",
                WebsiteUrl = "https://eduva.vn"
            };

            var user = new ApplicationUser { Id = command.SchoolAdminId };

            _userRepoMock.Setup(r => r.GetByIdAsync(command.SchoolAdminId))
                .ReturnsAsync(user);

            _schoolRepoMock.Setup(r => r.AddAsync(It.IsAny<School>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.SetupSequence(u => u.CommitAsync())
                .ReturnsAsync(1)
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo(command.Name));
                Assert.That(result.ContactEmail, Is.EqualTo(command.ContactEmail));
                Assert.That(result.ContactPhone, Is.EqualTo(command.ContactPhone));
                Assert.That(result.Address, Is.EqualTo(command.Address));
                Assert.That(result.WebsiteUrl, Is.EqualTo(command.WebsiteUrl));
                Assert.That(result.Status, Is.EqualTo(EntityStatus.Inactive));
            });

            _schoolRepoMock.Verify(r => r.AddAsync(It.IsAny<School>()), Times.Once);
            _userRepoMock.Verify(r => r.Update(It.Is<ApplicationUser>(u => u.SchoolId != null)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));
        }

        private static async Task<TException?> TestDelegateWithException<TException>(Func<Task> testDelegate)
            where TException : Exception
        {
            try
            {
                await testDelegate();
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (TException ex)
            {
                return ex;
            }
            return null;
        }
    }
}