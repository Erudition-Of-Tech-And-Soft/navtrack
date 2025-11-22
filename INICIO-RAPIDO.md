# NavTrack - Inicio Rápido

## ⚠️ IMPORTANTE: Solucionar Error de Line Endings

Si estás clonando este repositorio desde Windows y lo ejecutarás en Linux, **debes ejecutar esto primero**:

```bash
# Después de clonar el repositorio en Linux:
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh
```

Si ves el error `-bash: /bin/bash^M: bad interpreter`, consulta [FIX-WINDOWS-ISSUE.md](FIX-WINDOWS-ISSUE.md).

## 🚀 Instalación en 60 Segundos

### 1️⃣ Preparar

```bash
# En tu servidor Linux
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack

# Corregir line endings (si es necesario)
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh
```

### 2️⃣ Instalar

```bash
sudo ./install-navtrack.sh
```

### 3️⃣ Verificar

```bash
# Copiar script de gestión
sudo cp navtrack-manage.sh /usr/local/bin/navtrack
sudo chmod +x /usr/local/bin/navtrack

# Verificar estado
navtrack status
navtrack health
```

## ✅ Pre-requisitos

Antes de instalar, asegúrate de tener:

- [ ] **DNS configurado** (todos los dominios apuntando a tu IP):
  - gps-qa.inversionespereztaveras.com
  - gps-api-qa.inversionespereztaveras.com
  - gps-odoo-qa.inversionespereztaveras.com
  - gps-listener-qa.inversionespereztaveras.com

- [ ] **Puertos abiertos** en firewall:
  - 22 (SSH)
  - 80 (HTTP)
  - 443 (HTTPS)
  - 7002-7100 (GPS)

- [ ] **Servidor Linux**:
  - Ubuntu 20.04+ o Debian 10+
  - 4GB RAM mínimo
  - 20GB disco disponible

## 📍 URLs de Acceso

Después de instalar, accede a:

- **Frontend**: https://gps-qa.inversionespereztaveras.com
- **Backend API**: https://gps-api-qa.inversionespereztaveras.com
- **Odoo API**: https://gps-odoo-qa.inversionespereztaveras.com

Configura tus dispositivos GPS:
- **Servidor**: gps-listener-qa.inversionespereztaveras.com
- **Puerto**: Ver [GPS-PORTS.md](GPS-PORTS.md)

## 🎯 Comandos Esenciales

```bash
# Ver estado
navtrack status

# Ver logs
navtrack logs
navtrack logs listener

# Reiniciar
navtrack restart

# Crear backup
navtrack backup

# Verificar salud
navtrack health

# Ver ayuda
navtrack help
```

## 🆘 Problemas Comunes

### Error "bad interpreter"
```bash
# Solución
dos2unix *.sh
chmod +x *.sh
```
Ver [FIX-WINDOWS-ISSUE.md](FIX-WINDOWS-ISSUE.md)

### Servicios no inician
```bash
# Diagnóstico
sudo ./troubleshoot.sh

# Ver logs
navtrack logs
```

### Certificados SSL fallan
```bash
# Verificar DNS primero
host gps-qa.inversionespereztaveras.com

# Renovar manualmente
sudo certbot renew
```

## 📚 Documentación Completa

- **[INSTALL-QUICK.md](INSTALL-QUICK.md)** - Instalación detallada
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Guía completa de operación
- **[GPS-PORTS.md](GPS-PORTS.md)** - Puertos y protocolos GPS
- **[INSTALLATION-CHECKLIST.md](INSTALLATION-CHECKLIST.md)** - Checklist completo
- **[FIX-WINDOWS-ISSUE.md](FIX-WINDOWS-ISSUE.md)** - Solución line endings

## 🔧 Scripts Disponibles

| Script | Propósito |
|--------|-----------|
| `install-navtrack.sh` | Instalación inicial |
| `navtrack-manage.sh` | Gestión diaria |
| `monitor-navtrack.sh` | Monitoreo automático |
| `troubleshoot.sh` | Diagnóstico de problemas |
| `fix-line-endings.sh` | Corregir line endings |

## 📞 ¿Necesitas Ayuda?

1. Ejecuta diagnóstico: `sudo ./troubleshoot.sh`
2. Revisa logs: `navtrack logs`
3. Consulta documentación: [DEPLOYMENT.md](DEPLOYMENT.md)

---

**Tiempo estimado de instalación**: 10-15 minutos

**Siguiente paso**: Leer [DEPLOYMENT.md](DEPLOYMENT.md) para configuración avanzada.
