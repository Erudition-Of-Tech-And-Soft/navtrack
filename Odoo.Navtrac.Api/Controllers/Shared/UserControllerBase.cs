using System.Threading.Tasks;
using IdentityServer4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Odoo.Navtrac.Api.Model.Account;
using Odoo.Navtrac.Api.Model.Common;
using Odoo.Navtrac.Api.Model.User;
using Odoo.Navtrac.Api.Services.Requests;
using Odoo.Navtrac.Api.Services.User;

namespace Odoo.Navtrac.Api.Controllers.Shared;

[ApiController]
[Authorize(IdentityServerConstants.LocalApi.PolicyName)]
public abstract class UserControllerBase(IRequestHandler requestHandler)
    : ControllerBase
{
    [HttpPost(ApiPaths.User)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] UpdateUserModel model)
    {
        await requestHandler.Handle(new UpdateUserRequest
        {
            Model = model
        });
    
        return Ok();
    }

    [HttpPost(ApiPaths.UserChangePassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        await requestHandler.Handle(new ChangePasswordRequest
        {
            Model = model
        });

        return Ok();
    }
}