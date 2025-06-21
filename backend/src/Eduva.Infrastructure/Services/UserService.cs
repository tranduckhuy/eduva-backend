using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Eduva.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomCode> CreateUserByAdminAsync(CreateUserByAdminRequestDto request, Guid creatorId)
        {
            var role = request.Role;

            if (role is Role.SystemAdmin or Role.SchoolAdmin)
            {
                throw new InvalidRestrictedRoleException();
            }

            if (string.IsNullOrWhiteSpace(request.InitialPassword))
            {
                throw new AppException(CustomCode.ProvidedInformationIsInValid);
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new EmailAlreadyExistsException();
            }

            var creator = await _userManager.FindByIdAsync(creatorId.ToString()) ?? throw new UserNotExistsException();

            if (creator.SchoolId == null)
            {
                throw new UserNotPartOfSchoolException();
            }

            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                UserName = request.Email,
                FullName = request.FullName,
                SchoolId = creator.SchoolId,
            };

            var result = await _userManager.CreateAsync(newUser, request.InitialPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new AppException(CustomCode.ProvidedInformationIsInValid, errors);
            }

            await _userManager.AddToRoleAsync(newUser, role.ToString());

            return CustomCode.Success;
        }

        public async Task<(CustomCode, FileResponseDto?)> ImportUsersFromExcelAsync(IFormFile file, Guid creatorId)
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;

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

            var fileEmailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasErrors = false;
            var validDtos = new List<(int Row, CreateUserByAdminRequestDto Dto)>();

            for (int row = 2; row <= rowCount; row++)
            {
                var email = worksheet.Cells[row, 1].Text.Trim();
                var fullName = worksheet.Cells[row, 2].Text.Trim();
                var roleStr = worksheet.Cells[row, 3].Text.Trim();
                var password = worksheet.Cells[row, 4].Text.Trim();

                var columnErrors = new Dictionary<int, string>();

                if (!Enum.TryParse(roleStr, ignoreCase: true, out Role parsedRole))
                    columnErrors[3] = "Invalid role";

                if (!fileEmailSet.Add(email))
                    columnErrors[1] = "Duplicate email in file";

                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                    columnErrors[1] = "Email already exists";

                var dto = new CreateUserByAdminRequestDto
                {
                    Email = email,
                    FullName = fullName,
                    Role = parsedRole,
                    InitialPassword = password
                };

                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(dto);
                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    foreach (var result in validationResults)
                    {
                        var member = result.MemberNames.FirstOrDefault()?.ToLower() ?? "";
                        var col = member switch
                        {
                            var n when n.Contains("email") => 1,
                            var n when n.Contains("fullname") => 2,
                            var n when n.Contains("role") => 3,
                            var n when n.Contains("password") => 4,
                            _ => 1
                        };
                        columnErrors[col] = result.ErrorMessage ?? "Invalid value";
                    }
                }

                if (columnErrors.Count > 0)
                {
                    hasErrors = true;
                    foreach (var (col, msg) in columnErrors)
                    {
                        var cell = worksheet.Cells[row, col];
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.MistyRose);
                        if (cell.Comment == null)
                            cell.AddComment(msg, "System").AutoFit = true;
                    }
                    continue;
                }

                validDtos.Add((row, dto));
            }

            var creationErrors = new Dictionary<(int Row, int Col), string>();

            foreach (var (row, dto) in validDtos)
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email
                };

                foreach (var validator in _userManager.PasswordValidators)
                {
                    var result = await validator.ValidateAsync(_userManager, user, dto.InitialPassword);
                    if (!result.Succeeded)
                    {
                        hasErrors = true;
                        foreach (var error in result.Errors)
                        {
                            creationErrors[(row, 4)] = error.Description;
                        }
                    }
                }
            }

            if (hasErrors)
            {
                foreach (var ((row, col), msg) in creationErrors)
                {
                    var cell = worksheet.Cells[row, col];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.MistyRose);
                    if (cell.Comment == null)
                        cell.AddComment(msg, "System").AutoFit = true;
                }

                worksheet.Cells.AutoFitColumns();
                using var output = new MemoryStream();
                await package.SaveAsAsync(output);

                return (CustomCode.ProvidedInformationIsInValid, new FileResponseDto
                {
                    FileName = $"user_import_error_{DateTime.Now:dd_MM_yyyy}.xlsx",
                    Content = output.ToArray()
                });
            }

            foreach (var (_, dto) in validDtos)
            {
                await CreateUserByAdminAsync(dto, creatorId);
            }

            return (CustomCode.Success, null);
        }
    }
}