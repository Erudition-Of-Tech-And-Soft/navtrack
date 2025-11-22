# 🚀 NavTrack - Instalación Linux

## ⚠️ IMPORTANTE: Ejecutar ESTO PRIMERO

Como los archivos fueron creados en Windows, **debes ejecutar este comando primero** en tu servidor Linux:

```bash
cd /tmp/navtrack
sudo apt-get update && sudo apt-get install -y dos2unix
dos2unix *.sh *.md 2>/dev/null
chmod +x *.sh
```

## 📦 Instalación Completa en Un Comando

Después de corregir los line endings, ejecuta:

```bash
sudo ./install-navtrack.sh
```

---

## 🎯 Instalación Paso a Paso (Método Recomendado)

### 1. Clonar Repositorio

```bash
git clone <url-repositorio> /tmp/navtrack
cd /tmp/navtrack
```

### 2. Corregir Line Endings (OBLIGATORIO)

```bash
# Instalar dos2unix
sudo apt-get update
sudo apt-get install -y dos2unix

# Convertir TODOS los archivos
dos2unix *.sh *.md 2>/dev/null

# Dar permisos
chmod +x *.sh
```

### 3. Verificar Conversión

```bash
# Este comando NO debe mostrar "CRLF"
file *.sh
```

Salida correcta:
```
install-navtrack.sh: Bourne-Again shell script, ASCII text executable
navtrack-manage.sh: Bourne-Again shell script, ASCII text executable
...
```

Salida incorrecta (si ves esto, repite paso 2):
```
install-navtrack.sh: ... with CRLF line terminators
```

### 4. Ejecutar Instalación

```bash
sudo ./install-navtrack.sh
```

---

## 🔥 Método Alternativo: Copiar y Pegar

Si el método anterior no funciona, usa este script completo de instalación (copia y pega todo):

```bash
#!/bin/bash

# NavTrack - Instalación con corrección automática

echo "=== NavTrack - Instalación Automática ==="
echo ""

# Ubicación
REPO_DIR="/tmp/navtrack"

# Navegar al directorio
if [ ! -d "$REPO_DIR" ]; then
    echo "Error: El directorio $REPO_DIR no existe"
    echo "Primero clone el repositorio:"
    echo "  git clone <url-repo> $REPO_DIR"
    exit 1
fi

cd "$REPO_DIR"

# Instalar dos2unix
echo "Instalando dos2unix..."
sudo apt-get update -qq
sudo apt-get install -y dos2unix

# Convertir archivos
echo "Corrigiendo line endings..."
dos2unix *.sh 2>/dev/null
dos2unix *.md 2>/dev/null

# Permisos
echo "Estableciendo permisos..."
chmod +x *.sh

# Verificar
echo "Verificando conversión..."
if file install-navtrack.sh | grep -q "CRLF"; then
    echo "ERROR: Los archivos todavía tienen CRLF"
    echo "Intentando método alternativo..."
    for f in *.sh; do
        sed -i 's/\r$//' "$f"
    done
fi

# Ejecutar instalación
echo ""
echo "Iniciando instalación de NavTrack..."
echo ""
sudo ./install-navtrack.sh
```

**Para usar**: Guarda este script en un archivo llamado `quick-install.sh` y ejecútalo:

```bash
nano quick-install.sh
# Pega el contenido de arriba
# Presiona Ctrl+X, luego Y, luego Enter

bash quick-install.sh
```

---

## 🛠 Método Manual (Si Todo lo Demás Falla)

### Paso 1: Instalar Dependencias

```bash
sudo apt-get update
sudo apt-get install -y docker.io docker-compose nginx certbot python3-certbot-nginx dos2unix git curl
```

### Paso 2: Habilitar Docker

```bash
sudo systemctl start docker
sudo systemctl enable docker
```

### Paso 3: Corregir Archivos

```bash
cd /tmp/navtrack
for file in *.sh *.md; do
    sed -i 's/\r$//' "$file"
    if [[ $file == *.sh ]]; then
        chmod +x "$file"
    fi
done
```

### Paso 4: Ejecutar Instalación

```bash
sudo ./install-navtrack.sh
```

---

## 📋 Pre-requisitos (Verificar ANTES)

Antes de ejecutar la instalación, asegúrate de tener:

### 1. DNS Configurado

Los siguientes dominios deben apuntar a la IP de tu servidor:

```bash
# Verificar con:
host gps-qa.inversionespereztaveras.com
host gps-api-qa.inversionespereztaveras.com
host gps-odoo-qa.inversionespereztaveras.com
host gps-listener-qa.inversionespereztaveras.com
```

Todos deben mostrar la IP de tu servidor.

### 2. Puertos Abiertos

```bash
# Verificar firewall del proveedor de hosting
# Asegúrate de que estos puertos estén abiertos:
# - 22 (SSH)
# - 80 (HTTP)
# - 443 (HTTPS)
# - 7002-7100 (GPS)
```

### 3. Recursos del Servidor

```bash
# Verificar RAM
free -h
# Debe tener al menos 4GB

# Verificar disco
df -h
# Debe tener al menos 20GB disponibles
```

---

## ✅ Verificación Post-Instalación

Después de la instalación:

```bash
# 1. Copiar script de gestión
sudo cp navtrack-manage.sh /usr/local/bin/navtrack
sudo chmod +x /usr/local/bin/navtrack

# 2. Verificar servicios
navtrack status

# 3. Verificar salud
navtrack health

# 4. Ver logs
navtrack logs
```

---

## 🌐 Acceso a las Aplicaciones

Una vez instalado, accede a:

- **Frontend**: https://gps-qa.inversionespereztaveras.com
- **Backend API**: https://gps-api-qa.inversionespereztaveras.com
- **Odoo API**: https://gps-odoo-qa.inversionespereztaveras.com

---

## 🆘 Solución de Problemas

### Problema: "bad interpreter"

```bash
# Solución rápida
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh
```

### Problema: Servicios no inician

```bash
# Ver logs
cd /opt/navtrack
docker compose -f docker-compose.prod.yml logs

# Diagnóstico
sudo bash troubleshoot.sh
```

### Problema: Certificados SSL fallan

```bash
# Verificar DNS primero
host gps-qa.inversionespereztaveras.com

# Si DNS no está configurado, configúralo y espera
# Luego ejecuta:
sudo certbot --nginx -d gps-qa.inversionespereztaveras.com
```

### Problema: GPS no conecta

```bash
# Ver logs del listener
navtrack logs listener

# Verificar puertos
sudo netstat -tuln | grep ":700"
```

---

## 📚 Documentación Adicional

- **Configuración GPS**: Ver `GPS-PORTS.md`
- **Gestión diaria**: Ver `DEPLOYMENT.md`
- **Solución line endings**: Ver `FIX-WINDOWS-ISSUE.md`

---

## 🎯 Resumen de Comandos

```bash
# 1. Clonar
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack

# 2. Corregir line endings (OBLIGATORIO)
sudo apt-get install -y dos2unix
dos2unix *.sh
chmod +x *.sh

# 3. Instalar
sudo ./install-navtrack.sh

# 4. Post-instalación
sudo cp navtrack-manage.sh /usr/local/bin/navtrack
navtrack status
```

---

## ⏱ Tiempo Estimado

- Corrección de archivos: 2 minutos
- Instalación completa: 10-15 minutos
- **Total**: ~15-20 minutos

---

## 📞 Ayuda

Si tienes problemas:

1. Verifica que ejecutaste `dos2unix *.sh`
2. Ejecuta `sudo bash troubleshoot.sh`
3. Revisa los logs con `navtrack logs`
4. Consulta `DEPLOYMENT.md` para más detalles

---

**¡Listo para instalar!**

Recuerda: **Siempre ejecuta `dos2unix *.sh` primero** antes de ejecutar cualquier script.
