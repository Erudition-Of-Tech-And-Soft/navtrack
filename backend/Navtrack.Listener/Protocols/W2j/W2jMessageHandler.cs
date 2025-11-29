using System;
using System.Collections.Generic;
using System.Text;
using Navtrack.DataAccess.Model.Devices.Messages;
using Navtrack.Listener.Models;
using Navtrack.Listener.Server;
using Navtrack.Shared.Library.DI;

namespace Navtrack.Listener.Protocols.W2j;

/// <summary>
/// Manejador de mensajes para dispositivos GPS W2j (4G)
/// Protocolo basado en JT/T 808
///
/// Estructura de trama:
/// [0x7E] [MsgID 2B] [MsgBodyAttr 2B] [DeviceID 6B] [SeqNum 2B] [Body...] [Checksum 1B] [0x7E]
/// </summary>
[Service(typeof(ICustomMessageHandler<W2jProtocol>))]
public class W2jMessageHandler : BaseMessageHandler<W2jProtocol>
{
    public override IEnumerable<DeviceMessageDocument>? ParseRange(MessageInput input)
    {
        // Verificar longitud mínima (sin los delimitadores 0x7E)
        // MsgID(2) + MsgBodyAttr(2) + DeviceID(6) + SeqNum(2) + Checksum(1) = 13 bytes mínimo
        if (input.DataMessage.Bytes.Length < 13)
        {
            return null;
        }

        // Realizar unescape de la trama (protocolo JT808 usa escape)
        byte[] unescapedData = UnescapeMessage(input.DataMessage.Bytes);

        // Verificar checksum
        if (!VerifyChecksum(unescapedData))
        {
            return null;
        }

        // Parsear header
        ushort messageId = (ushort)((unescapedData[0] << 8) | unescapedData[1]);
        ushort bodyAttributes = (ushort)((unescapedData[2] << 8) | unescapedData[3]);
        int bodyLength = bodyAttributes & 0x03FF; // Bits 0-9 = longitud del cuerpo

        // Extraer Device ID (6 bytes BCD)
        string deviceId = GetDeviceId(unescapedData, 4);

        // Número de secuencia
        ushort sequenceNumber = (ushort)((unescapedData[10] << 8) | unescapedData[11]);

        // Cuerpo del mensaje comienza en byte 12
        byte[] messageBody = new byte[bodyLength];
        if (bodyLength > 0 && unescapedData.Length >= 12 + bodyLength)
        {
            Array.Copy(unescapedData, 12, messageBody, 0, bodyLength);
        }

        // Procesar según tipo de mensaje
        DeviceMessageDocument? message = messageId switch
        {
            MessageType.TerminalRegistration => HandleRegistration(input, deviceId, sequenceNumber, messageBody),
            MessageType.TerminalAuthentication => HandleAuthentication(input, deviceId, sequenceNumber),
            MessageType.TerminalHeartbeat => HandleHeartbeat(input, deviceId, sequenceNumber),
            MessageType.LocationReport => HandleLocationReport(input, deviceId, sequenceNumber, messageBody),
            MessageType.BatchLocationReport => HandleBatchLocationReport(input, deviceId, sequenceNumber, messageBody),
            _ => HandleUnknownMessage(input, deviceId, messageId)
        };

        return message != null ? new[] { message } : null;
    }

    /// <summary>
    /// Maneja mensaje de registro del terminal
    /// Estructura del body:
    /// [ProvinceID 2B] [CityID 2B] [ManufacturerID 5B] [TerminalModel 20B] [TerminalID 7B] [PlateColor 1B] [PlateNumber...]
    /// </summary>
    private DeviceMessageDocument? HandleRegistration(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        input.ConnectionContext.SetDevice(deviceId);

        // Enviar respuesta de registro exitoso
        byte[] response = BuildRegistrationResponse(deviceId, sequenceNumber, 0, "OK");
        input.NetworkStream.Write(response);

        // Extraer información del registro si está disponible
        if (body.Length >= 37)
        {
            // Modelo del terminal (bytes 9-28, 20 bytes)
            string terminalModel = Encoding.ASCII.GetString(body, 9, 20).Trim('\0', ' ');

            // ID del terminal (bytes 29-35, 7 bytes)
            string terminalId = Encoding.ASCII.GetString(body, 29, 7).Trim('\0', ' ');

            // Placa si está disponible (después del byte 37)
            string plate = "";
            if (body.Length > 37)
            {
                plate = Encoding.ASCII.GetString(body, 37, body.Length - 37).Trim('\0', ' ');
            }
        }

        // No retornar mensaje sin datos de posición
        return null;
    }

    /// <summary>
    /// Maneja mensaje de autenticación
    /// </summary>
    private DeviceMessageDocument? HandleAuthentication(MessageInput input, string deviceId, ushort sequenceNumber)
    {
        input.ConnectionContext.SetDevice(deviceId);

        // Enviar respuesta genérica de éxito
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.TerminalAuthentication, 0);
        input.NetworkStream.Write(response);

        // No retornar mensaje sin datos de posición
        return null;
    }

    /// <summary>
    /// Maneja mensaje de heartbeat
    /// </summary>
    private DeviceMessageDocument? HandleHeartbeat(MessageInput input, string deviceId, ushort sequenceNumber)
    {
        input.ConnectionContext.SetDevice(deviceId);

        // Enviar respuesta de heartbeat
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.TerminalHeartbeat, 0);
        input.NetworkStream.Write(response);

        // No retornar mensaje sin datos de posición
        return null;
    }

    /// <summary>
    /// Maneja reporte de ubicación
    /// Estructura del body:
    /// [AlarmFlag 4B] [Status 4B] [Latitude 4B] [Longitude 4B] [Altitude 2B] [Speed 2B] [Direction 2B] [DateTime 6B BCD]
    /// </summary>
    private DeviceMessageDocument? HandleLocationReport(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        if (body.Length < 28)
        {
            return null;
        }

        input.ConnectionContext.SetDevice(deviceId);

        // Enviar respuesta
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.LocationReport, 0);
        input.NetworkStream.Write(response);

        // Parsear datos de ubicación
        uint alarmFlags = (uint)((body[0] << 24) | (body[1] << 16) | (body[2] << 8) | body[3]);
        uint status = (uint)((body[4] << 24) | (body[5] << 16) | (body[6] << 8) | body[7]);

        // Coordenadas (en unidades de 1/1,000,000 de grado)
        int latitudeRaw = (body[8] << 24) | (body[9] << 16) | (body[10] << 8) | body[11];
        int longitudeRaw = (body[12] << 24) | (body[13] << 16) | (body[14] << 8) | body[15];

        double latitude = latitudeRaw / 1_000_000.0;
        double longitude = longitudeRaw / 1_000_000.0;

        // Aplicar hemisferio según status
        // Bit 2: 0=Este, 1=Oeste
        // Bit 3: 0=Norte, 1=Sur
        if ((status & 0x04) != 0) longitude = -longitude;
        if ((status & 0x08) != 0) latitude = -latitude;

        // Altitud (metros)
        ushort altitude = (ushort)((body[16] << 8) | body[17]);

        // Velocidad (1/10 km/h)
        ushort speedRaw = (ushort)((body[18] << 8) | body[19]);
        double speed = speedRaw / 10.0;

        // Dirección (grados)
        ushort heading = (ushort)((body[20] << 8) | body[21]);

        // Fecha/hora BCD (YY MM DD HH mm ss)
        DateTime dateTime = ParseBcdDateTime(body, 22);

        // Verificar si tiene GPS válido
        bool hasGps = (status & 0x02) != 0;

        return new DeviceMessageDocument
        {
            Position = new PositionElement
            {
                Date = dateTime,
                Valid = hasGps,
                Latitude = latitude,
                Longitude = longitude,
                Altitude = hasGps ? altitude : null,
                Speed = hasGps ? (float)speed : null,
                Heading = hasGps ? heading : null
            }
        };
    }

    /// <summary>
    /// Maneja reporte de ubicación en lote
    /// </summary>
    private DeviceMessageDocument? HandleBatchLocationReport(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        input.ConnectionContext.SetDevice(deviceId);

        // Por ahora solo respondemos, la implementación completa requeriría devolver múltiples entidades
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.BatchLocationReport, 0);
        input.NetworkStream.Write(response);

        // No retornar mensaje sin implementación completa
        return null;
    }

    /// <summary>
    /// Maneja mensajes desconocidos
    /// </summary>
    private DeviceMessageDocument? HandleUnknownMessage(MessageInput input, string deviceId, ushort messageId)
    {
        input.ConnectionContext.SetDevice(deviceId);

        // No retornar mensaje para tipos desconocidos
        return null;
    }

    #region Utilidades de parsing

    /// <summary>
    /// Extrae el Device ID de 6 bytes BCD
    /// En JT/T 808, el Device ID es BCD (Binary Coded Decimal)
    /// Cada byte representa 2 dígitos decimales
    /// Ejemplo: 0x01 0x84 0x04 0x22 0x83 0x23 → "18404228323" (sin leading zeros)
    /// </summary>
    private string GetDeviceId(byte[] data, int offset)
    {
        StringBuilder sb = new StringBuilder(12);
        for (int i = 0; i < 6; i++)
        {
            // Convertir byte BCD a 2 dígitos decimales
            byte bcd = data[offset + i];
            int high = (bcd >> 4) & 0x0F;  // Nibble alto
            int low = bcd & 0x0F;           // Nibble bajo
            sb.Append(high);
            sb.Append(low);
        }
        return sb.ToString().TrimStart('0');
    }

    /// <summary>
    /// Parsea fecha/hora en formato BCD
    /// </summary>
    private DateTime ParseBcdDateTime(byte[] data, int offset)
    {
        try
        {
            int year = 2000 + BcdToDec(data[offset]);
            int month = BcdToDec(data[offset + 1]);
            int day = BcdToDec(data[offset + 2]);
            int hour = BcdToDec(data[offset + 3]);
            int minute = BcdToDec(data[offset + 4]);
            int second = BcdToDec(data[offset + 5]);

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Convierte byte BCD a decimal
    /// </summary>
    private int BcdToDec(byte bcd)
    {
        return ((bcd >> 4) * 10) + (bcd & 0x0F);
    }

    /// <summary>
    /// Convierte decimal a byte BCD
    /// </summary>
    private byte DecToBcd(int dec)
    {
        return (byte)(((dec / 10) << 4) | (dec % 10));
    }

    #endregion

    #region Escape/Unescape del protocolo

    /// <summary>
    /// Remueve escape del mensaje (0x7D 0x02 -> 0x7E, 0x7D 0x01 -> 0x7D)
    /// </summary>
    private byte[] UnescapeMessage(byte[] data)
    {
        using var ms = new System.IO.MemoryStream();
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == 0x7D && i + 1 < data.Length)
            {
                if (data[i + 1] == 0x02)
                {
                    ms.WriteByte(0x7E);
                    i++;
                }
                else if (data[i + 1] == 0x01)
                {
                    ms.WriteByte(0x7D);
                    i++;
                }
                else
                {
                    ms.WriteByte(data[i]);
                }
            }
            else
            {
                ms.WriteByte(data[i]);
            }
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Aplica escape al mensaje (0x7E -> 0x7D 0x02, 0x7D -> 0x7D 0x01)
    /// </summary>
    private byte[] EscapeMessage(byte[] data)
    {
        using var ms = new System.IO.MemoryStream();
        foreach (byte b in data)
        {
            if (b == 0x7E)
            {
                ms.WriteByte(0x7D);
                ms.WriteByte(0x02);
            }
            else if (b == 0x7D)
            {
                ms.WriteByte(0x7D);
                ms.WriteByte(0x01);
            }
            else
            {
                ms.WriteByte(b);
            }
        }
        return ms.ToArray();
    }

    #endregion

    #region Verificación de checksum

    /// <summary>
    /// Verifica el checksum XOR del mensaje
    /// </summary>
    private bool VerifyChecksum(byte[] data)
    {
        if (data.Length < 2)
            return false;

        byte calculated = 0;
        // XOR de todos los bytes excepto el último (que es el checksum)
        for (int i = 0; i < data.Length - 1; i++)
        {
            calculated ^= data[i];
        }

        return calculated == data[data.Length - 1];
    }

    /// <summary>
    /// Calcula checksum XOR
    /// </summary>
    private byte CalculateChecksum(byte[] data)
    {
        byte checksum = 0;
        foreach (byte b in data)
        {
            checksum ^= b;
        }
        return checksum;
    }

    #endregion

    #region Construcción de respuestas

    /// <summary>
    /// Construye respuesta genérica de la plataforma (0x8001)
    /// </summary>
    private byte[] BuildGeneralResponse(string deviceId, ushort sequenceNumber, ushort responseMessageId, byte result)
    {
        // Body: [SeqNum 2B] [MsgID 2B] [Result 1B]
        byte[] body = new byte[5];
        body[0] = (byte)(sequenceNumber >> 8);
        body[1] = (byte)(sequenceNumber & 0xFF);
        body[2] = (byte)(responseMessageId >> 8);
        body[3] = (byte)(responseMessageId & 0xFF);
        body[4] = result;

        return BuildMessage(MessageType.PlatformGeneralResponse, deviceId, body);
    }

    /// <summary>
    /// Construye respuesta de registro de la plataforma (0x8100)
    /// </summary>
    private byte[] BuildRegistrationResponse(string deviceId, ushort sequenceNumber, byte result, string authCode)
    {
        // Body: [SeqNum 2B] [Result 1B] [AuthCode...]
        byte[] authBytes = Encoding.ASCII.GetBytes(authCode);
        byte[] body = new byte[3 + authBytes.Length];
        body[0] = (byte)(sequenceNumber >> 8);
        body[1] = (byte)(sequenceNumber & 0xFF);
        body[2] = result;
        Array.Copy(authBytes, 0, body, 3, authBytes.Length);

        return BuildMessage(MessageType.PlatformRegistrationResponse, deviceId, body);
    }

    /// <summary>
    /// Construye un mensaje completo con header, body y checksum
    /// </summary>
    private byte[] BuildMessage(ushort messageId, string deviceId, byte[] body)
    {
        using var ms = new System.IO.MemoryStream();

        // Header
        // Message ID (2 bytes)
        ms.WriteByte((byte)(messageId >> 8));
        ms.WriteByte((byte)(messageId & 0xFF));

        // Body attributes (2 bytes) - solo longitud por ahora
        ushort bodyAttr = (ushort)(body.Length & 0x03FF);
        ms.WriteByte((byte)(bodyAttr >> 8));
        ms.WriteByte((byte)(bodyAttr & 0xFF));

        // Device ID (6 bytes BCD)
        byte[] deviceIdBytes = ParseDeviceIdToBytes(deviceId);
        ms.Write(deviceIdBytes, 0, 6);

        // Sequence number (2 bytes) - usamos 0
        ms.WriteByte(0);
        ms.WriteByte(0);

        // Body
        ms.Write(body, 0, body.Length);

        byte[] messageData = ms.ToArray();

        // Calcular checksum
        byte checksum = CalculateChecksum(messageData);

        // Agregar checksum al mensaje ANTES de escapar
        ms.WriteByte(checksum);
        byte[] messageWithChecksum = ms.ToArray();

        // Aplicar escape al mensaje completo (incluyendo checksum)
        byte[] escapedData = EscapeMessage(messageWithChecksum);

        // Construir mensaje final con delimitadores
        using var finalMs = new System.IO.MemoryStream();
        finalMs.WriteByte(0x7E);
        finalMs.Write(escapedData, 0, escapedData.Length);
        finalMs.WriteByte(0x7E);

        return finalMs.ToArray();
    }

    /// <summary>
    /// Convierte Device ID string a bytes BCD
    /// Ejemplo: "18404228323" → PadLeft → "018404228323" → BCD bytes: 01 84 04 22 83 23
    /// </summary>
    private byte[] ParseDeviceIdToBytes(string deviceId)
    {
        byte[] result = new byte[6];
        deviceId = deviceId.PadLeft(12, '0');

        for (int i = 0; i < 6; i++)
        {
            // Obtener 2 dígitos decimales
            string digits = deviceId.Substring(i * 2, 2);
            int value = int.Parse(digits);

            // Convertir a BCD: cada dígito va en un nibble
            byte bcd = (byte)(((value / 10) << 4) | (value % 10));
            result[i] = bcd;
        }

        return result;
    }

    #endregion
}
