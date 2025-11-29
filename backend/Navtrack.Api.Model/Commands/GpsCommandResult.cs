using System;

namespace Navtrack.Api.Model.Commands;

public class GpsCommandResult
{
    /// <summary>
    /// Indica si el comando fue enviado exitosamente
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Fecha y hora en que se envió el comando (UTC)
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Tipo de comando enviado
    /// </summary>
    public string CommandType { get; set; }
}
