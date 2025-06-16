using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.Base.Auth
{
    public abstract class BaseAuthController<TController> : BaseController<TController>
    {
        protected BaseAuthController(ILogger<TController> logger) : base(logger) { }

        protected async Task<IActionResult> HandleRequestAsync<TResponse>(
            Func<Task<(CustomCode code, TResponse result)>> func)
            where TResponse : class
        {
            var check = CheckModelStateValidity();
            if (check != null)
                return check;

            try
            {
                var (code, result) = await func();
                return Respond(code, result);
            }
            catch (AppException appEx)
            {
                return Respond(appEx.StatusCode, default(TResponse), appEx.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in {ControllerName}", typeof(TController).Name);
                return Respond(CustomCode.SystemError);
            }
        }

        protected async Task<IActionResult> HandleRequestAsync(
            Func<Task> func,
            CustomCode successCode = CustomCode.Success)
        {
            var check = CheckModelStateValidity();
            if (check != null)
                return check;

            try
            {
                await func();
                return Respond(successCode);
            }
            catch (AppException appEx)
            {
                return Respond(appEx.StatusCode, null, appEx.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in {ControllerName}", typeof(TController).Name);
                return Respond(CustomCode.SystemError);
            }
        }
    }
}
