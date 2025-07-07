using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Shared.Constants;

namespace Eduva.Infrastructure.Configurations.ExcelTemplate
{
    public class ImportTemplateConfig
    {
        private readonly ISystemConfigHelper _systemConfigHelper;

        public ImportTemplateConfig(ISystemConfigHelper systemConfigHelper)
        {
            _systemConfigHelper = systemConfigHelper;
        }

        public async Task<string?> GetUrl(ImportTemplateType type)
        {
            return type switch
            {
                ImportTemplateType.User => await _systemConfigHelper.GetValueAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE, ""),
                _ => null
            };
        }
    }
}