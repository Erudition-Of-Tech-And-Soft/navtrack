using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Navtrack.DataAccess.Model.Assets;
using Navtrack.DataAccess.Mongo;
using Navtrack.DataAccess.Services.Assets;

namespace Navtrack.Api.Services.Background;

public class AssetStatusUpdateService : BackgroundService
{
    private readonly ILogger<AssetStatusUpdateService> _logger;
    private readonly IRepository _repository;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(1);

    public AssetStatusUpdateService(
        ILogger<AssetStatusUpdateService> logger,
        IRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Asset Status Update Service iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Ejecutando actualización de estados de assets...");

                // Actualizar estado de GPS inactivo
                await UpdateGpsInactiveStatus();

                // Actualizar estado de pagos atrasados
                await UpdateDelayedStatus();

                _logger.LogInformation("Actualización de estados completada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estados de assets");
            }

            // Esperar 1 hora antes de la próxima actualización
            await Task.Delay(UpdateInterval, stoppingToken);
        }

        _logger.LogInformation("Asset Status Update Service detenido");
    }

    private async Task UpdateGpsInactiveStatus()
    {
        try
        {
            _logger.LogInformation("Actualizando estado de GPS inactivo...");

            // Obtener todos los assets
            var allAssets = await _repository.GetQueryable<AssetDocument>().ToListAsync();

            var now = DateTime.UtcNow;
            var inactiveThreshold = now.AddDays(-2); // 2 días atrás
            int updatedCount = 0;

            foreach (var asset in allAssets)
            {
                bool shouldBeInactive = false;

                // Verificar si tiene última posición
                if (asset.LastPositionMessage != null)
                {
                    // Obtener el timestamp del último mensaje
                    var lastPositionTime = asset.LastPositionMessage.CreatedDate;

                    // Si la última posición es más antigua que 2 días, marcar como inactivo
                    shouldBeInactive = lastPositionTime < inactiveThreshold;
                }
                else
                {
                    // Si no tiene ninguna posición registrada, también es inactivo
                    shouldBeInactive = true;
                }

                // Solo actualizar si el estado ha cambiado
                if (asset.GpsInactive != shouldBeInactive)
                {
                    await _repository.GetCollection<AssetDocument>()
                        .UpdateOneAsync(
                            x => x.Id == asset.Id,
                            Builders<AssetDocument>.Update.Set(x => x.GpsInactive, shouldBeInactive)
                        );
                    updatedCount++;

                    _logger.LogInformation(
                        "Asset {AssetName} ({AssetId}) GPS inactivo actualizado a: {Status}",
                        asset.Name,
                        asset.Id,
                        shouldBeInactive ? "INACTIVO" : "ACTIVO"
                    );
                }
            }

            _logger.LogInformation(
                "Estado de GPS inactivo actualizado. {Count} assets modificados de {Total} totales",
                updatedCount,
                allAssets.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado de GPS inactivo");
            throw;
        }
    }

    private async Task UpdateDelayedStatus()
    {
        // TODO: Implementar lógica de verificación de pagos atrasados
        // Este método se debe implementar según la lógica de negocio específica
        // de cómo se manejan los pagos y cuándo un miembro está atrasado

        _logger.LogInformation("UpdateDelayedStatus: Pendiente de implementación");

        // Pseudo-código para referencia futura:
        /*
        try
        {
            _logger.LogInformation("Actualizando estado de pagos atrasados...");

            // 1. Obtener todos los assets que tienen members asignados
            var assetsWithMembers = await _assetRepository.GetAssetsWithMembers();

            int updatedCount = 0;

            foreach (var asset in assetsWithMembers)
            {
                // 2. Para cada member del asset, verificar su estado de pago
                bool hasMemberDelayed = false;

                foreach (var user in asset.Users)
                {
                    if (user.Role == AssetUserRole.Member)
                    {
                        // 3. Consultar sistema de pagos/facturación
                        // var paymentStatus = await _paymentService.GetPaymentStatus(user.UserId, asset.OrganizationId);

                        // 4. Determinar si está atrasado
                        // if (paymentStatus.IsDelayed)
                        // {
                        //     hasMemberDelayed = true;
                        //     break;
                        // }
                    }
                }

                // 5. Actualizar flag si cambió
                if (asset.IsDelayed != hasMemberDelayed)
                {
                    asset.IsDelayed = hasMemberDelayed;
                    await _assetRepository.Update(asset);
                    updatedCount++;

                    _logger.LogInformation(
                        "Asset {AssetName} ({AssetId}) estado de pago actualizado a: {Status}",
                        asset.Name,
                        asset.Id,
                        hasMemberDelayed ? "ATRASADO" : "AL DÍA"
                    );

                    // 6. Enviar notificación push si cambió a atrasado
                    // if (hasMemberDelayed)
                    // {
                    //     await _notificationService.SendDelayedPaymentNotification(asset, user);
                    // }
                }
            }

            _logger.LogInformation(
                "Estado de pagos actualizado. {Count} assets modificados",
                updatedCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado de pagos");
            throw;
        }
        */

        await Task.CompletedTask;
    }
}
