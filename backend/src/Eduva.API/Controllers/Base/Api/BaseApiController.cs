namespace Eduva.API.Controllers.Base.Api
{
    public abstract class BaseApiController<TController> : BaseController<TController>
    {
        protected BaseApiController(ILogger<TController> logger) : base(logger) { }
    }
}