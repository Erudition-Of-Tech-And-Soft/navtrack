# NavTrack - Instalación Rápida

## Pre-requisitos

1. **Servidor Linux** (Ubuntu 20.04+ recomendado)
2. **DNS configurado** apuntando a la IP de tu servidor:
   - `gps-qa.inversionespereztaveras.com`
   - `gps-api-qa.inversionespereztaveras.com`
   - `gps-odoo-qa.inversionespereztaveras.com`
   - `gps-listener-qa.inversionespereztaveras.com`

3. **Puertos abiertos** en firewall:
   - 22 (SSH)
   - 80 (HTTP)
   - 443 (HTTPS)
   - 7002-7100 (GPS Listeners)

## Instalación en 3 Pasos

### 1. Clonar repositorio y dar permisos

```bash
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack
chmod +x install-navtrack.sh
chmod +x navtrack-manage.sh
```

### 2. Ejecutar instalación

```bash
sudo ./install-navtrack.sh
```

El script instalará automáticamente:
- Docker & Docker Compose
- Nginx
- Certbot (Let's Encrypt)
- NavTrack (todos los servicios)
- Certificados SSL
- Configuración de firewall

**Importante**: Cuando el script solicite continuar con los certificados SSL, asegúrate de que los DNS estén configurados correctamente.

### 3. Verificar instalación

```bash
# Copiar script de gestión
sudo cp /tmp/navtrack/navtrack-manage.sh /usr/local/bin/navtrack
sudo chmod +x /usr/local/bin/navtrack

# Verificar servicios
navtrack status
navtrack health
```

## Acceso

Después de la instalación, accede a:

- **Frontend**: https://gps-qa.inversionespereztaveras.com
- **API**: https://gps-api-qa.inversionespereztaveras.com
- **Odoo API**: https://gps-odoo-qa.inversionespereztaveras.com

## Configurar GPS Devices

Configura tus dispositivos GPS con:
- **Servidor**: `gps-listener-qa.inversionespereztaveras.com`
- **Puerto**: Depende del protocolo (ver tabla de puertos)
- **Protocolo**: TCP

### Tabla de Puertos GPS Principales

| Puerto | Protocolo |
|--------|-----------|
| 7001 | Meitrack |
| 7002 | Teltonika |
| 7007 | Coban |
| 7008 | Queclink |
| 7010 | Suntech |
| 7013 | Concox |

## Comandos Útiles

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

# Ver información del sistema
navtrack info

# Ver todos los comandos
navtrack help
```

## Troubleshooting

### Los servicios no inician
```bash
navtrack logs
```

### Problemas con SSL
```bash
sudo certbot certificates
sudo certbot renew --dry-run
```

### GPS no conecta
```bash
navtrack logs listener
# Verificar puerto correcto del dispositivo
# Verificar firewall
```

## Próximos Pasos

1. Configurar backup automático (ver [DEPLOYMENT.md](DEPLOYMENT.md))
2. Configurar autenticación en MongoDB
3. Configurar monitoreo
4. Revisar documentación completa en [DEPLOYMENT.md](DEPLOYMENT.md)

## Soporte

Ver documentación completa: [DEPLOYMENT.md](DEPLOYMENT.md)
