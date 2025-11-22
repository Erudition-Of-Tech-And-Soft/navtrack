# NavTrack - Sistema de Rastreo GPS

Sistema completo de rastreo GPS con backend, frontend, integración con Odoo y soporte para múltiples protocolos GPS.

## 📋 Índice

- [Descripción](#descripción)
- [Arquitectura](#arquitectura)
- [Instalación Rápida](#instalación-rápida)
- [Documentación Completa](#documentación-completa)
- [Scripts Disponibles](#scripts-disponibles)
- [URLs de Acceso](#urls-de-acceso)
- [Soporte](#soporte)

## 🎯 Descripción

NavTrack es un sistema completo de rastreo GPS que incluye:

- **Frontend Web**: Interfaz de usuario para visualización y gestión
- **Backend API**: API REST para procesamiento de datos
- **Odoo API**: Integración con Odoo ERP
- **GPS Listener**: Servicio que recibe datos de dispositivos GPS (soporta 60+ protocolos)
- **MongoDB**: Base de datos para almacenamiento
- **Nginx**: Reverse proxy con SSL/TLS
- **Let's Encrypt**: Certificados SSL automáticos

## 🏗 Arquitectura

```
Internet
    │
    ├─── HTTPS (443) ──► Nginx ──┬──► Frontend (React)
    │                            │
    │                            ├──► Backend API (.NET)
    │                            │
    │                            ├──► Odoo API (.NET)
    │                            │
    └─── TCP (7002-7100) ────────┴──► GPS Listener (.NET)
                                      │
                                      └──► MongoDB
```

### Componentes

| Componente | Tecnología | Puerto | URL |
|------------|-----------|--------|-----|
| Frontend | React/Node.js | 3000 | https://gps-qa.inversionespereztaveras.com |
| Backend API | .NET 9 | 8080 | https://gps-api-qa.inversionespereztaveras.com |
| Odoo API | .NET 9 | 8081 | https://gps-odoo-qa.inversionespereztaveras.com |
| GPS Listener | .NET 9 | 7002-7100 | gps-listener-qa.inversionespereztaveras.com |
| MongoDB | MongoDB 7 | 27017 | localhost only |
| Nginx | Nginx | 80, 443 | - |

## ⚡ Instalación Rápida

### Pre-requisitos

1. Servidor Linux (Ubuntu 20.04+ recomendado)
2. Acceso root/sudo
3. DNS configurado apuntando a la IP del servidor
4. Puertos 22, 80, 443, 7002-7100 abiertos

### Instalación en 3 Pasos

```bash
# 1. Clonar repositorio
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack

# 2. Dar permisos
chmod +x install-navtrack.sh
chmod +x navtrack-manage.sh
chmod +x monitor-navtrack.sh
chmod +x troubleshoot.sh

# 3. Ejecutar instalación
sudo ./install-navtrack.sh
```

El script instalará automáticamente:
- ✅ Docker & Docker Compose
- ✅ Nginx
- ✅ Certbot (Let's Encrypt)
- ✅ NavTrack (todos los servicios)
- ✅ Certificados SSL
- ✅ Firewall (UFW)
- ✅ Auto-start systemd service

Ver [INSTALL-QUICK.md](INSTALL-QUICK.md) para más detalles.

## 📚 Documentación Completa

### Guías Principales

- **[INSTALL-QUICK.md](INSTALL-QUICK.md)**: Instalación rápida paso a paso
- **[DEPLOYMENT.md](DEPLOYMENT.md)**: Guía completa de despliegue y operación
- **[GPS-PORTS.md](GPS-PORTS.md)**: Tabla de puertos GPS y configuración de dispositivos

### Configuraciones

- **[.env.example](.env.example)**: Variables de entorno
- **[nginx-security.conf](nginx-security.conf)**: Configuración de seguridad Nginx

## 🛠 Scripts Disponibles

### 1. `install-navtrack.sh` - Instalación Inicial

Script principal de instalación.

```bash
sudo ./install-navtrack.sh
```

**Funciones**:
- Instala dependencias (Docker, Nginx, Certbot)
- Configura servicios
- Obtiene certificados SSL
- Configura firewall
- Inicia servicios

### 2. `navtrack-manage.sh` - Gestión del Sistema

Script para operaciones diarias.

```bash
# Copiar a /usr/local/bin para uso global
sudo cp navtrack-manage.sh /usr/local/bin/navtrack
sudo chmod +x /usr/local/bin/navtrack

# Uso
navtrack [comando]
```

**Comandos disponibles**:

```bash
# Gestión de servicios
navtrack start           # Iniciar servicios
navtrack stop            # Detener servicios
navtrack restart         # Reiniciar servicios
navtrack status          # Ver estado
navtrack logs [servicio] # Ver logs

# Actualización
navtrack update          # Actualizar a última versión
navtrack rebuild         # Reconstruir imágenes

# Base de datos
navtrack backup          # Crear backup
navtrack restore [file]  # Restaurar backup
navtrack db-shell        # Acceder a MongoDB shell

# Monitoreo
navtrack stats           # Ver uso de recursos
navtrack health          # Verificar salud del sistema

# Mantenimiento
navtrack clean           # Limpiar recursos Docker
navtrack ssl-renew       # Renovar certificados SSL

# Información
navtrack info            # Ver información del sistema
navtrack help            # Ayuda
```

### 3. `monitor-navtrack.sh` - Monitoreo Automático

Script para monitoreo continuo y alertas.

```bash
sudo chmod +x monitor-navtrack.sh

# Ejecutar verificación manual
sudo ./monitor-navtrack.sh check

# Generar reporte
sudo ./monitor-navtrack.sh report
```

**Comandos**:
- `check` - Ejecutar verificaciones de salud
- `report` - Generar reporte detallado
- `containers` - Verificar contenedores
- `resources` - Verificar recursos
- `ssl` - Verificar certificados SSL
- `mongodb` - Verificar MongoDB

**Configurar monitoreo automático**:

```bash
# Editar crontab
sudo crontab -e

# Agregar líneas:
# Verificación cada 5 minutos
*/5 * * * * /opt/navtrack/monitor-navtrack.sh check >> /var/log/navtrack-monitor.log 2>&1

# Reporte diario a las 8 AM
0 8 * * * /opt/navtrack/monitor-navtrack.sh report | mail -s "NavTrack Daily Report" admin@example.com
```

### 4. `troubleshoot.sh` - Diagnóstico de Problemas

Script para diagnosticar problemas.

```bash
sudo chmod +x troubleshoot.sh

# Diagnóstico completo
sudo ./troubleshoot.sh

# Diagnósticos específicos
sudo ./troubleshoot.sh dns
sudo ./troubleshoot.sh containers
sudo ./troubleshoot.sh nginx
sudo ./troubleshoot.sh ssl
sudo ./troubleshoot.sh database
sudo ./troubleshoot.sh logs
```

**Verifica**:
- Pre-requisitos (Docker, Nginx, etc.)
- DNS
- Puertos
- Firewall
- Contenedores Docker
- Configuración Nginx
- Certificados SSL
- Base de datos MongoDB
- Endpoints API
- Espacio en disco
- Uso de memoria
- Errores en logs

## 🌐 URLs de Acceso

### Producción

- **Frontend**: https://gps-qa.inversionespereztaveras.com
- **Backend API**: https://gps-api-qa.inversionespereztaveras.com
- **Odoo API**: https://gps-odoo-qa.inversionespereztaveras.com
- **GPS Listener**: gps-listener-qa.inversionespereztaveras.com (puertos 7002-7100)

### Acceso Interno (desde el servidor)

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:8080
- **Odoo API**: http://localhost:8081
- **MongoDB**: mongodb://localhost:27017

## 📡 Configuración de Dispositivos GPS

Ver [GPS-PORTS.md](GPS-PORTS.md) para la lista completa de puertos y protocolos soportados.

### Ejemplo de Configuración (Teltonika)

```
Servidor: gps-listener-qa.inversionespereztaveras.com
Puerto: 7002
Protocolo: TCP
```

### Protocolos Más Comunes

| Protocolo | Puerto | Dispositivos |
|-----------|--------|--------------|
| Teltonika | 7002 | FM1100, FMB120, FMB130, etc. |
| Coban | 7007 | TK102, TK103, GPS303, etc. |
| Concox | 7013 | GT06, GT06N, GK310, etc. |
| Queclink | 7008 | GV55, GV65, GL300, etc. |
| Suntech | 7010 | ST310, ST340, ST4505, etc. |

## 🔧 Comandos Útiles

### Docker Compose

```bash
cd /opt/navtrack

# Ver estado
docker compose -f docker-compose.prod.yml ps

# Ver logs
docker compose -f docker-compose.prod.yml logs -f

# Reiniciar servicio específico
docker compose -f docker-compose.prod.yml restart api

# Reconstruir imágenes
docker compose -f docker-compose.prod.yml build

# Detener todo
docker compose -f docker-compose.prod.yml down
```

### MongoDB

```bash
# Acceder a shell
docker exec -it navtrack-mongodb mongosh navtrack

# Backup
docker exec navtrack-mongodb mongodump --out /data/backup --db navtrack

# Restaurar
docker exec navtrack-mongodb mongorestore --db navtrack /data/backup/navtrack
```

### Nginx

```bash
# Verificar configuración
sudo nginx -t

# Recargar
sudo systemctl reload nginx

# Ver logs
sudo tail -f /var/log/nginx/error.log
```

### SSL/Certbot

```bash
# Ver certificados
sudo certbot certificates

# Renovar
sudo certbot renew

# Test de renovación
sudo certbot renew --dry-run
```

## 📊 Monitoreo

### Ver Estado de Servicios

```bash
# Usando script de gestión
navtrack status
navtrack health

# Docker
docker ps
docker stats

# Systemd
systemctl status navtrack
```

### Ver Logs

```bash
# Todos los servicios
navtrack logs

# Servicio específico
navtrack logs listener
navtrack logs api

# Logs de sistema
journalctl -u navtrack -f
```

## 🔒 Seguridad

### Características Implementadas

- ✅ SSL/TLS con Let's Encrypt
- ✅ Auto-renovación de certificados
- ✅ Firewall configurado (UFW)
- ✅ Nginx como reverse proxy
- ✅ Headers de seguridad HTTP
- ✅ Contenedores aislados
- ✅ MongoDB solo accesible localmente

### Recomendaciones Adicionales

1. **Configurar autenticación en MongoDB**
2. **Instalar fail2ban** para protección SSH
3. **Actualizar regularmente** el sistema
4. **Configurar backups automáticos**
5. **Revisar logs periódicamente**

## 🔄 Actualización

### Actualizar NavTrack

```bash
# Usando script de gestión
navtrack update

# Manual
cd /opt/navtrack
git pull
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

### Actualizar Sistema

```bash
sudo apt update
sudo apt upgrade -y
```

## 💾 Backup y Restauración

### Crear Backup

```bash
# Automático con script
navtrack backup

# Manual
cd /opt/navtrack
docker compose -f docker-compose.prod.yml exec -T database \
  mongodump --archive --db navtrack | \
  gzip > backup-$(date +%Y%m%d).archive.gz
```

### Restaurar Backup

```bash
# Con script
navtrack restore /path/to/backup.archive.gz

# Manual
gunzip < backup.archive.gz | \
  docker compose -f docker-compose.prod.yml exec -T database \
  mongorestore --archive --db navtrack --drop
```

### Backup Automático

```bash
# Editar crontab
sudo crontab -e

# Agregar backup diario a las 2 AM
0 2 * * * /usr/local/bin/navtrack backup >> /var/log/navtrack-backup.log 2>&1
```

## 🆘 Troubleshooting

### Servicios no inician

```bash
# Ver logs
navtrack logs

# Diagnóstico completo
sudo ./troubleshoot.sh
```

### Problemas con SSL

```bash
# Verificar certificados
sudo certbot certificates

# Renovar manualmente
sudo certbot renew --force-renewal
```

### GPS no conecta

```bash
# Ver logs del listener
navtrack logs listener

# Verificar puerto correcto
# Ver GPS-PORTS.md

# Verificar firewall
sudo ufw status
```

### Base de datos llena

```bash
# Ver espacio
navtrack stats

# Limpiar logs antiguos
navtrack clean

# Crear backup y limpiar
navtrack backup
```

## 📞 Soporte

### Recursos

- **Instalación Rápida**: [INSTALL-QUICK.md](INSTALL-QUICK.md)
- **Guía Completa**: [DEPLOYMENT.md](DEPLOYMENT.md)
- **Puertos GPS**: [GPS-PORTS.md](GPS-PORTS.md)

### Comandos de Ayuda

```bash
navtrack help              # Ayuda del script de gestión
./monitor-navtrack.sh help # Ayuda del monitor
./troubleshoot.sh help     # Ayuda del troubleshooter
```

## 📝 Notas Importantes

1. **DNS**: Asegúrese de que los dominios apunten al servidor antes de instalar
2. **Puertos**: Los puertos 7002-7100 deben estar abiertos para GPS
3. **SSL**: Los certificados se renuevan automáticamente cada 60 días
4. **Backups**: Configure backups automáticos después de la instalación
5. **Monitoreo**: Configure monitoreo automático para producción

## 📄 Licencia

Ver archivo LICENSE del proyecto NavTrack.

---

**Última actualización**: 2025-11-21

Para más información, consulte la documentación completa en [DEPLOYMENT.md](DEPLOYMENT.md).
