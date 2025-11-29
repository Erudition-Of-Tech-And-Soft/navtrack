using Navtrack.Api.Model.Commands;

namespace Navtrack.Api.Services.Commands;

public class SendGpsCommandRequest
{
    public string AssetId { get; set; }
    public SendGpsCommand Model { get; set; }
}
