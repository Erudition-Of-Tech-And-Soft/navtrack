using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Navtrack.Listener.Server;

/// <summary>
/// Implementación del gestor de conexiones de dispositivos GPS
/// Mantiene un diccionario de conexiones activas indexadas por número de serie
/// </summary>
public class DeviceConnectionManager : IDeviceConnectionManager
{
    private readonly ILogger<DeviceConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, IDeviceConnection> _activeConnections;

    public DeviceConnectionManager(ILogger<DeviceConnectionManager> logger)
    {
        _logger = logger;
        _activeConnections = new ConcurrentDictionary<string, IDeviceConnection>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> SendCommandAsync(string deviceSerialNumber, byte[] commandBytes)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber))
        {
            _logger.LogWarning("Intento de enviar comando con deviceSerialNumber vacío");
            return false;
        }

        if (!_activeConnections.TryGetValue(deviceSerialNumber, out var connection))
        {
            _logger.LogWarning(
                "Dispositivo {DeviceSerialNumber} no está conectado. No se puede enviar comando.",
                deviceSerialNumber
            );
            return false;
        }

        if (!connection.IsConnected)
        {
            _logger.LogWarning(
                "Conexión del dispositivo {DeviceSerialNumber} no está activa",
                deviceSerialNumber
            );
            RemoveConnection(deviceSerialNumber);
            return false;
        }

        try
        {
            _logger.LogInformation(
                "Enviando comando de {Length} bytes al dispositivo {DeviceSerialNumber}",
                commandBytes.Length,
                deviceSerialNumber
            );

            bool success = await connection.SendAsync(commandBytes);

            if (success)
            {
                _logger.LogInformation(
                    "Comando enviado exitosamente al dispositivo {DeviceSerialNumber}",
                    deviceSerialNumber
                );
            }
            else
            {
                _logger.LogWarning(
                    "Falló el envío del comando al dispositivo {DeviceSerialNumber}",
                    deviceSerialNumber
                );
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Excepción al enviar comando al dispositivo {DeviceSerialNumber}",
                deviceSerialNumber
            );
            return false;
        }
    }

    public bool IsDeviceConnected(string deviceSerialNumber)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber))
        {
            return false;
        }

        if (!_activeConnections.TryGetValue(deviceSerialNumber, out var connection))
        {
            return false;
        }

        if (!connection.IsConnected)
        {
            RemoveConnection(deviceSerialNumber);
            return false;
        }

        return true;
    }

    public int GetConnectedDevicesCount()
    {
        // Limpiar conexiones muertas antes de contar
        foreach (var kvp in _activeConnections)
        {
            if (!kvp.Value.IsConnected)
            {
                _activeConnections.TryRemove(kvp.Key, out _);
            }
        }

        return _activeConnections.Count;
    }

    public void RegisterConnection(string deviceSerialNumber, IDeviceConnection connection)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber))
        {
            _logger.LogWarning("Intento de registrar conexión con deviceSerialNumber vacío");
            return;
        }

        if (connection == null)
        {
            _logger.LogWarning("Intento de registrar conexión nula");
            return;
        }

        // Si ya existe una conexión, cerrar la anterior
        if (_activeConnections.TryGetValue(deviceSerialNumber, out var existingConnection))
        {
            _logger.LogInformation(
                "Dispositivo {DeviceSerialNumber} ya tiene una conexión. Cerrando la anterior.",
                deviceSerialNumber
            );

            try
            {
                _ = existingConnection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cerrar conexión anterior del dispositivo {DeviceSerialNumber}", deviceSerialNumber);
            }
        }

        _activeConnections[deviceSerialNumber] = connection;

        _logger.LogInformation(
            "Dispositivo {DeviceSerialNumber} registrado. Total conectados: {Count}",
            deviceSerialNumber,
            _activeConnections.Count
        );
    }

    public void RemoveConnection(string deviceSerialNumber)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber))
        {
            return;
        }

        if (_activeConnections.TryRemove(deviceSerialNumber, out var connection))
        {
            _logger.LogInformation(
                "Dispositivo {DeviceSerialNumber} desconectado. Total conectados: {Count}",
                deviceSerialNumber,
                _activeConnections.Count
            );

            try
            {
                _ = connection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cerrar conexión del dispositivo {DeviceSerialNumber}", deviceSerialNumber);
            }
        }
    }
}
