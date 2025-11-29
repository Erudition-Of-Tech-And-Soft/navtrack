using System.Threading.Tasks;
using Navtrack.Api.Model.Commands;
using Navtrack.Api.Model.Errors;
using Navtrack.Api.Services.Common.Exceptions;
using Navtrack.Api.Services.Requests;
using Navtrack.DataAccess.Model.Assets;
using Navtrack.DataAccess.Services.Assets;
using Navtrack.Shared.Library.DI;

namespace Navtrack.Api.Services.Commands;

[Service(typeof(IRequestHandler<SendGpsCommandRequest, GpsCommandResult>))]
public class SendGpsCommandRequestHandler(
    IAssetRepository assetRepository,
    IGpsCommandService gpsCommandService)
    : BaseRequestHandler<SendGpsCommandRequest, GpsCommandResult>
{
    private AssetDocument? asset;

    public override async Task Validate(RequestValidationContext<SendGpsCommandRequest> context)
    {
        asset = await assetRepository.GetById(context.Request.AssetId);
        asset.Return404IfNull();

        // Validar que el comando sea válido
        var validCommands = new[]
        {
            "CutFuel", "RestoreFuel", "Fortify", "Withdraw",
            "QueryLocation", "Restart", "RestoreFactory", "StopRecordings"
        };

        bool isInvalidCommand = !System.Array.Exists(validCommands,
            cmd => cmd.Equals(context.Request.Model.CommandType, System.StringComparison.OrdinalIgnoreCase));

        context.ValidationException.AddErrorIfTrue(
            isInvalidCommand,
            nameof(context.Request.Model.CommandType),
            ApiErrorCodes.Validation_000001_Generic);
    }

    public override Task<GpsCommandResult> Handle(SendGpsCommandRequest request)
    {
        return gpsCommandService.SendCommand(asset!, request.Model);
    }
}
