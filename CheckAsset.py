#!/usr/bin/env python3
import sys
from pymongo import MongoClient
from datetime import datetime
from bson import ObjectId

# Configuración
client = MongoClient("mongodb://31.97.146.1:27017")
db = client["navtrack"]

# Asset ID proporcionado
asset_id = ObjectId("6928844454413d1c0bb50ee9")

print("=" * 80)
print(f"VALIDACION DE ASSET: {asset_id}")
print("=" * 80)
print()

# 1. Verificar Asset
print("1. ASSET CONFIGURADO:")
asset = db.assets.find_one({"_id": asset_id})
if asset:
    print(f"   ID: {asset['_id']}")
    print(f"   Nombre: {asset.get('n', 'N/A')}")

    # Mostrar estructura completa del documento
    print()
    print("   Estructura completa del Asset:")
    for key, value in asset.items():
        if key == '_id':
            continue
        print(f"     {key}: {value}")

    # Manejar diferentes estructuras del documento
    serial_number = None
    device_type_id = None

    if 'd' in asset and asset['d']:
        serial_number = asset['d'].get('sn', None)
        device_type_id = asset['d'].get('dti', None)
        print()
        print(f"   Serial Number (d.sn): {serial_number}")
        print(f"   Device Type ID (d.dti): {device_type_id}")

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

    # Mostrar estructura completa del device
    print()
    print("   Estructura completa del Device:")
    for key, value in device.items():
        if key == '_id':
            continue
        if isinstance(value, bytes):
            print(f"     {key}: (bytes)")
        else:
            print(f"     {key}: {value}")
    print()
else:
    print("   No hay device asociado")
    device_id = None
    print()

# 3. Buscar conexiones recientes en puerto 7013 (GT06)
print("3. CONEXIONES RECIENTES EN PUERTO 7013 (GT06):")

# Primero buscar si hay conexiones con serial number del asset
if serial_number:
    print(f"   Buscando conexiones con serial: {serial_number}")

# Buscar todas las conexiones recientes en puerto 7013
connections = list(db.devices_connections.find(
    {"pp": 7013}
).sort("cd", -1).limit(5))

if connections:
    print(f"   Se encontraron {len(connections)} conexiones recientes en puerto 7013:")
    print()

    for i, conn in enumerate(connections, 1):
        print(f"   Conexión #{i}:")
        print(f"     ID: {conn['_id']}")
        print(f"     Fecha: {conn.get('cd', 'N/A')}")
        print(f"     IP: {conn.get('ip', 'N/A')}")
        print(f"     Puerto Protocolo: {conn.get('pp', 'N/A')}")

        # Verificar si está asociada a este device
        if 'md' in conn and conn['md'] and 'did' in conn['md']:
            conn_device_id = conn['md']['did']
            print(f"     Device ID de la conexión: {conn_device_id}")
            if device_id and conn_device_id == device_id:
                print(f"     ✓ MATCH: Esta conexión pertenece a este Asset!")

        # Analizar mensajes en la conexión
        if 'm' in conn and conn['m']:
            messages = conn['m']
            print(f"     Mensajes raw: {len(messages)}")

            # Mostrar primeros bytes del primer mensaje para debug
            if len(messages) > 0:
                first_msg = bytes(messages[0])
                hex_str = ' '.join([f'{b:02X}' for b in first_msg[:30]])
                print(f"     Primer mensaje: {hex_str}")

                # Intentar extraer IMEI de GT06
                if first_msg[0] == 0x78 and first_msg[1] == 0x78:
                    # Protocolo GT06
                    packet_len = first_msg[2]
                    if packet_len >= 13 and first_msg[3] == 0x01:  # Login packet
                        # IMEI en bytes 4-11 (8 bytes)
                        imei_bytes = first_msg[4:12]
                        # Convertir a string IMEI
                        imei = ''.join([f'{b:02X}' for b in imei_bytes])
                        print(f"     IMEI (GT06): {imei}")
                elif first_msg[0] == 0x7E:
                    # Podría ser JT/T 808
                    # Device ID está en bytes 5-10 (6 bytes BCD)
                    if len(first_msg) >= 11:
                        device_id_bytes = first_msg[5:11]
                        device_id_str = ''.join([f'{b:02X}' for b in device_id_bytes])
                        print(f"     Device ID (JT808): {device_id_str}")
        else:
            print(f"     Mensajes: 0")

        print()
else:
    print("   No se encontraron conexiones en puerto 7013")
    print()

# 4. Verificar mensajes guardados
print("4. MENSAJES GUARDADOS:")

if device_id:
    # Buscar por device_id
    messages = list(db.devices_messages.find(
        {"md.did": device_id}
    ).sort("cd", -1).limit(5))

    print(f"   Buscando mensajes del Device ID: {device_id}")
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
                print(f"       Válido: {pos.get('v', False)}")
            else:
                print(f"     Sin datos de ubicación")
            print()
    else:
        print("   No hay mensajes guardados aún")
        print()
else:
    print("   No hay Device asociado, no se pueden buscar mensajes")
    print()

# 5. Resumen
print("=" * 80)
print("RESUMEN:")
print("=" * 80)

if device_id and len(connections) > 0:
    # Verificar si alguna conexión pertenece a este device
    has_match = False
    for conn in connections:
        if 'md' in conn and conn['md'] and 'did' in conn['md']:
            if conn['md']['did'] == device_id:
                has_match = True
                break

    if has_match:
        print("STATUS: ✓ DISPOSITIVO CONECTADO")
        print(f"  - Asset configurado correctamente")
        print(f"  - Serial Number: {serial_number}")
        print(f"  - Device Type ID: {device_type_id}")
        if len(messages) > 0:
            print(f"  - Mensajes guardados: {len(messages)}")
            print("  - ✓ FUNCIONANDO CORRECTAMENTE")
        else:
            print(f"  - Mensajes guardados: 0")
            print("  - ⚠ Dispositivo conecta pero no envía ubicaciones")
    else:
        print("STATUS: ⚠ SIN MATCH")
        print(f"  - Asset configurado con Serial: {serial_number}")
        print(f"  - Hay conexiones en puerto 7013 pero no coinciden con este Asset")
        print(f"  - Verifica que el Serial Number sea correcto")
elif not device_id:
    print("STATUS: ⚠ SIN DEVICE")
    print(f"  - Asset existe pero no tiene Device asociado")
    print(f"  - Serial configurado: {serial_number}")
    print(f"  - Device Type: {device_type_id}")
    print(f"  - El device se creará en la primera conexión exitosa")
else:
    print("STATUS: ✗ SIN CONEXIONES")
    print(f"  - Asset y Device configurados")
    print(f"  - No hay conexiones recientes en puerto 7013")

print()
