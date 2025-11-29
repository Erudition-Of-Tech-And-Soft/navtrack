#!/usr/bin/env python3
import sys
from pymongo import MongoClient
from datetime import datetime
from bson import ObjectId

# Configuración
client = MongoClient("mongodb://31.97.146.1:27017")
db = client["navtrack"]

# Asset ID del dispositivo Edgar Test
asset_id = ObjectId("69246acd54413d1c0bb50ee5")

print("=" * 80)
print("VALIDACION DE PROTOCOLO GT06 - PUERTO 7013")
print("=" * 80)
print()

# 1. Verificar Asset
print("1. ASSET CONFIGURADO:")
asset = db.assets.find_one({"_id": asset_id})
if asset:
    print(f"   ID: {asset['_id']}")
    print(f"   Nombre: {asset.get('n', 'N/A')}")

    # Manejar diferentes estructuras del documento
    serial_number = None
    device_type_id = None

    if 'd' in asset and asset['d']:
        serial_number = asset['d'].get('sn', 'N/A')
        device_type_id = asset['d'].get('dti')
    elif 'sn' in asset:
        serial_number = asset.get('sn', 'N/A')

    print(f"   Serial Number: {serial_number}")
    print(f"   Device Type ID: {device_type_id}")

    # Buscar el nombre del device type
    if device_type_id:
        # El device type está en DeviceTypes.cs, pero podemos inferir por el ID
        # GT06 protocols suelen estar en el rango 200-300
        print(f"   Device Type: (ID {device_type_id})")
    print()
else:
    print("   ERROR: Asset no encontrado!")
    sys.exit(1)

# 2. Verificar Device asociado
print("2. DEVICE ASOCIADO:")
device = db.devices.find_one({"aid": asset_id})
if device:
    device_id = device["_id"]
    print(f"   Device ID: {device_id}")
    print(f"   Serial Number: {device.get('sn', 'N/A')}")
    print()
else:
    print("   No hay device asociado (se creará en la primera conexión)")
    device_id = None
    print()

# 3. Buscar conexiones recientes en puerto 7013 (GT06)
print("3. CONEXIONES RECIENTES EN PUERTO 7013 (GT06):")
connections = list(db.devices_connections.find(
    {"pp": 7013}
).sort("cd", -1).limit(10))

if connections:
    print(f"   Se encontraron {len(connections)} conexiones recientes:")
    print()

    for i, conn in enumerate(connections, 1):
        print(f"   Conexión #{i}:")
        print(f"     ID: {conn['_id']}")
        print(f"     Fecha: {conn.get('cd', 'N/A')}")
        print(f"     IP: {conn.get('ip', 'N/A')}")
        print(f"     Puerto Protocolo: {conn.get('pp', 'N/A')}")

        # Analizar mensajes en la conexión
        if 'm' in conn and conn['m']:
            messages = conn['m']
            print(f"     Mensajes raw: {len(messages)}")

            # Mostrar primeros bytes del primer mensaje para debug
            if len(messages) > 0:
                first_msg = bytes(messages[0])
                hex_str = ' '.join([f'{b:02X}' for b in first_msg[:20]])
                print(f"     Primer mensaje (primeros 20 bytes): {hex_str}")
        else:
            print(f"     Mensajes: 0")

        print()
else:
    print("   No se encontraron conexiones en puerto 7013")
    print("   NOTA: Verifica que el dispositivo esté configurado correctamente")
    print("         y que el Listener esté escuchando en el puerto 7013")
    print()

# 4. Verificar mensajes guardados con serial number
print("4. MENSAJES GUARDADOS:")

if device_id:
    # Buscar por device_id
    messages = list(db.devices_messages.find(
        {"md.did": device_id}
    ).sort("cd", -1).limit(5))

    print(f"   Buscando mensajes del Device ID: {device_id}")
else:
    # No hay device, buscar conexiones sin device asociado
    messages = []
    print(f"   No hay Device ID aún, buscando solo conexiones...")

print(f"   Total de mensajes guardados: {len(messages)}")
print()

if len(messages) > 0:
    print("   ULTIMOS MENSAJES:")
    for i, msg in enumerate(messages, 1):
        print(f"   Mensaje #{i}:")
        print(f"     ID: {msg['_id']}")
        print(f"     Fecha: {msg.get('cd', 'N/A')}")

        if 'pos' in msg and msg['pos']:
            pos = msg['pos']
            print(f"     Ubicación:")
            print(f"       Latitud:  {pos.get('lat', 'N/A')}")
            print(f"       Longitud: {pos.get('lon', 'N/A')}")
            print(f"       Velocidad: {pos.get('spd', 'N/A')} km/h")
            print(f"       Rumbo: {pos.get('hdg', 'N/A')}°")
            print(f"       Válido: {pos.get('v', False)}")
        else:
            print(f"     Sin datos de ubicación")
        print()

# 5. Resumen
print("=" * 80)
print("RESUMEN:")
print("=" * 80)

if len(connections) > 0 and len(messages) > 0:
    print("STATUS: ✓ FUNCIONANDO CORRECTAMENTE")
    print("  - El dispositivo está conectándose en puerto 7013")
    print("  - Los mensajes se están guardando en la base de datos")
    print("  - Verifica la interfaz web para confirmar que aparece la ubicación")
elif len(connections) > 0:
    print("STATUS: ⚠ PARCIAL")
    print("  - El dispositivo se está conectando en puerto 7013")
    print("  - PERO no se están guardando mensajes de ubicación")
    print("  - Posible problema: Serial number no coincide o protocolo mal configurado")
    print(f"  - Serial esperado: {serial_number}")
    print("  - Revisa los logs del Listener para más detalles")
else:
    print("STATUS: ✗ SIN CONEXIONES")
    print("  - No se detectan conexiones en puerto 7013")
    print("  - Verifica que el dispositivo esté configurado para usar GT06 en puerto 7013")
    print("  - Verifica que el Listener esté corriendo y escuchando en ese puerto")

print()
