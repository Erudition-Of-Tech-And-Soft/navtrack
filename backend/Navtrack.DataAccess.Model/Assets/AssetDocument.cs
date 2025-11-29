using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Navtrack.DataAccess.Model.Devices.Messages;
using Navtrack.DataAccess.Model.Shared;
using Navtrack.DataAccess.Mongo;

namespace Navtrack.DataAccess.Model.Assets;

[Collection("assets")]
public class AssetDocument : UpdatedAuditDocument
{
    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("chasisNumber")]
    public string ChasisNumber { get; set; }

    [BsonElement("ownerId")]
    public ObjectId OwnerId { get; set; }

    [BsonElement("organizationId")]
    public ObjectId OrganizationId { get; set; }

    [BsonElement("device")]
    public AssetDeviceElement? Device { get; set; }

    [BsonElement("lastMessage")]
    public DeviceMessageDocument? LastMessage { get; set; }

    [BsonElement("lastPositionMessage")]
    public DeviceMessageDocument? LastPositionMessage { get; set; }

    [BsonElement("teams")]
    public IEnumerable<AssetTeamElement>? Teams { get; set; }

    /// <summary>
    /// Indica si el asset del member está atrasado (manejado por el sistema)
    /// </summary>
    [BsonElement("isDelayed")]
    public bool IsDelayed { get; set; }

    /// <summary>
    /// Indica si el asset tiene un incaute activo
    /// Visible solo para Owner, Employee y Seizer
    /// </summary>
    [BsonElement("hasActiveSeizure")]
    public bool HasActiveSeizure { get; set; }

    /// <summary>
    /// Fecha y hora de expiración del incaute
    /// Después de esta fecha, el incaute deja de estar activo para Seizers
    /// </summary>
    [BsonElement("seizureExpirationDate")]
    public DateTime? SeizureExpirationDate { get; set; }

    /// <summary>
    /// Indica si el GPS del asset tiene más de 2 días sin enviar ubicación (manejado por el sistema)
    /// </summary>
    [BsonElement("gpsInactive")]
    public bool GpsInactive { get; set; }
}