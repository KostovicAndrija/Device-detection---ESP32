# Device-detection---ESP32

Ovaj repozitorij sada sadrži početnu arhitekturu sistema iz zahtjeva:

- **backend/**
  - `src/Api` (ASP.NET Core Web API + SignalR hub + auth i ingestion endpointi)
  - `src/Application` (use-case sloj i ingesting pipeline apstrakcije)
  - `src/Domain` (domen entiteti)
  - `src/Infrastructure` (infrastrukturne implementacije pipeline-a)
  - `src/Worker` (BackgroundService za ingestiju MQTT/RSSI poruka)
  - `tests/UnitTests`
  - `tests/IntegrationTests`
- **frontend/**
  - Angular 20 aplikacija sa standalone komponentama
  - feature-based struktura (`dashboard`, `floor-map`, `devices`, `alerts`, `whitelist`, `reports`)
  - `core` servisni sloj i `layouts`

## Pokretanje

### Backend

```bash
dotnet build /home/runner/work/Device-detection---ESP32/Device-detection---ESP32/backend/DeviceDetection.slnx
dotnet test /home/runner/work/Device-detection---ESP32/Device-detection---ESP32/backend/DeviceDetection.slnx
```

### Frontend

```bash
cd /home/runner/work/Device-detection---ESP32/Device-detection---ESP32/frontend
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```
