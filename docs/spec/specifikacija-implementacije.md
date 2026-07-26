# Specifikacija implementacije

## 1. Opseg
Ova specifikacija definise kako se sistem implementira na osnovu postavljene strukture repozitorija i arhitekture po slojevima.

## 2. Arhitektura po komponentama

### 2.1 Backend
- `backend/src/Domain`: cisti domen modeli i pravila
- `backend/src/Application`: use-case logika i pipeline orkestracija
- `backend/src/Infrastructure`: pristup bazi, MQTT, vanjski adapteri
- `backend/src/Api`: REST endpointi, auth, SignalR hub
- `backend/src/Worker`: background obrada i ingestion

Arhitekturni stil: modularni monolit sa jasnim granicama slojeva.

### 2.2 Frontend
- `frontend/src/app/core`: servisi, interseptori, auth i shared infrastruktura
- `frontend/src/app/features`: funkcionalnosti (dashboard, alerts, devices, whitelist, reports)
- `frontend/src/app/layouts`: shell i layout komponente

Arhitekturni stil: feature-based Angular aplikacija sa signal-first state pristupom.

## 3. Podaci i skladistenje

### 3.1 Primarne tabele
- `devices` (hash_id, first_seen, last_seen, type)
- `device_observations` (device_id, sensor_id, rssi, signal_type, observed_at, raw_meta)
- `exam_sessions` (name, room_id, starts_at, ends_at, status)
- `alerts` (session_id, device_id, risk_score, reason, created_at, acknowledged_at)
- `whitelist_entries` (session_id, student_ref, device_hash, valid_from, valid_to)

### 3.2 Pravila privatnosti
- trajno cuvati samo hashirani identifikator
- retention za sirove logove ograniciti na 24h nakon sesije
- audit tragovi za pristup alarmima i whitelist izmjenama

## 4. API specifikacija (minimalni skup)

### 4.1 Ingestion
- `POST /api/ingestion/observation`
- `POST /api/ingestion/batch`

### 4.2 Monitoring
- `GET /api/devices/active`
- `GET /api/sessions/{id}/alerts`
- `GET /api/sessions/{id}/positions`

### 4.3 Session i whitelist
- `POST /api/sessions`
- `POST /api/sessions/{id}/start`
- `POST /api/sessions/{id}/stop`
- `GET /api/sessions/{id}/whitelist`
- `POST /api/sessions/{id}/whitelist`
- `DELETE /api/sessions/{id}/whitelist/{entryId}`

### 4.4 Real-time kanal
- SignalR hub: `/hubs/monitoring`
- Eventi: `deviceUpdated`, `alertCreated`, `alertAcknowledged`, `sessionStateChanged`

## 5. Algoritamski tok
1. ESP32 salje observation poruku
2. worker primi i validira payload
3. observation se normalizuje i upisuje
4. agregator grupise mjerenja po vremenskom prozoru
5. localization servis racuna poziciju i confidence
6. anomaly servis racuna risk score
7. rezultat se upisuje i push-uje preko SignalR

## 6. Nefunkcionalni zahtjevi
- dostupnost sistema tokom ispita >= 99% za trajanje sesije
- latency od ingestion do UI prikaza <= 2 sekunde
- autentikacija za sve profesorske i admin endpointe
- detaljno logovanje gresaka bez cuvanja osjetljivih podataka

## 7. Strategija testiranja
- unit test: scoring, localization, filteri
- integration test: API + Postgres + SignalR
- end-to-end: osnovni tok sesije od detekcije do alarma

## 8. Kriterijumi prihvatanja
- aktivni uredjaji i alerti su vidljivi u realnom vremenu
- whitelist pravila uticu na score i tip alarma
- report sesije prikazuje istoriju i trendove detekcija
- privatnosna pravila i retention su primijenjeni
