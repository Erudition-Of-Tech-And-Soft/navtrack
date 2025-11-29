using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
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
    private readonly IDeviceConnectionManager _connectionManager;
    private readonly ILogger<W2jMessageHandler> _logger;

    public W2jMessageHandler(
        IDeviceConnectionManager connectionManager,
        ILogger<W2jMessageHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }
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
            MessageType.LocationQueryResponse => HandleLocationQueryResponse(input, deviceId, sequenceNumber, messageBody),
            MessageType.BatchLocationReport => HandleBatchLocationReport(input, deviceId, sequenceNumber, messageBody),
            MessageType.TerminalGeneralResponse => HandleTerminalGeneralResponse(input, deviceId, sequenceNumber, messageBody),
            MessageType.QueryParametersResponse => HandleQueryParametersResponse(input, deviceId, sequenceNumber, messageBody),
            MessageType.TextInformationSubmit => HandleTextInformationSubmit(input, deviceId, sequenceNumber, messageBody),
            MessageType.MultimediaDataUpload => HandleMultimediaDataUpload(input, deviceId, sequenceNumber, messageBody),
            _ => HandleUnknownMessage(input, deviceId, messageId)
        };

        return message != null ? new[] { message } : null;
    }

    /// <summary>
    /// Registra la conexión del dispositivo con el DeviceConnectionManager
    /// </summary>
    private void RegisterDeviceConnection(MessageInput input, string deviceId)
    {
        input.ConnectionContext.SetDevice(deviceId);

        // Registrar conexión TCP con el DeviceConnectionManager
        if (!_connectionManager.IsDeviceConnected(deviceId))
        {
            var tcpConnection = new TcpDeviceConnection(
                input.NetworkStream.NetworkStream,
                deviceId,
                _logger
            );

            _connectionManager.RegisterConnection(deviceId, tcpConnection);

            _logger.LogInformation(
                "Device {SerialNumber} registered with connection manager",
                deviceId
            );
        }
    }

    /// <summary>
    /// Maneja mensaje de registro del terminal
    /// Estructura del body:
    /// [ProvinceID 2B] [CityID 2B] [ManufacturerID 5B] [TerminalModel 20B] [TerminalID 7B] [PlateColor 1B] [PlateNumber...]
    /// </summary>
    private DeviceMessageDocument? HandleRegistration(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        RegisterDeviceConnection(input, deviceId);

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
        RegisterDeviceConnection(input, deviceId);

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
        RegisterDeviceConnection(input, deviceId);

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

        RegisterDeviceConnection(input, deviceId);

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

        // Parsear información adicional si existe (después del byte 28)
        var additionalInfo = new Dictionary<string, object>();
        if (body.Length > 28)
        {
            ParseAdditionalLocationInfo(body, 28, additionalInfo);
        }

        var positionElement = new PositionElement
        {
            Date = dateTime,
            Valid = hasGps,
            Latitude = latitude,
            Longitude = longitude,
            Altitude = hasGps ? altitude : null,
            Speed = hasGps ? (float)speed : null,
            Heading = hasGps ? heading : null
        };

        // Agregar información adicional como propiedades
        if (additionalInfo.Count > 0)
        {
            foreach (var kvp in additionalInfo)
            {
                // TODO: Agregar a propiedades extendidas del PositionElement si se requiere
            }
        }

        return new DeviceMessageDocument
        {
            Position = positionElement
        };
    }

    /// <summary>
    /// Maneja reporte de ubicación en lote
    /// </summary>
    private DeviceMessageDocument? HandleBatchLocationReport(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        RegisterDeviceConnection(input, deviceId);

        // Por ahora solo respondemos, la implementación completa requeriría devolver múltiples entidades
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.BatchLocationReport, 0);
        input.NetworkStream.Write(response);

        // No retornar mensaje sin implementación completa
        return null;
    }

    /// <summary>
    /// Maneja respuesta de consulta de ubicación (0x0201)
    /// Mismo formato que 0x0200
    /// </summary>
    private DeviceMessageDocument? HandleLocationQueryResponse(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        // Mismo formato que LocationReport
        return HandleLocationReport(input, deviceId, sequenceNumber, body);
    }

    /// <summary>
    /// Maneja respuesta general del terminal (0x0001)
    /// Body: [SeqNum 2B] [MsgID 2B] [Result 1B]
    /// </summary>
    private DeviceMessageDocument? HandleTerminalGeneralResponse(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        RegisterDeviceConnection(input, deviceId);

        if (body.Length >= 5)
        {
            ushort responseSeqNum = (ushort)((body[0] << 8) | body[1]);
            ushort responseMsgId = (ushort)((body[2] << 8) | body[3]);
            byte result = body[4];

            // Log: Terminal respondió al mensaje {responseMsgId} con resultado {result}
        }

        return null;
    }

    /// <summary>
    /// Maneja respuesta de consulta de parámetros (0x0104)
    /// Body: [SeqNum 2B] [ParamCount 1B] [Parameters...]
    /// </summary>
    private DeviceMessageDocument? HandleQueryParametersResponse(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        RegisterDeviceConnection(input, deviceId);

        // Enviar confirmación
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.QueryParametersResponse, 0);
        input.NetworkStream.Write(response);

        // TODO: Parsear parámetros si es necesario para logging/config
        return null;
    }

    /// <summary>
    /// Maneja envío de texto del terminal (0x6006)
    /// Body: [Flag 1B] [Text...]
    /// </summary>
    private DeviceMessageDocument? HandleTextInformationSubmit(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        RegisterDeviceConnection(input, deviceId);

        if (body.Length > 1)
        {
            byte flag = body[0];
            string text = Encoding.GetEncoding("GBK").GetString(body, 1, body.Length - 1).TrimEnd('\0');

            // Log: Texto recibido del terminal: {text}
        }

        // Enviar confirmación
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.TextInformationSubmit, 0);
        input.NetworkStream.Write(response);

        return null;
    }

    /// <summary>
    /// Maneja subida de datos multimedia (0x0801)
    /// Body: [MultimediaID 4B] [Type 1B] [Format 1B] [EventCode 1B] [ChannelID 1B] [Data...]
    /// </summary>
    private DeviceMessageDocument? HandleMultimediaDataUpload(MessageInput input, string deviceId, ushort sequenceNumber, byte[] body)
    {
        RegisterDeviceConnection(input, deviceId);

        if (body.Length >= 8)
        {
            uint multimediaId = (uint)((body[0] << 24) | (body[1] << 16) | (body[2] << 8) | body[3]);
            byte mediaType = body[4]; // 0:imagen, 1:audio, 2:video
            byte format = body[5]; // 0:JPEG, 1:TIF, 2:MP3, 3:WAV, 4:WMV
            byte eventCode = body[6];
            byte channelId = body[7];

            // TODO: Guardar datos multimedia si es necesario
        }

        // Enviar confirmación
        byte[] response = BuildGeneralResponse(deviceId, sequenceNumber, MessageType.MultimediaDataUpload, 0);
        input.NetworkStream.Write(response);

        return null;
    }

    /// <summary>
    /// Maneja mensajes desconocidos
    /// </summary>
    private DeviceMessageDocument? HandleUnknownMessage(MessageInput input, string deviceId, ushort messageId)
    {
        RegisterDeviceConnection(input, deviceId);

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

    /// <summary>
    /// Parsea información adicional de ubicación según Tabla 20 del protocolo JT808
    /// </summary>
    private void ParseAdditionalLocationInfo(byte[] data, int offset, Dictionary<string, object> info)
    {
        int pos = offset;
        while (pos + 2 <= data.Length)
        {
            byte id = data[pos];
            byte length = data[pos + 1];
            pos += 2;

            if (pos + length > data.Length)
                break;

            switch (id)
            {
                case 0x01: // Mileage (DWORD, 1/10km)
                    if (length == 4)
                    {
                        uint mileage = (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
                        info["Mileage"] = mileage / 10.0; // km
                    }
                    break;

                case 0x2B: // Fuel consumption (DWORD)
                    if (length == 4)
                    {
                        uint fuel = (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
                        info["FuelConsumption"] = fuel;
                    }
                    break;

                case 0x30: // Network signal strength CSQ (BYTE)
                    if (length == 1)
                    {
                        info["CSQ"] = data[pos];
                    }
                    break;

                case 0x31: // GPS satellite count (BYTE)
                    if (length == 1)
                    {
                        info["SatelliteCount"] = data[pos];
                    }
                    break;

                case 0x52: // Forward/Reverse (BYTE)
                    if (length == 1)
                    {
                        // 0:unknown, 1:forward(empty), 2:reverse(loaded), 3:stop
                        info["Direction"] = data[pos];
                    }
                    break;

                case 0x53: // 2G base station data (1+n*8)
                    if (length >= 1)
                    {
                        int bsCount = data[pos];
                        var baseStations = new List<Dictionary<string, object>>();
                        for (int i = 0; i < bsCount && pos + 1 + (i + 1) * 8 <= pos + length; i++)
                        {
                            int bsOffset = pos + 1 + i * 8;
                            var bs = new Dictionary<string, object>
                            {
                                ["MCC"] = (data[bsOffset] << 8) | data[bsOffset + 1],
                                ["MNC"] = data[bsOffset + 2],
                                ["LAC"] = (data[bsOffset + 3] << 8) | data[bsOffset + 4],
                                ["CellID"] = (data[bsOffset + 5] << 8) | data[bsOffset + 6],
                                ["Signal"] = data[bsOffset + 7]
                            };
                            baseStations.Add(bs);
                        }
                        info["BaseStations2G"] = baseStations;
                    }
                    break;

                case 0x54: // WiFi data (1+n*7) - NUEVO en v1.1
                    if (length >= 1)
                    {
                        int wifiCount = data[pos];
                        var wifiNetworks = new List<Dictionary<string, object>>();
                        for (int i = 0; i < wifiCount && pos + 1 + (i + 1) * 7 <= pos + length; i++)
                        {
                            int wifiOffset = pos + 1 + i * 7;
                            var wifi = new Dictionary<string, object>
                            {
                                ["MAC"] = BitConverter.ToString(data, wifiOffset, 6).Replace("-", ":"),
                                ["Signal"] = data[wifiOffset + 6]
                            };
                            wifiNetworks.Add(wifi);
                        }
                        info["WiFiNetworks"] = wifiNetworks;
                    }
                    break;

                case 0x56: // Internal battery capacity (2 bytes)
                    if (length == 2)
                    {
                        info["BatteryLevel"] = data[pos];
                        // byte 2 reserved
                    }
                    break;

                case 0x5D: // 4G base station data (1+n*10)
                    if (length >= 1)
                    {
                        int bsCount = data[pos];
                        var baseStations = new List<Dictionary<string, object>>();
                        for (int i = 0; i < bsCount && pos + 1 + (i + 1) * 10 <= pos + length; i++)
                        {
                            int bsOffset = pos + 1 + i * 10;
                            var bs = new Dictionary<string, object>
                            {
                                ["MCC"] = (data[bsOffset] << 8) | data[bsOffset + 1],
                                ["MNC"] = data[bsOffset + 2],
                                ["LAC"] = (data[bsOffset + 3] << 8) | data[bsOffset + 4],
                                ["CellID"] = (data[bsOffset + 5] << 24) | (data[bsOffset + 6] << 16) |
                                            (data[bsOffset + 7] << 8) | data[bsOffset + 8],
                                ["Signal"] = data[bsOffset + 9]
                            };
                            baseStations.Add(bs);
                        }
                        info["BaseStations4G"] = baseStations;
                    }
                    break;

                case 0x61: // Main power supply voltage (WORD, 0.01V)
                    if (length == 2)
                    {
                        ushort voltage = (ushort)((data[pos] << 8) | data[pos + 1]);
                        info["Voltage"] = voltage / 100.0; // V
                    }
                    break;

                case 0xF1: // ICCID (20 bytes)
                    if (length == 20)
                    {
                        info["ICCID"] = Encoding.ASCII.GetString(data, pos, length).TrimEnd('\0');
                    }
                    break;

                case 0xF3: // Fortification/withdrawal status - NUEVO en v1.1
                    if (length == 1)
                    {
                        info["Fortified"] = data[pos] == 0x01;
                    }
                    break;
            }

            pos += length;
        }
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

    #region Métodos para enviar comandos desde la plataforma al terminal

    /// <summary>
    /// Envía comando de control al terminal (0x8105)
    /// </summary>
    /// <param name="deviceId">ID del dispositivo</param>
    /// <param name="command">Código de comando según Tabla 14</param>
    /// <param name="parameters">Parámetros del comando (opcional)</param>
    /// <returns>Bytes del mensaje completo para enviar</returns>
    public static byte[] BuildTerminalControlCommand(string deviceId, byte command, string? parameters = null)
    {
        using var ms = new System.IO.MemoryStream();
        ms.WriteByte(command);

        if (!string.IsNullOrEmpty(parameters))
        {
            byte[] paramBytes = Encoding.GetEncoding("GBK").GetBytes(parameters);
            ms.Write(paramBytes, 0, paramBytes.Length);
        }

        byte[] body = ms.ToArray();
        return BuildMessageStatic(MessageType.TerminalControl, deviceId, body);
    }

    /// <summary>
    /// Envía comando de consulta de ubicación (0x8201)
    /// </summary>
    public static byte[] BuildLocationQueryCommand(string deviceId)
    {
        // Body vacío
        return BuildMessageStatic(MessageType.LocationInformationQuery, deviceId, Array.Empty<byte>());
    }

    /// <summary>
    /// Envía comando de consulta de parámetros (0x8104)
    /// </summary>
    public static byte[] BuildQueryParametersCommand(string deviceId)
    {
        // Body vacío
        return BuildMessageStatic(MessageType.QueryTerminalParameters, deviceId, Array.Empty<byte>());
    }

    /// <summary>
    /// Envía comando de configuración de parámetros (0x8103)
    /// </summary>
    /// <param name="deviceId">ID del dispositivo</param>
    /// <param name="parameters">Diccionario de parámetros: Key=ParameterID, Value=byte[]</param>
    public static byte[] BuildSetParametersCommand(string deviceId, Dictionary<uint, byte[]> parameters)
    {
        using var ms = new System.IO.MemoryStream();

        // Total de parámetros
        ms.WriteByte((byte)parameters.Count);

        // Escribir cada parámetro
        foreach (var param in parameters)
        {
            // Parameter ID (4 bytes)
            ms.WriteByte((byte)(param.Key >> 24));
            ms.WriteByte((byte)((param.Key >> 16) & 0xFF));
            ms.WriteByte((byte)((param.Key >> 8) & 0xFF));
            ms.WriteByte((byte)(param.Key & 0xFF));

            // Parameter length
            ms.WriteByte((byte)param.Value.Length);

            // Parameter value
            ms.Write(param.Value, 0, param.Value.Length);
        }

        byte[] body = ms.ToArray();
        return BuildMessageStatic(MessageType.SetTerminalParameters, deviceId, body);
    }

    /// <summary>
    /// Envía mensaje de texto al terminal (0x8300)
    /// </summary>
    /// <param name="deviceId">ID del dispositivo</param>
    /// <param name="text">Texto a enviar (codificación GBK)</param>
    /// <param name="flags">Flags del mensaje (por defecto 0x02 = transmisión de texto)</param>
    public static byte[] BuildTextDistributionCommand(string deviceId, string text, byte flags = 0x02)
    {
        using var ms = new System.IO.MemoryStream();
        ms.WriteByte(flags);

        byte[] textBytes = Encoding.GetEncoding("GBK").GetBytes(text);
        ms.Write(textBytes, 0, textBytes.Length);

        byte[] body = ms.ToArray();
        return BuildMessageStatic(MessageType.TextDistribution, deviceId, body);
    }

    /// <summary>
    /// Versión estática del BuildMessage para poder llamarla desde métodos estáticos
    /// </summary>
    private static byte[] BuildMessageStatic(ushort messageId, string deviceId, byte[] body)
    {
        using var ms = new System.IO.MemoryStream();

        // Header
        ms.WriteByte((byte)(messageId >> 8));
        ms.WriteByte((byte)(messageId & 0xFF));

        // Body attributes
        ushort bodyAttr = (ushort)(body.Length & 0x03FF);
        ms.WriteByte((byte)(bodyAttr >> 8));
        ms.WriteByte((byte)(bodyAttr & 0xFF));

        // Device ID (6 bytes BCD)
        byte[] deviceIdBytes = ParseDeviceIdToBytesStatic(deviceId);
        ms.Write(deviceIdBytes, 0, 6);

        // Sequence number (usando timestamp para variación)
        ushort seqNum = (ushort)(DateTime.UtcNow.Ticks & 0xFFFF);
        ms.WriteByte((byte)(seqNum >> 8));
        ms.WriteByte((byte)(seqNum & 0xFF));

        // Body
        ms.Write(body, 0, body.Length);

        byte[] messageData = ms.ToArray();

        // Checksum
        byte checksum = CalculateChecksumStatic(messageData);
        ms.WriteByte(checksum);
        byte[] messageWithChecksum = ms.ToArray();

        // Escape
        byte[] escapedData = EscapeMessageStatic(messageWithChecksum);

        // Final message with delimiters
        using var finalMs = new System.IO.MemoryStream();
        finalMs.WriteByte(0x7E);
        finalMs.Write(escapedData, 0, escapedData.Length);
        finalMs.WriteByte(0x7E);

        return finalMs.ToArray();
    }

    private static byte[] ParseDeviceIdToBytesStatic(string deviceId)
    {
        byte[] result = new byte[6];
        deviceId = deviceId.PadLeft(12, '0');

        for (int i = 0; i < 6; i++)
        {
            string digits = deviceId.Substring(i * 2, 2);
            int value = int.Parse(digits);
            byte bcd = (byte)(((value / 10) << 4) | (value % 10));
            result[i] = bcd;
        }

        return result;
    }

    private static byte CalculateChecksumStatic(byte[] data)
    {
        byte checksum = 0;
        foreach (byte b in data)
        {
            checksum ^= b;
        }
        return checksum;
    }

    private static byte[] EscapeMessageStatic(byte[] data)
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
}
