# NavTrack - Checklist de Instalación

Use este checklist para asegurar una instalación exitosa de NavTrack en producción.

## Pre-Instalación

### ☐ Servidor

- [ ] Servidor Linux (Ubuntu 20.04+ o Debian 10+)
- [ ] Mínimo 4GB RAM
- [ ] Mínimo 20GB espacio en disco disponible
- [ ] Acceso SSH con privilegios root/sudo
- [ ] Dirección IP pública estática
- [ ] Conexión a Internet estable

### ☐ Dominios y DNS

Configure los siguientes registros DNS tipo A apuntando a la IP de su servidor:

- [ ] `gps-qa.inversionespereztaveras.com` → IP del servidor
- [ ] `gps-api-qa.inversionespereztaveras.com` → IP del servidor
- [ ] `gps-odoo-qa.inversionespereztaveras.com` → IP del servidor
- [ ] `gps-listener-qa.inversionespereztaveras.com` → IP del servidor

**Verificación**:
```bash
# Verificar que los dominios resuelven correctamente
host gps-qa.inversionespereztaveras.com
host gps-api-qa.inversionespereztaveras.com
host gps-odoo-qa.inversionespereztaveras.com
host gps-listener-qa.inversionespereztaveras.com
```

### ☐ Firewall y Puertos

Asegúrese de que los siguientes puertos estén abiertos en el firewall del servidor y del proveedor de hosting:

- [ ] Puerto 22 (SSH)
- [ ] Puerto 80 (HTTP - para certificados SSL)
- [ ] Puerto 443 (HTTPS - aplicaciones web)
- [ ] Puertos 7002-7100 (TCP - GPS Listeners)

**Verificación desde fuera del servidor**:
```bash
# Desde otro equipo
telnet IP_SERVIDOR 80
telnet IP_SERVIDOR 443
telnet IP_SERVIDOR 7002
```

### ☐ Información Requerida

- [ ] Email válido para certificados SSL: `___________________________`
- [ ] Usuario SSH: `___________________________`
- [ ] Contraseña/clave SSH: `___________________________`

## Instalación

### ☐ Preparar el Servidor

```bash
# 1. Conectar al servidor
ssh usuario@IP_SERVIDOR

# 2. Actualizar sistema
sudo apt update && sudo apt upgrade -y

# 3. Instalar git si no está instalado
sudo apt install -y git

# 4. Verificar fecha/hora del servidor
date
timedatectl  # Debe estar en zona horaria correcta
```

- [ ] Conexión SSH exitosa
- [ ] Sistema actualizado
- [ ] Git instalado
- [ ] Zona horaria correcta

### ☐ Clonar Repositorio

```bash
# Clonar repositorio (ajustar URL según sea necesario)
git clone <URL_REPOSITORIO> /tmp/navtrack
cd /tmp/navtrack
```

- [ ] Repositorio clonado exitosamente
- [ ] Directorio `/tmp/navtrack` existe

### ☐ Configurar Variables (Opcional)

```bash
# Editar el script de instalación si necesita cambiar configuraciones
nano install-navtrack.sh
```

Revisar y ajustar si es necesario:
- [ ] DOMAIN_FRONTEND
- [ ] DOMAIN_API
- [ ] DOMAIN_ODOO_API
- [ ] DOMAIN_LISTENER
- [ ] EMAIL (para Let's Encrypt)
- [ ] INSTALL_DIR
- [ ] MONGO_DATABASE

### ☐ Ejecutar Instalación

```bash
# Dar permisos de ejecución a los scripts
chmod +x install-navtrack.sh
chmod +x navtrack-manage.sh
chmod +x monitor-navtrack.sh
chmod +x troubleshoot.sh

# Ejecutar instalación
sudo ./install-navtrack.sh
```

- [ ] Scripts tienen permisos de ejecución
- [ ] Instalación iniciada
- [ ] Docker instalado correctamente
- [ ] Nginx instalado correctamente
- [ ] Certbot instalado correctamente
- [ ] Archivos copiados a `/opt/navtrack`
- [ ] Docker Compose creado
- [ ] Configuraciones Nginx creadas
- [ ] Firewall configurado
- [ ] Imágenes Docker construidas
- [ ] Servicios iniciados
- [ ] Certificados SSL obtenidos (¡IMPORTANTE!)
- [ ] Servicio systemd creado

**Nota**: Cuando el script solicite continuar con los certificados SSL, presione Enter solo si los DNS están configurados correctamente.

### ☐ Copiar Scripts de Gestión

```bash
# Copiar scripts a ubicaciones convenientes
sudo cp /tmp/navtrack/navtrack-manage.sh /usr/local/bin/navtrack
sudo chmod +x /usr/local/bin/navtrack

sudo cp /tmp/navtrack/monitor-navtrack.sh /opt/navtrack/
sudo cp /tmp/navtrack/troubleshoot.sh /opt/navtrack/
```

- [ ] Script `navtrack` disponible globalmente
- [ ] Scripts de monitoreo y troubleshooting copiados

## Post-Instalación

### ☐ Verificar Instalación

```bash
# Verificar estado de servicios
navtrack status

# Verificar salud del sistema
navtrack health

# Ejecutar diagnóstico completo
sudo /opt/navtrack/troubleshoot.sh
```

- [ ] Todos los contenedores están running
- [ ] Frontend responde (HTTP 200)
- [ ] Backend API responde
- [ ] Odoo API responde
- [ ] MongoDB conecta correctamente
- [ ] Nginx está corriendo
- [ ] Sin errores en logs

### ☐ Verificar Acceso Web

Abrir en un navegador:

- [ ] https://gps-qa.inversionespereztaveras.com (Frontend)
- [ ] https://gps-api-qa.inversionespereztaveras.com (API)
- [ ] https://gps-odoo-qa.inversionespereztaveras.com (Odoo API)

**Verificar**:
- [ ] Páginas cargan correctamente
- [ ] Certificados SSL válidos (candado verde)
- [ ] Sin errores de certificado

### ☐ Verificar Certificados SSL

```bash
# Ver certificados instalados
sudo certbot certificates

# Verificar renovación automática
sudo certbot renew --dry-run
```

- [ ] Certificados instalados para todos los dominios
- [ ] Certificados válidos (no expirados)
- [ ] Auto-renovación funciona correctamente

### ☐ Verificar GPS Listener

```bash
# Verificar puertos escuchando
sudo netstat -tuln | grep ":700"

# Ver logs del listener
navtrack logs listener
```

- [ ] Múltiples puertos 7002-7100 están escuchando
- [ ] Listener inicia sin errores

### ☐ Configurar Monitoreo (Opcional pero Recomendado)

```bash
# Editar crontab
sudo crontab -e

# Agregar estas líneas:
# Verificación cada 5 minutos
*/5 * * * * /opt/navtrack/monitor-navtrack.sh check >> /var/log/navtrack-monitor.log 2>&1

# Reporte diario
0 8 * * * /opt/navtrack/monitor-navtrack.sh report
```

- [ ] Cron job de monitoreo configurado
- [ ] Cron job de reporte configurado

### ☐ Configurar Backup Automático (Opcional pero Recomendado)

```bash
# Editar crontab
sudo crontab -e

# Agregar backup diario a las 2 AM
0 2 * * * /usr/local/bin/navtrack backup >> /var/log/navtrack-backup.log 2>&1
```

- [ ] Backup automático configurado
- [ ] Directorio de backups creado

### ☐ Crear Backup Manual Inicial

```bash
# Crear primer backup
navtrack backup

# Verificar que se creó
ls -lh /opt/navtrack/backups/
```

- [ ] Backup inicial creado
- [ ] Backup se puede listar

## Configuración de Dispositivos GPS

### ☐ Identificar Protocolo del Dispositivo

- [ ] Marca/modelo del dispositivo GPS: `___________________________`
- [ ] Protocolo del dispositivo: `___________________________`
- [ ] Puerto asignado (ver GPS-PORTS.md): `___________________________`

### ☐ Configurar Dispositivo GPS

Configurar en el dispositivo:
- [ ] Servidor: `gps-listener-qa.inversionespereztaveras.com`
- [ ] Puerto: `___________________________` (según protocolo)
- [ ] Protocolo: TCP
- [ ] APN configurado según proveedor SIM

### ☐ Registrar Dispositivo en NavTrack

- [ ] Acceder al frontend web
- [ ] Crear cuenta de usuario
- [ ] Registrar dispositivo con su IMEI
- [ ] Verificar que el dispositivo aparece en la lista

### ☐ Verificar Recepción de Datos

```bash
# Ver logs en tiempo real
navtrack logs listener
```

- [ ] Dispositivo GPS conecta al servidor
- [ ] Datos GPS recibidos en el listener
- [ ] Datos aparecen en el frontend web

## Seguridad Post-Instalación

### ☐ Configurar MongoDB con Autenticación (Recomendado)

```bash
# Acceder a MongoDB
navtrack db-shell

# Crear usuario admin (dentro de mongosh)
use admin
db.createUser({
  user: "admin",
  pwd: "CAMBIAR_ESTA_CONTRASEÑA",
  roles: ["root"]
})
```

- [ ] Usuario admin de MongoDB creado
- [ ] Contraseña segura configurada
- [ ] Actualizar docker-compose.prod.yml con credenciales

### ☐ Configurar Fail2Ban (Recomendado)

```bash
# Instalar fail2ban
sudo apt install fail2ban -y
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

- [ ] Fail2ban instalado
- [ ] Fail2ban corriendo

### ☐ Actualizar Sistema Operativo

```bash
# Configurar actualizaciones automáticas
sudo apt install unattended-upgrades -y
sudo dpkg-reconfigure -plow unattended-upgrades
```

- [ ] Actualizaciones automáticas configuradas

### ☐ Cambiar Puerto SSH (Opcional)

```bash
# Editar configuración SSH
sudo nano /etc/ssh/sshd_config

# Cambiar línea:
# Port 22
# a
# Port NUEVO_PUERTO

# Reiniciar SSH
sudo systemctl restart sshd

# Actualizar firewall
sudo ufw allow NUEVO_PUERTO/tcp
sudo ufw delete allow 22/tcp
```

- [ ] Puerto SSH cambiado (si se desea)
- [ ] Firewall actualizado

## Documentación

### ☐ Documentar Instalación

Guardar información de:
- [ ] IP del servidor: `___________________________`
- [ ] Usuario SSH: `___________________________`
- [ ] Puertos usados: `___________________________`
- [ ] Credenciales MongoDB: `___________________________`
- [ ] Email SSL: `___________________________`
- [ ] Ubicación de backups: `___________________________`
- [ ] Fecha de instalación: `___________________________`

### ☐ Revisar Documentación

- [ ] Leer [DEPLOYMENT.md](DEPLOYMENT.md) completo
- [ ] Revisar [GPS-PORTS.md](GPS-PORTS.md) para protocolos GPS
- [ ] Familiarizarse con comandos `navtrack`

## Pruebas Finales

### ☐ Prueba de Reinicio

```bash
# Reiniciar servidor
sudo reboot

# Después de reiniciar, verificar que todo inicia automáticamente
navtrack status
```

- [ ] Servidor reiniciado
- [ ] Todos los servicios iniciaron automáticamente
- [ ] Aplicaciones web accesibles
- [ ] GPS listener escuchando

### ☐ Prueba de Certificados SSL

- [ ] Abrir todas las URLs HTTPS en navegador
- [ ] Verificar candado verde (certificado válido)
- [ ] No hay warnings de certificado

### ☐ Prueba de GPS

- [ ] Dispositivo GPS conectado
- [ ] Datos recibidos en logs
- [ ] Posición visible en frontend

### ☐ Prueba de Backup/Restore

```bash
# Crear backup
navtrack backup

# Listar backups
ls -lh /opt/navtrack/backups/

# (Opcional) Probar restauración en ambiente de prueba
# navtrack restore /opt/navtrack/backups/nombre-backup.archive.gz
```

- [ ] Backup creado exitosamente
- [ ] Backup tiene tamaño razonable (>0 bytes)

## Troubleshooting

Si algo falla, ejecutar:

```bash
# Diagnóstico completo
sudo /opt/navtrack/troubleshoot.sh

# Ver logs
navtrack logs

# Verificar salud
navtrack health
```

## Checklist Completado

- [ ] **INSTALACIÓN COMPLETADA EXITOSAMENTE**
- [ ] **TODOS LOS SERVICIOS FUNCIONANDO**
- [ ] **CERTIFICADOS SSL VÁLIDOS**
- [ ] **GPS RECIBIENDO DATOS**
- [ ] **BACKUPS CONFIGURADOS**
- [ ] **MONITOREO CONFIGURADO**
- [ ] **DOCUMENTACIÓN GUARDADA**

---

**Fecha de instalación**: ___________________________

**Instalado por**: ___________________________

**Notas adicionales**:
```
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
```

---

## Comandos de Referencia Rápida

```bash
# Estado
navtrack status
navtrack health

# Logs
navtrack logs
navtrack logs listener

# Reiniciar
navtrack restart

# Backup
navtrack backup

# Monitoreo
sudo /opt/navtrack/monitor-navtrack.sh check

# Diagnóstico
sudo /opt/navtrack/troubleshoot.sh

# Ayuda
navtrack help
```

## Contactos de Soporte

- Servidor: `___________________________`
- Proveedor hosting: `___________________________`
- Soporte técnico: `___________________________`

---

**¡Felicitaciones! NavTrack está instalado y funcionando.**

Para operación diaria, consulte [DEPLOYMENT.md](DEPLOYMENT.md).
