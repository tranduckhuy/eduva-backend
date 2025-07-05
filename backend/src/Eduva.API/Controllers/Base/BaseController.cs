using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
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

        // Unified internal handler to avoid repetition
        private async Task<IActionResult> ExecuteAsync<T>(
            Func<Task<(CustomCode code, T result)>> func,
            Func<T, object?> resultSelector,
            T defaultResult,
            CustomCode? overrideSuccessCode = null)
        {
            var modelCheck = CheckModelStateValidity();
            if (modelCheck != null)
                return modelCheck;

            try
            {
                var (code, result) = await func();
                return Respond(overrideSuccessCode ?? code, resultSelector(result));
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, resultSelector(defaultResult), [ex.Message]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in {ControllerName}", typeof(TController).Name);
                return Respond(CustomCode.SystemError, null, [ex.Message]);
            }
        }

        // Generic result
        protected Task<IActionResult> HandleRequestAsync<TResponse>(
            Func<Task<(CustomCode code, TResponse result)>> func)
            where TResponse : class
            => ExecuteAsync(func, result => result, default(TResponse)!);

        // No result (void flow)
        protected Task<IActionResult> HandleRequestAsync(
            Func<Task> func,
            CustomCode successCode = CustomCode.Success)
            => ExecuteAsync<object?>(
                async () =>
                {
                    await func();
                    return (successCode, (object?)null);
                },
                _ => null,
                null);

        // Pagination result
        protected Task<IActionResult> HandlePaginatedRequestAsync<T>(
            Func<Task<(CustomCode code, Pagination<T> result)>> func)
            where T : class
            => ExecuteAsync(func, result => result, default(Pagination<T>)!);
    }
}