using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Navtrack.DataAccess.Mongo;
using Navtrack.Listener.Server;
using Navtrack.Shared.Library.DI;

namespace Navtrack.Listener;

public class Program
{
    public static async Task Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        // Crear WebApplicationBuilder en lugar de HostApplicationBuilder
        // para soportar tanto TCP listeners como HTTP endpoints
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOptions<MongoOptions>()
            .Bind(builder.Configuration.GetSection(nameof(MongoOptions)));

        builder.Services.AddCustomServices<Program>();

        // Registrar DeviceConnectionManager como singleton
        builder.Services.AddSingleton<IDeviceConnectionManager, DeviceConnectionManager>();

        // Agregar controladores para endpoints HTTP internos
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        WebApplication app = builder.Build();

        // Configurar endpoints HTTP solo para red interna
        app.MapControllers();

        await app.RunAsync();
    }
}