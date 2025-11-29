namespace Navtrack.Listener.Protocols.W2j;

/// <summary>
/// Tipos de mensaje del protocolo W2j/JT808 v1.1
/// </summary>
public static class MessageType
{
    // ===== MENSAJES DEL TERMINAL (Terminal → Platform) =====

    /// <summary>
    /// Respuesta general del terminal (0x0001)
    /// </summary>
    public const ushort TerminalGeneralResponse = 0x0001;

    /// <summary>
    /// Heartbeat/latido del terminal (0x0002)
    /// </summary>
    public const ushort TerminalHeartbeat = 0x0002;

    /// <summary>
    /// Mensaje de registro del terminal (0x0100)
    /// </summary>
    public const ushort TerminalRegistration = 0x0100;

    /// <summary>
    /// Mensaje de autenticación del terminal (0x0102)
    /// </summary>
    public const ushort TerminalAuthentication = 0x0102;

    /// <summary>
    /// Respuesta de consulta de parámetros del terminal (0x0104)
    /// </summary>
    public const ushort QueryParametersResponse = 0x0104;

    /// <summary>
    /// Reporte de ubicación del terminal (0x0200)
    /// </summary>
    public const ushort LocationReport = 0x0200;

    /// <summary>
    /// Respuesta de consulta de ubicación (0x0201)
    /// </summary>
    public const ushort LocationQueryResponse = 0x0201;

    /// <summary>
    /// Envío de información de texto del terminal (0x6006)
    /// </summary>
    public const ushort TextInformationSubmit = 0x6006;

    /// <summary>
    /// Reporte de ubicación en lote/suplementario (0x0704)
    /// </summary>
    public const ushort BatchLocationReport = 0x0704;

    /// <summary>
    /// Subida de datos multimedia (0x0801)
    /// </summary>
    public const ushort MultimediaDataUpload = 0x0801;

    // ===== MENSAJES DE LA PLATAFORMA (Platform → Terminal) =====

    /// <summary>
    /// Respuesta genérica de la plataforma (0x8001)
    /// </summary>
    public const ushort PlatformGeneralResponse = 0x8001;

    /// <summary>
    /// Respuesta de registro de la plataforma (0x8100)
    /// </summary>
    public const ushort PlatformRegistrationResponse = 0x8100;

    /// <summary>
    /// Configuración de parámetros del terminal (0x8103)
    /// </summary>
    public const ushort SetTerminalParameters = 0x8103;

    /// <summary>
    /// Consulta de parámetros del terminal (0x8104)
    /// </summary>
    public const ushort QueryTerminalParameters = 0x8104;

    /// <summary>
    /// Control del terminal (0x8105)
    /// </summary>
    public const ushort TerminalControl = 0x8105;

    /// <summary>
    /// Consulta de información de ubicación (0x8201)
    /// </summary>
    public const ushort LocationInformationQuery = 0x8201;

    /// <summary>
    /// Distribución de texto (0x8300)
    /// </summary>
    public const ushort TextDistribution = 0x8300;

    /// <summary>
    /// Resultado de subida de datos multimedia (0x8800)
    /// </summary>
    public const ushort MultimediaDataUploadResult = 0x8800;
}
