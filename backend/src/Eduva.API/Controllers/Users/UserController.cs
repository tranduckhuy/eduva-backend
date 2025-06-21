using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Features.Users.DTOs;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Requests;
using Eduva.Application.Features.Users.Responses;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Configurations.ExcelTemplate;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Eduva.API.Controllers.Users
{
    [Route("api/users")]
    public class UserController : BaseController<UserController>
    {
        private readonly IMediator _mediator;
        private readonly ImportTemplateConfig _importTemplateConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public UserController(ILogger<UserController> logger, IOptions<ImportTemplateConfig> importTemplateOptions, IHttpClientFactory httpClientFactory, IMediator mediator) : base(logger)
        {
            _mediator = mediator;
            _importTemplateConfig = importTemplateOptions.Value;
            _httpClientFactory = httpClientFactory;
        }

        // Get the current user information
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> GetUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
                return Respond(CustomCode.UserIdNotFound);

            var query = new GetUserProfileQuery(UserId: id);

            return await HandleRequestAsync(async () =>
                {
                    var result = await _mediator.Send(query);
                    return (CustomCode.Success, result);
                }
            );
        }

        // Get user information by ID
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid userId)
        {
            var query = new GetUserProfileQuery(UserId: userId);
            return await HandleRequestAsync(async () =>
                {
                    var result = await _mediator.Send(query);
                    return (CustomCode.Success, result);
                }
            );
        }

        // Update user profile
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateUserProfileCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
                return Respond(CustomCode.UserIdNotFound);

            command.UserId = id;
            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            }
            );
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserByAdminCommand command)
        {
            var creatorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(creatorIdString, out var creatorId))
                return Respond(CustomCode.UserIdNotFound);

            command.CreatorId = creatorId;
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPost("import")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [ProducesResponseType(typeof(ApiResponse<FileResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportUsersFromExcel([FromForm] ImportUsersFromExcelRequest request)
        {
            var file = request.File;

            if (file is null || file.Length == 0)
                return Respond(CustomCode.FileIsRequired);

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Respond(CustomCode.InvalidFileType);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var creatorId))
                return Respond(CustomCode.UserIdNotFound);

            var (code, fileResponse) = await _mediator.Send(new ImportUsersFromExcelCommand
            {
                File = file,
                CreatorId = creatorId
            });

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