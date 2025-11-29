using System.Threading.Tasks;

namespace Navtrack.Listener.Server;

/// <summary>
/// Gestiona las conexiones activas de dispositivos GPS y permite enviar comandos
/// </summary>
public interface IDeviceConnectionManager
{
    /// <summary>
    /// Envía un comando a un dispositivo GPS
    /// </summary>
    /// <param name="deviceSerialNumber">Número de serie del dispositivo (IMEI)</param>
    /// <param name="commandBytes">Bytes del comando en formato JT808</param>
    /// <returns>True si el comando fue enviado exitosamente, False si el dispositivo no está conectado</returns>
    Task<bool> SendCommandAsync(string deviceSerialNumber, byte[] commandBytes);

    /// <summary>
    /// Verifica si un dispositivo está actualmente conectado
    /// </summary>
    /// <param name="deviceSerialNumber">Número de serie del dispositivo (IMEI)</param>
    /// <returns>True si el dispositivo tiene una conexión activa</returns>
    bool IsDeviceConnected(string deviceSerialNumber);

    /// <summary>
    /// Obtiene el número total de dispositivos conectados
    /// </summary>
    /// <returns>Cantidad de dispositivos con conexión activa</returns>
    int GetConnectedDevicesCount();

    /// <summary>
    /// Registra una nueva conexión de dispositivo
    /// </summary>
    /// <param name="deviceSerialNumber">Número de serie del dispositivo</param>
    /// <param name="connection">Objeto de conexión</param>
    void RegisterConnection(string deviceSerialNumber, IDeviceConnection connection);

    /// <summary>
    /// Remueve una conexión de dispositivo
    /// </summary>
    /// <param name="deviceSerialNumber">Número de serie del dispositivo</param>
    void RemoveConnection(string deviceSerialNumber);
}

/// <summary>
/// Representa una conexión activa con un dispositivo GPS
/// </summary>
public interface IDeviceConnection
{
    /// <summary>
    /// Número de serie del dispositivo
    /// </summary>
    string DeviceSerialNumber { get; }

    /// <summary>
    /// Indica si la conexión está activa
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Envía datos al dispositivo
    /// </summary>
    /// <param name="data">Bytes a enviar</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendAsync(byte[] data);

    /// <summary>
    /// Cierra la conexión
    /// </summary>
    Task CloseAsync();
}
