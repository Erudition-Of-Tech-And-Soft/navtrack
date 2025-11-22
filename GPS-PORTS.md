# NavTrack - Puertos GPS Listener

Este documento lista todos los puertos utilizados por el servicio Navtrack.Listener para diferentes protocolos GPS.

## Configuración de Dispositivos GPS

Para configurar un dispositivo GPS:

1. **Servidor**: `gps-listener-qa.inversionespereztaveras.com` (o la IP del servidor)
2. **Puerto**: Ver tabla abajo según el protocolo de tu dispositivo
3. **Protocolo**: TCP
4. **APN**: Configurar según tu proveedor de tarjeta SIM

## Tabla Completa de Puertos GPS

| Puerto | Protocolo GPS | Fabricante/Modelo |
|--------|---------------|-------------------|
| 7001 | Meitrack | Meitrack |
| 7002 | Teltonika | Teltonika |
| 7003 | Meiligao | Meiligao |
| 7004 | Megastek | Megastek |
| 7005 | Totem | Totem |
| 7006 | Tzone | Tzone |
| 7007 | Coban | Coban |
| 7008 | Queclink | Queclink |
| 7009 | Fifotrack | Fifotrack |
| 7010 | Suntech | Suntech |
| 7011 | TkStar | TkStar |
| 7012 | SinoTrack | SinoTrack |
| 7013 | Concox | Concox |
| 7014 | CanTrack | CanTrack |
| 7015 | LKGPS | LKGPS |
| 7016 | Carscop | Carscop |
| 7017 | Xexun | Xexun |
| 7018 | iStartek | iStartek |
| 7019 | XeElectech | XeElectech |
| 7020 | VjoyCar | VjoyCar |
| 7021 | Eelink | Eelink |
| 7022 | Gosafe | Gosafe |
| 7023 | Skypatrol | Skypatrol |
| 7024 | Xirgo | Xirgo |
| 7025 | Smartrack | Smartrack |
| 7026 | ReachFar | ReachFar |
| 7027 | iCarGPS | iCarGPS |
| 7028 | iTracGPS | iTracGPS |
| 7029 | Alematics | Alematics |
| 7030 | Pretrace | Pretrace |
| 7031 | Arknav | Arknav |
| 7032 | Haicom | Haicom |
| 7033 | CarTrackGPS | CarTrackGPS |
| 7034 | KingSword | KingSword |
| 7035 | Amwell | Amwell |
| 7036 | Sanav | Sanav |
| 7037 | Gotop | Gotop |
| 7038 | GlobalSat | GlobalSat |
| 7039 | GoPass | GoPass |
| 7040 | Jointech | Jointech |
| 7041 | KeSon | KeSon |
| 7042 | Bofan | Bofan |
| 7043 | VSun | VSun |
| 7044 | BlueIdea | BlueIdea |
| 7045 | ManPower | ManPower |
| 7046 | WondeProud | WondeProud |
| 7047 | GPSMarker | GPSMarker |
| 7048 | Eview | Eview |
| 7049 | Freedom | Freedom |
| 7050 | Topfly | Topfly |
| 7051 | StarLink/ERM | StarLink |
| 7052 | Laipac | Laipac |
| 7054 | Navtelecom | Navtelecom |
| 7055 | Galileosky | Galileosky |
| 7056 | Ruptela | Ruptela |
| 7057 | Arusnavi | Arusnavi |
| 7058 | Neomatica | Neomatica |
| 7059 | Satellite | Satellite |
| 7060 | Autofon | Autofon |
| 7061 | ATrack | ATrack |

## Protocolos Más Comunes

Los siguientes protocolos son los más utilizados:

### Teltonika (Puerto 7002)
- Dispositivos: FM1100, FMB120, FMB130, FMB140, FMT100, etc.
- Configuración típica:
  ```
  Servidor: gps-listener-qa.inversionespereztaveras.com
  Puerto: 7002
  Protocolo: TCP
  ```

### Coban (Puerto 7007)
- Dispositivos: TK102, TK103, TK104, TK106, GPS303, etc.
- Configuración típica:
  ```
  server#gps-listener-qa.inversionespereztaveras.com#7007#
  ```

### Concox (Puerto 7013)
- Dispositivos: GT06, GT06N, GK310, etc.
- Configuración por SMS:
  ```
  SERVER,1,gps-listener-qa.inversionespereztaveras.com,7013,0#
  ```

### Queclink (Puerto 7008)
- Dispositivos: GV55, GV65, GV75, GL300, etc.
- Configuración:
  ```
  AT+GTBSI=gv300,0,0,0,0,0,0,0,0,0,0,gps-listener-qa.inversionespereztaveras.com,7008,0,,,,,,,,0,0,FFFF$
  ```

### Suntech (Puerto 7010)
- Dispositivos: ST310, ST340, ST4315, ST4505, etc.
- Configuración:
  ```
  ST300CMD;imei;02;gps-listener-qa.inversionespereztaveras.com:7008
  ```

## Verificar Conectividad

### Desde el servidor
Para verificar que los puertos están escuchando:

```bash
# Verificar todos los puertos GPS
netstat -tuln | grep ":700[0-9]"

# Verificar un puerto específico (ej: 7002)
netstat -tuln | grep ":7002"

# Ver conexiones activas
netstat -tunp | grep ":700"
```

### Desde un dispositivo externo
Para verificar conectividad desde fuera del servidor:

```bash
# Verificar que el puerto está abierto
telnet gps-listener-qa.inversionespereztaveras.com 7002

# O con netcat
nc -zv gps-listener-qa.inversionespereztaveras.com 7002
```

## Troubleshooting

### El dispositivo GPS no envía datos

1. **Verificar puerto correcto**:
   - Confirme que está usando el puerto correcto para el protocolo de su dispositivo
   - Consulte el manual del dispositivo o contacte al fabricante

2. **Verificar firewall**:
   ```bash
   sudo ufw status
   sudo ufw allow 7002:7100/tcp
   ```

3. **Verificar logs del listener**:
   ```bash
   cd /opt/navtrack
   docker compose -f docker-compose.prod.yml logs -f listener
   ```

4. **Verificar conexión del dispositivo**:
   - Confirme que el dispositivo tiene señal GPS
   - Confirme que el dispositivo tiene conexión celular/internet
   - Verifique el balance de la tarjeta SIM
   - Verifique la configuración del APN

5. **Test de conectividad**:
   ```bash
   # Desde el servidor
   telnet localhost 7002

   # Verificar que el contenedor listener está escuchando
   docker exec navtrack-listener netstat -tuln | grep ":7002"
   ```

### Ver dispositivos conectados

```bash
# Ver conexiones activas a los puertos GPS
netstat -tunp | grep ":700" | grep ESTABLISHED

# Ver logs de conexiones en tiempo real
cd /opt/navtrack
docker compose -f docker-compose.prod.yml logs -f listener | grep "listening"
```

## Comandos de Configuración por Protocolo

### Teltonika (SMS)
```
setparam 2001:gps-listener-qa.inversionespereztaveras.com
setparam 2002:7002
```

### Coban (SMS)
```
server#gps-listener-qa.inversionespereztaveras.com#7007#
```

### Concox (SMS)
```
SERVER,1,gps-listener-qa.inversionespereztaveras.com,7013,0#
```

### TkStar (SMS)
```
SERVER#gps-listener-qa.inversionespereztaveras.com#7011#
```

### SinoTrack (SMS)
```
adminip gps-listener-qa.inversionespereztaveras.com 7012
```

## Notas Importantes

1. **Protocolo TCP**: Todos los listeners usan protocolo TCP, no UDP
2. **Sin autenticación en puerto**: Los dispositivos GPS no requieren autenticación a nivel de puerto
3. **Identificación por IMEI**: Los dispositivos se identifican por su IMEI
4. **Registro en NavTrack**: Debe registrar el IMEI del dispositivo en la aplicación web NavTrack antes de que los datos se procesen correctamente

## Monitoreo de Listeners

Para monitorear el estado de los listeners:

```bash
# Ver estadísticas del listener
cd /opt/navtrack
docker stats navtrack-listener

# Ver logs filtrados por puerto
docker compose -f docker-compose.prod.yml logs listener | grep "7002"

# Ver todos los puertos escuchando
docker exec navtrack-listener netstat -tuln | grep LISTEN | grep ":70"
```

## Soporte

Si tiene problemas con la configuración de un dispositivo GPS específico:

1. Consulte el manual del dispositivo
2. Verifique los logs del listener para errores
3. Confirme que el protocolo del dispositivo está soportado
4. Verifique la conectividad de red del dispositivo
