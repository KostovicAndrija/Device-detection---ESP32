# ESP32 sensor simulator

Simulator publishes the same MQTT payloads as the ESP32 sensors to
`sensors/{sensorId}/rssi`. It can create and start its own exam session through
the API, so no IDs have to be copied manually.

## Full end-to-end suite

With the main stack running:

```powershell
docker compose --profile tools run --rm --build simulator suite
```

The suite covers low-risk traffic, a whitelisted device, a high-risk Bluetooth
pairing event, three-sensor localization, duplicate event IDs, RSSI boundaries,
unknown sensors, old/future timestamps, omitted optional fields, malformed JSON,
missing required fields, and out-of-range RSSI values. Expected validation
errors are visible in the worker logs:

```powershell
docker compose logs --tail 100 worker
```

## Individual scenarios

### Manual registration on the classroom map

Log in, open **Classroom map**, select a classroom and click the
registration scan button. Copy the registration ID shown on that page, then run:

```powershell
docker compose --profile tools run --rm --build simulator registration --session <registration-id> --x 2.5 --y 4
```

The simulator publishes the same device from every configured classroom sensor.
The application shows the candidate and its estimated position on the classroom
map. Enter a label and confirm it on **Classroom map**. The simulator detects the
confirmation and exits successfully. `--device`, `--signal`, `--duration` and
`--interval` can be used to change the simulated device and stream.

After confirming the device, start a normal exam session from the dashboard, copy
the session ID from the map URL, and emit the same identifier during monitoring:

```powershell
docker compose --profile tools run --rm --build simulator registered-device --session <exam-session-id> --device SIM-MY-DEVICE-E2E --x 2.5 --y 4
```

The value passed to `--device` must be identical to the value used during the
registration simulation. This command defaults to `SIM-MY-DEVICE-E2E`, runs for
five minutes and does not stop the exam session when it exits.

### Separate professor and assistant demos

```powershell
docker compose --profile tools run --rm --build simulator demo-professor
docker compose --profile tools run --rm --build simulator demo-assistant
```

Both use the same five-device movement and channel output. Professor defaults to
`UC-101`; assistant defaults to `UC-202`, so both can run simultaneously. Override
the room with `--room UC-101`. An existing active session must be stopped before
starting a new one in that room, or use `--session <id>` for an accessible active
session. Duration remains 900 seconds and can be changed with `--duration`.

The professor command uses `APP_USERNAME` / `APP_PASSWORD`. The assistant command
uses `ASSISTANT_USERNAME` / `ASSISTANT_PASSWORD`. Local demo defaults are
`demo-asistent` / `AsistentDemo2026!`; these can be overridden in the root `.env`.
On first use the professor creates that assistant through the public API. Existing
account passwords and roles are never reset. If an existing username has a different
password, supply its correct credentials or choose a new demo username.

The assistant command first registers its own synthetic device through an isolated
registration scan, waits for ingestion, and confirms the sole observed candidate.
Multiple candidates cause cancellation rather than automatic registration. Subsequent
runs reuse the saved registration. The first moving device is this personal device,
automatically whitelisted by the server. The assistant never calls professor-only
manual whitelist endpoints. Complete any other registration or active monitoring
before first-time setup. This automation applies to a known synthetic demo device;
real device registration still requires the user's selection in the application.

Log in as `demo-asistent` to see its sessions. Professors see both sets of sessions.
Other assistants cannot see this account's sessions. Ending the stream, including
Ctrl+C, does not stop the session; stop monitoring in the application when finished.
The original `demo` command remains available with its original behavior.

```powershell
docker compose --profile tools run --rm simulator normal
docker compose --profile tools run --rm simulator high-risk
docker compose --profile tools run --rm simulator whitelist
docker compose --profile tools run --rm simulator localization
docker compose --profile tools run --rm simulator edge-cases
docker compose --profile tools run --rm simulator load --devices 100 --count 1000 --rate 50
```

For a live classroom presentation, run a fifteen-minute stream with up to five devices.
They gradually appear and move inside separate classroom zones so their markers remain easy to distinguish:

```powershell
docker compose --profile tools run --rm --build simulator demo
```

To use a session already started from the application, only pass its ID:

```powershell
docker compose --profile tools run --rm --build simulator demo --session <exam-session-id>
```

The first device in this stream is `SIM-MY-DEVICE-E2E`, the same default identifier
used by the **Classroom map** registration simulator. It is treated as allowed only if
it was registered before the exam session started. The other simulated devices remain
unregistered so the same stream demonstrates both allowed and suspicious traffic.

The demo console mirrors the ESP32 serial channel-hopping output (`Sniffing channel 1` through
`Sniffing channel 13`). This is diagnostic serial output only; simulated detections are still
published as JSON to `sensors/{sensorId}/rssi`, matching the backend MQTT contract.

Each command creates a test session by default. To publish into an existing
session, add `--session <guid>`. `APP_USERNAME` and `APP_PASSWORD` from the root
`.env` file are used for automatic API login.

## Run outside Docker

```powershell
cd simulator
npm install
$env:MQTT_URL = "mqtt://localhost:1883"
$env:API_URL = "http://localhost:7108"
npm start -- suite
```
