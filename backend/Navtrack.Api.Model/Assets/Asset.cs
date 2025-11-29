using System;
using System.ComponentModel.DataAnnotations;
using Navtrack.Api.Model.Devices;
using Navtrack.Api.Model.Messages;

namespace Navtrack.Api.Model.Assets;

public class Asset
{
    [Required]
    public string Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string ChasisNumber { get; set; }

    [Required]
    public string OrganizationId { get; set; }

    [Required]
    public bool Online { get; set; }

    [Required]
    public int MaxSpeed => 400; // TODO update this property

    public Message? LastMessage { get; set; }
    public Message? LastPositionMessage { get; set; }

    public Device? Device { get; set; }

    /// <summary>
    /// Indica si el asset del member está atrasado (manejado por el sistema)
    /// </summary>
    public bool IsDelayed { get; set; }

    /// <summary>
    /// Indica si el asset tiene un incaute activo
    /// Visible solo para Owner, Employee y Seizer
    /// </summary>
    public bool HasActiveSeizure { get; set; }

    /// <summary>
    /// Fecha y hora de expiración del incaute (UTC)
    /// </summary>
    public DateTime? SeizureExpirationDate { get; set; }

    /// <summary>
    /// Indica si el GPS del asset tiene más de 2 días sin enviar ubicación
    /// </summary>
    public bool GpsInactive { get; set; }
}