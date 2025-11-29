#!/usr/bin/env python3
import sys
from pymongo import MongoClient
from datetime import datetime
from bson import ObjectId

# Conectar a MongoDB
client = MongoClient("mongodb://31.97.146.1:27017")
db = client["navtrack"]

print("=" * 80)
print("VALIDACIÓN COMPLETA DE LA BASE DE DATOS - PROTOCOLO W2J")
print("=" * 80)

# 1. Verificar Asset
print("\n1. VERIFICACIÓN DEL ASSET")
print("-" * 80)
asset_id = ObjectId("69246acd54413d1c0bb50ee5")
asset = db.assets.find_one({"_id": asset_id})

if asset:
    print(f"✅ Asset encontrado: {asset['name']}")
    print(f"   Asset ID: {asset['_id']}")

    if 'device' in asset and asset['device']:
        device = asset['device']
        print(f"\n   Device configurado:")
        print(f"   - Device ID: {device.get('_id', 'N/A')}")
        print(f"   - Serial Number: {device.get('serialNumber', 'N/A')}")
        print(f"   - Device Type ID: {device.get('deviceTypeId', 'N/A')}")
        print(f"   - Protocol Port: {device.get('protocolPort', 'N/A')}")

        serial_number = device.get('serialNumber')
        protocol_port = device.get('protocolPort')
        device_id_obj = device.get('_id')
    else:
        print("   ❌ ERROR: Asset no tiene Device configurado")
        sys.exit(1)
else:
    print(f"❌ ERROR: Asset no encontrado")
    sys.exit(1)

# 2. Verificar conexiones en el puerto 7053
print("\n2. CONEXIONES EN PUERTO 7053 (Últimas 5)")
print("-" * 80)
connections = list(db.devices_connections.find({"pp": 7053}).sort("cd", -1).limit(5))

if connections:
    print(f"✅ Encontradas {len(connections)} conexiones recientes")

    for idx, conn in enumerate(connections, 1):
        print(f"\n   Conexión #{idx}:")
        print(f"   - Connection ID: {conn['_id']}")
        print(f"   - Fecha: {conn.get('cd', 'N/A')}")
        print(f"   - IP: {conn.get('ip', 'N/A')}")
        print(f"   - Puerto: {conn.get('pp', 'N/A')}")

        if 'm' in conn and conn['m']:
            messages = conn['m']
            print(f"   - Mensajes: {len(messages)}")

            # Analizar primer mensaje
            if len(messages) > 0:
                first_msg = bytes(messages[0])
                hex_msg = ' '.join(f'{b:02X}' for b in first_msg)
                print(f"   - Primer mensaje (hex): {hex_msg[:60]}...")

                # Extraer Device ID del mensaje (bytes 5-10 después de quitar 0x7E inicial)
                if len(first_msg) >= 12 and first_msg[0] == 0x7E:
                    # Mensaje con delimitador 0x7E al inicio
                    # Estructura: 7E [MsgID 2B] [BodyAttr 2B] [DeviceID 6B] ...
                    # Device ID está en posición 5-10 (después del 0x7E inicial)
                    device_id_bytes = first_msg[5:11]

                    # Convertir BCD a string
                    device_id_bcd = ''.join(f'{(b >> 4)}{(b & 0x0F)}' for b in device_id_bytes)
                    device_id_trimmed = device_id_bcd.lstrip('0')

                    print(f"   - Device ID del mensaje (BCD completo): {device_id_bcd}")
                    print(f"   - Device ID del mensaje (sin leading zeros): {device_id_trimmed}")

                    # Verificar si coincide con el Asset
                    if device_id_trimmed == serial_number:
                        print(f"   - ✅ Device ID COINCIDE con Serial Number del Asset")
                    else:
                        print(f"   - ❌ Device ID NO COINCIDE:")
                        print(f"      Mensaje: {device_id_trimmed}")
                        print(f"      Asset:   {serial_number}")

                # Identificar tipo de mensaje
                if len(first_msg) >= 3:
                    msg_id = (first_msg[1] << 8) | first_msg[2]
                    msg_type = {
                        0x0100: "Terminal Registration (0x0100)",
                        0x0102: "Terminal Authentication (0x0102)",
                        0x0002: "Terminal Heartbeat (0x0002)",
                        0x0200: "Location Report (0x0200)",
                        0x0704: "Batch Location Report (0x0704)"
                    }.get(msg_id, f"Unknown (0x{msg_id:04X})")
                    print(f"   - Tipo de mensaje: {msg_type}")
else:
    print("❌ No se encontraron conexiones en el puerto 7053")

# 3. Verificar mensajes guardados
print("\n3. MENSAJES GUARDADOS DEL DISPOSITIVO")
print("-" * 80)
message_count = db.devices_messages.count_documents({"md.did": device_id_obj})
print(f"Total de mensajes guardados: {message_count}")

if message_count > 0:
    print("✅ Hay mensajes guardados")

    # Obtener últimos 3 mensajes
    messages = list(db.devices_messages.find({"md.did": device_id_obj}).sort("cd", -1).limit(3))

    for idx, msg in enumerate(messages, 1):
        print(f"\n   Mensaje #{idx}:")
        print(f"   - Message ID: {msg['_id']}")
        print(f"   - Fecha creación: {msg.get('cd', 'N/A')}")
        print(f"   - Connection ID: {msg.get('cid', 'N/A')}")

        if 'pos' in msg and msg['pos']:
            pos = msg['pos']
            print(f"   - Position:")
            print(f"     • Latitud: {pos.get('lat', 'N/A')}")
            print(f"     • Longitud: {pos.get('lon', 'N/A')}")
            print(f"     • Fecha GPS: {pos.get('dt', 'N/A')}")
            print(f"     • Válido: {pos.get('v', 'N/A')}")
            print(f"     • Velocidad: {pos.get('spd', 'N/A')}")
            print(f"     • Rumbo: {pos.get('hdg', 'N/A')}")
        else:
            print(f"   - ⚠️ Sin datos de posición")

        if 'md' in msg and msg['md']:
            md = msg['md']
            print(f"   - Metadata:")
            print(f"     • Asset ID: {md.get('aid', 'N/A')}")
            print(f"     • Device ID: {md.get('did', 'N/A')}")
else:
    print("⚠️ NO hay mensajes guardados - El dispositivo NO está enviando ubicaciones")
    print("   Posibles causas:")
    print("   1. El dispositivo solo envía mensajes de registro (0x0100)")
    print("   2. El dispositivo no recibe respuestas válidas del servidor")
    print("   3. El Serial Number no coincide con el Device ID del mensaje")

# 4. Buscar Asset con diferentes variantes del Device ID
print("\n4. BÚSQUEDA DE ASSET POR SERIAL NUMBER")
print("-" * 80)

# Extraer Device ID de la última conexión
if connections and 'm' in connections[0] and connections[0]['m']:
    first_msg = bytes(connections[0]['m'][0])
    if len(first_msg) >= 12 and first_msg[0] == 0x7E:
        device_id_bytes = first_msg[5:11]
        device_id_bcd = ''.join(f'{(b >> 4)}{(b & 0x0F)}' for b in device_id_bytes)
        device_id_trimmed = device_id_bcd.lstrip('0')

        test_serials = [
            device_id_trimmed,
            device_id_bcd,
            serial_number
        ]

        for test_serial in set(test_serials):
            found = db.assets.find_one({
                "device.serialNumber": test_serial,
                "device.protocolPort": 7053
            })

            if found:
                print(f"✅ Asset encontrado con Serial Number: '{test_serial}'")
                print(f"   Asset ID: {found['_id']}")
                print(f"   Asset Name: {found['name']}")
            else:
                print(f"❌ NO encontrado con Serial Number: '{test_serial}'")

# 5. Resumen y recomendaciones
print("\n5. RESUMEN Y RECOMENDACIONES")
print("=" * 80)

if message_count > 0:
    print("✅ TODO ESTÁ BIEN - El dispositivo está funcionando correctamente")
    print("   - Asset configurado correctamente")
    print("   - Device ID coincide")
    print("   - Mensajes se están guardando")
else:
    print("⚠️ PROBLEMA DETECTADO - El dispositivo NO está enviando ubicaciones")
    print("\nAcciones requeridas:")
    print("1. Verificar que el Listener esté ejecutándose con el código actualizado")
    print("2. Reiniciar el Listener para que cargue las correcciones")
    print("3. El dispositivo debe registrarse nuevamente después del reinicio")
    print("4. Verificar que el Serial Number en el Asset sea: '{}'".format(
        device_id_trimmed if 'device_id_trimmed' in locals() else serial_number))

print("\n" + "=" * 80)
