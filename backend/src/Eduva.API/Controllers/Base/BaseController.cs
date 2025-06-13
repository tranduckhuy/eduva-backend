using Eduva.API.Models;
using Eduva.Shared.Constants;
using Eduva.Shared.Enums;
using Eduva.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.Base
{
    public class BaseController : ControllerBase
    {
        protected IActionResult Respond(CustomCode code, object data = null!)
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
                Data = data
            };

            return StatusCode(msgDetail.HttpCode, responseData);
        }
    }
}
