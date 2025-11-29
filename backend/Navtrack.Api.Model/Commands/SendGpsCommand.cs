using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Navtrack.Api.Model.Commands;

public class SendGpsCommand
{
    /// <summary>
    /// Tipo de comando a enviar
    /// Valores válidos: "CutFuel", "RestoreFuel", "Fortify", "Withdraw", "QueryLocation", "Restart", "RestoreFactory"
    /// </summary>
    [Required]
    public string CommandType { get; set; }

    /// <summary>
    /// Parámetros adicionales del comando (opcional)
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}
