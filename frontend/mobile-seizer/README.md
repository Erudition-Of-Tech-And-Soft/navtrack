# Navtrack Incautadores - App Móvil

Aplicación móvil React Native para incautadores que permite monitorear vehículos incautados en tiempo real.

## Características

- 🔐 **Autenticación segura** - Login exclusivo para usuarios con rol de Incautador
- 📱 **Vista de vehículos incautados** - Lista de todos los vehículos con incaute activo
- ⏰ **Temporizador de expiración** - Contador en tiempo real del tiempo restante de incautación
- 🗺️ **Rastreo en tiempo real** - Ubicación actual y trayectoria del vehículo en mapa
- 🔔 **Notificaciones push** - Alertas cuando un incaute está por expirar o el vehículo se mueve
- 🌐 **Multi-organización** - Soporte para incautadores con acceso a múltiples organizaciones

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
│   ├── SeizedAssetsListScreen.tsx # Lista de vehículos incautados
│   └── AssetDetailScreen.tsx      # Detalles y mapa del vehículo
├── contexts/             # Contextos de React
│   └── AuthContext.tsx           # Autenticación y gestión de usuario
├── services/             # Servicios y lógica de negocio
│   ├── api.ts                    # Cliente API REST
│   └── notifications.ts          # Configuración de Firebase Messaging
└── App.tsx              # Componente raíz de la aplicación
```

## Funcionalidades Principales

### Login
- Solo usuarios con rol "Seizer" pueden acceder
- Autenticación mediante JWT
- Persistencia de sesión con AsyncStorage

### Lista de Vehículos Incautados
- Muestra vehículos con `hasActiveSeizure = true`
- Filtra automáticamente incautes expirados
- Actualización automática cada 30 segundos
- Indicadores de:
  - Estado de conexión (conectado/desconectado)
  - Tiempo restante hasta expiración
  - Última ubicación conocida

### Detalles del Vehículo
- Información completa del vehículo
- Mapa interactivo con:
  - Marcador de posición actual
  - Trayectoria del día (polyline)
- Datos de la última posición:
  - Coordenadas GPS
  - Velocidad
  - Altitud
  - Timestamp

### Notificaciones Push

La app está configurada para recibir notificaciones en tres casos:

1. **Incaute por expirar** - 24 horas antes de la fecha de expiración
2. **Vehículo en movimiento** - Cuando un vehículo incautado se mueve
3. **Vehículo desconectado** - Cuando un vehículo pierde conexión GPS

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

### Obtener Ubicaciones de Asset
```
GET /api/assets/{assetId}/locations
Headers: { Authorization: Bearer <token> }
Query: { startDate: ISO8601, endDate: ISO8601, limit: number }
Response: { items: Location[] }
```

## Notificaciones Firebase

Para enviar notificaciones desde el backend, usar Firebase Cloud Messaging:

```javascript
// Ejemplo de payload de notificación
{
  "notification": {
    "title": "Incaute por Expirar",
    "body": "El vehículo Toyota Corolla expira en 12 horas"
  },
  "data": {
    "type": "seizure_expiring",
    "assetId": "123456",
    "assetName": "Toyota Corolla"
  },
  "token": "<fcm_token_del_usuario>"
}
```

## Idioma

La aplicación está completamente en **español dominicano**, incluyendo:
- Interfaz de usuario
- Mensajes de error
- Notificaciones
- Formato de fechas y horas

## Notas de Desarrollo

- La app usa React Query para caché y sincronización de datos
- Las ubicaciones se actualizan automáticamente cada 10 segundos
- El token FCM se envía al backend automáticamente al hacer login
- La sesión persiste entre cierres de la app

## TODO

- [ ] Implementar envío de FCM token al backend
- [ ] Agregar pantalla de perfil de usuario
- [ ] Implementar búsqueda y filtros en lista de vehículos
- [ ] Agregar soporte para modo offline
- [ ] Implementar analytics con Firebase Analytics
