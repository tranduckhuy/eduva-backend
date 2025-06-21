using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Users.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Services.Interface;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Users
{
    [Route("api/users")]
    public class UserController : BaseController<UserController>
    {
        private readonly IUserService _userService;
        private readonly IExcelService _excelService;

        public UserController(IUserService userService, IExcelService excelService, ILogger<UserController> logger) : base(logger)
        {
            _userService = userService;
            _excelService = excelService;
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] CreateUserByAdminRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var creatorId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync(() => _userService.CreateUserByAdminAsync(request, creatorId));
        }

        [HttpPost("import")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        public async Task<IActionResult> ImportUsersFromExcel([FromForm] ImportUsersFromExcelRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return Respond(CustomCode.FileIsRequired);
            }

            if (!request.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Respond(CustomCode.InvalidFileType);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var creatorId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var (code, fileResponse) = await _userService.ImportUsersFromExcelAsync(request.File, creatorId);

            if (fileResponse != null)
            {
                return Ok(new ApiResponse<FileResponseDto>
                {
                    StatusCode = (int)code,
                    Message = "Import failed. Please fix the errors in the returned file.",
                    Data = fileResponse
                });
            }

            return Ok(new ApiResponse<object>
            {
                StatusCode = (int)CustomCode.Success,
                Message = "Import successful.",
                Data = null
            });
        }

        [HttpGet("import-template")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        public async Task<IActionResult> DownloadUserImportTemplate()
        {
            const string templateUrl =
                "https://firebasestorage.googleapis.com/v0/b/gdupa-2fa82.appspot.com/o/excel-template%2Fuser-import-template.xlsx?alt=media&token=a1863610-2ab1-4d81-893b-bef6f3f6f4e0";

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var fileBytes = await httpClient.GetByteArrayAsync(templateUrl);

                return Ok(new ApiResponse<FileResponseDto>
                {
                    StatusCode = (int)CustomCode.Success,
                    Message = "Template downloaded successfully.",
                    Data = new FileResponseDto
                    {
                        FileName = "user_import_template.xlsx",
                        Content = fileBytes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch import template from Firebase");

                return Ok(new ApiResponse<FileResponseDto>
                {
                    StatusCode = (int)CustomCode.SystemError,
                    Message = "Failed to download import template.",
                    Data = null
                });
            }
        }
    }
}