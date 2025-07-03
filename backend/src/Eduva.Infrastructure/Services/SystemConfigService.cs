using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.SystemConfigs;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Eduva.Infrastructure.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISystemConfigRepository _repository;
        private readonly IMemoryCache _cache;
        private const int CACHE_MINUTES = 15;

        public SystemConfigService(ISystemConfigRepository repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<string?> GetByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var cacheKey = $"SystemConfig:{key}";

            if (_cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            var config = await _repository.GetByKeyAsync(key);
            if (config == null)
                return null;

            var value = config.Value;
            _cache.Set(cacheKey, value, TimeSpan.FromMinutes(CACHE_MINUTES));
            return value;
        }

        public async Task<(IEnumerable<SystemConfigDto> configs, CustomCode code)> GetAllAsync()
        {
            const string cacheKey = "SystemConfig:All";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<SystemConfigDto>? cachedConfigs) && cachedConfigs != null)
            {
                return (cachedConfigs, CustomCode.Success);
            }

            var configs = await _repository.GetAllAsync();

            if (configs != null && configs.Any())
            {
                var configDtos = configs.Select(AppMapper.Mapper.Map<SystemConfigDto>).ToList();
                _cache.Set(cacheKey, configDtos, TimeSpan.FromMinutes(CACHE_MINUTES));
                return (configDtos, CustomCode.Success);
            }

            return (Enumerable.Empty<SystemConfigDto>(), CustomCode.Success);
        }

        public async Task<CustomCode> UpdateAsync(UpdateSystemConfigDto config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.Key))
                return CustomCode.ProvidedInformationIsInValid;

            var existing = await _repository.GetByKeyAsync(config.Key);
            if (existing == null)
                return CustomCode.KeyConfigNotFound;

            var updatedConfig = AppMapper.Mapper.Map<SystemConfig>(config);

            await _repository.UpdateAsync(updatedConfig);
            InvalidateCache(config.Key);
            return CustomCode.Success;
        }

        private void InvalidateCache(string key)
        {
            _cache.Remove($"SystemConfig:{key}");
            _cache.Remove("SystemConfig:All");
        }
    }
}
