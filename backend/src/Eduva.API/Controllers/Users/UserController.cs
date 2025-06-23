using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Users.Commands;
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
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportUsersFromExcel([FromForm] ImportUsersFromExcelRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var creatorId))
                return Respond(CustomCode.UserIdNotFound);

            try
            {
                var fileBytes = await _mediator.Send(new ImportUsersFromExcelCommand
                {
                    File = request.File,
                    CreatorId = creatorId
                });

                if (fileBytes != null)
                {
                    return File(
                        fileContents: fileBytes,
                        contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileDownloadName: $"user_import_error_{DateTime.Now:dd_MM_yyyy}.xlsx");
                }

                return File(
                        fileContents: [],
                        contentType: "application/octet-stream",
                        fileDownloadName: "Empty.xlsx");
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode);
            }
        }

        [HttpGet("import-template")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadUserImportTemplate()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("EduvaHttpClient");
                var fileBytes = await httpClient.GetByteArrayAsync(_importTemplateConfig.Url);

                return File(
                    fileContents: fileBytes,
                    contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileDownloadName: "user_import_template.xlsx"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to load template from Firebase.");
                return Respond(CustomCode.SystemError);
            }
        }
    }
}