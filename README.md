# ESP32 Device Detection

Uputstvo za nove funkcije: [učionice, istorija sesija i proširenja senzora i AI modela](docs/ucionice-istorija-i-prosirenja.md).

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

## Simulacija ESP32 senzora

### Registracija na stranici „Mapa učionice“

Profesor prvo otvara **Mapa učionice**, bira učionicu i pokreće registraciono
skeniranje. Na stranici se prikazuju plan učionice i ID skeniranja. Simulator se
zatim pokreće sa tim ID-em i emituje uređaj sa zadate pozicije:

```powershell
docker compose --profile tools run --rm --build simulator registration --session <id-skeniranja> --x 2.5 --y 4
```

Marker uređaja pojavljuje se na mapi. Profesor označava pronađeni uređaj, unosi
naziv i potvrđuje registraciju. Uređaj se nakon toga automatski whitelistuje u
svakoj sesiji koju taj profesor ili asistent pokrene.

Posle potvrde pokrenuti regularnu sesiju, kopirati njen ID iz URL-a mape i poslati
veliku simulaciju u tu sesiju:

```powershell
docker compose --profile tools run --rm --build simulator demo --session <id-sesije>
```

Prvi uređaj u velikoj simulaciji je registrovani `SIM-MY-DEVICE-E2E`, dok su
preostali uređaji neregistrovani. Tako se u jednom toku vide i dozvoljeni uređaj
i uređaji koji treba da izazovu upozorenja.

Kompletan MQTT end-to-end test bez fizičkih senzora:

```powershell
docker compose --profile tools run --rm --build simulator suite
```

Simulator automatski kreira i pokreće test sesiju, generiše normalne, rizične,
whitelist, lokalizacione i nevalidne događaje i proverava alarme, aktivne uređaje
i poziciju dobijenu sa tri senzora. Pojedinačni scenariji i load-test opcije su
opisani u [simulator/README.md](simulator/README.md).

Detaljno objašnjenje arhitekture, toka MQTT poruke, lokalizacije, animacije mape
i svih načina simulacije nalazi se u
[docs/simulacija-senzora-i-tok-sistema.md](docs/simulacija-senzora-i-tok-sistema.md).

Kontinuirani demo za mapu učionice, sa uređajima koji se pojavljuju, kreću,
postepeno nestaju i ponovo aktiviraju:

```powershell
docker compose --profile tools run --rm --build simulator demo
```

Mapa prikazuje status svakog senzora, uvećane indikatore nepoznatih uređaja i
kritične klupe izračunate iz 15-sekundnih intervala. Za demonstraciju samo
heartbeat-a senzora, bez saobraćaja uređaja, pokrenuti:

```powershell
docker compose --profile tools run --rm --build simulator sensors --room A-0-15 --duration 900
```

`demo` scenario automatski šalje i heartbeat poruke. Praćenje se ne može
pokrenuti dok sva tri senzora nisu aktivna.

### Podešavanje kriterijuma kritične klupe

Kriterijumi se menjaju u sekciji `DeskRisk` datoteke
`backend/src/Api/appsettings.json`. Formula je `R = 100 × max(A_tip / N)`, gde
je `A_tip` broj aktivnih intervala iste vrste signala kod iste klupe, a `N`
ukupan broj intervala u prozoru. Više identifikatora ili poruka u istom intervalu
računa se jednom. Podrazumevano se posmatra 5 intervala od 15 sekundi; prag
kritičnosti je 60 (3/5), a sumnjive klupe 40 (2/5). Prihvataju se samo
neodobreni signali potvrđeni sa najmanje 2 senzora, uz pouzdanost lokacije ≥35%,
udaljenost od klupe ≤0,75 m i razmak očitavanja senzora ≤5 sekundi. Identifikator
se koristi za povezivanje očitavanja različitih senzora, ne za povećanje rizika.
Broj identifikatora ne predstavlja pouzdan broj fizičkih uređaja.
Na mapi odeljak „Podešavanja detekcije” učitava početne vrednosti iz JSON-a.
Korisnik menja trajanje i broj intervala i pragove; primer odmah prikazuje
trajanje prozora i broj aktivnih intervala potreban za svaku oznaku. Dugme
„Primeni na prikaz” ponovo računa klupe na serveru iz sačuvanih očitavanja.
Promene važe za otvoreni prikaz, ne menjaju globalnu konfiguraciju ni alarme
za uređaje. „Vrati početna podešavanja” vraća vrednosti učitane iz JSON-a.
Reprodukcija koristi istorijski trenutak i iste izabrane kriterijume.
Za trajnu promenu početnih vrednosti izmeniti JSON. Posle izmene JSON-a treba
ponovo pokrenuti API; kod Docker pokretanja potrebno je ponovo izgraditi API
image.

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

## Profesor, asistent i lični uređaji

Profesor kreira nalog asistenta na stranici „Asistenti”. Asistent registruje svoje uređaje na stranici „Mapa učionice” kratkim skeniranjem i potvrdom izbora. Oni se automatski dodaju na listu dozvoljenih uređaja njegovih narednih sesija. Asistent vidi i kontroliše svoje sesije. Detalji postupka i povezivanja sa simulatorom nalaze se u [uputstvu za asistenta](docs/asistent-i-registracija-uredjaja.md).

Prošireni završni rad i uputstvo za dopunu slika nalaze se u [docs/zavrsni-rad](docs/zavrsni-rad/PROCITAJ.md).
