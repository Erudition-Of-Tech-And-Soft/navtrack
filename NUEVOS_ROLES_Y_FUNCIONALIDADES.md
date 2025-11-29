# Nuevos Roles y Funcionalidades - Sistema Navtrack

## 📋 Resumen de Cambios Solicitados

Este documento detalla los cambios implementados y pendientes para el sistema de roles ampliado y nuevas funcionalidades.

---

## ✅ CAMBIOS IMPLEMENTADOS (Backend)

### 1. Nuevos Roles de Organización

**Archivo**: `backend/Navtrack.DataAccess.Model/Organizations/OrganizationUserRole.cs`

```csharp
public enum OrganizationUserRole
{
    Owner,      // Propietario (control total)
    Employee,   // Empleado (solo lectura de todo)
    Member,     // Miembro (acceso limitado)
    Seizer      // Incautador (solo assets con incaute activo)
}
```

#### Permisos por Rol:

| Funcionalidad | Owner | Employee | Member | Seizer |
|---------------|-------|----------|--------|--------|
| Editar configuración | ✅ | ❌ | ❌ | ❌ |
| Ver todos los assets | ✅ | ✅ | ❌ | Solo incautados |
| Enviar comandos GPS | ✅ | ✅ | ❌ | ❌ |
| Ver filtros avanzados | ✅ | ✅ | ❌ | ❌ |
| Ver flag HasActiveSeizure | ✅ | ✅ | ❌ | ✅ |
| Gestionar usuarios | ✅ | ❌ | ❌ | ❌ |

### 2. Nuevos Campos en AssetDocument

**Archivo**: `backend/Navtrack.DataAccess.Model/Assets/AssetDocument.cs`

```csharp
/// <summary>
/// Indica si el asset del member está atrasado (manejado por el sistema)
/// </summary>
[BsonElement("isDelayed")]
public bool IsDelayed { get; set; }

/// <summary>
/// Indica si el asset tiene un incaute activo
/// Visible solo para Owner, Employee y Seizer
/// </summary>
[BsonElement("hasActiveSeizure")]
public bool HasActiveSeizure { get; set; }

/// <summary>
/// Fecha y hora de expiración del incaute
/// Después de esta fecha, el incaute deja de estar activo para Seizers
/// </summary>
[BsonElement("seizureExpirationDate")]
public DateTime? SeizureExpirationDate { get; set; }

/// <summary>
/// Indica si el GPS del asset tiene más de 2 días sin enviar ubicación
/// </summary>
[BsonElement("gpsInactive")]
public bool GpsInactive { get; set; }
```

### 3. Lógica de Autorización Actualizada

**Archivo**: `backend/Navtrack.Api.Services/Common/Context/NavtrackContext.cs`

#### Nuevo método para Seizers:

```csharp
/// <summary>
/// Verifica si el usuario puede ver un asset como Seizer
/// Solo puede ver assets con HasActiveSeizure=true y con SeizureExpirationDate no vencida
/// </summary>
public bool CanSeizerViewAsset(AssetDocument asset)
{
    // 1. Verificar que el usuario sea Seizer
    // 2. Verificar que el asset tenga incaute activo
    // 3. Verificar que no haya expirado
}
```

#### Actualización de HasOrganizationUserRole:

```csharp
OrganizationUserRole.Employee =>
    userOrganization?.UserRole is OrganizationUserRole.Owner
    or OrganizationUserRole.Employee,

OrganizationUserRole.Member =>
    userOrganization?.UserRole is OrganizationUserRole.Owner
    or OrganizationUserRole.Employee
    or OrganizationUserRole.Member,

OrganizationUserRole.Seizer =>
    userOrganization?.UserRole is OrganizationUserRole.Owner
    or OrganizationUserRole.Employee
    or OrganizationUserRole.Seizer,
```

---

## 🚧 CAMBIOS PENDIENTES (Por Implementar)

### BACKEND

#### 1. Modelos de API

- [ ] Crear/actualizar DTOs para incluir los nuevos campos:
  - `AssetModel.cs` - Agregar IsDelayed, HasActiveSeizure, GpsInactive, SeizureExpirationDate
  - `CreateAssetModel.cs` - Agregar campos opcionales
  - `UpdateAssetModel.cs` - Permitir actualizar HasActiveSeizure y SeizureExpirationDate

#### 2. Endpoints de API

- [ ] **GET /api/assets** - Agregar parámetros de filtro:
  - `?isDelayed=true`
  - `?hasActiveSeizure=true`
  - `?gpsInactive=true`

- [ ] **GET /api/assets/{id}** - Incluir nuevos campos en respuesta

- [ ] **PUT /api/assets/{id}/seizure** - Nuevo endpoint para activar/desactivar incaute:
  ```csharp
  public class UpdateSeizureRequest
  {
      public bool HasActiveSeizure { get; set; }
      public DateTime? SeizureExpirationDate { get; set; }
  }
  ```

- [ ] **POST /api/assets/{id}/commands** - Nuevo endpoint para enviar comandos GPS:
  ```csharp
  public class SendGpsCommandRequest
  {
      public string CommandType { get; set; } // "CutFuel", "Fortify", etc.
      public Dictionary<string, object>? Parameters { get; set; }
  }
  ```

#### 3. Servicios de Negocio

- [ ] **Servicio de actualización automática de flags**:
  - Crear job/servicio que actualice `IsDelayed` según criterios de negocio
  - Crear job/servicio que actualice `GpsInactive` si no hay mensaje en >48h
  - Crear job/servicio que desactive incautes expirados

Archivo sugerido: `backend/Navtrack.Api.Services/Assets/AssetStatusUpdateService.cs`

```csharp
public class AssetStatusUpdateService : IHostedService
{
    public async Task UpdateAssetStatuses()
    {
        // Actualizar IsDelayed
        // Actualizar GpsInactive
        // Desactivar incautes expirados
    }
}
```

#### 4. Filtrado para Seizers

- [ ] **Endpoint GET /api/assets/seized** - Solo assets con incaute activo para el Seizer:

```csharp
[AuthorizeOrganization(OrganizationUserRole.Seizer)]
public async Task<IEnumerable<AssetModel>> GetSeizedAssets()
{
    // Filtrar solo assets con:
    // - HasActiveSeizure = true
    // - SeizureExpirationDate > DateTime.UtcNow
    // - Pertenecen a la organización del Seizer
}
```

#### 5. Permisos de Solo Lectura para Employee

- [ ] Crear atributo `[ReadOnlyForEmployee]` para endpoints que Employees pueden ver pero no modificar
- [ ] Aplicar a endpoints de actualización/eliminación

---

### FRONTEND WEB

#### 1. Actualizar Modelos TypeScript

- [ ] Regenerar modelos con `npm run generate:api` después de actualizar backend
- [ ] Verificar que `OrganizationUserRole` incluya `Employee` y `Seizer`
- [ ] Verificar que `Asset` incluya los nuevos campos

#### 2. Hook de Autorización

**Archivo**: `frontend/shared/src/hooks/current/useAuthorize.ts`

- [ ] Agregar funciones para Employee y Seizer:

```typescript
const authorizeEmployee = useCallback((action: 'view' | 'edit') => {
  const organization = currentUser.data?.organizations?.find(
    (x) => x.organizationId === currentOrganization.id
  );

  if (action === 'view') {
    return organization?.userRole === OrganizationUserRole.Owner ||
           organization?.userRole === OrganizationUserRole.Employee;
  }

  // Employee solo puede ver, no editar
  return organization?.userRole === OrganizationUserRole.Owner;
}, [currentOrganization.id, currentUser.data?.organizations]);

const authorizeSeizer = useCallback(() => {
  const organization = currentUser.data?.organizations?.find(
    (x) => x.organizationId === currentOrganization.id
  );

  return organization?.userRole === OrganizationUserRole.Seizer ||
         organization?.userRole === OrganizationUserRole.Owner;
}, [currentOrganization.id, currentUser.data?.organizations]);

return {
  organization: authorizeOrganization,
  asset: assetAuthorize,
  employee: authorizeEmployee,
  seizer: authorizeSeizer
};
```

#### 3. Componente de Filtros de Assets

**Archivo nuevo**: `frontend/web/src/components/asset/AssetFilters.tsx`

```tsx
export function AssetFilters() {
  const [filters, setFilters] = useState({
    isDelayed: false,
    hasActiveSeizure: false,
    gpsInactive: false
  });

  const { employee } = useAuthorize();

  // Solo visible para Owner y Employee
  if (!employee('view')) {
    return null;
  }

  return (
    <div className="filters">
      <Checkbox
        label="Assets Atrasados"
        checked={filters.isDelayed}
        onChange={(checked) => setFilters(f => ({ ...f, isDelayed: checked }))}
      />
      <Checkbox
        label="Con Incaute Activo"
        checked={filters.hasActiveSeizure}
        onChange={(checked) => setFilters(f => ({ ...f, hasActiveSeizure: checked }))}
      />
      <Checkbox
        label="GPS Inactivo (+2 días)"
        checked={filters.gpsInactive}
        onChange={(checked) => setFilters(f => ({ ...f, gpsInactive: checked }))}
      />
    </div>
  );
}
```

#### 4. Ventana de Comandos GPS

**Archivo nuevo**: `frontend/web/src/components/asset/commands/GpsCommandsModal.tsx`

```tsx
export function GpsCommandsModal({ assetId, onClose }: Props) {
  const { employee } = useAuthorize();
  const sendCommandMutation = useSendGpsCommand();

  // Solo visible para Owner y Employee
  if (!employee('view')) {
    return null;
  }

  const commands = [
    { id: 'cutFuel', label: 'Cortar Combustible', icon: '🔴' },
    { id: 'restoreFuel', label: 'Restaurar Combustible', icon: '🟢' },
    { id: 'fortify', label: 'Activar Fortificación', icon: '🛡️' },
    { id: 'withdraw', label: 'Retirar Fortificación', icon: '🔓' },
    { id: 'queryLocation', label: 'Consultar Ubicación', icon: '📍' },
    { id: 'restart', label: 'Reiniciar Terminal', icon: '🔄' },
  ];

  const handleSendCommand = (commandType: string) => {
    sendCommandMutation.mutate({
      assetId,
      commandType,
      parameters: {}
    });
  };

  return (
    <Modal title="Comandos GPS" onClose={onClose}>
      <div className="commands-grid">
        {commands.map(cmd => (
          <Button
            key={cmd.id}
            onClick={() => handleSendCommand(cmd.id)}
            disabled={!employee('view')} // Employee puede ver pero Owner envía
          >
            {cmd.icon} {cmd.label}
          </Button>
        ))}
      </div>
    </Modal>
  );
}
```

#### 5. Indicadores Visuales en Lista de Assets

**Archivo**: `frontend/web/src/components/asset/AssetListItem.tsx`

- [ ] Agregar badges/iconos para los flags:
  - 🔴 Asset atrasado (isDelayed)
  - 🛡️ Incaute activo (hasActiveSeizure) - solo Owner/Employee/Seizer
  - 📡 GPS inactivo (gpsInactive)

```tsx
{asset.isDelayed && <Badge color="red">Atrasado</Badge>}
{asset.hasActiveSeizure && canViewSeizure && <Badge color="orange">Incautado</Badge>}
{asset.gpsInactive && <Badge color="gray">GPS Inactivo</Badge>}
```

#### 6. Vista para Seizers

**Archivo nuevo**: `frontend/web/src/pages/SeizedAssetsPage.tsx`

- [ ] Página que solo muestre assets con incaute activo
- [ ] Solo accesible por usuarios con rol Seizer
- [ ] Filtrar automáticamente por `hasActiveSeizure=true` y no expirados

---

### APPS MÓVILES REACT NATIVE

#### App 1: Navtrack Seizer (Para Incautadores)

**Estructura sugerida**:

```
mobile/seizer-app/
├── App.tsx
├── src/
│   ├── screens/
│   │   ├── LoginScreen.tsx
│   │   ├── SeizedAssetsListScreen.tsx
│   │   ├── AssetDetailScreen.tsx
│   │   └── AssetMapScreen.tsx
│   ├── components/
│   │   ├── AssetCard.tsx
│   │   ├── Map.tsx
│   │   └── ExpirationTimer.tsx
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   ├── useSeizedAssets.ts
│   │   └── useAssetLocation.ts
│   ├── api/
│   │   └── client.ts
│   └── navigation/
│       └── RootNavigator.tsx
```

**Funcionalidades**:
- ✅ Login exclusivo para usuarios con rol Seizer
- ✅ Lista de assets con incaute activo
- ✅ Mostrar tiempo restante de incaute
- ✅ Ver ubicación en tiempo real
- ✅ Ver historial de ubicaciones
- ❌ No puede editar nada
- ❌ No puede enviar comandos

#### App 2: Navtrack Member (Para Miembros)

**Estructura sugerida**:

```
mobile/member-app/
├── App.tsx
├── src/
│   ├── screens/
│   │   ├── LoginScreen.tsx
│   │   ├── MyAssetsScreen.tsx
│   │   ├── AssetDetailScreen.tsx
│   │   ├── AssetMapScreen.tsx
│   │   └── AssetHistoryScreen.tsx
│   ├── components/
│   │   ├── AssetCard.tsx
│   │   ├── Map.tsx
│   │   ├── TripHistory.tsx
│   │   └── Stats.tsx
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   ├── useMyAssets.ts
│   │   └── useAssetLocation.ts
│   ├── api/
│   │   └── client.ts
│   └── navigation/
│       └── RootNavigator.tsx
```

**Funcionalidades**:
- ✅ Login para usuarios con rol Member
- ✅ Ver solo sus assets asignados
- ✅ Ver ubicación en tiempo real
- ✅ Ver historial de viajes
- ✅ Ver estadísticas (km recorridos, tiempo de uso, etc.)
- ❌ No ve flags de incautes
- ❌ No ve assets atrasados/GPS inactivo
- ❌ No puede editar configuración
- ❌ No puede enviar comandos

---

## 📊 Resumen de Tareas Pendientes

### Backend (Alta Prioridad)
1. ✅ Actualizar OrganizationUserRole ✅
2. ✅ Agregar campos a AssetDocument ✅
3. ✅ Actualizar lógica de autorización ✅
4. ⏳ Crear DTOs/Modelos de API
5. ⏳ Crear endpoints de filtrado
6. ⏳ Crear endpoint de comandos GPS
7. ⏳ Crear servicio de actualización automática de flags
8. ⏳ Implementar restricciones de solo lectura para Employee

### Frontend Web (Alta Prioridad)
1. ⏳ Regenerar modelos TypeScript
2. ⏳ Actualizar hook useAuthorize
3. ⏳ Crear componente AssetFilters
4. ⏳ Crear modal GpsCommandsModal
5. ⏳ Agregar indicadores visuales en lista de assets
6. ⏳ Crear página para Seizers

### Apps Móviles (Media Prioridad)
1. ⏳ Crear app React Native para Seizers
2. ⏳ Crear app React Native para Members
3. ⏳ Implementar autenticación por rol
4. ⏳ Implementar funcionalidades específicas por rol

---

## 🎯 Próximos Pasos Recomendados

1. **Completar Backend**:
   - Crear los DTOs y endpoints de API
   - Implementar servicio de actualización automática
   - Probar autorización por roles

2. **Actualizar Frontend Web**:
   - Regenerar modelos
   - Implementar filtros
   - Crear ventana de comandos GPS

3. **Desarrollar Apps Móviles**:
   - Configurar proyecto React Native
   - Implementar navegación
   - Integrar con API

---

¿Quieres que continúe con alguna de las tareas pendientes? Por ejemplo:
- Crear los DTOs y endpoints de API
- Implementar el servicio de actualización automática
- Crear el componente de filtros en el frontend
- Configurar las apps React Native
