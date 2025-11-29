namespace Navtrack.Listener.Protocols.W2j;

/// <summary>
/// Tipos de mensaje del protocolo W2j/JT808
/// </summary>
public static class MessageType
{
    /// <summary>
    /// Mensaje de registro/autenticación del terminal (0x0100)
    /// </summary>
    public const ushort TerminalRegistration = 0x0100;

    /// <summary>
    /// Mensaje de autenticación del terminal (0x0102)
    /// </summary>
    public const ushort TerminalAuthentication = 0x0102;

    /// <summary>
    /// Heartbeat/latido del terminal (0x0002)
    /// </summary>
    public const ushort TerminalHeartbeat = 0x0002;

    /// <summary>
    /// Reporte de ubicación del terminal (0x0200)
    /// </summary>
    public const ushort LocationReport = 0x0200;

    /// <summary>
    /// Reporte de ubicación en lote (0x0704)
    /// </summary>
    public const ushort BatchLocationReport = 0x0704;

    /// <summary>
    /// Respuesta genérica de la plataforma (0x8001)
    /// </summary>
    public const ushort PlatformGeneralResponse = 0x8001;

    /// <summary>
    /// Respuesta de registro de la plataforma (0x8100)
    /// </summary>
    public const ushort PlatformRegistrationResponse = 0x8100;
}
