using Microsoft.AspNetCore.Identity;

namespace Eduva.Infrastructure.Identity.Providers
{
    /// <summary>
    /// Configuration options for the SixDigitTokenProvider.
    /// Inherits from DataProtectionTokenProviderOptions to allow DI binding and custom lifespan.
    /// </summary>
    public class SixDigitTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public SixDigitTokenProviderOptions()
        {
            // Set default token lifespan (can be overridden in ConfigureOptions)
            TokenLifespan = TimeSpan.FromMinutes(2);
        }
    }
}