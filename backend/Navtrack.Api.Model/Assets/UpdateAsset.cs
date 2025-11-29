using System;
using System.ComponentModel.DataAnnotations;

namespace Navtrack.Api.Model.Assets;

public class UpdateAsset
{
    public string Name { get; set; }

    [Required]
    public string ChasisNumber { get; set; }

    /// <summary>
    /// Indica si el asset tiene un incaute activo
    /// Solo puede ser modificado por Owner
    /// </summary>
    public bool? HasActiveSeizure { get; set; }

    /// <summary>
    /// Fecha y hora de expiración del incaute (UTC)
    /// Solo puede ser modificado por Owner
    /// </summary>
    public DateTime? SeizureExpirationDate { get; set; }
}