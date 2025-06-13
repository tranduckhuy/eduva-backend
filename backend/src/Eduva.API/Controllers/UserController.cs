using Eduva.API.Controllers.Base;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : BaseController
    {
        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            if (id == 123)
            {
                // Trả về thành công
                return Respond(CustomCode.Success, new { Id = 123, Name = "John Doe" });
            }
            else
            {
                return Respond(CustomCode.UserNotFound);
            }
        }
    }
}
