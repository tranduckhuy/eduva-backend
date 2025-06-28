using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Eduva.Application.Features.Users.Commands;

public class ImportUsersFromExcelCommandHandler : IRequestHandler<ImportUsersFromExcelCommand, byte[]?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IValidator<CreateUserByAdminCommand> _validator;
    private readonly ISchoolValidationService _schoolValidationService;

    public ImportUsersFromExcelCommandHandler(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        IValidator<CreateUserByAdminCommand> validator,
        ISchoolValidationService schoolValidationService)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _validator = validator;
        _schoolValidationService = schoolValidationService;
    }

    public async Task<byte[]?> Handle(ImportUsersFromExcelCommand request, CancellationToken cancellationToken)
    {
        ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

        var creator = await ValidateRequestAsync(request);

        using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        try
        {
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = GetLastDataRow(worksheet);

            ClearOldComments(worksheet, rowCount);

            var (validCommands, creationErrors) = await ValidateWorksheetAsync(worksheet, rowCount, cancellationToken);

            if (creationErrors.Count > 0)
            {
                AddErrorCommentsToWorksheet(worksheet, creationErrors);
                using var output = new MemoryStream();
                await package.SaveAsAsync(output, cancellationToken);
                return output.ToArray();
            }

            await _schoolValidationService.ValidateCanAddUsersAsync(creator.SchoolId!.Value, validCommands.Count, cancellationToken);

            try
            {
                foreach (var (_, cmd) in validCommands)
                {
                    cmd.CreatorId = request.CreatorId;
                    await _mediator.Send(cmd, cancellationToken);
                }
                await _unitOfWork.CommitAsync();
                return null;
            }
            catch
            {
                throw new AppException(CustomCode.SystemError);
            }
        }
        catch
        {
            throw new AppException(CustomCode.InvalidFileType);
        }
    }

    private async Task<ApplicationUser> ValidateRequestAsync(ImportUsersFromExcelCommand request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new AppException(CustomCode.FileIsRequired);
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(CustomCode.InvalidFileType);
        }

        if (!request.File.ContentType.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(CustomCode.InvalidFileType);
        }

        var userRepo = _unitOfWork.GetCustomRepository<IUserRepository>();
        var user = await userRepo.GetByIdAsync(request.CreatorId) ?? throw new AppException(CustomCode.UserNotExists);

        if (user.SchoolId == null)
        {
            throw new SchoolNotFoundException();
        }

        return user;
    }

    private static void ClearOldComments(ExcelWorksheet worksheet, int rowCount)
    {
        for (int row = 2; row <= rowCount; row++)
            for (int col = 1; col <= 4; col++)
            {
                var cell = worksheet.Cells[row, col];
                if (cell.Comment != null) worksheet.Comments.Remove(cell.Comment);
                cell.Style.Fill.PatternType = ExcelFillStyle.None;
            }
    }
    private static int GetLastDataRow(ExcelWorksheet worksheet)
    {
        var lastRow = worksheet.Dimension.End.Row;

        for (int row = lastRow; row >= 2; row--)
        {
            var email = worksheet.Cells[row, 1].Text?.Trim();
            var fullName = worksheet.Cells[row, 2].Text?.Trim();
            var role = worksheet.Cells[row, 3].Text?.Trim();
            var password = worksheet.Cells[row, 4].Text?.Trim();

            if (!string.IsNullOrWhiteSpace(email) ||
                !string.IsNullOrWhiteSpace(fullName) ||
                !string.IsNullOrWhiteSpace(role) ||
                !string.IsNullOrWhiteSpace(password))
            {
                return row;
            }
        }

        return 1;
    }

    private async Task<(List<(int, CreateUserByAdminCommand)> ValidCommands, Dictionary<(int, int), string> Errors)>
        ValidateWorksheetAsync(ExcelWorksheet worksheet, int rowCount, CancellationToken cancellationToken)
    {
        var emailRowMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var validCommands = new List<(int, CreateUserByAdminCommand)>();
        var errors = new Dictionary<(int, int), string>();

        for (int row = 2; row <= rowCount; row++)
        {
            var cmd = ParseCommandFromRow(worksheet, row);
            var columnErrors = new Dictionary<int, string>();

            ValidateRole(cmd.Role, columnErrors);
            await ValidateDuplicateAndExistingEmail(cmd.Email, row, emailRowMap, errors, columnErrors);
            await ValidateFluent(cmd, columnErrors, cancellationToken);
            await ValidatePasswordPolicy(cmd, columnErrors);

            foreach (var (col, msg) in columnErrors)
                errors[(row, col)] = msg;

            if (columnErrors.Count == 0)
                validCommands.Add((row, cmd));
        }

        return (validCommands, errors);
    }

    private static CreateUserByAdminCommand ParseCommandFromRow(ExcelWorksheet worksheet, int row)
    {
        var email = worksheet.Cells[row, 1].Text.Trim();
        var fullName = worksheet.Cells[row, 2].Text.Trim();
        var roleStr = worksheet.Cells[row, 3].Text.Trim();
        var password = worksheet.Cells[row, 4].Text.Trim();

        return new CreateUserByAdminCommand
        {
            Email = email,
            FullName = fullName,
            Role = TryMapRole(roleStr),
            InitialPassword = password
        };
    }

    private static Role TryMapRole(string input)
    {
        return input.Trim().ToLower().Replace(" ", "") switch
        {
            "giáoviên" => Role.Teacher,
            "họcsinh" => Role.Student,
            "kiểmduyệtviên" => Role.ContentModerator,
            "quảntrịtrường" => Role.SchoolAdmin,
            "quảntrịhệthống" => Role.SystemAdmin,
            _ => (Role)(-1)
        };
    }

    private static void ValidateRole(Role role, Dictionary<int, string> columnErrors)
    {
        if (!Enum.IsDefined(typeof(Role), role))
        {
            columnErrors[3] = "Vai trò không hợp lệ";
        }
        else if (role == Role.SystemAdmin)
        {
            columnErrors[3] = "Không được chọn vai trò Quản trị hệ thống";
        }
        else if (role == Role.SchoolAdmin)
        {
            columnErrors[3] = "Không được chọn vai trò Quản trị trường";
        }
    }

    private async Task ValidateDuplicateAndExistingEmail(string email, int row, Dictionary<string, int> emailRowMap,
        Dictionary<(int, int), string> errors, Dictionary<int, string> columnErrors)
    {
        if (emailRowMap.TryGetValue(email, out var originalRow))
        {
            columnErrors[1] = $"Email trùng với dòng {originalRow}";
            errors[(originalRow, 1)] = $"Email trùng với dòng {row}";
        }
        else
        {
            emailRowMap[email] = row;
        }

        if (await _userManager.FindByEmailAsync(email) != null)
            columnErrors[1] = "Email đã tồn tại trong hệ thống";
    }

    private async Task ValidatePasswordPolicy(CreateUserByAdminCommand cmd, Dictionary<int, string> columnErrors)
    {
        var user = new ApplicationUser { Email = cmd.Email, UserName = cmd.Email };
        var messages = new List<string>();

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, cmd.InitialPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    var message = error.Code switch
                    {
                        "PasswordTooShort" => "Mật khẩu phải có ít nhất 8 ký tự",
                        "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất một chữ số",
                        "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất một chữ thường",
                        "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất một chữ hoa",
                        "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ký tự đặc biệt",
                        _ => "Mật khẩu không hợp lệ"
                    };

                    messages.Add(message);
                }
            }
        }

        if (messages.Count != 0)
        {
            columnErrors[4] = string.Join("\n", messages);
        }
    }

    private async Task ValidateFluent(CreateUserByAdminCommand cmd, Dictionary<int, string> columnErrors, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(cmd, cancellationToken);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                var col = error.PropertyName.ToLower() switch
                {
                    var n when n.Contains("email") => 1,
                    var n when n.Contains("fullname") => 2,
                    var n when n.Contains("role") => 3,
                    var n when n.Contains("password") => 4,
                    _ => 1
                };

                columnErrors[col] = TranslateToVietnamese(error.ErrorMessage);
            }
        }
    }

    private static string TranslateToVietnamese(string message)
    {
        return message switch
        {
            "Email is required" => "Vui lòng nhập email",
            "Invalid email address" => "Email không hợp lệ",
            "Full name is required" => "Vui lòng nhập họ và tên",
            "Full name must be less than 100 characters" => "Họ và tên không được vượt quá 100 ký tự",
            "Invalid role" => "Vai trò không hợp lệ",
            "Role cannot be SystemAdmin" => "Không được chọn vai trò Quản trị hệ thống",
            "Role cannot be SchoolAdmin" => "Không được chọn vai trò Quản trị trường",
            "Password is required" => "Vui lòng nhập mật khẩu",
            "Password must be at least 8 characters long" => "Mật khẩu phải có ít nhất 8 ký tự",
            "Password must be less than or equal to 255 characters" => "Mật khẩu không được vượt quá 255 ký tự",
            _ => "Giá trị không hợp lệ"
        };
    }

    private static void AddErrorCommentsToWorksheet(ExcelWorksheet worksheet, Dictionary<(int Row, int Col), string> errors)
    {
        foreach (var ((row, col), msg) in errors)
        {
            var cell = worksheet.Cells[row, col];
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.MistyRose);
            if (cell.Comment == null)
                cell.AddComment(msg, "Hệ thống").AutoFit = true;
        }

        worksheet.Cells.AutoFitColumns();
    }
}