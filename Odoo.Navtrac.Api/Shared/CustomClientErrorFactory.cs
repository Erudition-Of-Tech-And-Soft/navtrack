using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Odoo.Navtrac.Api.Model.Common;
using Odoo.Navtrac.Api.Services.Common.Mappers;

namespace Odoo.Navtrac.Api.Shared;

public class CustomClientErrorFactory : IClientErrorFactory
{
    private readonly ProblemDetailsFactory problemDetailsFactory;

    public CustomClientErrorFactory(ProblemDetailsFactory problemDetailsFactory)
    {
        this.problemDetailsFactory = problemDetailsFactory;
    }

    public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        ProblemDetails problemDetails =
            problemDetailsFactory.CreateProblemDetails(actionContext.HttpContext, clientError.StatusCode);

        Error model = ErrorMapper.Map(problemDetails);

        return new JsonResult(model)
        {
            StatusCode = clientError.StatusCode
        };
    }
}