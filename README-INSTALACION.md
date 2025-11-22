# 🚀 NavTrack - Instalación en Linux

Sistema completo de rastreo GPS con backend, frontend, Odoo API y soporte para 60+ protocolos GPS.

## ⚡ Instalación Rápida (Método Recomendado)

```bash
# 1. Clonar repositorio en el servidor Linux
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack

# 2. Ejecutar setup (corrige line endings e instala)
sudo bash setup.sh
```

**Eso es todo!** El script `setup.sh` se encarga de:
- ✅ Corregir terminaciones de línea (Windows → Unix)
- ✅ Dar permisos de ejecución
- ✅ Ejecutar instalación completa

---

## 📋 Pre-requisitos

### Antes de Ejecutar la Instalación

1. **Servidor Linux**
   - Ubuntu 20.04+ o Debian 10+
   - 4GB RAM mínimo
   - 20GB disco disponible
   - Acceso root/sudo

2. **DNS Configurado** (IMPORTANTE)

   Los siguientes dominios deben apuntar a la IP de tu servidor:
   ```
   gps-qa.inversionespereztaveras.com
   gps-api-qa.inversionespereztaveras.com
   gps-odoo-qa.inversionespereztaveras.com
   gps-listener-qa.inversionespereztaveras.com
   ```

   Verificar con:
   ```bash
   host gps-qa.inversionespereztaveras.com
   ```

3. **Puertos Abiertos en Firewall**
   - 22 (SSH)
   - 80 (HTTP - certificados SSL)
   - 443 (HTTPS - apps web)
   - 7002-7100 (TCP - GPS devices)

---

## 🛠 Métodos de Instalación

### Método 1: Setup Automático (Recomendado)

```bash
cd /tmp/navtrack
sudo bash setup.sh
```

Este método detecta y corrige automáticamente problemas de line endings.

### Método 2: Instalación Directa

```bash
cd /tmp/navtrack

# Corregir line endings
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh

# Instalar
sudo ./install-navtrack.sh
```

### Método 3: Una Línea

```bash
git clone <url-repo> /tmp/navtrack && cd /tmp/navtrack && sudo bash setup.sh
```

---

## 🎯 ¿Qué se Instala?

El script instala y configura automáticamente:

| Componente | Descripción | Puerto |
|------------|-------------|--------|
| **Frontend** | Aplicación web React | 3000 → 443 (HTTPS) |
| **Backend API** | API REST .NET | 8080 → 443 (HTTPS) |
| **Odoo API** | Integración Odoo | 8081 → 443 (HTTPS) |
| **GPS Listener** | Recibe datos GPS | 7002-7100 (TCP) |
| **MongoDB** | Base de datos | 27017 (local) |
| **Nginx** | Reverse proxy | 80, 443 |
| **Let's Encrypt** | Certificados SSL | - |
| **Docker** | Contenedores | - |

---

## 🌐 URLs de Acceso

Después de la instalación, accede a:

- **Frontend**: https://gps-qa.inversionespereztaveras.com
- **Backend API**: https://gps-api-qa.inversionespereztaveras.com
- **Odoo API**: https://gps-odoo-qa.inversionespereztaveras.com

Para dispositivos GPS:
- **Servidor**: gps-listener-qa.inversionespereztaveras.com
- **Puerto**: Según protocolo (ver [GPS-PORTS.md](GPS-PORTS.md))

---

## 📱 Configurar Dispositivos GPS

Ver lista completa de protocolos en [GPS-PORTS.md](GPS-PORTS.md).

### Ejemplos de Configuración

**Teltonika (Puerto 7002)**
```
Servidor: gps-listener-qa.inversionespereztaveras.com
Puerto: 7002
Protocolo: TCP
```

**Coban (Puerto 7007)**
```sms
server#gps-listener-qa.inversionespereztaveras.com#7007#
```

**Concox (Puerto 7013)**
```sms
SERVER,1,gps-listener-qa.inversionespereztaveras.com,7013,0#
```

---

## 🔧 Gestión Post-Instalación

### Copiar Script de Gestión

```bash
sudo cp /tmp/navtrack/navtrack-manage.sh /usr/local/bin/navtrack
sudo chmod +x /usr/local/bin/navtrack
```

### Comandos Principales

```bash
# Ver estado
navtrack status

# Ver logs
navtrack logs
navtrack logs listener

# Reiniciar servicios
navtrack restart

# Crear backup
navtrack backup

# Verificar salud
navtrack health

# Actualizar sistema
navtrack update

# Ver todos los comandos
navtrack help
```

---

## 🆘 Solución de Problemas

### Error: "bad interpreter: No such file or directory"

Este error ocurre por terminaciones de línea Windows. **Solución**:

```bash
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh
sudo ./install-navtrack.sh
```

Ver [FIX-WINDOWS-ISSUE.md](FIX-WINDOWS-ISSUE.md) para más detalles.

### Servicios no Inician

```bash
# Diagnóstico completo
sudo bash troubleshoot.sh

# Ver logs detallados
cd /opt/navtrack
docker compose -f docker-compose.prod.yml logs
```

### Certificados SSL Fallan

```bash
# Verificar DNS primero
host gps-qa.inversionespereztaveras.com

# Debe responder con la IP de tu servidor
# Si no, configura DNS y espera propagación (hasta 24h)

# Renovar certificados
sudo certbot renew
```

### GPS No Conecta

```bash
# Ver logs del listener
navtrack logs listener

# Verificar puertos abiertos
sudo netstat -tuln | grep ":700"

# Verificar firewall
sudo ufw status
```

---

## 📚 Documentación Completa

### Guías de Instalación
- **[INICIO-RAPIDO.md](INICIO-RAPIDO.md)** - Inicio rápido
- **[INSTALL-QUICK.md](INSTALL-QUICK.md)** - Instalación paso a paso
- **[INSTALLATION-CHECKLIST.md](INSTALLATION-CHECKLIST.md)** - Checklist completo

### Guías de Operación
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Guía completa de operación
- **[GPS-PORTS.md](GPS-PORTS.md)** - Puertos y protocolos GPS

### Solución de Problemas
- **[FIX-WINDOWS-ISSUE.md](FIX-WINDOWS-ISSUE.md)** - Solución line endings
- **[README-DEPLOYMENT.md](README-DEPLOYMENT.md)** - Documentación general

---

## 🛠 Scripts Incluidos

| Script | Descripción | Uso |
|--------|-------------|-----|
| `setup.sh` | Setup inicial con corrección automática | `sudo bash setup.sh` |
| `install-navtrack.sh` | Instalación principal | `sudo ./install-navtrack.sh` |
| `navtrack-manage.sh` | Gestión diaria del sistema | `navtrack [comando]` |
| `monitor-navtrack.sh` | Monitoreo automático | `sudo ./monitor-navtrack.sh check` |
| `troubleshoot.sh` | Diagnóstico de problemas | `sudo bash troubleshoot.sh` |
| `fix-line-endings.sh` | Corrección line endings | `bash fix-line-endings.sh` |

---

## ⚙️ Configuración Avanzada

### Variables de Entorno

Editar `.env` o las variables en `install-navtrack.sh`:

```bash
DOMAIN_FRONTEND="gps-qa.inversionespereztaveras.com"
DOMAIN_API="gps-api-qa.inversionespereztaveras.com"
DOMAIN_ODOO_API="gps-odoo-qa.inversionespereztaveras.com"
DOMAIN_LISTENER="gps-listener-qa.inversionespereztaveras.com"
EMAIL="admin@inversionespereztaveras.com"
INSTALL_DIR="/opt/navtrack"
```

### Backup Automático

```bash
# Editar crontab
sudo crontab -e

# Agregar backup diario a las 2 AM
0 2 * * * /usr/local/bin/navtrack backup >> /var/log/navtrack-backup.log 2>&1
```

### Monitoreo Automático

```bash
# Editar crontab
sudo crontab -e

# Agregar monitoreo cada 5 minutos
*/5 * * * * /opt/navtrack/monitor-navtrack.sh check >> /var/log/navtrack-monitor.log 2>&1
```

---

## 🔒 Seguridad

### Características de Seguridad Implementadas

- ✅ SSL/TLS con Let's Encrypt
- ✅ Auto-renovación de certificados SSL
- ✅ Firewall UFW configurado
- ✅ Nginx como reverse proxy
- ✅ Headers de seguridad HTTP
- ✅ Contenedores Docker aislados
- ✅ MongoDB solo accesible localmente

### Recomendaciones Post-Instalación

1. **Configurar autenticación en MongoDB**
2. **Instalar fail2ban** para protección SSH
3. **Cambiar puerto SSH** (opcional)
4. **Actualizar sistema regularmente**
5. **Configurar alertas de monitoreo**

Ver [DEPLOYMENT.md](DEPLOYMENT.md) para guía de seguridad completa.

---

## 📊 Monitoreo del Sistema

### Comandos de Monitoreo

```bash
# Estado general
navtrack status
navtrack health

# Uso de recursos
navtrack stats

# Información del sistema
navtrack info

# Logs en tiempo real
navtrack logs -f
```

### Reporte de Salud

```bash
# Generar reporte completo
sudo /opt/navtrack/monitor-navtrack.sh report
```

---

## 🔄 Actualización

### Actualizar NavTrack

```bash
# Método simple
navtrack update

# Método manual
cd /opt/navtrack
git pull
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

---

## 💾 Backup y Restauración

### Crear Backup

```bash
navtrack backup
```

Los backups se guardan en `/opt/navtrack/backups/`

### Restaurar Backup

```bash
navtrack restore /opt/navtrack/backups/navtrack-YYYYMMDD_HHMMSS.archive.gz
```

---

## ❓ FAQ

### ¿Cuánto tiempo toma la instalación?

Entre 10-15 minutos, dependiendo de la velocidad de internet.

### ¿Puedo cambiar los dominios después?

Sí, editando las configuraciones en `/opt/navtrack/docker-compose.prod.yml` y `/etc/nginx/sites-available/`.

### ¿Qué protocolos GPS soporta?

Más de 60 protocolos. Ver lista completa en [GPS-PORTS.md](GPS-PORTS.md).

### ¿Necesito conocimientos de Docker?

No, el script automatiza todo. Pero conocimientos básicos ayudan para troubleshooting.

### ¿Puedo usar IPs en lugar de dominios?

Sí, pero no podrás obtener certificados SSL. Se recomienda usar dominios.

---

## 📞 Soporte y Ayuda

### Diagnóstico Automático

```bash
sudo bash troubleshoot.sh
```

### Documentación

- Instalación: [INSTALL-QUICK.md](INSTALL-QUICK.md)
- Operación: [DEPLOYMENT.md](DEPLOYMENT.md)
- GPS: [GPS-PORTS.md](GPS-PORTS.md)
- Problemas: [FIX-WINDOWS-ISSUE.md](FIX-WINDOWS-ISSUE.md)

### Comandos de Ayuda

```bash
navtrack help
bash troubleshoot.sh help
bash monitor-navtrack.sh help
```

---

## 📝 Checklist de Instalación

- [ ] DNS configurado apuntando al servidor
- [ ] Puertos 22, 80, 443, 7002-7100 abiertos
- [ ] Servidor Linux con 4GB RAM y 20GB disco
- [ ] Repositorio clonado
- [ ] Ejecutado `sudo bash setup.sh`
- [ ] Todos los servicios iniciados
- [ ] Certificados SSL obtenidos
- [ ] Frontend accesible vía HTTPS
- [ ] Script `navtrack` copiado a `/usr/local/bin/`
- [ ] Backup automático configurado (opcional)
- [ ] Monitoreo configurado (opcional)

Ver [INSTALLATION-CHECKLIST.md](INSTALLATION-CHECKLIST.md) para checklist completo.

---

## 🎉 ¡Listo!

Una vez completada la instalación:

1. ✅ Accede al frontend: https://gps-qa.inversionespereztaveras.com
2. ✅ Crea tu cuenta de usuario
3. ✅ Registra tus dispositivos GPS
4. ✅ Configura tus dispositivos GPS con el servidor y puerto
5. ✅ ¡Comienza a rastrear!

---

**Tiempo total estimado**: 15 minutos
**Dificultad**: Fácil (automatizado)
**Última actualización**: 2025-11-21

Para configuración avanzada y operación diaria, consulta [DEPLOYMENT.md](DEPLOYMENT.md).
