using System.ComponentModel.DataAnnotations;

namespace Navtrack.Api.Model.Assets;

public class UpdateAsset
{
    public string Name { get; set; }

    [Required]
    public string ChasisNumber { get; set; }
}