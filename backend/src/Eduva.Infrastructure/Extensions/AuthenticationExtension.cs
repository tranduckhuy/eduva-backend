using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Extensions.Providers;
using Eduva.Infrastructure.Identity;
using Eduva.Infrastructure.Identity.Interfaces;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eduva.Infrastructure.Extensions
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Register JWT services
            services.AddScoped<SixDigitTokenProvider<ApplicationUser>>();
            services.AddScoped<JwtHandler>();
            services.AddScoped<ITokenBlackListService, TokenBlackListService>();
            services.AddScoped<IAuthService, AuthService>();

            // Configure JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Set to true in production
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = configuration["JwtSettings:ValidIssuer"],
                    ValidAudience = configuration["JwtSettings:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!)
                    )
                };

                // Add custom token validation for blacklisted tokens
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var tokenBlacklistService = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenBlackListService>();

                        var token = context.Request.Headers["Authorization"]
                            .FirstOrDefault()?.Split(" ").Last();

                        // Check if individual token is blacklisted
                        if (!string.IsNullOrEmpty(token) && await tokenBlacklistService.IsTokenBlacklistedAsync(token))
                        {
                            context.Fail("Token has been revoked");
                            return;
                        }

                        // Check if all user tokens have been invalidated
                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userIdClaim))
                        {
                            var tokenHandler = new JwtSecurityTokenHandler();
                            var jwtToken = tokenHandler.ReadJwtToken(token);
                            var tokenIssuedAt = jwtToken.IssuedAt; // Use DateTime directly

                            if (await tokenBlacklistService.AreUserTokensInvalidatedAsync(userIdClaim, tokenIssuedAt))
                            {
                                var exceptionToken = await tokenBlacklistService.GetExceptionTokenAsync(userIdClaim);

                                if (!string.IsNullOrEmpty(exceptionToken))
                                {
                                    if (token != exceptionToken)
                                    {
                                        context.Fail("This session was logged out by user.");
                                        return;
                                    }
                                }
                                else
                                {
                                    context.Fail("All user tokens have been invalidated");
                                    return;
                                }
                            }
                        }
                    }
                };
            });

            return services;
        }

        public static IdentityBuilder AddApplicationIdentity<TUser>(this IServiceCollection services) where TUser : class
        {
            var builder = services.AddIdentity<TUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.Tokens.ProviderMap["OTP"] = new TokenProviderDescriptor(typeof(SixDigitTokenProvider<TUser>));
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<SixDigitTokenProvider<ApplicationUser>>("OTP");

            builder.Services.Configure<SixDigitTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromSeconds(120);
            });

            builder.Services.AddScoped<SixDigitTokenProvider<ApplicationUser>>();

            return builder;
        }
    }
}