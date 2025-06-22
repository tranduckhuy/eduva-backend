using Eduva.API.Attributes;
using Eduva.API.Models;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Eduva.API.Middlewares
{
    public class SubscriptionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SubscriptionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ISchoolSubscriptionService subscriptionService)
        {
            var endpoint = context.GetEndpoint();
            var accessAttr = endpoint?.Metadata?.GetMetadata<SubscriptionAccessAttribute>();
            var accessLevel = accessAttr?.Level ?? SubscriptionAccessLevel.None;

            if (accessLevel == SubscriptionAccessLevel.None)
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var schoolId = context.User.Claims.FirstOrDefault(c => c.Type == "SchoolId")?.Value;
            var roles = context.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            context.Response.ContentType = "application/json";

            if (string.IsNullOrEmpty(userId) || roles.Count == 0)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new ApiResponse<object>
                {
                    StatusCode = (int)CustomCode.Unauthorized,
                    Message = "Unauthorized. User ID or roles not found.",
                }));

                return;
            }

            // Bypass system admin
            if (roles.Contains(nameof(Role.SystemAdmin)))
            {
                await _next(context);
                return;
            }

            if (roles.Contains(nameof(Role.SchoolAdmin)) && string.IsNullOrEmpty(schoolId)
                && !context.Request.Path.StartsWithSegments("/api/schools"))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new ApiResponse<object>
                {
                    StatusCode = (int)CustomCode.SchoolAndSubscriptionRequired,
                    Message = "Forbidden. You must complete school and subscription information to access this resource.",
                }));
                return;
            }

            if (string.IsNullOrEmpty(schoolId) || !int.TryParse(schoolId, out int schoolIdInt))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new ApiResponse<object>
                {
                    StatusCode = (int)CustomCode.SchoolNotFound,
                    Message = "School not found or invalid school ID.",
                }));
                return;
            }

            var subscription = await subscriptionService.GetCurrentSubscriptionAsync(schoolIdInt);

            if (subscription == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new ApiResponse<object>
                {
                    StatusCode = (int)CustomCode.SchoolSubscriptionNotFound,
                    Message = "School subscription not found.",
                }));
                return;
            }

            var now = DateTimeOffset.UtcNow;
            bool expired = subscription.EndDate < now;
            bool withinGracePeriod = expired && subscription.EndDate.AddDays(14) >= now;

            if (expired)
            {
                if (accessLevel == SubscriptionAccessLevel.ReadOnly && withinGracePeriod)
                {
                    await _next(context);
                    return;
                }

                context.Response.StatusCode = 402; // Payment Required
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new ApiResponse<object>
                {
                    StatusCode = (int)CustomCode.SubscriptionExpiredWithDataLossRisk,
                    Message = "School subscription has expired. Access denied. Please renew your subscription.",
                }));
                return;
            }

            await _next(context);
        }
    }
}