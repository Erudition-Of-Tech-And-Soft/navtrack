#!/usr/bin/env python3

# Simular extracción de IMEI del protocolo Concox

# Mensaje recibido
msg_hex = "78 78 0D 01 00 00 01 84 04 22 83 23 00 03 67 86 0D 0A"
msg_bytes = bytes.fromhex(msg_hex.replace(" ", ""))

print("=" * 80)
print("ANALISIS DE IMEI CONCOX/GT06")
print("=" * 80)
print()

print("Mensaje completo (hex):", msg_hex)
print("Mensaje completo (bytes):", [f"{b:02X}" for b in msg_bytes])
print()

# Verificar que es login packet
start_bit = msg_bytes[0]
extended = (start_bit == 0x79)

def get_index(index):
    return (index + 1) if extended else index

print(f"Start bit: 0x{start_bit:02X}")
print(f"Extended packet: {extended}")
print(f"Packet length: {msg_bytes[2]}")
print(f"Protocol number: 0x{msg_bytes[get_index(3)]:02X} (0x01 = Login)")
print()

# Extraer IMEI (índices 4-12 en el código original)
# Eso significa 8 bytes desde índice 4
imei_bytes = msg_bytes[get_index(4):get_index(12)]
print(f"IMEI bytes (índices {get_index(4)} a {get_index(12)-1}):", [f"{b:02X}" for b in imei_bytes])

# Convertir a string hexadecimal
imei_hex = ''.join([f"{b:02X}" for b in imei_bytes])
print(f"IMEI (hex string): {imei_hex}")

# Aplicar lógica del código: quitar 1 cero del inicio
if imei_hex.startswith("0"):
    imei_trimmed_once = imei_hex[1:]
else:
    imei_trimmed_once = imei_hex

print(f"IMEI después de quitar 1 '0': {imei_trimmed_once}")

# TrimStart agresivo (quitar todos los ceros)
imei_trimmed_all = imei_hex.lstrip('0')
print(f"IMEI después de TrimStart('0'): {imei_trimmed_all}")
print()

# Verificar contra serial del Asset
asset_serial = "18404228323"
print("Serial Number del Asset:", asset_serial)
print()

print("COMPARACION:")
print(f"  IMEI (original):      '{imei_hex}' == '{asset_serial}' ? {imei_hex == asset_serial}")
print(f"  IMEI (quita 1 '0'):   '{imei_trimmed_once}' == '{asset_serial}' ? {imei_trimmed_once == asset_serial}")
print(f"  IMEI (TrimStart):     '{imei_trimmed_all}' == '{asset_serial}' ? {imei_trimmed_all == asset_serial}")
print()

if imei_trimmed_all == asset_serial:
    print("✓ MATCH! Con TrimStart('0') el IMEI coincide con el Serial Number del Asset")
else:
    print("✗ NO MATCH - Hay un problema con la extracción del IMEI")

print()
print("=" * 80)
print("RECOMENDACION:")
print("=" * 80)
if imei_hex.startswith("0") and imei_trimmed_all == asset_serial:
    print("Cambiar el código de ConcoxMessageHandler.cs línea 128-131:")
    print()
    print("  ACTUAL:")
    print('    if (imei.StartsWith("0"))')
    print("    {")
    print("        imei = imei[1..];  // Solo quita 1 cero")
    print("    }")
    print()
    print("  RECOMENDADO:")
    print("    imei = imei.TrimStart('0');  // Quita todos los ceros al inicio")
print()
