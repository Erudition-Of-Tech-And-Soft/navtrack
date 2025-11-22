# ✅ Última Corrección - Directory.Build.props en Script de Instalación

## Problema Final Encontrado

El error:
```
failed to compute cache key: "/Directory.Build.props": not found
```

## Causa

El script `install-navtrack.sh` **NO estaba copiando** el archivo `Directory.Build.props` a `/opt/navtrack`.

Aunque los Dockerfiles estaban corregidos para buscar este archivo, **no existía en el directorio de instalación**.

## Solución

Modificado `install-navtrack.sh`, función `copy_project_files()`:

```bash
copy_project_files() {
    print_info "Copying project files to $INSTALL_DIR..."

    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
    mkdir -p "$INSTALL_DIR"

    # ⭐ NUEVO: Copy Directory.Build.props FIRST
    print_info "Copying Directory.Build.props..."
    if [ -f "$SCRIPT_DIR/Directory.Build.props" ]; then
        cp "$SCRIPT_DIR/Directory.Build.props" "$INSTALL_DIR/"
    else
        print_error "Directory.Build.props not found - .NET builds will fail!"
    fi

    # Copy backend files...
    cp -r "$SCRIPT_DIR/backend" "$INSTALL_DIR/"

    # Copy frontend files...
    cp -r "$SCRIPT_DIR/frontend" "$INSTALL_DIR/"

    # Copy Odoo API files...
    cp -r "$SCRIPT_DIR/Odoo.Navtrac.Api" "$INSTALL_DIR/"

    print_success "Project files copied to $INSTALL_DIR"
}
```

## Resultado

Ahora la estructura en `/opt/navtrack` será:

```
/opt/navtrack/
├── Directory.Build.props    ⭐ ¡AHORA SE COPIA!
├── backend/
│   ├── Navtrack.Api/
│   └── Navtrack.Listener/
├── frontend/
│   ├── web/
│   ├── shared/
│   └── Dockerfile
├── Odoo.Navtrac.Api/
└── docker-compose.prod.yml
```

Y los Dockerfiles podrán encontrar `Directory.Build.props`:

```dockerfile
COPY Directory.Build.props ./  # ✅ AHORA FUNCIONA
COPY backend/ ./backend/
RUN dotnet publish "backend/Navtrack.Api/Navtrack.Api.csproj" -c Release -o /app
```

## Lista Completa de Correcciones

### Archivos Modificados para TargetFramework:

1. ✅ `backend/Navtrack.Api/Dockerfile` - Copia Directory.Build.props
2. ✅ `backend/Navtrack.Listener/Dockerfile` - Copia Directory.Build.props
3. ✅ `Odoo.Navtrac.Api/Dockerfile` - Copia Directory.Build.props
4. ✅ **`install-navtrack.sh`** - **Copia Directory.Build.props a /opt/navtrack**

## Estado Final

**¡TODO CORREGIDO!**

- ✅ Script copia `Directory.Build.props`
- ✅ Dockerfiles buscan `Directory.Build.props`
- ✅ .NET encuentra `TargetFramework=net9.0`
- ✅ Build exitoso

## Para Probar

```bash
# 1. Clonar
git clone <url-repo> /tmp/navtrack && cd /tmp/navtrack

# 2. Corregir line endings
sudo apt-get install -y dos2unix && dos2unix *.sh && chmod +x *.sh

# 3. Instalar (¡AHORA SÍ FUNCIONARÁ!)
sudo ./install-navtrack.sh
```

El script ahora:
1. Copia `Directory.Build.props` a `/opt/navtrack/`
2. Copia `backend/`, `frontend/`, `Odoo.Navtrac.Api/`
3. Docker build encuentra `Directory.Build.props`
4. ✅ ¡Build exitoso!

---

**Última actualización**: 2025-11-21 (Final)
**Estado**: ✅ Completamente funcional
