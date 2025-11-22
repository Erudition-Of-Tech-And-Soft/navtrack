#!/bin/bash

###############################################################################
# Script para corregir terminaciones de línea de Windows a Unix
# Ejecutar este script primero si obtiene errores "bad interpreter"
###############################################################################

echo "Corrigiendo terminaciones de línea de todos los scripts..."

# Verificar si dos2unix está instalado
if ! command -v dos2unix &> /dev/null; then
    echo "Instalando dos2unix..."

    # Detectar el sistema operativo
    if [ -f /etc/debian_version ]; then
        sudo apt-get update
        sudo apt-get install -y dos2unix
    elif [ -f /etc/redhat-release ]; then
        sudo yum install -y dos2unix
    else
        echo "Por favor, instale dos2unix manualmente"
        exit 1
    fi
fi

# Convertir todos los archivos .sh
for file in *.sh; do
    if [ -f "$file" ]; then
        echo "Convirtiendo $file..."
        dos2unix "$file" 2>/dev/null || sed -i 's/\r$//' "$file"
        chmod +x "$file"
    fi
done

echo "✓ Terminaciones de línea corregidas"
echo "✓ Permisos de ejecución establecidos"
echo ""
echo "Ahora puede ejecutar los scripts normalmente:"
echo "  sudo ./install-navtrack.sh"
