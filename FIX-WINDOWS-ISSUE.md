# Solución: "bad interpreter: No such file or directory"

## Problema

Si ves este error al ejecutar los scripts:
```
-bash: /bin/bash^M: bad interpreter: No such file or directory
```

Este error ocurre porque los archivos fueron creados en Windows y tienen terminaciones de línea Windows (CRLF) en lugar de Unix (LF).

## Solución Rápida

### Opción 1: Usar dos2unix (Recomendado)

```bash
# 1. Instalar dos2unix
sudo apt-get update
sudo apt-get install -y dos2unix

# 2. Convertir todos los scripts
dos2unix *.sh

# 3. Dar permisos de ejecución
chmod +x *.sh

# 4. Ejecutar instalación
sudo ./install-navtrack.sh
```

### Opción 2: Usar sed

```bash
# Convertir todos los scripts con sed
for file in *.sh; do
    sed -i 's/\r$//' "$file"
    chmod +x "$file"
done

# Ejecutar instalación
sudo ./install-navtrack.sh
```

### Opción 3: Usar el script fix-line-endings.sh

```bash
# 1. Convertir el script de corrección primero
sed -i 's/\r$//' fix-line-endings.sh
chmod +x fix-line-endings.sh

# 2. Ejecutar el script de corrección
./fix-line-endings.sh

# 3. Ejecutar instalación
sudo ./install-navtrack.sh
```

### Opción 4: Manual (para un solo archivo)

```bash
# Convertir un archivo específico
sed -i 's/\r$//' install-navtrack.sh
chmod +x install-navtrack.sh

# Ejecutar
sudo ./install-navtrack.sh
```

## Prevenir el Problema

### En Git (Configuración Global)

Agregar al repositorio un archivo `.gitattributes`:

```bash
cat > .gitattributes << 'EOF'
# Set default behavior to automatically normalize line endings
* text=auto

# Force bash scripts to have unix line endings
*.sh text eol=lf
*.md text eol=lf

# Windows scripts should have CRLF
*.bat text eol=crlf
*.cmd text eol=crlf
*.ps1 text eol=crlf
EOF
```

### En VS Code

1. Abrir VS Code
2. Buscar el archivo `.sh`
3. En la barra de estado inferior derecha, hacer clic en "CRLF"
4. Seleccionar "LF"
5. Guardar el archivo

### En Notepad++

1. Abrir el archivo .sh
2. Ir a: Edit > EOL Conversion > Unix (LF)
3. Guardar

### Al clonar el repositorio en Linux

```bash
# Configurar Git para convertir automáticamente
git config --global core.autocrlf input

# Re-clonar el repositorio
git clone <url-repo>
```

## Script Completo de Instalación (Con Corrección Automática)

```bash
#!/bin/bash

# Instalación NavTrack con corrección automática de line endings

echo "Preparando instalación de NavTrack..."

# 1. Navegar al directorio del proyecto
cd /path/to/navtrack

# 2. Instalar dos2unix si no está instalado
if ! command -v dos2unix &> /dev/null; then
    echo "Instalando dos2unix..."
    sudo apt-get update
    sudo apt-get install -y dos2unix
fi

# 3. Convertir todos los scripts
echo "Convirtiendo terminaciones de línea..."
dos2unix *.sh 2>/dev/null || {
    for file in *.sh; do
        sed -i 's/\r$//' "$file"
    done
}

# 4. Dar permisos de ejecución
echo "Estableciendo permisos..."
chmod +x *.sh

# 5. Ejecutar instalación
echo "Iniciando instalación..."
sudo ./install-navtrack.sh
```

## Verificar si un archivo tiene el problema

```bash
# Ver las terminaciones de línea
cat -A install-navtrack.sh | head -5

# Si ves ^M al final de las líneas, tiene terminaciones Windows
# Ejemplo con problema:
#   #!/bin/bash^M$
#
# Ejemplo correcto:
#   #!/bin/bash$
```

## Otra forma de verificar

```bash
# Con file command
file install-navtrack.sh

# Salida con problema:
#   install-navtrack.sh: Bourne-Again shell script, ASCII text executable, with CRLF line terminators
#
# Salida correcta:
#   install-navtrack.sh: Bourne-Again shell script, ASCII text executable
```

## Solución Permanente para el Repositorio

Crear archivo `.gitattributes` en la raíz del repositorio:

```bash
cat > .gitattributes << 'EOF'
# Auto detect text files and perform LF normalization
* text=auto

# Bash scripts must use LF
*.sh text eol=lf

# Markdown files
*.md text eol=lf

# YAML files
*.yml text eol=lf
*.yaml text eol=lf

# JSON files
*.json text eol=lf

# Environment files
.env* text eol=lf

# Configuration files
*.conf text eol=lf
*.config text eol=lf

# Windows scripts should use CRLF
*.bat text eol=crlf
*.cmd text eol=crlf
*.ps1 text eol=crlf
EOF

# Commit el archivo
git add .gitattributes
git commit -m "Add .gitattributes to enforce LF line endings for shell scripts"

# Re-normalizar el repositorio
git add --renormalize .
git commit -m "Normalize all line endings"
```

## Instalación Paso a Paso (Infalible)

```bash
# Paso 1: Clonar repo
git clone <url-repo> /tmp/navtrack
cd /tmp/navtrack

# Paso 2: Instalar herramientas necesarias
sudo apt-get update
sudo apt-get install -y dos2unix git

# Paso 3: Convertir line endings
dos2unix *.sh

# Paso 4: Verificar conversión
file *.sh | grep -v CRLF

# Paso 5: Dar permisos
chmod +x *.sh

# Paso 6: Verificar shebang
head -1 install-navtrack.sh

# Paso 7: Ejecutar instalación
sudo ./install-navtrack.sh
```

## Resumen

**El problema**: Archivos creados en Windows tienen CRLF (`\r\n`)
**La solución**: Convertir a LF (`\n`) usando `dos2unix` o `sed`
**La prevención**: Usar `.gitattributes` para forzar LF en archivos .sh

**Comando más rápido**:
```bash
sudo apt-get install -y dos2unix && dos2unix *.sh && chmod +x *.sh && sudo ./install-navtrack.sh
```

## ¿Necesitas ayuda?

Si sigues teniendo problemas, ejecuta este diagnóstico:

```bash
# Diagnóstico completo
echo "=== Diagnóstico de Line Endings ==="
echo ""
echo "1. Verificando archivos:"
ls -la *.sh
echo ""
echo "2. Verificando tipo de archivo:"
file *.sh
echo ""
echo "3. Verificando line endings (primeras 5 líneas):"
cat -A install-navtrack.sh | head -5
echo ""
echo "4. Verificando si dos2unix está disponible:"
which dos2unix || echo "dos2unix NO está instalado"
echo ""
echo "5. Verificando permisos:"
stat -c "%a %n" *.sh
```

Guarda la salida y úsala para diagnosticar el problema.
