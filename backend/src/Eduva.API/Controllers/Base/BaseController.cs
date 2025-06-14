using Eduva.API.Models;
using Eduva.Shared.Constants;
using Eduva.Shared.Enums;
using Eduva.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Eduva.API.Controllers.Base
{
    [ApiController]
    public abstract class BaseController<TController> : ControllerBase
    {
        private readonly ILogger<BaseController<TController>> _logger;

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

                var statusCode = CustomCode.ModelInvalid;

                return Respond(statusCode, null, errors);
            }

            return null!;
        }


        protected IActionResult Respond(CustomCode code, object? data = null, IEnumerable<string>? errors = null)
        {
            if (!ResponseMessages.Messages.TryGetValue(code, out var msgDetail))
            {
                msgDetail = new MessageDetail
                {
                    HttpCode = StatusCodes.Status500InternalServerError,
                    Message = "Unknown error"
                };
            }

            var responseData = new ApiResponse<object>
            {
                StatusCode = (int)code,
                Message = msgDetail.Message,
                Data = data,
                Errors = errors
            };

            return StatusCode(msgDetail.HttpCode, responseData);
        }
    }
}
