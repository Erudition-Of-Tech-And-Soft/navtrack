#!/usr/bin/env python3
import sys
import time
from pymongo import MongoClient
from datetime import datetime
from bson import ObjectId

client = MongoClient("mongodb://31.97.146.1:27017")
db = client["navtrack"]

device_id = ObjectId("692a51accc7cfd0ee2d5b49e")
last_connection_id = None
last_message_count = 0

print("=" * 80)
print("MONITOREO EN TIEMPO REAL - PROTOCOLO W2J")
print("=" * 80)
print()
print("Esperando que el dispositivo se conecte...")
print("(Actualiza cada 10 segundos, presiona Ctrl+C para salir)")
print()

try:
    while True:
        # Verificar última conexión
        conn = db.devices_connections.find_one({"pp": 7053}, sort=[("cd", -1)])

        if conn:
            conn_id = conn["_id"]

            # Si es una nueva conexión
            if conn_id != last_connection_id:
                last_connection_id = conn_id

                print(f"\n[{datetime.now().strftime('%H:%M:%S')}] NUEVA CONEXION DETECTADA")
                print(f"  Connection ID: {conn_id}")
                print(f"  Fecha: {conn['cd']}")
                print(f"  IP: {conn.get('ip', 'N/A')}")

                if 'm' in conn and conn['m']:
                    messages = conn['m']
                    print(f"  Mensajes en conexion: {len(messages)}")

                    # Analizar tipos de mensaje
                    msg_types = []
                    has_location = False

                    for msg in messages:
                        msg_bytes = bytes(msg)
                        if len(msg_bytes) >= 3:
                            msg_id = (msg_bytes[1] << 8) | msg_bytes[2]

                            if msg_id == 0x0100:
                                msg_types.append("Registration")
                            elif msg_id == 0x0102:
                                msg_types.append("Authentication")
                            elif msg_id == 0x0002:
                                msg_types.append("Heartbeat")
                            elif msg_id == 0x0200:
                                msg_types.append("LOCATION REPORT")
                                has_location = True
                            elif msg_id == 0x0704:
                                msg_types.append("Batch Location")
                            else:
                                msg_types.append(f"0x{msg_id:04X}")

                    print(f"  Tipos: {' | '.join(msg_types)}")

                    if has_location:
                        print()
                        print("  *** SUCCESS! MENSAJE DE UBICACION DETECTADO! ***")
                        print()

        # Verificar mensajes guardados
        msg_count = db.devices_messages.count_documents({"md.did": device_id})

        if msg_count != last_message_count:
            last_message_count = msg_count

            print(f"\n[{datetime.now().strftime('%H:%M:%S')}] MENSAJES GUARDADOS: {msg_count}")

            if msg_count > 0:
                # Obtener último mensaje
                msg = db.devices_messages.find_one({"md.did": device_id}, sort=[("cd", -1)])

                if msg and 'pos' in msg and msg['pos']:
                    pos = msg['pos']
                    print(f"  Ultima ubicacion:")
                    print(f"    Latitud:  {pos.get('lat', 'N/A')}")
                    print(f"    Longitud: {pos.get('lon', 'N/A')}")
                    print(f"    Velocidad: {pos.get('spd', 'N/A')} km/h")
                    print(f"    Rumbo: {pos.get('hdg', 'N/A')}°")
                    print(f"    Fecha GPS: {pos.get('dt', 'N/A')}")
                    print(f"    Valido: {pos.get('v', False)}")
                    print()
                    print("=" * 80)
                    print("EL PROTOCOLO W2J ESTA FUNCIONANDO CORRECTAMENTE!")
                    print("=" * 80)
                    print()
                    print("Puedes verificar en la interfaz web que la ubicacion aparece en el mapa.")
                    print()
                    break

        # Esperar 10 segundos
        time.sleep(10)

except KeyboardInterrupt:
    print("\n\nMonitoreo detenido por el usuario.")
    print()

    # Mostrar resumen final
    msg_count = db.devices_messages.count_documents({"md.did": device_id})
    print(f"Resumen final:")
    print(f"  Mensajes guardados: {msg_count}")

    if msg_count > 0:
        print("  Status: FUNCIONANDO CORRECTAMENTE")
    else:
        print("  Status: Esperando que el dispositivo envie ubicaciones")
        print("          Asegurate de que el Listener este corriendo con el codigo actualizado")
