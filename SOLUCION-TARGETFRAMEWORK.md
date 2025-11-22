# Solución al Problema de TargetFramework

## ❌ Problema

Error durante build de Docker:
```
error NETSDK1013: The TargetFramework value '' was not recognized
```

## 🔍 Causa Raíz

Los archivos `.csproj` de NavTrack **NO tienen `TargetFramework` definido explícitamente**. En su lugar, dependen de un archivo `Directory.Build.props` en la raíz del proyecto que define:

```xml
<Project>
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
    </PropertyGroup>
</Project>
```

Cuando hacíamos `COPY . .` en los Dockerfiles, este archivo se copiaba, pero .NET no lo encontraba correctamente en la jerarquía de directorios.

## ✅ Solución Implementada

Modificados **TODOS** los Dockerfiles de .NET para copiar explícitamente `Directory.Build.props` primero:

### 1. Backend API (`backend/Navtrack.Api/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Build.props first (defines TargetFramework)
COPY Directory.Build.props ./

# Copy all backend projects
COPY backend/ ./backend/

# Publish the API project
RUN dotnet publish "backend/Navtrack.Api/Navtrack.Api.csproj" -c Release -o /app
```

### 2. GPS Listener (`backend/Navtrack.Listener/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Build.props first (defines TargetFramework)
COPY Directory.Build.props ./

# Copy all backend projects
COPY backend/ ./backend/

# Publish the Listener project
RUN dotnet publish "backend/Navtrack.Listener/Navtrack.Listener.csproj" -c Release -o /app
```

### 3. Odoo API (`Odoo.Navtrac.Api/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Build.props first (defines TargetFramework)
COPY Directory.Build.props ./

# Copy backend (for dependencies) and Odoo.Navtrac.Api
COPY backend/ ./backend/
COPY Odoo.Navtrac.Api/ ./Odoo.Navtrac.Api/

# Publish the Odoo API project
RUN dotnet publish "Odoo.Navtrac.Api/Odoo.Navtrac.Api.csproj" -c Release -o /app
```

## 🎯 Cambios Clave

**Antes** (No funcionaba):
```dockerfile
COPY . .
RUN dotnet publish "backend/Navtrack.Api/Navtrack.Api.csproj" -c Release -o /app
```

**Ahora** (Funciona):
```dockerfile
# 1. Copiar Directory.Build.props primero
COPY Directory.Build.props ./

# 2. Copiar solo lo necesario
COPY backend/ ./backend/

# 3. Compilar
RUN dotnet publish "backend/Navtrack.Api/Navtrack.Api.csproj" -c Release -o /app
```

## 📋 Estructura en Docker

Después de los COPY, la estructura en `/src` es:

```
/src/
├── Directory.Build.props        ← Define TargetFramework=net9.0
├── backend/
│   ├── Navtrack.Api/
│   │   └── Navtrack.Api.csproj  ← Hereda de Directory.Build.props
│   ├── Navtrack.Listener/
│   │   └── Navtrack.Listener.csproj
│   └── ...
└── Odoo.Navtrac.Api/
    └── Odoo.Navtrac.Api.csproj
```

Cuando .NET compila un proyecto, busca `Directory.Build.props` hacia arriba en la jerarquía de directorios y lo encuentra en `/src/`.

## ✅ Archivos Modificados

| Archivo | Cambio |
|---------|--------|
| `backend/Navtrack.Api/Dockerfile` | Copia explícita de Directory.Build.props |
| `backend/Navtrack.Listener/Dockerfile` | Copia explícita de Directory.Build.props |
| `Odoo.Navtrac.Api/Dockerfile` | Copia explícita de Directory.Build.props |

## 🚀 Resultado

Ahora todos los proyectos .NET pueden encontrar correctamente el `TargetFramework` y compilar sin errores.

---

**Última actualización**: 2025-11-21
