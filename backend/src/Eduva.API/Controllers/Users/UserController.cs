using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Users.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Configurations.ExcelTemplate;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Eduva.API.Controllers.Users
{
    [Route("api/users")]
    public class UserController : BaseController<UserController>
    {
        private readonly IUserService _userService;
        private readonly ImportTemplateConfig _importTemplateConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public UserController(IUserService userService, IOptions<ImportTemplateConfig> importTemplateOptions, IHttpClientFactory httpClientFactory, ILogger<UserController> logger) : base(logger)
        {
            _userService = userService;
            _importTemplateConfig = importTemplateOptions.Value;
            _httpClientFactory = httpClientFactory;
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
            try
            {
                var httpClient = _httpClientFactory.CreateClient("EduvaHttpClient");
                var fileBytes = await httpClient.GetByteArrayAsync(_importTemplateConfig.Url);

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