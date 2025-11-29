# Navtrack Miembros - App Móvil

Aplicación móvil React Native para miembros que permite ver sus vehículos asignados y rastrearlos en tiempo real.

## Características

- 🔐 **Autenticación segura** - Login exclusivo para usuarios con rol de Miembro
- 📱 **Mis vehículos** - Lista de vehículos asignados al miembro
- ⚠️ **Alertas de pago** - Indicador visual si el miembro está atrasado en pagos
- 🗺️ **Rastreo del día** - Ubicaciones y trayectoria solo del día actual
- 🔔 **Notificaciones push** - Alertas cuando cambia el estado de pago
- 🌐 **Multi-organización** - Soporte para miembros en múltiples organizaciones

## Requisitos Previos

- Node.js >= 18
- React Native CLI
- Android Studio (para Android)
- Xcode (para iOS, solo en macOS)
- Firebase proyecto configurado

## Instalación

1. Instalar dependencias:
```bash
npm install
```

2. Configurar Firebase:
   - Agregar `google-services.json` en `android/app/` (para Android)
   - Agregar `GoogleService-Info.plist` en `ios/` (para iOS)

3. Instalar pods de iOS (solo macOS):
```bash
cd ios && pod install && cd ..
```

## Ejecución

### Android
```bash
npm run android
```

### iOS
```bash
npm run ios
```

## Estructura del Proyecto

```
src/
├── screens/              # Pantallas de la aplicación
│   ├── LoginScreen.tsx           # Pantalla de inicio de sesión
│   ├── MyAssetsListScreen.tsx    # Lista de mis vehículos
│   └── AssetDetailScreen.tsx     # Detalles y mapa del vehículo
├── contexts/             # Contextos de React
│   └── AuthContext.tsx           # Autenticación y gestión de usuario
├── services/             # Servicios y lógica de negocio
│   ├── api.ts                    # Cliente API REST
│   └── notifications.ts          # Configuración de Firebase Messaging
└── App.tsx              # Componente raíz de la aplicación
```

## Funcionalidades Principales

### Login
- Solo usuarios con rol "Member" pueden acceder
- Autenticación mediante JWT
- Persistencia de sesión con AsyncStorage

### Mis Vehículos
- Muestra solo vehículos donde el usuario está asignado
- Filtra automáticamente por `asset.users` contiene al usuario
- Actualización automática cada 30 segundos
- Indicadores de:
  - Estado de conexión (conectado/desconectado)
  - Estado de pago (atrasado/al día)
  - Última ubicación conocida

### Badge de Atrasado
Cuando `isDelayed = true`:
- Badge rojo prominente "⚠️ ATRASADO EN PAGOS"
- Mensaje recordatorio para ponerse al día
- Color de fondo rojo en la sección de advertencia

### Detalles del Vehículo
- Información completa del vehículo
- **Restricción importante**: Solo muestra ubicaciones del día actual
- Mapa interactivo con:
  - Marcador de posición actual
  - Trayectoria del día (polyline verde)
- Estadísticas del día:
  - Número total de posiciones
  - Primera posición del día
  - Última posición registrada
- Datos de la ubicación actual:
  - Coordenadas GPS
  - Velocidad
  - Altitud
  - Timestamp

### Notificaciones Push

La app está configurada para recibir notificaciones en estos casos:

1. **Estado de pago cambiado** - Cuando `isDelayed` cambia a `true`
2. **Vehículo en movimiento** (opcional) - Cuando el vehículo se mueve
3. **Vehículo desconectado** (opcional) - Cuando el vehículo pierde conexión GPS

## Configuración del Backend

La app espera los siguientes endpoints en el backend:

### Autenticación
```
POST /api/account/login
Body: { email: string, password: string }
Response: { accessToken: string, user: User }
```

### Obtener Assets de Organización
```
GET /api/organizations/{organizationId}/assets
Headers: { Authorization: Bearer <token> }
Response: { items: Asset[] }
```

El frontend filtrará localmente los assets que tienen al usuario en el array `users`.

### Obtener Ubicaciones de Asset (Solo Día Actual)
```
GET /api/assets/{assetId}/locations
Headers: { Authorization: Bearer <token> }
Query: {
  startDate: ISO8601 (inicio del día actual),
  endDate: ISO8601 (fin del día actual),
  limit: 1000
}
Response: { items: Location[] }
```

**Importante**: Las fechas se calculan en la app para obtener solo el día actual:
```javascript
const now = new Date();
const startOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate());
const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
```

## Notificaciones Firebase

Para enviar notificaciones desde el backend cuando cambia el estado de pago:

```javascript
// Ejemplo de payload de notificación
{
  "notification": {
    "title": "Estado de Pago",
    "body": "Su cuenta está atrasada. Por favor póngase al día con sus pagos."
  },
  "data": {
    "type": "payment_delayed",
    "assetId": "123456",
    "assetName": "Toyota Corolla"
  },
  "token": "<fcm_token_del_usuario>"
}
```

## Esquema de Colores

La app usa una paleta de colores verde para diferenciarse de la app de Incautadores:

- **Verde oscuro principal**: `#064e3b`
- **Verde medio**: `#047857`
- **Verde claro**: `#10b981`
- **Verde muy claro**: `#a7f3d0`
- **Texto claro**: `#d1fae5`
- **Rojo para advertencias**: `#dc2626`
- **Amarillo para alertas**: `#fbbf24`

## Idioma

La aplicación está completamente en **español dominicano**, incluyendo:
- Interfaz de usuario
- Mensajes de error
- Notificaciones
- Formato de fechas y horas (locale: 'es-DO')

## Diferencias con App de Incautadores

| Característica | Incautadores | Miembros |
|---------------|--------------|----------|
| **Color theme** | Azul/Gris | Verde |
| **Filtro de assets** | hasActiveSeizure = true | user en asset.users |
| **Badge especial** | Tiempo restante | Atrasado en pagos |
| **Historial** | Completo | Solo día actual |
| **Notificación principal** | Incaute expirando | Pago atrasado |
| **Multi-org** | Sí | Sí |

## Notas de Desarrollo

- La app usa React Query para caché y sincronización de datos
- Las ubicaciones se actualizan automáticamente cada 10 segundos
- Solo se cargan ubicaciones del día actual para optimizar rendimiento
- El token FCM se envía al backend automáticamente al hacer login
- La sesión persiste entre cierres de la app
- El filtro de assets por usuario se hace en el frontend

## TODO

- [ ] Implementar envío de FCM token al backend
- [ ] Agregar pantalla de perfil de usuario
- [ ] Agregar historial de pagos
- [ ] Implementar soporte para modo offline
- [ ] Agregar analytics con Firebase Analytics
- [ ] Agregar opción para ver estadísticas mensuales

## Contacto y Soporte

Para soporte técnico o preguntas sobre pagos, contacte al administrador de su organización.
