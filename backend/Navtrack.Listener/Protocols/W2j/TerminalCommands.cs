namespace Navtrack.Listener.Protocols.W2j;

/// <summary>
/// Comandos de control del terminal (0x8105) según Tabla 14 del protocolo JT808 v1.1
/// </summary>
public static class TerminalCommands
{
    /// <summary>
    /// Reiniciar terminal
    /// </summary>
    public const byte Reset = 0x04;

    /// <summary>
    /// Restaurar configuración de fábrica
    /// </summary>
    public const byte RestoreFactorySettings = 0x05;

    /// <summary>
    /// Activar grabación de voz
    /// </summary>
    public const byte TurnOnVoiceRecording = 0x17;

    /// <summary>
    /// Activar grabación continua
    /// Parámetros: 2 bytes con tiempo de grabación en minutos
    /// </summary>
    public const byte TurnOnContinuousRecording = 0x18;

    /// <summary>
    /// Detener todas las grabaciones
    /// </summary>
    public const byte StopAllRecordings = 0x19;

    /// <summary>
    /// Cortar combustible y electricidad
    /// </summary>
    public const byte CutOffOilAndElectricity = 0x64;

    /// <summary>
    /// Restaurar combustible y electricidad
    /// </summary>
    public const byte RestoreOilAndElectricity = 0x65;

    /// <summary>
    /// Activar fortificación externa (nuevo en v1.1)
    /// </summary>
    public const byte ExternalFortification = 0x66;

    /// <summary>
    /// Retirar fortificación externa (nuevo en v1.1)
    /// </summary>
    public const byte ExternalWithdrawal = 0x67;
}

/// <summary>
/// Parámetros del terminal (0x8103) según Tabla 11 del protocolo JT808 v1.1
/// </summary>
public static class TerminalParameters
{
    /// <summary>
    /// Intervalo de heartbeat (DWORD, segundos)
    /// </summary>
    public const uint HeartbeatInterval = 0x0001;

    /// <summary>
    /// APN del servidor principal (STRING)
    /// </summary>
    public const uint MainServerAPN = 0x0010;

    /// <summary>
    /// Dirección del servidor principal (STRING, IP o dominio)
    /// </summary>
    public const uint PrimaryServerAddress = 0x0013;

    /// <summary>
    /// Dirección del servidor de respaldo (STRING, IP o dominio)
    /// </summary>
    public const uint BackupServerAddress = 0x0017;

    /// <summary>
    /// Puerto TCP del servidor (DWORD)
    /// </summary>
    public const uint ServerTcpPort = 0x0018;

    /// <summary>
    /// Estrategia de reporte de ubicación (DWORD)
    /// 0: Regular reporting
    /// 1: Distance report
    /// 2: Timing and Interval Report
    /// </summary>
    public const uint LocationReportingStrategy = 0x0020;

    /// <summary>
    /// Intervalo de reporte cuando está durmiendo (DWORD, segundos, > 0)
    /// </summary>
    public const uint ReportIntervalWhenSleeping = 0x0027;

    /// <summary>
    /// Intervalo de reporte por tiempo por defecto (DWORD, segundos, > 0)
    /// </summary>
    public const uint DefaultTimeReportingInterval = 0x0029;

    /// <summary>
    /// Intervalo de reporte por distancia por defecto (DWORD, metros, > 0)
    /// </summary>
    public const uint DefaultDistanceReportingInterval = 0x002C;

    /// <summary>
    /// Ángulo de transmisión suplementaria en punto de giro (DWORD, < 180 grados)
    /// </summary>
    public const uint TurningPointSupplementAngle = 0x0030;

    /// <summary>
    /// Velocidad máxima (DWORD, km/h)
    /// </summary>
    public const uint MaximumSpeed = 0x0055;

    /// <summary>
    /// Duración de exceso de velocidad (DWORD, segundos)
    /// </summary>
    public const uint OverspeedDuration = 0x0056;

    /// <summary>
    /// Lectura del odómetro del vehículo (DWORD, 1/10 km)
    /// </summary>
    public const uint VehicleOdometerReading = 0x0080;

    /// <summary>
    /// ID de provincia del vehículo (DWORD)
    /// </summary>
    public const uint ProvinceIdOfVehicle = 0x0081;

    /// <summary>
    /// ID de ciudad del vehículo (DWORD)
    /// </summary>
    public const uint CityIdOfVehicle = 0x0082;

    /// <summary>
    /// Placa del vehículo (STRING)
    /// </summary>
    public const uint VehicleLicensePlate = 0x0083;

    /// <summary>
    /// Color de la placa (BYTE)
    /// Según JT/T 415-2006 5.4.12
    /// </summary>
    public const uint LicensePlateColor = 0x0084;
}
