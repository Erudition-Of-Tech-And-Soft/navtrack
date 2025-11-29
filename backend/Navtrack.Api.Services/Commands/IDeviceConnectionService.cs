using System.Threading.Tasks;

namespace Navtrack.Api.Services.Commands;

/// <summary>
/// Servicio para enviar comandos a dispositivos GPS
/// Abstrae la comunicación con el Listener y el DeviceConnectionManager
/// </summary>
public interface IDeviceConnectionService
{
    /// <summary>
    /// Envía un comando a un dispositivo GPS
    /// </summary>
    /// <param name="deviceSerialNumber">Número de serie del dispositivo (IMEI)</param>
    /// <param name="commandBytes">Bytes del comando en formato JT808</param>
    /// <returns>True si el comando fue enviado exitosamente</returns>
    Task<bool> SendCommandToDeviceAsync(string deviceSerialNumber, byte[] commandBytes);

    /// <summary>
    /// Verifica si un dispositivo está conectado
    /// </summary>
    /// <param name="deviceSerialNumber">Número de serie del dispositivo</param>
    /// <returns>True si está conectado</returns>
    bool IsDeviceConnected(string deviceSerialNumber);
}
