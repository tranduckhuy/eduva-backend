using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
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


namespace Eduva.Application.Test.Features.Users.Commands;

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

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync())
        .ReturnsAsync(Mock.Of<IDbContextTransaction>());

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
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);
        _unitOfWorkMock.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);

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
        _unitOfWorkMock.Verify(x => x.RollbackAsync(), Times.Once);
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

    #endregion

}