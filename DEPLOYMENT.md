# NavTrack - Guía de Despliegue en Producción

Esta guía describe cómo instalar y configurar NavTrack en un servidor Linux de producción.

## Requisitos Previos

### Servidor
- Ubuntu 20.04 LTS o superior (también compatible con Debian)
- Mínimo 4GB RAM
- Mínimo 20GB de espacio en disco
- Acceso root o sudo
- Conexión a Internet

### DNS
Antes de ejecutar el script de instalación, configure los siguientes registros DNS tipo A apuntando a la IP de su servidor:

- `gps-qa.inversionespereztaveras.com` → IP del servidor (Frontend)
- `gps-api-qa.inversionespereztaveras.com` → IP del servidor (Backend API)
- `gps-odoo-qa.inversionespereztaveras.com` → IP del servidor (Odoo API)
- `gps-listener-qa.inversionespereztaveras.com` → IP del servidor (GPS Listener)

### Puertos Requeridos
Asegúrese de que los siguientes puertos estén abiertos en su firewall:

- **22**: SSH (administración)
- **80**: HTTP (redirección a HTTPS)
- **443**: HTTPS (aplicaciones web)
- **7002-7100**: TCP (GPS Listeners - uno por cada protocolo GPS)

## Instalación

### 1. Preparar el Servidor

```bash
# Actualizar el sistema
sudo apt update && sudo apt upgrade -y

# Instalar git si no está instalado
sudo apt install -y git
```

### 2. Clonar el Repositorio

```bash
# Clonar el repositorio en el servidor
git clone <url-del-repositorio> /tmp/navtrack
cd /tmp/navtrack
```

### 3. Configurar Variables (Opcional)

Edite el archivo `install-navtrack.sh` si desea cambiar las configuraciones predeterminadas:

```bash
nano install-navtrack.sh
```

Variables importantes:
```bash
DOMAIN_FRONTEND="gps-qa.inversionespereztaveras.com"
DOMAIN_API="gps-api-qa.inversionespereztaveras.com"
DOMAIN_ODOO_API="gps-odoo-qa.inversionespereztaveras.com"
DOMAIN_LISTENER="gps-listener-qa.inversionespereztaveras.com"
EMAIL="admin@inversionespereztaveras.com"  # Para certificados SSL
MONGO_DATABASE="navtrack"
INSTALL_DIR="/opt/navtrack"
LISTENER_PORT_START=7002
LISTENER_PORT_END=7100
```

### 4. Ejecutar el Script de Instalación

```bash
# Dar permisos de ejecución
chmod +x install-navtrack.sh

# Ejecutar el script como root
sudo ./install-navtrack.sh
```

El script realizará automáticamente:
1. ✓ Instalación de Docker y Docker Compose
2. ✓ Instalación de Nginx
3. ✓ Instalación de Certbot (Let's Encrypt)
4. ✓ Copia de archivos del proyecto a `/opt/navtrack`
5. ✓ Creación de configuración de Docker Compose para producción
6. ✓ Configuración de Nginx con reverse proxy para cada servicio
7. ✓ Configuración del firewall (UFW)
8. ✓ Construcción de imágenes Docker
9. ✓ Inicio de servicios
10. ✓ Obtención de certificados SSL de Let's Encrypt
11. ✓ Configuración de servicio systemd para auto-inicio

**Nota**: Cuando el script solicite continuar con la generación de certificados SSL, presione Enter. Asegúrese de que los registros DNS estén configurados correctamente antes de este paso.

## Arquitectura de Despliegue

```
Internet
    │
    ├─── Port 443 (HTTPS) ──► Nginx ──► Frontend (Port 3000)
    │                           │
    ├─── Port 443 (HTTPS) ──────┼──► Backend API (Port 8080)
    │                           │
    ├─── Port 443 (HTTPS) ──────┼──► Odoo API (Port 8081)
    │                           │
    └─── Ports 7002-7100 (TCP) ─┴──► GPS Listener
                                      │
                                      └──► MongoDB (Port 27017 - interno)
```

### Servicios Desplegados

| Servicio | Contenedor | Puerto Interno | Puerto Externo | URL |
|----------|------------|----------------|----------------|-----|
| Frontend Web | navtrack-frontend | 3000 | 443 (HTTPS) | https://gps-qa.inversionespereztaveras.com |
| Backend API | navtrack-api | 8080 | 443 (HTTPS) | https://gps-api-qa.inversionespereztaveras.com |
| Odoo API | navtrack-odoo-api | 8081 | 443 (HTTPS) | https://gps-odoo-qa.inversionespereztaveras.com |
| GPS Listener | navtrack-listener | 7002-7100 | 7002-7100 (TCP) | gps-listener-qa.inversionespereztaveras.com |
| MongoDB | navtrack-mongodb | 27017 | Local only | N/A |

## Gestión de Servicios

### Ver Estado de Servicios

```bash
# Ver estado del servicio systemd
sudo systemctl status navtrack

# Ver contenedores en ejecución
cd /opt/navtrack
sudo docker compose -f docker-compose.prod.yml ps

# Ver logs de todos los servicios
sudo docker compose -f docker-compose.prod.yml logs -f

# Ver logs de un servicio específico
sudo docker compose -f docker-compose.prod.yml logs -f frontend
sudo docker compose -f docker-compose.prod.yml logs -f api
sudo docker compose -f docker-compose.prod.yml logs -f odoo-api
sudo docker compose -f docker-compose.prod.yml logs -f listener
sudo docker compose -f docker-compose.prod.yml logs -f database
```

### Iniciar/Detener Servicios

```bash
cd /opt/navtrack

# Detener todos los servicios
sudo docker compose -f docker-compose.prod.yml down

# Iniciar todos los servicios
sudo docker compose -f docker-compose.prod.yml up -d

# Reiniciar todos los servicios
sudo docker compose -f docker-compose.prod.yml restart

# Reiniciar un servicio específico
sudo docker compose -f docker-compose.prod.yml restart api
```

### Actualizar la Aplicación

```bash
cd /opt/navtrack

# Obtener últimos cambios
git pull

# Reconstruir imágenes
sudo docker compose -f docker-compose.prod.yml build

# Reiniciar servicios con las nuevas imágenes
sudo docker compose -f docker-compose.prod.yml up -d
```

## Configuración de GPS Devices

### Puertos del Listener

El servicio Navtrack.Listener escucha en múltiples puertos (7002-7100), uno para cada protocolo GPS soportado. Los principales son:

| Puerto | Protocolo GPS |
|--------|---------------|
| 7001 | Meitrack |
| 7002 | Teltonika |
| 7003 | Meiligao |
| 7004 | Megastek |
| 7005 | Totem |
| 7006 | Tzone |
| 7007 | Coban |
| 7008 | Queclink |
| 7009 | Fifotrack |
| 7010 | Suntech |
| ... | ... |

### Configurar Dispositivo GPS

Para configurar un dispositivo GPS para enviar datos a NavTrack:

1. **Servidor**: `gps-listener-qa.inversionespereztaveras.com` o la IP del servidor
2. **Puerto**: Depende del protocolo del dispositivo (ver tabla arriba)
3. **Protocolo**: TCP
4. **APN**: Configurar según su proveedor de SIM

Ejemplo de configuración para dispositivo Teltonika:
```
Servidor: gps-listener-qa.inversionespereztaveras.com
Puerto: 7002
Protocolo: TCP
```

## Gestión de Certificados SSL

Los certificados SSL son proporcionados por Let's Encrypt y se renuevan automáticamente.

### Verificar Certificados

```bash
# Ver certificados instalados
sudo certbot certificates

# Probar renovación (sin aplicar)
sudo certbot renew --dry-run

# Forzar renovación
sudo certbot renew
```

### Renovación Automática

La renovación automática está configurada mediante systemd timer:

```bash
# Ver estado del timer de renovación
sudo systemctl status certbot.timer

# Ver logs de renovaciones
sudo journalctl -u certbot.renew.service
```

## Base de Datos

### Acceso a MongoDB

```bash
# Conectar a MongoDB
cd /opt/navtrack
sudo docker compose -f docker-compose.prod.yml exec database mongosh navtrack

# Ver bases de datos
show dbs

# Ver colecciones
use navtrack
show collections
```

### Backup de Base de Datos

```bash
# Crear backup
cd /opt/navtrack
sudo docker compose -f docker-compose.prod.yml exec database mongodump --out /data/backup --db navtrack

# Copiar backup al host
sudo docker cp navtrack-mongodb:/data/backup ./mongodb-backup-$(date +%Y%m%d)

# Restaurar backup
sudo docker compose -f docker-compose.prod.yml exec database mongorestore --db navtrack /data/backup/navtrack
```

### Backup Automatizado (Opcional)

Crear script de backup en `/opt/navtrack/backup-mongodb.sh`:

```bash
#!/bin/bash
BACKUP_DIR="/opt/navtrack/backups"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

cd /opt/navtrack
docker compose -f docker-compose.prod.yml exec -T database mongodump --archive --db navtrack | gzip > $BACKUP_DIR/navtrack-$DATE.archive.gz

# Mantener solo los últimos 7 días
find $BACKUP_DIR -name "navtrack-*.archive.gz" -mtime +7 -delete

echo "Backup completado: navtrack-$DATE.archive.gz"
```

Configurar cron para backup diario:
```bash
chmod +x /opt/navtrack/backup-mongodb.sh
sudo crontab -e

# Agregar línea para backup diario a las 2 AM
0 2 * * * /opt/navtrack/backup-mongodb.sh >> /var/log/navtrack-backup.log 2>&1
```

## Monitoreo y Logs

### Ver Logs de Nginx

```bash
# Logs de acceso
sudo tail -f /var/log/nginx/access.log

# Logs de error
sudo tail -f /var/log/nginx/error.log

# Logs específicos de un dominio (si existen)
sudo tail -f /var/log/nginx/navtrack-*.log
```

### Ver Logs de Docker

```bash
cd /opt/navtrack

# Logs en tiempo real de todos los servicios
sudo docker compose -f docker-compose.prod.yml logs -f

# Logs de las últimas 100 líneas
sudo docker compose -f docker-compose.prod.yml logs --tail=100

# Logs de un servicio específico
sudo docker compose -f docker-compose.prod.yml logs -f listener
```

### Monitoreo de Recursos

```bash
# Ver uso de recursos de contenedores
sudo docker stats

# Ver espacio en disco
df -h

# Ver uso de volúmenes Docker
sudo docker system df -v
```

## Troubleshooting

### Los servicios no inician

```bash
# Ver logs de Docker Compose
cd /opt/navtrack
sudo docker compose -f docker-compose.prod.yml logs

# Ver estado de contenedores
sudo docker compose -f docker-compose.prod.yml ps

# Verificar configuración
sudo docker compose -f docker-compose.prod.yml config
```

### Error en certificados SSL

```bash
# Verificar configuración de Nginx
sudo nginx -t

# Renovar certificados manualmente
sudo certbot renew --force-renewal

# Recargar Nginx
sudo systemctl reload nginx
```

### GPS no envía datos

1. Verificar que el puerto correcto esté abierto en el firewall
2. Verificar logs del listener:
   ```bash
   cd /opt/navtrack
   sudo docker compose -f docker-compose.prod.yml logs -f listener
   ```
3. Verificar que el dispositivo GPS tenga el puerto correcto configurado
4. Verificar conectividad de red del dispositivo GPS

### Base de datos llena

```bash
# Ver espacio usado
sudo docker exec navtrack-mongodb df -h

# Limpiar logs antiguos de MongoDB
cd /opt/navtrack
sudo docker compose -f docker-compose.prod.yml exec database mongosh navtrack --eval "db.adminCommand({logRotate:1})"

# Compactar base de datos
sudo docker compose -f docker-compose.prod.yml exec database mongosh navtrack --eval "db.runCommand({compact: 'collection_name'})"
```

## Seguridad

### Mejores Prácticas Implementadas

1. ✓ SSL/TLS mediante Let's Encrypt
2. ✓ Firewall configurado (UFW)
3. ✓ Nginx como reverse proxy
4. ✓ Contenedores aislados en red Docker
5. ✓ MongoDB solo accesible localmente
6. ✓ Headers de seguridad HTTP configurados
7. ✓ Auto-renovación de certificados SSL

### Recomendaciones Adicionales

1. **Configurar autenticación en MongoDB**:
   ```bash
   # Crear usuario admin en MongoDB
   cd /opt/navtrack
   sudo docker compose -f docker-compose.prod.yml exec database mongosh navtrack

   # En el shell de MongoDB:
   use admin
   db.createUser({
     user: "admin",
     pwd: "strong_password_here",
     roles: ["root"]
   })
   ```

2. **Configurar fail2ban** para proteger SSH:
   ```bash
   sudo apt install fail2ban
   sudo systemctl enable fail2ban
   sudo systemctl start fail2ban
   ```

3. **Actualizar regularmente el sistema**:
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

## Soporte

Para problemas o preguntas:
- Revisar logs de servicios
- Consultar documentación de NavTrack
- Verificar estado de servicios y recursos del servidor

## Desinstalación

Si necesita desinstalar NavTrack:

```bash
# Detener y eliminar contenedores
cd /opt/navtrack
sudo docker compose -f docker-compose.prod.yml down -v

# Desactivar servicio systemd
sudo systemctl disable navtrack
sudo rm /etc/systemd/system/navtrack.service
sudo systemctl daemon-reload

# Eliminar configuraciones de Nginx
sudo rm /etc/nginx/sites-enabled/navtrack-*
sudo rm /etc/nginx/sites-available/navtrack-*
sudo systemctl reload nginx

# Revocar certificados SSL (opcional)
sudo certbot revoke --cert-name gps-qa.inversionespereztaveras.com
sudo certbot revoke --cert-name gps-api-qa.inversionespereztaveras.com
sudo certbot revoke --cert-name gps-odoo-qa.inversionespereztaveras.com
sudo certbot revoke --cert-name gps-listener-qa.inversionespereztaveras.com

# Eliminar archivos de instalación
sudo rm -rf /opt/navtrack

# Eliminar reglas de firewall
sudo ufw delete allow 7002:7100/tcp
```
