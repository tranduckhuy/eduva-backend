using Eduva.Domain.Enums;

namespace Eduva.API.Extensions
{
    public static class AuthorizationPolicyNames
    {
        public const string EducatorOnly = "EducatorOnly";
        public const string AdminOnly = "AdminOnly";
    }

    public static class AuthorizationPolicyExtensions
    {
        public static IServiceCollection AddCustomAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicyNames.EducatorOnly, policy =>
                    policy.RequireRole(
                        Role.SystemAdmin.ToString(),
                        Role.SchoolAdmin.ToString(),
                        Role.ContentModerator.ToString(),
                        Role.Teacher.ToString()
                    ));

                options.AddPolicy(AuthorizationPolicyNames.AdminOnly, policy =>
                    policy.RequireRole(
                        Role.SystemAdmin.ToString()
                    ));
            });

            return services;
        }
    }
}
