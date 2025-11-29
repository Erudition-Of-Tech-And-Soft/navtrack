using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Navtrack.Shared.Library.DI;

namespace Navtrack.Api.Services.Commands;

/// <summary>
/// Servicio que se comunica con el Listener vía HTTP para enviar comandos a dispositivos
/// </summary>
[Service(typeof(IDeviceConnectionService))]
public class HttpDeviceConnectionService : IDeviceConnectionService
{
    private readonly ILogger<HttpDeviceConnectionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _listenerBaseUrl;

    public HttpDeviceConnectionService(
        ILogger<HttpDeviceConnectionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ListenerClient");

        // Obtener URL del Listener desde configuración
        // Por defecto: http://localhost:5001
        _listenerBaseUrl = configuration.GetValue<string>("Listener:BaseUrl")
                          ?? "http://localhost:5001";

        _logger.LogInformation("HttpDeviceConnectionService configurado. Listener URL: {Url}", _listenerBaseUrl);
    }

    public async Task<bool> SendCommandToDeviceAsync(string deviceSerialNumber, byte[] commandBytes)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber))
        {
            _logger.LogWarning("Intento de enviar comando con deviceSerialNumber vacío");
            return false;
        }

        if (commandBytes == null || commandBytes.Length == 0)
        {
            _logger.LogWarning("Intento de enviar comando vacío");
            return false;
        }

        try
        {
            var url = $"{_listenerBaseUrl}/internal/devices/{deviceSerialNumber}/command";

            _logger.LogInformation(
                "Enviando comando de {Length} bytes al Listener para dispositivo {SerialNumber}",
                commandBytes.Length,
                deviceSerialNumber
            );

            // Enviar comando al Listener
            var content = new ByteArrayContent(commandBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Listener respondió con error {StatusCode} para dispositivo {SerialNumber}: {Error}",
                    response.StatusCode,
                    deviceSerialNumber,
                    errorContent
                );
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ListenerCommandResponse>(responseContent);

            if (result?.Success == true)
            {
                _logger.LogInformation(
                    "Comando enviado exitosamente al dispositivo {SerialNumber} a través del Listener",
                    deviceSerialNumber
                );
                return true;
            }
            else
            {
                _logger.LogWarning(
                    "Listener indicó que no se pudo enviar comando al dispositivo {SerialNumber}: {Message}",
                    deviceSerialNumber,
                    result?.Message ?? "Sin mensaje"
                );
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Error de conexión HTTP al intentar comunicarse con el Listener para dispositivo {SerialNumber}. " +
                "Verifique que el Listener esté ejecutándose en {Url}",
                deviceSerialNumber,
                _listenerBaseUrl
            );
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Excepción al enviar comando al dispositivo {SerialNumber} vía Listener",
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

        try
        {
            var url = $"{_listenerBaseUrl}/internal/devices/{deviceSerialNumber}/status";

            var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonSerializer.Deserialize<ListenerStatusResponse>(responseContent);

            return result?.Connected ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error al verificar estado del dispositivo {SerialNumber}",
                deviceSerialNumber
            );
            return false;
        }
    }

    private class ListenerCommandResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class ListenerStatusResponse
    {
        public string? SerialNumber { get; set; }
        public bool Connected { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
