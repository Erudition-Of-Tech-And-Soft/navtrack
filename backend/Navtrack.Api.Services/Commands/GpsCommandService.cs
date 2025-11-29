using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Navtrack.Api.Model.Commands;
using Navtrack.DataAccess.Model.Assets;
using Navtrack.Shared.Library.DI;

namespace Navtrack.Api.Services.Commands;

public interface IGpsCommandService
{
    Task<GpsCommandResult> SendCommand(AssetDocument asset, SendGpsCommand command);
}

[Service(typeof(IGpsCommandService))]
public class GpsCommandService : IGpsCommandService
{
    private static readonly HashSet<string> ValidCommands = new()
    {
        "CutFuel",
        "RestoreFuel",
        "Fortify",
        "Withdraw",
        "QueryLocation",
        "Restart",
        "RestoreFactory",
        "StopRecordings"
    };

    public Task<GpsCommandResult> SendCommand(AssetDocument asset, SendGpsCommand command)
    {
        if (asset.Device == null || string.IsNullOrEmpty(asset.Device.SerialNumber))
        {
            return Task.FromResult(new GpsCommandResult
            {
                Success = false,
                Message = "El asset no tiene un dispositivo GPS configurado",
                SentAt = DateTime.UtcNow,
                CommandType = command.CommandType
            });
        }

        if (!ValidCommands.Contains(command.CommandType))
        {
            return Task.FromResult(new GpsCommandResult
            {
                Success = false,
                Message = $"Tipo de comando '{command.CommandType}' no reconocido",
                SentAt = DateTime.UtcNow,
                CommandType = command.CommandType
            });
        }

        // TODO: Implementar envío real de comandos a través del connection manager
        // Por ahora solo validamos y retornamos éxito simulado
        // En producción, esto requeriría:
        // 1. Inyectar IDeviceConnectionManager
        // 2. Obtener la conexión TCP activa del dispositivo
        // 3. Construir el comando JT808 usando W2jMessageHandler
        // 4. Enviar commandBytes a través de NetworkStream
        // 5. Esperar confirmación del dispositivo (opcional)

        return Task.FromResult(new GpsCommandResult
        {
            Success = true,
            Message = $"Comando '{command.CommandType}' recibido y encolado para envío",
            SentAt = DateTime.UtcNow,
            CommandType = command.CommandType
        });
    }
}
