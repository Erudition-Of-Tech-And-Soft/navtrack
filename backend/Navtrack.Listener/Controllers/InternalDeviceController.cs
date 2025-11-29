using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Navtrack.Listener.Server;

namespace Navtrack.Listener.Controllers;

/// <summary>
/// Controlador interno para enviar comandos a dispositivos GPS
/// Solo debe ser accesible desde la red interna (API)
/// </summary>
[ApiController]
[Route("internal/devices")]
public class InternalDeviceController : ControllerBase
{
    private readonly ILogger<InternalDeviceController> _logger;
    private readonly IDeviceConnectionManager _connectionManager;

    public InternalDeviceController(
        ILogger<InternalDeviceController> logger,
        IDeviceConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Envía un comando a un dispositivo GPS
    /// </summary>
    /// <param name="serialNumber">Número de serie del dispositivo (IMEI)</param>
    /// <param name="commandBytes">Bytes del comando en formato JT808</param>
    /// <returns>Resultado del envío</returns>
    [HttpPost("{serialNumber}/command")]
    public async Task<IActionResult> SendCommand(
        string serialNumber,
        [FromBody] byte[] commandBytes)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para enviar comando al dispositivo {SerialNumber}. Tamaño: {Size} bytes",
                serialNumber,
                commandBytes?.Length ?? 0
            );

            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                return BadRequest(new { success = false, error = "Serial number es requerido" });
            }

            if (commandBytes == null || commandBytes.Length == 0)
            {
                return BadRequest(new { success = false, error = "Command bytes es requerido" });
            }

            bool sent = await _connectionManager.SendCommandAsync(serialNumber, commandBytes);

            if (sent)
            {
                _logger.LogInformation(
                    "Comando enviado exitosamente al dispositivo {SerialNumber}",
                    serialNumber
                );

                return Ok(new
                {
                    success = true,
                    message = "Comando enviado exitosamente",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning(
                    "No se pudo enviar comando al dispositivo {SerialNumber} - Dispositivo no conectado",
                    serialNumber
                );

                return Ok(new
                {
                    success = false,
                    message = "Dispositivo no está conectado",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al procesar comando para dispositivo {SerialNumber}",
                serialNumber
            );

            return StatusCode(500, new
            {
                success = false,
                error = "Error interno al procesar comando",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Verifica si un dispositivo está conectado
    /// </summary>
    /// <param name="serialNumber">Número de serie del dispositivo</param>
    /// <returns>Estado de conexión</returns>
    [HttpGet("{serialNumber}/status")]
    public IActionResult GetDeviceStatus(string serialNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                return BadRequest(new { error = "Serial number es requerido" });
            }

            bool connected = _connectionManager.IsDeviceConnected(serialNumber);

            return Ok(new
            {
                serialNumber,
                connected,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al verificar estado del dispositivo {SerialNumber}",
                serialNumber
            );

            return StatusCode(500, new
            {
                error = "Error interno al verificar estado",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de conexiones
    /// </summary>
    /// <returns>Estadísticas</returns>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            int connectedCount = _connectionManager.GetConnectedDevicesCount();

            return Ok(new
            {
                connectedDevices = connectedCount,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas");

            return StatusCode(500, new
            {
                error = "Error interno al obtener estadísticas",
                message = ex.Message
            });
        }
    }
}
