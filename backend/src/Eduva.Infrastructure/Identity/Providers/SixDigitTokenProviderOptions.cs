using Microsoft.AspNetCore.Identity;

namespace Eduva.Infrastructure.Identity.Providers
{
    /// <summary>
    /// Configuration options for the SixDigitTokenProvider.
    /// Inherits from DataProtectionTokenProviderOptions to allow DI binding and custom lifespan.
    /// </summary>
    public class SixDigitTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        // This class is intentionally left empty to support DI-based configuration.
        // Options like TokenLifespan can be set via appsettings or in ConfigureOptions<T>.
    }
}