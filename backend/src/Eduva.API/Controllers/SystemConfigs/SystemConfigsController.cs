using Eduva.API.Controllers.Base;
using Eduva.API.Extensions;
using Eduva.Application.Features.SystemConfigs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.SystemConfigs
{
    [ApiController]
    [Route("api/admin/system-configs")]
    public class SystemConfigsController : BaseController<SystemConfigsController>
    {
        private readonly ISystemConfigService _systemConfigService;

        public SystemConfigsController(
            ISystemConfigService systemConfigService,
            ILogger<SystemConfigsController> logger) : base(logger)
        {
            _systemConfigService = systemConfigService;
        }

        /// <summary>
        /// Get all system configurations - For admin dashboard only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> GetAllAsync()
        {
            return await HandleRequestAsync(async () =>
            {
                var (configs, code) = await _systemConfigService.GetAllAsync();
                return (code, configs);
            });
        }

        /// <summary>
        /// Update an existing system configuration
        /// </summary>
        [HttpPut("{key}")]
        [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
        public async Task<IActionResult> UpdateAsync(string key, [FromBody] UpdateSystemConfigDto updateDto)
        {
            updateDto.Key = key; // Ensure consistency between route and body
            return await HandleRequestAsync(async () =>
            {
                var code = await _systemConfigService.UpdateAsync(updateDto);
                return (code, new object());
            });
        }
    }
}