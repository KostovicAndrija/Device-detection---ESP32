# Plan realizacije sistema

## 1. Cilj plana
Cilj je da se postojece inicijalno postavljen projekat razvije do stabilnog MVP-a i zatim do verzije spremne za ispitne sesije u realnom okruzenju.

## 2. Predlozeni tehnoloski pravac
- Backend: .NET 9 (ASP.NET Core Web API + SignalR + Worker)
- Baza: PostgreSQL 16+
- Frontend: Angular 20 (standalone komponente + signals)
- Integracija senzora: MQTT broker (npr. Mosquitto) izmedju ESP32 i backenda

## 3. Fazni plan

### Faza 0 - Uskladjivanje osnove (1-2 dana)
- potvrditi strukturu solution-a i dependency granice izmedju `Domain`, `Application`, `Infrastructure`, `Api`, `Worker`
- dogovoriti naming, coding style i branching pravila
- definisati format poruke koju ESP32 salje (Wi-Fi, BLE, BT classic)

Isporuka:
- potvrden tehnicki ugovor (payload schema + topic naming)

### Faza 1 - Data ingestion i cuvanje (4-6 dana)
- implementirati MQTT subscriber u `Worker`
- validirati i normalizovati ulazne poruke
- hashirati identifikatore uredjaja prije trajnog cuvanja
- upisivati observation dogadjaje u PostgreSQL

Isporuka:
- stabilan ingestion pipeline sa idempotentnim upisom

### Faza 2 - Obrada RSSI i lokalizacija (5-8 dana)
- agregacija mjerenja po vremenskim prozorima
- primjena RSSI->distance modela
- weighted least squares procjena pozicije
- Kalman smoothing po uredjaju

Isporuka:
- servis koji vraca procijenjenu poziciju i confidence

### Faza 3 - Pravila rizika i alerting (4-6 dana)
- anomaly pravila (nepoznat uredjaj, blizina, neocekivana zona, pairing pokusaj)
- racunanje risk score (0-100)
- pragovi upozorenja i eskalacija
- push notifikacije ka Angular klijentu preko SignalR

Isporuka:
- real-time alert panel sa istorijom alarma

### Faza 4 - Frontend dashboard (5-8 dana)
- exam dashboard i routing
- floor map sa prikazom uredjaja i tragom kretanja
- device list i filteri
- whitelist management i reports
- stanje aplikacije preko Angular signals

Isporuka:
- funkcionalan UI za profesora tokom ispitne sesije

### Faza 5 - Kvalitet i bezbjednost (4-6 dana)
- unit i integration testovi (backend)
- component i service testovi (frontend)
- audit log i retention politika (24h nakon ispita)
- performance test ingestion toka

Isporuka:
- release candidate spreman za pilot test

## 4. Milestones
- M1: ingestion + baza rade end-to-end
- M2: lokalizacija i confidence dostupni kroz API
- M3: risk score i real-time alerting aktivni
- M4: kompletan dashboard sa whitelist i report modulom
- M5: stabilna pilot verzija u ucionici

## 5. Kljucni rizici i mitigacije
- MAC randomization: oslanjati se na kratkorocne sesijske obrasce i multi-signal korelaciju
- RSSI sum: kalibracija po ucionici + Kalman + robust weighting
- vremenska nesinhronizacija senzora: sinkronizacija vremena i tolerancija prozora
- false positives: podesivi pragovi i postepena validacija pravila

## 6. Definicija uspjeha
- sistem prikazuje aktivne uredjaje i njihovu procijenjenu zonu u realnom vremenu
- alerting kasnjenje od detekcije do prikaza <= 2s u lokalnoj mrezi
- validirana privatnost (metapodaci bez payload presretanja)
- svi glavni use-case-ovi pokriveni testovima i dokumentacijom
