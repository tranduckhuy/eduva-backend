using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Eduva.API.Extensions
{
    public static class RateLimitPolicyNames
    {
        public const string RegisterPolicy = "register-policy";
        public const string AiJobPolicy = "ai-job-policy";
        public const string LoginPolicy = "login-policy";
        public const string ForgotPasswordPolicy = "forgot-password-policy";
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter(policyName: "register-policy", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(10);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // AI job creation rate limit
                options.AddFixedWindowLimiter(policyName: "ai-job-policy", opt =>
                {
                    opt.PermitLimit = 20;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Login attempts rate limit
                options.AddSlidingWindowLimiter(policyName: "login-policy", opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Forgot password requests rate limit
                options.AddSlidingWindowLimiter(policyName: "forgot-password-policy", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(10);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }
}
