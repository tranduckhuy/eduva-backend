using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Constants;

namespace Eduva.Infrastructure.Services
{
    public class SystemConfigHelper : ISystemConfigHelper
    {
        private readonly ISystemConfigService _systemConfigService;

        public SystemConfigHelper(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        public async Task<string> GetValueAsync(string key, string defaultValue = "")
        {
            var value = await _systemConfigService.GetByKeyAsync(key);
            return value ?? defaultValue;
        }

        public async Task<string> GetDefaultAvatarUrlAsync()
        {
            return await GetValueAsync(
                SystemConfigKeys.DEFAULT_AVATAR_URL,
                "https://via.placeholder.com/150x150/CCCCCC/FFFFFF?text=Avatar"
            );
        }

        public async Task<string> GetImportUsersTemplateAsync()
        {
            return await GetValueAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE, "https://example.com/import-users-template");
        }

        public async Task<string> GetPayosReturnUrlPlanAsync()
        {
            return await GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN, "https://example.com/payos-return-plan");
        }

        public async Task<string> GetPayosReturnUrlPackAsync()
        {
            return await GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK, "https://example.com/payos-return-pack");
        }
    }
}