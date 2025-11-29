using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Navtrack.Api.Controllers.Shared;
using Navtrack.Api.Model.Assets;
using Navtrack.Api.Model.Commands;
using Navtrack.Api.Model.Common;
using Navtrack.Api.Services.Assets;
using Navtrack.Api.Services.Commands;
using Navtrack.Api.Services.Common.ActionFilters;
using Navtrack.Api.Services.Requests;
using Navtrack.DataAccess.Model.Assets;
using Navtrack.DataAccess.Model.Organizations;

namespace Navtrack.Api.Controllers;

public class AssetsController(IRequestHandler requestHandler) : BaseAssetsController(requestHandler)
{
    [HttpGet(ApiPaths.OrganizationAssets)]
    [ProducesResponseType(typeof(List<Asset>), StatusCodes.Status200OK)]
    [AuthorizeOrganization(OrganizationUserRole.Member)]
    public async Task<List<Asset>> GetList([FromRoute] string organizationId)
    {
        List<Asset> result =
            await requestHandler.Handle<GetAssetsRequest, List<Asset>>(
                new GetAssetsRequest
                {
                    OrganizationId = organizationId
                });

        return result;
    }

    /// <summary>
    /// Envía un comando GPS al dispositivo del asset
    /// Solo Owner y Employee pueden enviar comandos
    /// </summary>
    [HttpPost(ApiPaths.OrganizationAssets + "/{assetId}/commands")]
    [ProducesResponseType(typeof(GpsCommandResult), StatusCodes.Status200OK)]
    [AuthorizeOrganization(OrganizationUserRole.Employee)]
    [AuthorizeAsset(AssetUserRole.Viewer)]
    public async Task<GpsCommandResult> SendCommand(
        [FromRoute] string organizationId,
        [FromRoute] string assetId,
        [FromBody] SendGpsCommand model)
    {
        GpsCommandResult result =
            await requestHandler.Handle<SendGpsCommandRequest, GpsCommandResult>(
                new SendGpsCommandRequest
                {
                    AssetId = assetId,
                    Model = model
                });

        return result;
    }
}