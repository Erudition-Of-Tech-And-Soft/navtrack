# Guía de Configuración de Dispositivos GPS Coban

## Introducción

Esta guía explica cómo agregar y configurar dispositivos GPS de la marca **Coban Electronics** en la plataforma NavTrack. Los dispositivos Coban son rastreadores GPS populares y económicos, ampliamente utilizados para rastreo vehicular y de activos.

## Modelos Coban Soportados

NavTrack es compatible con los siguientes modelos de Coban Electronics:

| Modelo | ID | Protocolo | Puerto |
|--------|-----|-----------|--------|
| GPS-102B | 143 | coban | 7007 |
| GPS-103A | 144 | coban | 7007 |
| GPS-103B | 145 | coban | 7007 |
| GPS-104 | 146 | coban | 7007 |
| GPS-105A | 147 | coban | 7007 |
| GPS-105B | 148 | coban | 7007 |
| GPS-108 | 149 | coban | 7007 |
| GPS-303F | 150 | coban | 7007 |
| GPS-303G | 151 | coban | 7007 |
| GPS-303H | 152 | coban | 7007 |
| GPS-303I | 153 | coban | 7007 |
| GPS-306 | 154 | coban | 7007 |
| GPS-308 | 155 | coban | 7007 |
| GPS-310 | 156 | coban | 7007 |
| GPS-311 | 157 | coban | 7007 |

**Puerto TCP:** 7007

## Protocolo de Comunicación Coban

### Formato de Mensajes

El protocolo Coban utiliza mensajes de texto ASCII que terminan con punto y coma (`;`):

#### 1. Autenticación (IMEI)
```
##,imei:359586015829802,A;
```
- El dispositivo envía su IMEI al conectarse
- La plataforma responde con: `LOAD`

#### 2. Heartbeat (Latido)
```
359586015829802
```
- El dispositivo envía solo el IMEI como heartbeat
- La plataforma responde con: `ON`

#### 3. Datos de Ubicación
```
imei:359587010124900,tracker,0809231929,13554900601,F,112909.397,A,2234.4669,N,11354.3287,E,0.11,;
```

**Campos del mensaje de ubicación:**
1. `imei:359587010124900` - IMEI del dispositivo
2. `tracker` - Tipo de mensaje
3. `0809231929` - Fecha y hora (DDMMYYHHNN)
4. `13554900601` - Número de teléfono
5. `F` - Estado (F = Fix GPS válido, L = Sin fix GPS)
6. `112909.397` - Hora UTC (HHMMSS.SSS)
7. `A` - Estado de validez (A = Válido, V = No válido)
8. `2234.4669` - Latitud en formato DDmm.mmmm
9. `N` - Hemisferio Norte/Sur
10. `11354.3287` - Longitud en formato DDDmm.mmmm
11. `E` - Hemisferio Este/Oeste
12. `0.11` - Velocidad en nudos
13. `;` - Terminador del mensaje

## Configuración Paso a Paso

### Paso 1: Verificar que el Servidor Esté Escuchando en el Puerto 7007

En el servidor Linux donde está instalado NavTrack:

```bash
# Verificar que el puerto 7007 está abierto
sudo netstat -tlnp | grep 7007

# Verificar el firewall
sudo ufw status | grep 7007

# Si el puerto no está abierto, agregarlo al firewall
sudo ufw allow 7007/tcp
sudo ufw reload
```

### Paso 2: Registrar el Dispositivo en NavTrack

#### Opción A: Desde la API REST

1. **Crear un Asset (Activo) en el sistema**

```bash
curl -X POST https://gps-api-qa.inversionespereztaveras.com/api/assets \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Vehículo - Coban GPS-303G",
    "deviceType": 151
  }'
```

**Nota:** El `deviceType: 151` corresponde al modelo GPS-303G de Coban (ver tabla arriba).

2. **Asociar el IMEI del dispositivo**

El IMEI se registra automáticamente cuando el dispositivo se conecta por primera vez y envía su mensaje de autenticación.

#### Opción B: Desde la Interfaz Web

1. Accede a: `https://gps-qa.inversionespereztaveras.com`
2. Inicia sesión en tu cuenta
3. Ve a **Dispositivos** → **Agregar Nuevo Dispositivo**
4. Completa el formulario:
   - **Nombre:** Nombre descriptivo (ej: "Camión 01 - Coban GPS-303G")
   - **Marca:** Coban Electronics
   - **Modelo:** GPS-303G (u otro modelo que tengas)
   - **IMEI:** 359586015829802 (el IMEI de tu dispositivo)
5. Guarda el dispositivo

### Paso 3: Configurar el Dispositivo GPS Coban

Los dispositivos Coban se configuran mediante comandos SMS. Envía un SMS al número de la SIM instalada en el dispositivo GPS.

#### Comandos SMS Principales

1. **Configurar el servidor de rastreo:**

```
adminip123456 TUDOMINIO 7007
```

Ejemplo para el entorno QA:
```
adminip123456 gps-listener-qa.inversionespereztaveras.com 7007
```

**Explicación:**
- `adminip123456` - Comando + contraseña por defecto
- `gps-listener-qa.inversionespereztaveras.com` - Dominio o IP del servidor
- `7007` - Puerto TCP del protocolo Coban

2. **Configurar el APN de tu operador móvil:**

```
apn123456 NOMBRE_APN
```

Ejemplos por operador en República Dominicana:
```
# Claro
apn123456 internet.ideasclaro.com.do

# Altice
apn123456 internet.altice.com.do

# Viva
apn123456 internet.viva.com.do
```

3. **Configurar intervalo de reporte:**

```
upload123456 30
```

Esto configura el dispositivo para enviar ubicaciones cada 30 segundos.

4. **Verificar la configuración:**

```
check123456
```

El dispositivo responderá con su configuración actual.

5. **Reiniciar el dispositivo:**

```
reset123456
```

### Paso 4: Verificar la Conexión

#### En el Servidor

1. **Ver logs del contenedor Listener:**

```bash
sudo docker logs navtrack-listener --tail 100 -f
```

Deberías ver mensajes como:
```
[2025-11-21 10:30:15] [Coban] Device connected: 359586015829802
[2025-11-21 10:30:15] [Coban] Authentication successful: 359586015829802
[2025-11-21 10:30:20] [Coban] Location received from: 359586015829802
```

2. **Monitorear conexiones TCP al puerto 7007:**

```bash
sudo netstat -anp | grep :7007
```

Deberías ver conexiones ESTABLISHED desde la IP del dispositivo GPS.

#### En la Plataforma Web

1. Accede a: `https://gps-qa.inversionespereztaveras.com`
2. Ve a **Dispositivos** → Tu dispositivo Coban
3. Deberías ver:
   - **Estado:** Conectado (icono verde)
   - **Última ubicación:** Hace pocos segundos
   - **Posición en el mapa:** Actualizada
   - **Velocidad, rumbo, altitud:** Datos en tiempo real

## Solución de Problemas

### El dispositivo no se conecta

1. **Verificar configuración del servidor en el GPS:**
   ```
   check123456
   ```
   Debe mostrar el dominio y puerto correcto.

2. **Verificar conectividad de red del GPS:**
   ```
   # Pedir que el GPS envíe su ubicación vía SMS
   tracker123456
   ```

3. **Verificar firewall del servidor:**
   ```bash
   sudo ufw status | grep 7007
   # Debe mostrar: 7007/tcp ALLOW Anywhere
   ```

4. **Verificar que el contenedor Listener esté corriendo:**
   ```bash
   sudo docker ps | grep navtrack-listener
   ```

### El dispositivo se conecta pero no envía ubicaciones

1. **Verificar señal GPS del dispositivo:**
   - El LED GPS del dispositivo debe estar parpadeando (tiene señal GPS)
   - Si es sólido o apagado, el GPS no tiene fix satelital
   - Coloca el dispositivo en un lugar con vista al cielo

2. **Verificar intervalo de reporte:**
   ```
   upload123456 30
   ```

3. **Verificar que el dispositivo esté configurado en modo de rastreo continuo:**
   ```
   tracker123456
   ```

### Datos incorrectos en la plataforma

1. **Verificar logs del parser:**
   ```bash
   sudo docker logs navtrack-listener | grep -A 10 "Coban"
   ```

2. **Verificar zona horaria:**
   Los Coban envían hora UTC. La plataforma debe convertir a tu zona horaria.

## Comandos SMS Útiles

| Comando | Descripción |
|---------|-------------|
| `check123456` | Verificar configuración actual |
| `adminip123456 SERVIDOR PUERTO` | Configurar servidor de rastreo |
| `apn123456 APN` | Configurar APN del operador |
| `upload123456 SEGUNDOS` | Configurar intervalo de reporte |
| `tracker123456` | Obtener ubicación actual vía SMS |
| `monitor123456` | Activar modo de escucha (llamada de voz) |
| `sleep123456 10 22` | Modo ahorro: dormir entre 10pm-10am |
| `nosleep123456` | Desactivar modo ahorro |
| `begin123456` | Activar rastreo continuo |
| `stockade123456 LAT,LON;RADIO` | Configurar geocerca |
| `speed123456 80` | Alerta de velocidad (80 km/h) |
| `move123456` | Alerta de movimiento |
| `reset123456` | Reiniciar dispositivo |
| `adminip123456` | Cambiar contraseña de administrador |

**Nota:** `123456` es la contraseña por defecto. Es altamente recomendable cambiarla:

```
password123456 NUEVA_CONTRASEÑA
```

## Características Soportadas

✅ **Soportado por NavTrack:**
- Ubicación GPS (latitud, longitud)
- Altitud
- Velocidad
- Rumbo (dirección)
- Fecha y hora
- Estado de GPS (válido/no válido)
- Identificación por IMEI
- Heartbeat (latido de conexión)

❌ **No soportado actualmente:**
- Nivel de batería
- Señal GSM
- Estado de entradas/salidas digitales
- Lectura de sensor de combustible
- Lectura de temperatura

## Consejos de Instalación Física

1. **Antena GPS:** Debe tener vista clara al cielo, instalar cerca del parabrisas
2. **Antena GSM:** Alejar de componentes metálicos
3. **Alimentación:** Conectar directamente a batería del vehículo (cable rojo +12V, negro GND)
4. **Protección:** Usar fusible de 2A en línea de alimentación
5. **Ocultamiento:** Instalar en lugar discreto y difícil de acceder

## Información Técnica

- **Protocolo:** TCP/IP
- **Puerto:** 7007
- **Encoding:** ASCII
- **Terminador de mensaje:** `;` (punto y coma, byte 0x3B)
- **Formato de coordenadas:** Grados-Minutos Decimales (DDmm.mmmm)
- **Sistema de coordenadas:** WGS84
- **Velocidad:** Nudos (convertida automáticamente a km/h)

## Archivos de Código Relacionados

Para desarrolladores que quieran entender o modificar el protocolo Coban:

- **Definición del protocolo:** [backend/Navtrack.Listener/Protocols/Coban/CobanProtocol.cs](backend/Navtrack.Listener/Protocols/Coban/CobanProtocol.cs)
- **Parser de mensajes:** [backend/Navtrack.Listener/Protocols/Coban/CobanMessageHandler.cs](backend/Navtrack.Listener/Protocols/Coban/CobanMessageHandler.cs)
- **Tests unitarios:** [backend/Navtrack.Listener.Tests/Protocols/Coban/CobanProtocolTests.cs](backend/Navtrack.Listener.Tests/Protocols/Coban/CobanProtocolTests.cs)

## Soporte

Si tienes problemas:

1. Revisa los logs del contenedor Listener
2. Verifica que el firewall permita el puerto 7007
3. Confirma que el dispositivo esté configurado correctamente
4. Revisa que el IMEI esté registrado en la plataforma

Para más ayuda, consulta la documentación principal en [LEEME-PRIMERO.txt](LEEME-PRIMERO.txt).
