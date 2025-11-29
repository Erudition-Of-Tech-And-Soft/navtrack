using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Navtrack.Listener.Server;

/// <summary>
/// Represents a TCP connection to a GPS device
/// </summary>
public class TcpDeviceConnection : IDeviceConnection
{
    private readonly NetworkStream _stream;
    private readonly ILogger _logger;
    private readonly string _serialNumber;
    private DateTime _lastActivity;

    public string DeviceSerialNumber => _serialNumber;
    public bool IsConnected => _stream.Socket?.Connected ?? false;
    public DateTime LastActivity => _lastActivity;

    public TcpDeviceConnection(
        NetworkStream stream,
        string serialNumber,
        ILogger logger)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _serialNumber = serialNumber ?? throw new ArgumentNullException(nameof(serialNumber));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lastActivity = DateTime.UtcNow;
    }

    public async Task<bool> SendAsync(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            _logger.LogWarning(
                "Attempted to send empty data to device {SerialNumber}",
                _serialNumber
            );
            return false;
        }

        if (!IsConnected)
        {
            _logger.LogWarning(
                "Cannot send data to device {SerialNumber} - not connected",
                _serialNumber
            );
            return false;
        }

        try
        {
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _lastActivity = DateTime.UtcNow;

            _logger.LogInformation(
                "Sent {Length} bytes to device {SerialNumber}",
                data.Length,
                _serialNumber
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending data to device {SerialNumber}",
                _serialNumber
            );
            return false;
        }
    }

    public void UpdateActivity()
    {
        _lastActivity = DateTime.UtcNow;
    }

    public Task CloseAsync()
    {
        try
        {
            _stream?.Close();
            _stream?.Dispose();

            _logger.LogInformation(
                "Closed connection for device {SerialNumber}",
                _serialNumber
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error closing connection for device {SerialNumber}",
                _serialNumber
            );
        }

        return Task.CompletedTask;
    }
}
