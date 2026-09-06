# ESP32 Device Detection

Sistem za pasivnu detekciju Wi-Fi/BLE aktivnosti tokom ispitnih sesija. ESP32 senzori šalju RSSI metapodatke kroz MQTT, .NET worker ih obrađuje i čuva u PostgreSQL, a Angular dashboard prikazuje uređaje, alarme i procenjene pozicije u realnom vremenu.

## Komponente

- `backend/src/Api` — REST API, JWT autentikacija, Swagger i SignalR
- `backend/src/Worker` — MQTT subscriber i ingestion pipeline
- `backend/src/Application` — sessions, risk scoring, WLS lokalizacija, Kalman smoothing i reporting
- `backend/src/Infrastructure` — PostgreSQL/EF Core, repositories i hashiranje identifikatora
- `frontend` — Angular dashboard za profesora

## Najbrže pokretanje

Ako je Docker instaliran:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Zatim otvoriti:

- aplikacija: http://localhost:4200
- API/Swagger: http://localhost:7108/swagger
- razvojni login: `admin` / `admin` (promeniti kroz `.env`)

## Lokalno pokretanje bez Dockera

Potrebni su PostgreSQL 16+, Mosquitto, .NET 9 i Node.js 22.

```powershell
cd backend
dotnet run --project src/Api/Api.csproj
```

U drugom terminalu:

```powershell
cd backend
dotnet run --project src/Worker/Worker.csproj
```

U trećem terminalu:

```powershell
cd frontend
npm install
npm start
```

Angular development server prosleđuje `/api` i `/hubs` na `https://localhost:7108`.

## Verifikacija

```powershell
cd backend
dotnet build DeviceDetection.slnx
dotnet test DeviceDetection.slnx

cd ../frontend
npm run build
npx tsc -p tsconfig.spec.json --noEmit
```

## Konfiguracija i tajne

Produkcione vrednosti ne treba čuvati u repozitorijumu. Koristiti environment varijable:

- `ConnectionStrings__DefaultConnection`
- `Jwt__SigningKey`
- `Hashing__Pepper`
- `DevelopmentUser__Username`
- `DevelopmentUser__Password`
- `Mqtt__Host`
- `Monitoring__HubUrl`

MQTT ugovor je opisan u [docs/spec/mqtt-contract.md](docs/spec/mqtt-contract.md).
