# MQTT ingestion ugovor

## Topic

Worker se podrazumevano pretplaćuje na:

```text
sensors/+/rssi
```

Preporučeni topic po senzoru:

```text
sensors/{sensorId}/rssi
```

Primer: `sensors/S1/rssi`.

## Payload

```json
{
  "eventId": "S1-1721980800000-a1b2c3",
  "deviceIdentifier": "AA:BB:CC:DD:EE:FF",
  "sensorId": "S1",
  "sessionId": "9e7f06ae-73cb-44e8-bcbf-5df7447fbc61",
  "signalType": "wifi",
  "rssi": -62,
  "timestamp": "2026-07-26T12:00:00Z"
}
```

Pravila:

- `eventId` mora biti jedinstven i služi za idempotentnost QoS 1 poruka.
- `deviceIdentifier` se hashira pre trajnog čuvanja.
- `sensorId` mora odgovarati senzoru iz `Localization:Sensors`.
- `sessionId` treba da bude GUID aktivne sesije.
- `signalType`: `wifi`, `ble`, `bluetooth` ili `bluetooth_pairing`.
- `rssi`: od `-120` do `0` dBm.
- `timestamp`: UTC ISO-8601 vreme sa sinhronizovanog senzora.

MQTT QoS je `AtLeastOnce`. Duplirana poruka sa istim `eventId` se ignoriše.
