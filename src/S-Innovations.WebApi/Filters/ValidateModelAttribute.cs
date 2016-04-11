

namespace SInnovations.WebApi.Filters
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;
    using Newtonsoft.Json;

    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext.ModelState.IsValid == false)
            {
                Trace.TraceInformation("Bad ModelState: {0}, {1}", actionContext.Request.RequestUri.AbsoluteUri,
                    JsonConvert.SerializeObject(actionContext.ModelState));

                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest, actionContext.ModelState);
            }
        }
    }
}
