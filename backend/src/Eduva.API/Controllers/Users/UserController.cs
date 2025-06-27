using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Requests;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Features.Users.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Configurations.ExcelTemplate;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ILogger<UserController> logger, IOptions<ImportTemplateConfig> importTemplateOptions, IHttpClientFactory httpClientFactory, IMediator mediator, UserManager<ApplicationUser> userManager) : base(logger)
        {
            _mediator = mediator;
            _importTemplateConfig = importTemplateOptions.Value;
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
        }

        // Get the current user information
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> GetUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

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

        [HttpGet]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<UserResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsersAsync([FromQuery] UserSpecParam param)
        {
            if (User.IsInRole(nameof(Role.SchoolAdmin)))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var userId))
                {
                    return Respond(CustomCode.UserIdNotFound);
                }

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user?.SchoolId == null)
                {
                    return Respond(CustomCode.UserNotPartOfSchool);
                }

                param.SchoolId = user.SchoolId;
            }

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(new GetUsersBySpecQuery(param));
                return (CustomCode.Success, result);
            });
        }

        // Update user profile
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateUserProfileCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

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
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserByAdminCommand command)
        {
            var creatorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(creatorIdString, out var creatorId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.CreatorId = creatorId;
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPost("import")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportUsersFromExcel([FromForm] ImportUsersFromExcelRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var creatorId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

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

        [HttpPut("{userId:guid}/lock")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LockUserAccount(Guid userId)
        {
            var executorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(executorIdStr, out var executorId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var command = new LockAccountCommand(userId, executorId);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{userId:guid}/unlock")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlockUserAccount(Guid userId)
        {
            var executorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(executorIdStr, out var executorId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var command = new UnlockAccountCommand(userId, executorId);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpGet("import-template/{type}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadImportTemplate(ImportTemplateType type)
        {
            try
            {
                var url = _importTemplateConfig.GetUrl(type);

                if (string.IsNullOrWhiteSpace(url))
                {
                    return Respond(CustomCode.InvalidTemplateType);
                }

                var httpClient = _httpClientFactory.CreateClient("EduvaHttpClient");
                var fileBytes = await httpClient.GetByteArrayAsync(url);

                return File(
                    fileContents: fileBytes,
                    contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileDownloadName: $"{type.ToString().ToLowerInvariant()}_import_template.xlsx"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch template from remote server.");
                return Respond(CustomCode.FileDownloadFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while downloading template.");
                return Respond(CustomCode.SystemError);
            }
        }
    }
}