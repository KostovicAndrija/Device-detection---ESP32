# Taskovi realizacije

## 1. Taskovi za backend (.NET 9)

### 1.1 Domain
- [x] definisati entitete: Device, Observation, Session, Alert, WhitelistEntry
- [x] dodati value object-e za identifikatore i timestamp
- [x] definisati domen pravila za validaciju observation zapisa

### 1.2 Application
- [x] definirati use-case servise: ingest, process, scoring, whitelist, reporting
- [x] uvesti DTO modele i mapper-e bez curenja infrastrukturnih tipova
- [x] implementirati pipeline za obradu mjerenja po vremenskom prozoru

### 1.3 Infrastructure
- [x] podesiti EF Core + Npgsql + migracije
- [x] implementirati repository i query sloj
- [x] implementirati MQTT consumer i retry/logging mehanizam
- [x] implementirati hashiranje identifikatora (salt + pepper strategija)

### 1.4 Api
- [x] dovrsiti auth endpointe (login, refresh, revoke)
- [x] dovrsiti ingestion endpointe i validaciju request-a
- [x] dodati endpointe za sessions, alerts, devices i whitelist
- [x] dokumentovati API kroz OpenAPI/Swagger

### 1.5 Worker
- [x] subscribovati MQTT topic-e po senzoru
- [x] obraditi poruke i slati ih u processing pipeline
- [x] push rezultata ka SignalR hub-u

### 1.6 Testovi
- [x] unit testovi za scoring i localization logiku
- [ ] integration testovi za API + baza
- [ ] testovi idempotentnosti ingestije

## 2. Taskovi za frontend (Angular 20)

### 2.1 Core i app shell
- [x] podesiti auth guard i token storage strategiju
- [x] centralizovati API klijente u core servise
- [x] podesiti global error handling i user feedback

### 2.2 Feature moduli
- [x] dashboard ekran sa glavnim KPI metrikama
- [x] floor-map prikaz uredjaja (putanje ostaju za naprednu verziju)
- [x] device lista sa pretragom i filterima
- [x] alert panel sa prioritetima i potvrdom obrade
- [x] whitelist manager (CRUD)
- [x] reports stranica (pregled sesija i CSV izvoz)

### 2.3 State management
- [x] koristiti Angular signals za lokalno i feature stanje
- [x] koristiti computed signale za derived prikaze
- [x] koristiti effect samo za side-effect scenarije

### 2.4 Testovi
- [ ] component testovi za kriticne UI dijelove
- [ ] service testovi za API i SignalR integracije
- [ ] smoke e2e scenariji za exam dashboard tok

## 3. DevOps i operativa
- [x] docker-compose za postgres + mqtt + backend + frontend
- [x] environment varijable i secrets politika
- [x] CI koraci: build i test
- [ ] backup i retention strategija za logove i alarm istoriju

## 4. Prioriteti
- P0: ingestion, baza, osnovni dashboard, alerting
- P1: napredna lokalizacija i whitelist workflow
- P2: izvjestaji, tuning algoritama i operativni dashboard
