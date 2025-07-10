using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Eduva.API.Attributes
{
    public class ApiKeyAuthAttribute : Attribute, IAuthorizationFilter
    {
        private const string API_KEY_HEADER = "X-API-Key";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedApiKey = configuration["WorkerApiKey"];

            if (string.IsNullOrEmpty(expectedApiKey))
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "API Key is missing" });
                return;
            }

            if (!expectedApiKey.Equals(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Invalid API Key" });
                return;
            }
        }
    }
}
