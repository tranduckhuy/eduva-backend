using Eduva.Application.Features.SystemConfigs;
using Eduva.Shared.Enums;

namespace Eduva.Application.Interfaces.Services
{
    public interface ISystemConfigService
    {
        Task<string?> GetByKeyAsync(string key);
        Task<(IEnumerable<SystemConfigDto> configs, CustomCode code)> GetAllAsync();
        Task<CustomCode> UpdateAsync(UpdateSystemConfigDto config);
    }
}