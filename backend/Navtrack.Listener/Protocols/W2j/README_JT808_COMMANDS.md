# Protocolo JT808 v1.1 - Guía de Comandos

Este documento describe cómo usar los comandos implementados del protocolo JT808 v1.1 para dispositivos W2j.

## Tabla de Contenido

1. [Comandos de Control del Terminal](#comandos-de-control-del-terminal)
2. [Configuración de Parámetros](#configuración-de-parámetros)
3. [Consulta de Información](#consulta-de-información)
4. [Mensajes de Texto](#mensajes-de-texto)
5. [Información Adicional de Ubicación](#información-adicional-de-ubicación)

---

## Comandos de Control del Terminal

### Cortar Combustible y Electricidad

```csharp
// Construir comando para cortar combustible
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.CutOffOilAndElectricity
);

// Enviar por NetworkStream
networkStream.Write(command);
```

### Restaurar Combustible y Electricidad

```csharp
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.RestoreOilAndElectricity
);
networkStream.Write(command);
```

### Activar Fortificación (Nuevo en v1.1)

```csharp
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.ExternalFortification
);
networkStream.Write(command);
```

### Retirar Fortificación (Nuevo en v1.1)

```csharp
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.ExternalWithdrawal
);
networkStream.Write(command);
```

### Reiniciar Terminal

```csharp
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.Reset
);
networkStream.Write(command);
```

### Restaurar Configuración de Fábrica

```csharp
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.RestoreFactorySettings
);
networkStream.Write(command);
```

### Activar Grabación Continua

```csharp
// Grabar por 30 minutos
byte[] timeBytes = BitConverter.GetBytes((ushort)30);
if (BitConverter.IsLittleEndian)
    Array.Reverse(timeBytes); // Convertir a big-endian

string parameters = Encoding.ASCII.GetString(timeBytes);

byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.TurnOnContinuousRecording,
    parameters: parameters
);
networkStream.Write(command);
```

### Detener Todas las Grabaciones

```csharp
byte[] command = W2jMessageHandler.BuildTerminalControlCommand(
    deviceId: "18404228323",
    command: TerminalCommands.StopAllRecordings
);
networkStream.Write(command);
```

---

## Configuración de Parámetros

### Configurar Intervalo de Heartbeat

```csharp
var parameters = new Dictionary<uint, byte[]>();

// Intervalo de 60 segundos
byte[] interval = new byte[4];
interval[0] = 0x00;
interval[1] = 0x00;
interval[2] = 0x00;
interval[3] = 0x3C; // 60 en decimal

parameters[TerminalParameters.HeartbeatInterval] = interval;

byte[] command = W2jMessageHandler.BuildSetParametersCommand(
    deviceId: "18404228323",
    parameters: parameters
);
networkStream.Write(command);
```

### Configurar Intervalo de Reporte por Tiempo

```csharp
var parameters = new Dictionary<uint, byte[]>();

// Reportar cada 30 segundos
byte[] interval = BitConverter.GetBytes((uint)30);
if (BitConverter.IsLittleEndian)
    Array.Reverse(interval); // Convertir a big-endian

parameters[TerminalParameters.DefaultTimeReportingInterval] = interval;

byte[] command = W2jMessageHandler.BuildSetParametersCommand(
    deviceId: "18404228323",
    parameters: parameters
);
networkStream.Write(command);
```

### Configurar Estrategia de Reporte

```csharp
var parameters = new Dictionary<uint, byte[]>();

// 0: regular, 1: por distancia, 2: por tiempo e intervalo
byte[] strategy = BitConverter.GetBytes((uint)2);
if (BitConverter.IsLittleEndian)
    Array.Reverse(strategy);

parameters[TerminalParameters.LocationReportingStrategy] = strategy;

byte[] command = W2jMessageHandler.BuildSetParametersCommand(
    deviceId: "18404228323",
    parameters: parameters
);
networkStream.Write(command);
```

### Configurar Velocidad Máxima

```csharp
var parameters = new Dictionary<uint, byte[]>();

// Velocidad máxima 120 km/h
byte[] maxSpeed = BitConverter.GetBytes((uint)120);
if (BitConverter.IsLittleEndian)
    Array.Reverse(maxSpeed);

parameters[TerminalParameters.MaximumSpeed] = maxSpeed;

byte[] command = W2jMessageHandler.BuildSetParametersCommand(
    deviceId: "18404228323",
    parameters: parameters
);
networkStream.Write(command);
```

### Configurar Servidor y Puerto

```csharp
var parameters = new Dictionary<uint, byte[]>();

// Servidor principal
string serverAddress = "gps.example.com";
parameters[TerminalParameters.PrimaryServerAddress] =
    Encoding.GetEncoding("GBK").GetBytes(serverAddress + "\0");

// Puerto TCP 7053
byte[] port = BitConverter.GetBytes((uint)7053);
if (BitConverter.IsLittleEndian)
    Array.Reverse(port);
parameters[TerminalParameters.ServerTcpPort] = port;

byte[] command = W2jMessageHandler.BuildSetParametersCommand(
    deviceId: "18404228323",
    parameters: parameters
);
networkStream.Write(command);
```

### Configurar Múltiples Parámetros a la Vez

```csharp
var parameters = new Dictionary<uint, byte[]>();

// Heartbeat cada 60 segundos
byte[] heartbeat = BitConverter.GetBytes((uint)60);
if (BitConverter.IsLittleEndian) Array.Reverse(heartbeat);
parameters[TerminalParameters.HeartbeatInterval] = heartbeat;

// Reporte cada 30 segundos
byte[] reportInterval = BitConverter.GetBytes((uint)30);
if (BitConverter.IsLittleEndian) Array.Reverse(reportInterval);
parameters[TerminalParameters.DefaultTimeReportingInterval] = reportInterval;

// Velocidad máxima 100 km/h
byte[] maxSpeed = BitConverter.GetBytes((uint)100);
if (BitConverter.IsLittleEndian) Array.Reverse(maxSpeed);
parameters[TerminalParameters.MaximumSpeed] = maxSpeed;

byte[] command = W2jMessageHandler.BuildSetParametersCommand(
    deviceId: "18404228323",
    parameters: parameters
);
networkStream.Write(command);
```

---

## Consulta de Información

### Consultar Ubicación Actual

```csharp
byte[] command = W2jMessageHandler.BuildLocationQueryCommand(
    deviceId: "18404228323"
);
networkStream.Write(command);

// El terminal responderá con un mensaje 0x0201 (LocationQueryResponse)
// que tiene el mismo formato que 0x0200 (LocationReport)
```

### Consultar Parámetros del Terminal

```csharp
byte[] command = W2jMessageHandler.BuildQueryParametersCommand(
    deviceId: "18404228323"
);
networkStream.Write(command);

// El terminal responderá con un mensaje 0x0104 (QueryParametersResponse)
// conteniendo todos los parámetros configurados
```

---

## Mensajes de Texto

### Enviar Mensaje de Texto al Terminal

```csharp
byte[] command = W2jMessageHandler.BuildTextDistributionCommand(
    deviceId: "18404228323",
    text: "Mensaje de prueba desde el servidor",
    flags: 0x02 // 0x02 = transmisión de texto
);
networkStream.Write(command);
```

### Enviar Mensaje Urgente

```csharp
// Flag 0x01 = Urgente, 0x02 = Transmisión de texto
byte flags = 0x01 | 0x02; // 0x03

byte[] command = W2jMessageHandler.BuildTextDistributionCommand(
    deviceId: "18404228323",
    text: "¡MENSAJE URGENTE!",
    flags: flags
);
networkStream.Write(command);
```

### Enviar Mensaje con TTS (Text-to-Speech)

```csharp
// Flag 0x08 = TTS broadcast reading
byte flags = 0x02 | 0x08; // 0x0A

byte[] command = W2jMessageHandler.BuildTextDistributionCommand(
    deviceId: "18404228323",
    text: "Este mensaje será leído en voz alta",
    flags: flags
);
networkStream.Write(command);
```

---

## Información Adicional de Ubicación

### Datos Parseados Automáticamente

Al recibir un mensaje de ubicación (0x0200 o 0x0201), el handler parsea automáticamente la siguiente información adicional:

#### WiFi Networks (Nuevo en v1.1)
- **ID**: 0x54
- **Contenido**: Lista de redes WiFi detectadas con MAC y señal
- **Uso**: Mejora la geolocalización en interiores

```csharp
// Ejemplo de información WiFi parseada:
// additionalInfo["WiFiNetworks"] = [
//     { "MAC": "AA:BB:CC:DD:EE:FF", "Signal": 75 },
//     { "MAC": "11:22:33:44:55:66", "Signal": 60 }
// ]
```

#### Estado de Fortificación (Nuevo en v1.1)
- **ID**: 0xF3
- **Contenido**: true = fortificado, false = retirado
- **Uso**: Indica si el dispositivo está en modo fortificación

```csharp
// Ejemplo:
// additionalInfo["Fortified"] = true
```

#### Estaciones Base 4G
- **ID**: 0x5D
- **Contenido**: Información de torres celulares 4G cercanas
- **Uso**: Geolocalización por triangulación celular

```csharp
// additionalInfo["BaseStations4G"] = [
//     { "MCC": 334, "MNC": 20, "LAC": 1234, "CellID": 56789, "Signal": 85 }
// ]
```

#### Otros Datos Disponibles

- **0x01**: Kilometraje acumulado (km)
- **0x2B**: Consumo de combustible
- **0x30**: Intensidad de señal de red (CSQ 0-31)
- **0x31**: Número de satélites GPS
- **0x52**: Dirección adelante/reversa
- **0x53**: Estaciones base 2G
- **0x56**: Nivel de batería interna
- **0x61**: Voltaje de alimentación principal
- **0xF1**: ICCID de la tarjeta SIM

---

## Códigos de Resultado

Cuando el terminal responde a comandos (0x0001 o respuestas específicas), usa estos códigos:

- **0**: Éxito/Confirmación
- **1**: Fallo
- **2**: Mensaje incorrecto
- **3**: No soportado
- **4**: Confirmación de procesamiento de alarma (solo en 0x8001)

---

## Notas Importantes

1. **Encoding GBK**: Todos los textos usan codificación GBK (chino)
2. **Big-Endian**: Los valores multi-byte se transmiten en orden big-endian (network byte order)
3. **BCD para Device ID**: El Device ID se transmite en formato BCD (Binary Coded Decimal)
4. **Escape de bytes**: Los bytes 0x7E y 0x7D en el cuerpo del mensaje deben ser escapados
5. **Checksum XOR**: Se calcula XOR de todos los bytes del header y body
6. **Sequence Number**: Cada mensaje debe tener un número de secuencia único

---

## Ejemplo Completo de Flujo

```csharp
public async Task ManageDevice(string deviceId, NetworkStream stream)
{
    // 1. Consultar ubicación actual
    byte[] queryCmd = W2jMessageHandler.BuildLocationQueryCommand(deviceId);
    await stream.WriteAsync(queryCmd);

    // 2. Configurar parámetros
    var parameters = new Dictionary<uint, byte[]>();

    byte[] heartbeat = BitConverter.GetBytes((uint)60);
    if (BitConverter.IsLittleEndian) Array.Reverse(heartbeat);
    parameters[TerminalParameters.HeartbeatInterval] = heartbeat;

    byte[] setParamsCmd = W2jMessageHandler.BuildSetParametersCommand(deviceId, parameters);
    await stream.WriteAsync(setParamsCmd);

    // 3. Activar fortificación
    byte[] fortifyCmd = W2jMessageHandler.BuildTerminalControlCommand(
        deviceId,
        TerminalCommands.ExternalFortification
    );
    await stream.WriteAsync(fortifyCmd);

    // 4. Enviar mensaje de confirmación
    byte[] textCmd = W2jMessageHandler.BuildTextDistributionCommand(
        deviceId,
        "Configuración completada exitosamente"
    );
    await stream.WriteAsync(textCmd);
}
```

---

## Referencias

- Protocolo JT/T 808 v1.1 (2021.11.15)
- Archivo: `Universal version of JT808 protocol V1.1.pdf`
- Implementación: `W2jMessageHandler.cs`, `MessageType.cs`, `TerminalCommands.cs`
