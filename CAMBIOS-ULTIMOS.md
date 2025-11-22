# Cambios Recientes en el Sistema de Instalación

## Versión Actual - 2025-11-21

### ✅ Problemas Corregidos

1. **Docker Build Context Error** ✅ CORREGIDO
   - **Problema**: Los contextos de construcción en docker-compose estaban incorrectos
   - **Error**: `MSBUILD : error MSB1009: Project file does not exist`
   - **Solución**: Todos los servicios ahora usan `context: .` (raíz del proyecto)
   - **Archivo modificado**: `install-navtrack.sh` (función `create_docker_compose`)

2. **Frontend Dockerfile Context** ✅ CORREGIDO
   - **Problema**: Dockerfile del frontend no encontraba archivos cuando el contexto es raíz
   - **Error**: `"/run_web.sh": not found`
   - **Solución**: Actualizado Dockerfile para copiar desde `frontend/` explícitamente
   - **Archivo modificado**: `frontend/Dockerfile`
   - **Cambios realizados**:
     ```dockerfile
     COPY frontend/package*.json ./
     COPY frontend/web ./web
     COPY frontend/shared ./shared
     COPY frontend/run_web.sh /run_web.sh
     ```

3. **Line Endings Windows → Linux** ✅ DOCUMENTADO
   - **Problema**: Scripts tienen CRLF (Windows) en lugar de LF (Unix)
   - **Error**: `-bash: /bin/bash^M: bad interpreter`
   - **Solución**: Ejecutar `dos2unix *.sh` antes de los scripts
   - **Prevención**: Archivo `.gitattributes` creado para forzar LF

### 📝 Estructura de Docker Compose Corregida

```yaml
services:
  frontend:
    build:
      context: .                          # ← CORRECTO (raíz)
      dockerfile: frontend/Dockerfile

  api:
    build:
      context: .                          # ← CORRECTO (raíz)
      dockerfile: backend/Navtrack.Api/Dockerfile

  odoo-api:
    build:
      context: .                          # ← CORRECTO (raíz)
      dockerfile: Odoo.Navtrac.Api/Dockerfile

  listener:
    build:
      context: .                          # ← CORRECTO (raíz)
      dockerfile: backend/Navtrack.Listener/Dockerfile
```

**¿Por qué funciona ahora?**
- El script copia TODO el proyecto a `/opt/navtrack`
- Todos los Dockerfiles esperan estar en la raíz del proyecto
- Con `context: .`, Docker puede encontrar todas las rutas (`backend/...`, `frontend/`, etc.)

### 🚀 Instrucciones de Instalación Actualizadas

#### Método Correcto (Actualizado):

```bash
# 1. Clonar
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack

# 2. Corregir line endings (OBLIGATORIO)
sudo apt-get update
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh

# 3. Instalar
sudo ./install-navtrack.sh
```

### 📋 Qué Hace el Script de Instalación

1. **Copia archivos**: De `/tmp/navtrack` a `/opt/navtrack`
   - Copia `backend/` completo
   - Copia `frontend/` completo
   - Copia `Odoo.Navtrac.Api/` completo

2. **Crea docker-compose.prod.yml** en `/opt/navtrack`
   - Todos los servicios con `context: .` (apunta a `/opt/navtrack`)
   - Los Dockerfiles funcionan porque tienen acceso a toda la estructura

3. **Construye imágenes**: Desde `/opt/navtrack`
   ```bash
   cd /opt/navtrack
   docker compose -f docker-compose.prod.yml build
   ```

### 🔍 Verificación Post-Instalación

```bash
# Verificar estructura de archivos
ls -la /opt/navtrack/
# Debería mostrar: backend/, frontend/, Odoo.Navtrac.Api/, docker-compose.prod.yml

# Verificar que los contenedores se construyeron
docker images | grep navtrack

# Ver logs si hay errores
cd /opt/navtrack
docker compose -f docker-compose.prod.yml logs
```

### 📚 Archivos de Documentación Principales

- **`LEEME-PRIMERO.txt`** - Leer primero (instrucciones básicas)
- **`INSTALACION.md`** - Guía completa de instalación
- **`FIX-WINDOWS-ISSUE.md`** - Solución al problema de line endings
- **`DEPLOYMENT.md`** - Guía de operación y gestión
- **`GPS-PORTS.md`** - Puertos GPS y configuración de dispositivos

### ⚠️ Problemas Conocidos y Soluciones

#### 1. Error "bad interpreter"
**Solución**:
```bash
dos2unix *.sh && chmod +x *.sh
```

#### 2. Error "Project file does not exist"
**Causa**: Contexto de Docker incorrecto (YA CORREGIDO en install-navtrack.sh)

**Verificar que tu versión está actualizada**:
```bash
grep "context: \." install-navtrack.sh
```
Debe mostrar `context: .` para todos los servicios.

#### 3. DNS no resuelve
**Solución**:
```bash
# Verificar DNS primero
host gps-qa.inversionespereztaveras.com

# Si no resuelve, configurar DNS y esperar propagación
# Luego continuar con certificados SSL
```

#### 4. Puertos no están abiertos
**Solución**:
```bash
# Verificar firewall
sudo ufw status

# Abrir puertos necesarios
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 7002:7100/tcp
```

### 🎯 Próximos Pasos Después de Instalar

1. **Copiar script de gestión**:
   ```bash
   sudo cp /tmp/navtrack/navtrack-manage.sh /usr/local/bin/navtrack
   sudo chmod +x /usr/local/bin/navtrack
   ```

2. **Verificar servicios**:
   ```bash
   navtrack status
   navtrack health
   ```

3. **Configurar backups automáticos**:
   ```bash
   sudo crontab -e
   # Agregar: 0 2 * * * /usr/local/bin/navtrack backup
   ```

4. **Configurar monitoreo**:
   ```bash
   sudo crontab -e
   # Agregar: */5 * * * * /opt/navtrack/monitor-navtrack.sh check
   ```

### 📞 Soporte

Si encuentras problemas:

1. Ejecuta diagnóstico:
   ```bash
   cd /tmp/navtrack
   sudo dos2unix troubleshoot.sh
   sudo bash troubleshoot.sh
   ```

2. Revisa logs:
   ```bash
   navtrack logs
   ```

3. Consulta documentación:
   - `INSTALACION.md` - Instalación
   - `DEPLOYMENT.md` - Operación
   - `FIX-WINDOWS-ISSUE.md` - Line endings

### ✅ Lista de Verificación

- [x] Error de contexto Docker corregido
- [x] Documentación de line endings
- [x] Script de instalación actualizado
- [x] Archivos `.gitattributes` para prevenir problemas
- [x] Guías de instalación completas
- [x] Scripts de gestión y monitoreo
- [x] Documentación de GPS (60+ protocolos)

---

## Resumen

**Todo está listo para instalación en producción.**

Solo recuerda:
1. Ejecutar `dos2unix *.sh` primero
2. Verificar que DNS está configurado
3. Asegurar que puertos estén abiertos
4. Seguir instrucciones en `LEEME-PRIMERO.txt` o `INSTALACION.md`

**Tiempo estimado**: 15-20 minutos

**Dificultad**: Fácil (completamente automatizado)
