using System.Collections.Generic;
using Navtrack.Listener.Server;
using Navtrack.Shared.Library.DI;

namespace Navtrack.Listener.Protocols.W2j;

/// <summary>
/// Protocolo para dispositivos GPS W2j (4G)
/// Basado en el protocolo JT/T 808 (estándar chino)
/// </summary>
[Service(typeof(IProtocol))]
public class W2jProtocol : BaseProtocol
{
    public override int Port => 7053;

    // Inicio de trama: 0x7E
    public override byte[] MessageStart => [0x7E];

    // Fin de trama: 0x7E
    public override IEnumerable<byte[]> MessageEnd => new List<byte[]> { new byte[] { 0x7E } };
}
