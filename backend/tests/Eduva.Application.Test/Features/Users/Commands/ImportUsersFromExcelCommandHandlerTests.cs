using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OfficeOpenXml;

namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class ImportUsersFromExcelCommandHandlerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<IValidator<CreateUserByAdminCommand>> _validatorMock = default!;
        private ImportUsersFromExcelCommandHandler _handler = default!;

        #region ImportUsersFromExcelCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                new IUserValidator<ApplicationUser>[0],
                new IPasswordValidator<ApplicationUser>[0],
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>());

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mediatorMock = new Mock<IMediator>();
            _validatorMock = new Mock<IValidator<CreateUserByAdminCommand>>();

            _handler = new ImportUsersFromExcelCommandHandler(
                _userManagerMock.Object,
                _unitOfWorkMock.Object,
                _mediatorMock.Object,
                _validatorMock.Object);
        }

        #endregion

        #region ImportUsersFromExcelCommandHandler Tests

        [Test]
        public async Task Handle_ShouldReturnErrorFile_WhenValidationFails()
        {
            var file = CreateTestExcelFile([
                ("invalid", "", "SystemAdmin", "123")
            ]);

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult([
                    new FluentValidation.Results.ValidationFailure("Email", "Invalid email")
                ]));

            var command = new ImportUsersFromExcelCommand
            {
                File = file,
                CreatorId = Guid.NewGuid()
            };

            var (code, fileResponse) = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
                Assert.That(fileResponse, Is.Not.Null);
            });
            Assert.That(fileResponse!.Content.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Handle_ShouldThrow_WhenTransactionFails()
        {
            var file = CreateTestExcelFile([
                ("user@example.com", "Valid User", "Teacher", "StrongP@ss1")
            ]);

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);

            var transactionMock = new Mock<IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("fail"));
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var command = new ImportUsersFromExcelCommand
            {
                File = file,
                CreatorId = Guid.NewGuid()
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public async Task Handle_ShouldImportSuccessfully()
        {
            var file = CreateTestExcelFile([
                ("user@example.com", "Valid User", "Teacher", "StrongP@ss1")
            ]);

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);

            var transactionMock = new Mock<IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var command = new ImportUsersFromExcelCommand
            {
                File = file,
                CreatorId = Guid.NewGuid()
            };

            var (code, fileResponse) = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(fileResponse, Is.Null);
            });
        }

        #endregion

        #region Helper Methods

        private IFormFile CreateTestExcelFile(IEnumerable<(string Email, string Name, string Role, string Password)> rows)
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Users");

            ws.Cells[1, 1].Value = "Email";
            ws.Cells[1, 2].Value = "FullName";
            ws.Cells[1, 3].Value = "Role";
            ws.Cells[1, 4].Value = "Password";

            int row = 2;
            foreach (var (email, name, role, pass) in rows)
            {
                ws.Cells[row, 1].Value = email;
                ws.Cells[row, 2].Value = name;
                ws.Cells[row, 3].Value = role;
                ws.Cells[row, 4].Value = pass;
                row++;
            }

            using var originalStream = new MemoryStream();
            package.SaveAs(originalStream);

            var copiedStream = new MemoryStream(originalStream.ToArray());
            copiedStream.Position = 0;

            return new FormFile(copiedStream, 0, copiedStream.Length, "Data", "test.xlsx");
        }

        #endregion

    }
}