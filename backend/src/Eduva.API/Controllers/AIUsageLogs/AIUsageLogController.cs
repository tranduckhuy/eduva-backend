using Eduva.API.Controllers.Base;
using Eduva.Application.Features.AIUsageLogs.Queries;
using Eduva.Application.Features.AIUsageLogs.Specifications;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.AIUsageLogs
{
    [Route("api/ai-usage-logs")]
    public class AIUsageLogController : BaseController<AIUsageLogController>
    {
        private readonly IMediator _mediator;

        public AIUsageLogController(IMediator mediator, ILogger<AIUsageLogController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        // Get AI usage logs for the authenticated user
        [HttpGet]
        [Authorize(Policy = "EducatorOnly")]
        public async Task<IActionResult> GetAIUsageLogs([FromQuery] AIUsageLogSpecParam request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var query = new GetAIUsageLogsQuery
            {
                SpecParam = request,
                UserId = id
            };

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }
    }
}
