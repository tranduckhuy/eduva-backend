using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Users.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Eduva.Application.Features.Users.Commands
{
    public class ImportUsersFromExcelCommandHandler : IRequestHandler<ImportUsersFromExcelCommand, (CustomCode, FileResponseDto?)>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IValidator<CreateUserByAdminCommand> _validator;

        public ImportUsersFromExcelCommandHandler(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            IValidator<CreateUserByAdminCommand> validator)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _validator = validator;
        }

        public async Task<(CustomCode, FileResponseDto?)> Handle(ImportUsersFromExcelCommand request, CancellationToken cancellationToken)
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;

            ClearOldComments(worksheet, rowCount);

            var (validCommands, creationErrors) = await ValidateWorksheetAsync(worksheet, rowCount, cancellationToken);

            if (creationErrors.Count > 0)
            {
                AddErrorCommentsToWorksheet(worksheet, creationErrors);
                return await GenerateErrorFile(package);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var (_, cmd) in validCommands)
                {
                    cmd.CreatorId = request.CreatorId;
                    await _mediator.Send(cmd, cancellationToken);
                }

                await _unitOfWork.CommitAsync();
                return (CustomCode.Success, null);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw new AppException(CustomCode.SystemError);
            }
        }

        private static void ClearOldComments(ExcelWorksheet worksheet, int rowCount)
        {
            for (int row = 2; row <= rowCount; row++)
            {
                for (int col = 1; col <= 4; col++)
                {
                    var cell = worksheet.Cells[row, col];
                    if (cell.Comment != null)
                        worksheet.Comments.Remove(cell.Comment);
                    cell.Style.Fill.PatternType = ExcelFillStyle.None;
                }
            }
        }

        private async Task<(List<(int, CreateUserByAdminCommand)> ValidCommands, Dictionary<(int, int), string> Errors)> ValidateWorksheetAsync(
            ExcelWorksheet worksheet, int rowCount, CancellationToken cancellationToken)
        {
            var emailRowMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var validCommands = new List<(int Row, CreateUserByAdminCommand Cmd)>();
            var errors = new Dictionary<(int Row, int Col), string>();

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
            Enum.TryParse(roleStr, true, out Role parsedRole);

            return new CreateUserByAdminCommand
            {
                Email = email,
                FullName = fullName,
                Role = parsedRole,
                InitialPassword = password
            };
        }

        private static void ValidateRole(Role role, Dictionary<int, string> columnErrors)
        {
            if (role is Role.SystemAdmin or Role.SchoolAdmin || !Enum.IsDefined(typeof(Role), role))
                columnErrors[3] = "Invalid role";
        }

        private async Task ValidateDuplicateAndExistingEmail(string email, int row, Dictionary<string, int> emailRowMap,
            Dictionary<(int, int), string> errors, Dictionary<int, string> columnErrors)
        {
            if (emailRowMap.TryGetValue(email, out var originalRow))
            {
                columnErrors[1] = $"Duplicate email with row {originalRow}";
                errors[(originalRow, 1)] = $"Duplicate email with row {row}";
            }
            else
            {
                emailRowMap[email] = row;
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                columnErrors[1] = "Email already exists";
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
                    columnErrors[col] = error.ErrorMessage;
                }
            }
        }

        private async Task ValidatePasswordPolicy(CreateUserByAdminCommand cmd, Dictionary<int, string> columnErrors)
        {
            var user = new ApplicationUser { Email = cmd.Email, UserName = cmd.Email };
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, user, cmd.InitialPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        columnErrors[4] = error.Description;
                    }
                }
            }
        }

        private static void AddErrorCommentsToWorksheet(ExcelWorksheet worksheet, Dictionary<(int Row, int Col), string> errors)
        {
            foreach (var ((row, col), msg) in errors)
            {
                var cell = worksheet.Cells[row, col];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.MistyRose);
                if (cell.Comment == null)
                    cell.AddComment(msg, "System").AutoFit = true;
            }

            worksheet.Cells.AutoFitColumns();
        }

        private static async Task<(CustomCode, FileResponseDto?)> GenerateErrorFile(ExcelPackage package)
        {
            using var output = new MemoryStream();
            await package.SaveAsAsync(output);
            return (
                CustomCode.ProvidedInformationIsInValid,
                new FileResponseDto
                {
                    FileName = $"user_import_error_{DateTime.Now:dd_MM_yyyy}.xlsx",
                    Content = output.ToArray()
                });
        }
    }
}