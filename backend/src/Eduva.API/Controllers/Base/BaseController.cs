using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Constants;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.Base
{
    [ApiController]
    public abstract class BaseController<TController> : ControllerBase
    {
        protected readonly ILogger<BaseController<TController>> _logger;

        protected BaseController(ILogger<BaseController<TController>> logger)
        {
            _logger = logger;
        }

        protected IActionResult CheckModelStateValidity()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(x => x.Errors.Select(e => e.ErrorMessage))
                    .ToList();

                _logger.LogWarning("Request validation failed for {ControllerName}: {ErrorMessages}",
                    typeof(TController).Name,
                    string.Join(", ", errors));

                return Respond(CustomCode.ModelInvalid, null, errors);
            }

            return null!;
        }

        protected IActionResult Respond(CustomCode code, object? data = null, IEnumerable<string>? errors = null)
        {
            ResponseMessages.Messages.TryGetValue(code, out var msgDetail);

            var responseData = new ApiResponse<object>
            {
                StatusCode = (int)code,
                Message = msgDetail?.Message ?? "Unknown error",
                Data = data,
                Errors = errors
            };

            return StatusCode(msgDetail?.HttpCode ?? StatusCodes.Status500InternalServerError, responseData);
        }

        protected async Task<IActionResult> HandleRequestAsync<TResponse>(
            Func<Task<(CustomCode code, TResponse result)>> func)
            where TResponse : class
        {
            var modelCheck = CheckModelStateValidity();
            if (modelCheck != null)
                return modelCheck;

            try
            {
                var (code, result) = await func();
                return Respond(code, result);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, default(TResponse), ex.Errors);
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
            var modelCheck = CheckModelStateValidity();
            if (modelCheck != null)
                return modelCheck;

            try
            {
                await func();
                return Respond(successCode);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, null, ex.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in {ControllerName}", typeof(TController).Name);
                return Respond(CustomCode.SystemError);
            }
        }
    }
}