using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Constants;

namespace Eduva.Infrastructure.Services
{
    public class SystemConfigHelper
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

        public async Task<string> GetPayosReturnUrlAsync()
        {
            return await GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL, "https://example.com/payos-return");
        }

        public async Task<string> GetImportUsersTemplateAsync()
        {
            return await GetValueAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE, "https://example.com/import-users-template");
        }
    }
}
