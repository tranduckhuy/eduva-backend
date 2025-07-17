using Eduva.Domain.Enums;

namespace Eduva.API.Extensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static IServiceCollection AddCustomAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("EducatorOnly", policy =>
                    policy.RequireRole(
                        Role.SystemAdmin.ToString(),
                        Role.SchoolAdmin.ToString(),
                        Role.ContentModerator.ToString(),
                        Role.Teacher.ToString()
                    ));

                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole(
                        Role.SystemAdmin.ToString()
                    ));
            });

            return services;
        }
    }
}
