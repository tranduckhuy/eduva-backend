using Eduva.Shared.Enums;
using Eduva.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Eduva.Shared.Constants
{
    public static class ResponseMessages
    {
        public static readonly Dictionary<CustomCode, MessageDetail> Messages = new Dictionary<CustomCode, MessageDetail>
        {
            #region Success Codes
            { CustomCode.Success, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Success" } },

            #endregion


            #region Error Codes
            { CustomCode.EmailInvalid, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Email is invalid" } },

            { CustomCode.Unauthorized, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Unauthorized" } },

            { CustomCode.Forbidden, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Access denied" } },

            { CustomCode.InvalidToken, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Invalid token. Please log in again" } },

            { CustomCode.UserNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "User not found" } },
            #endregion
        };
    }
}
