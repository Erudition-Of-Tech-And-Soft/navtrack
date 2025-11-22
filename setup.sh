#!/bin/bash

###############################################################################
# NavTrack - Setup Script
# Script de configuración que corrige line endings y ejecuta la instalación
# Este es el script principal a ejecutar
###############################################################################

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║          NavTrack - Instalación Automatizada                ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Este script debe ejecutarse como root. Use sudo.${NC}"
    exit 1
fi

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo -e "${YELLOW}Paso 1: Verificando sistema...${NC}"

# Check if this is Linux
if [[ "$OSTYPE" != "linux-gnu"* ]]; then
    echo -e "${RED}Este script solo funciona en Linux${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Sistema operativo compatible${NC}"

# Install dos2unix if not present
echo ""
echo -e "${YELLOW}Paso 2: Instalando herramientas necesarias...${NC}"

if ! command -v dos2unix &> /dev/null; then
    echo "Instalando dos2unix..."

    if [ -f /etc/debian_version ]; then
        apt-get update -qq
        apt-get install -y dos2unix
    elif [ -f /etc/redhat-release ]; then
        yum install -y dos2unix
    else
        echo -e "${RED}No se pudo instalar dos2unix automáticamente${NC}"
        echo "Por favor instálelo manualmente e intente de nuevo"
        exit 1
    fi
fi

echo -e "${GREEN}✓ Herramientas instaladas${NC}"

# Fix line endings
echo ""
echo -e "${YELLOW}Paso 3: Corrigiendo terminaciones de línea...${NC}"

# Count files to convert
file_count=$(ls -1 *.sh 2>/dev/null | wc -l)

if [ "$file_count" -eq 0 ]; then
    echo -e "${RED}No se encontraron archivos .sh${NC}"
    exit 1
fi

echo "Convirtiendo $file_count archivos..."

for file in *.sh; do
    if [ -f "$file" ]; then
        # Check if file has CRLF
        if file "$file" | grep -q "CRLF"; then
            echo "  Convirtiendo: $file"
            dos2unix "$file" 2>/dev/null || sed -i 's/\r$//' "$file"
        fi

        # Set execute permission
        chmod +x "$file"
    fi
done

echo -e "${GREEN}✓ Terminaciones de línea corregidas${NC}"
echo -e "${GREEN}✓ Permisos de ejecución establecidos${NC}"

# Verify conversion
echo ""
echo -e "${YELLOW}Paso 4: Verificando conversión...${NC}"

has_crlf=false
for file in *.sh; do
    if file "$file" | grep -q "CRLF"; then
        echo -e "${RED}✗ $file todavía tiene CRLF${NC}"
        has_crlf=true
    fi
done

if [ "$has_crlf" = true ]; then
    echo -e "${RED}Algunos archivos todavía tienen CRLF. Por favor corrija manualmente.${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Todos los archivos convertidos correctamente${NC}"

# Check if install script exists
if [ ! -f "install-navtrack.sh" ]; then
    echo -e "${RED}No se encontró install-navtrack.sh${NC}"
    exit 1
fi

# Show summary
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║              Preparación Completada                         ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
echo -e "${GREEN}El sistema está listo para instalar NavTrack${NC}"
echo ""
echo "Archivos preparados:"
for file in *.sh; do
    echo "  ✓ $file"
done
echo ""

# Ask to continue
read -p "¿Desea continuar con la instalación ahora? (s/N) " -n 1 -r
echo
echo

if [[ $REPLY =~ ^[SsYy]$ ]]; then
    echo -e "${YELLOW}Iniciando instalación de NavTrack...${NC}"
    echo ""

    # Run installation
    ./install-navtrack.sh
else
    echo ""
    echo "Instalación cancelada."
    echo ""
    echo "Para instalar más tarde, ejecute:"
    echo "  sudo ./install-navtrack.sh"
    echo ""
fi
