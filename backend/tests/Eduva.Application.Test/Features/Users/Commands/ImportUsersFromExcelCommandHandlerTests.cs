using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OfficeOpenXml;
using System.Reflection;


namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class ImportUsersFromExcelCommandHandlerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<IValidator<CreateUserByAdminCommand>> _validatorMock = default!;
        private Mock<ISchoolValidationService> _schoolValidationServiceMock = default!;
        private ImportUsersFromExcelCommandHandler _handler = default!;

        #region ImportUsersFromExcelCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            var storeMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                storeMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mediatorMock = new Mock<IMediator>();
            _validatorMock = new Mock<IValidator<CreateUserByAdminCommand>>();
            _schoolValidationServiceMock = new Mock<ISchoolValidationService>();

            _handler = new ImportUsersFromExcelCommandHandler(
                _userManagerMock.Object,
                _unitOfWorkMock.Object,
                _mediatorMock.Object,
                _validatorMock.Object,
                _schoolValidationServiceMock.Object
            );
        }

        #endregion

        #region ImportUsersFromExcelCommandHandler Tests

        [Test]
        public async Task ValidateDuplicateAndExistingEmail_Should_Add_Errors_When_Email_Duplicated_In_File()
        {
            // Arrange
            var handlerType = typeof(ImportUsersFromExcelCommandHandler);
            var method = handlerType.GetMethod("ValidateDuplicateAndExistingEmail", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var emailRowMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["user@example.com"] = 2
            };
            var columnErrors = new Dictionary<int, string>();
            var errors = new Dictionary<(int, int), string>();

            var handler = new ImportUsersFromExcelCommandHandler(
                CreateUserManagerWithEmailStore(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<IMediator>(),
                Mock.Of<IValidator<CreateUserByAdminCommand>>(),
                Mock.Of<ISchoolValidationService>()
            );

            // Act
            await (Task)method.Invoke(handler,
            [
            "user@example.com",
            4,
            emailRowMap,
            errors,
            columnErrors
             ])!;

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(columnErrors[1], Is.EqualTo("Email trùng với dòng 2"));
                Assert.That(errors[(2, 1)], Is.EqualTo("Email trùng với dòng 4"));
            });
        }

        [Test]
        public async Task ValidateFluent_Should_MapPropertyToCorrectColumn()
        {
            // Arrange
            var handlerType = typeof(ImportUsersFromExcelCommandHandler);
            var method = handlerType.GetMethod("ValidateFluent", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var errors = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Email is required"),
            new("FullName", "Full name is required"),
            new("Role", "Invalid role"),
            new("Password", "Password is required"),
            new("SomethingElse", "Email is required")
        };

            var validator = new Mock<IValidator<CreateUserByAdminCommand>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new FluentValidation.Results.ValidationResult(errors));

            var handler = new ImportUsersFromExcelCommandHandler(
                CreateUserManagerWithEmailStore(), // mock đầy đủ UserManager
                Mock.Of<IUnitOfWork>(),
                Mock.Of<IMediator>(),
                validator.Object,
                Mock.Of<ISchoolValidationService>()
            );

            var cmd = new CreateUserByAdminCommand();
            var columnErrors = new Dictionary<int, string>();

            // Act
            await (Task)method.Invoke(handler, [cmd, columnErrors, CancellationToken.None])!;

            // Assert
            Assert.That(columnErrors, Has.Count.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(columnErrors[1], Is.EqualTo("Vui lòng nhập email"));
                Assert.That(columnErrors[2], Is.EqualTo("Vui lòng nhập họ và tên"));
                Assert.That(columnErrors[3], Is.EqualTo("Vai trò không hợp lệ"));
                Assert.That(columnErrors[4], Is.EqualTo("Vui lòng nhập mật khẩu"));
            });
        }

        [TestCase("Email is required", "Vui lòng nhập email")]
        [TestCase("Invalid email address", "Email không hợp lệ")]
        [TestCase("Unknown message", "Giá trị không hợp lệ")]
        public void TranslateToVietnamese_Should_Return_Expected_Message(string input, string expected)
        {
            var result = typeof(ImportUsersFromExcelCommandHandler)
                .GetMethod("TranslateToVietnamese", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [input]);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("giáo viên", Role.Teacher)]
        [TestCase("học sinh", Role.Student)]
        [TestCase("kiểm duyệt viên", Role.ContentModerator)]
        [TestCase("quản trị trường", Role.SchoolAdmin)]
        [TestCase("quản trị hệ thống", Role.SystemAdmin)]
        [TestCase("unknown", (Role)(-1))]
        public void TryMapRole_Should_Map_Correctly(string input, Role expected)
        {
            var result = typeof(ImportUsersFromExcelCommandHandler)
                .GetMethod("TryMapRole", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [input]);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ValidateRole_Should_Add_Error_For_SystemAdmin_And_SchoolAdmin()
        {
            var errors = new Dictionary<int, string>();

            typeof(ImportUsersFromExcelCommandHandler)
                .GetMethod("ValidateRole", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [Role.SystemAdmin, errors]);

            Assert.That(errors[3], Is.EqualTo("Không được chọn vai trò Quản trị hệ thống"));

            errors.Clear();

            typeof(ImportUsersFromExcelCommandHandler)
                .GetMethod("ValidateRole", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [Role.SchoolAdmin, errors]);

            Assert.That(errors[3], Is.EqualTo("Không được chọn vai trò Quản trị trường"));
        }

        [Test]
        public void GetLastDataRow_Should_Return_1_When_Empty()
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Sheet1");
            sheet.Cells[1, 1].Value = "Email";

            var method = typeof(ImportUsersFromExcelCommandHandler)
                .GetMethod("GetLastDataRow", BindingFlags.NonPublic | BindingFlags.Static)!;

            var result = (int)method.Invoke(null, new object[] { sheet })!;
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task ValidatePasswordPolicy_Should_Return_All_ErrorMessages()
        {
            var handlerType = typeof(ImportUsersFromExcelCommandHandler);
            var method = handlerType.GetMethod("ValidatePasswordPolicy", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var mockValidator = new Mock<IPasswordValidator<ApplicationUser>>();
            mockValidator.Setup(x => x.ValidateAsync(It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Code = "PasswordTooShort" },
                    new IdentityError { Code = "PasswordRequiresDigit" },
                    new IdentityError { Code = "PasswordRequiresLower" },
                    new IdentityError { Code = "PasswordRequiresUpper" },
                    new IdentityError { Code = "PasswordRequiresNonAlphanumeric" },
                    new IdentityError { Code = "UnknownCode" }
                ));

            var storeMock = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new UserManager<ApplicationUser>(
                storeMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                [],
                [mockValidator.Object],
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );

            var handler = new ImportUsersFromExcelCommandHandler(
                userManager,
                Mock.Of<IUnitOfWork>(),
                Mock.Of<IMediator>(),
                Mock.Of<IValidator<CreateUserByAdminCommand>>(),
                Mock.Of<ISchoolValidationService>()
            );

            var columnErrors = new Dictionary<int, string>();
            var cmd = new CreateUserByAdminCommand
            {
                Email = "user@example.com",
                InitialPassword = "short"
            };

            await (Task)method.Invoke(handler, [cmd, columnErrors])!;

            var result = columnErrors[4];
            var lines = result.Split('\n');

            Assert.That(lines, Does.Contain("Mật khẩu phải có ít nhất 8 ký tự"));
            Assert.That(lines, Does.Contain("Mật khẩu phải chứa ít nhất một chữ số"));
            Assert.That(lines, Does.Contain("Mật khẩu phải chứa ít nhất một chữ thường"));
            Assert.That(lines, Does.Contain("Mật khẩu phải chứa ít nhất một chữ hoa"));
            Assert.That(lines, Does.Contain("Mật khẩu phải chứa ký tự đặc biệt"));
            Assert.That(lines, Does.Contain("Mật khẩu không hợp lệ"));
        }

        [Test]
        public void Should_Throw_When_File_Is_Null()
        {
            var command = new ImportUsersFromExcelCommand
            {
                File = null!,
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FileIsRequired));
        }

        [Test]
        public void Should_Throw_When_File_Has_Invalid_Extension()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("users.txt");
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.InvalidFileType));
        }

        [Test]
        public void Should_Throw_When_File_Has_Invalid_ContentType()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("users.xlsx");
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.InvalidFileType));
        }

        [Test]
        public void Should_Throw_When_User_Not_Exists()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("users.xlsx");
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ApplicationUser?)null);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>()).Returns(userRepoMock.Object);

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Should_Throw_When_User_Has_No_School()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("users.xlsx");
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new ApplicationUser { SchoolId = null });

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>()).Returns(userRepoMock.Object);

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(command, default));
            Assert.That(ex, Is.TypeOf<SchoolNotFoundException>());
        }

        [Test]
        public async Task Should_Return_ErrorFile_When_Has_ValidationErrors()
        {
            var fileName = "users.xlsx";
            var fileContent = GenerateExcelWithInvalidRows();
            var stream = new MemoryStream(fileContent);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(fileContent.Length);
            fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            var user = new ApplicationUser { SchoolId = 1 };
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>()).Returns(userRepoMock.Object);

            _userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!, Is.Not.Empty);
        }

        [Test]
        public async Task Should_Import_Successfully_When_Valid()
        {
            // Arrange
            var fileName = "users.xlsx";
            var fileContent = GenerateValidExcel();
            var stream = new MemoryStream(fileContent);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(fileContent.Length);
            fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            var user = new ApplicationUser { SchoolId = 123 };
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>()).Returns(userRepoMock.Object);

            _userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _schoolValidationServiceMock.Setup(x =>
                x.ValidateCanAddUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            ).Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);


            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Null);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
            _mediatorMock.Verify(x => x.Send(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Should_Rollback_When_CreateUserFails()
        {
            // Arrange
            var fileContent = GenerateValidExcel();
            var stream = new MemoryStream(fileContent);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("users.xlsx");
            fileMock.Setup(f => f.Length).Returns(fileContent.Length);
            fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            var user = new ApplicationUser { SchoolId = 1 };
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserRepository>()).Returns(userRepoMock.Object);

            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _schoolValidationServiceMock
                .Setup(x => x.ValidateCanAddUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var dbTransactionMock = new Mock<IDbContextTransaction>();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Boom!"));

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            // Act + Assert
            AppException? ex = null;
            try
            {
                await _handler.Handle(command, default);
            }
            catch (AppException e)
            {
                ex = e;
            }

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.InvalidFileType));
        }

        [Test]
        public void Should_Throw_When_Exceeding_UserQuota()
        {
            var fileContent = GenerateValidExcel();
            var stream = new MemoryStream(fileContent);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("users.xlsx");
            fileMock.Setup(f => f.Length).Returns(fileContent.Length);
            fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            var user = new ApplicationUser { SchoolId = 1 };
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>()).Returns(userRepoMock.Object);

            _userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _schoolValidationServiceMock
                .Setup(x => x.ValidateCanAddUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AppException(CustomCode.ExceedUserLimit));

            var command = new ImportUsersFromExcelCommand
            {
                File = fileMock.Object,
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.InvalidFileType));
        }

        #endregion

        #region Helper Methods

        private static byte[] GenerateExcelWithInvalidRows()
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var stream = new MemoryStream();
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Sheet1");

                sheet.Cells[1, 1].Value = "Email";
                sheet.Cells[1, 2].Value = "Full Name";
                sheet.Cells[1, 3].Value = "Role";
                sheet.Cells[1, 4].Value = "Password";

                sheet.Cells[2, 1].Value = "";
                sheet.Cells[2, 2].Value = "User A";
                sheet.Cells[2, 3].Value = "Không hợp lệ";
                sheet.Cells[2, 4].Value = "123";

                package.SaveAs(stream);
            }

            return stream.ToArray();
        }

        private static byte[] GenerateValidExcel()
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var stream = new MemoryStream();
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Sheet1");

                sheet.Cells[1, 1].Value = "Email";
                sheet.Cells[1, 2].Value = "Full Name";
                sheet.Cells[1, 3].Value = "Role";
                sheet.Cells[1, 4].Value = "Password";

                sheet.Cells[2, 1].Value = "user1@example.com";
                sheet.Cells[2, 2].Value = "Nguyen Van A";
                sheet.Cells[2, 3].Value = "Giáo viên";
                sheet.Cells[2, 4].Value = "Password@123";

                sheet.Cells[3, 1].Value = "user2@example.com";
                sheet.Cells[3, 2].Value = "Tran Thi B";
                sheet.Cells[3, 3].Value = "Học sinh";
                sheet.Cells[3, 4].Value = "StrongP@ss2";

                package.SaveAs(stream);
            }

            return stream.ToArray();
        }

        private static UserManager<ApplicationUser> CreateUserManagerWithEmailStore()
        {
            var store = new Mock<IUserEmailStore<ApplicationUser>>();
            store.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ApplicationUser?)null);

            return new UserManager<ApplicationUser>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );
        }

        #endregion

    }
}