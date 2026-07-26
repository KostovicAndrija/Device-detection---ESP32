# Taskovi realizacije

## 1. Taskovi za backend (.NET 9)

### 1.1 Domain
- [ ] definisati entitete: Device, Observation, Session, Alert, WhitelistEntry
- [ ] dodati value object-e za identifikatore i timestamp
- [ ] definisati domen pravila za validaciju observation zapisa

### 1.2 Application
- [ ] definirati use-case servise: ingest, process, scoring, whitelist, reporting
- [ ] uvesti DTO modele i mapper-e bez curenja infrastrukturnih tipova
- [ ] implementirati pipeline za obradu mjerenja po vremenskom prozoru

### 1.3 Infrastructure
- [ ] podesiti EF Core + Npgsql + migracije
- [ ] implementirati repository i query sloj
- [ ] implementirati MQTT consumer i retry/logging mehanizam
- [ ] implementirati hashiranje identifikatora (salt + pepper strategija)

### 1.4 Api
- [ ] dovrsiti auth endpointe (login, refresh, revoke)
- [ ] dovrsiti ingestion endpointe i validaciju request-a
- [ ] dodati endpointe za sessions, alerts, devices i whitelist
- [ ] dokumentovati API kroz OpenAPI/Swagger

### 1.5 Worker
- [ ] subscribovati MQTT topic-e po senzoru
- [ ] obraditi poruke i slati ih u processing pipeline
- [ ] push rezultata ka SignalR hub-u

### 1.6 Testovi
- [ ] unit testovi za scoring i localization logiku
- [ ] integration testovi za API + baza
- [ ] testovi idempotentnosti ingestije

## 2. Taskovi za frontend (Angular 20)

### 2.1 Core i app shell
- [ ] podesiti auth guard i token storage strategiju
- [ ] centralizovati API klijente u core servise
- [ ] podesiti global error handling i user feedback

### 2.2 Feature moduli
- [ ] dashboard ekran sa glavnim KPI metrikama
- [ ] floor-map prikaz uredjaja i njihovih putanja
- [ ] device lista sa pretragom i filterima
- [ ] alert panel sa prioritetima i potvrdom obrade
- [ ] whitelist manager (CRUD)
- [ ] reports stranica (pregled sesija i izvoz)

### 2.3 State management
- [ ] koristiti Angular signals za lokalno i feature stanje
- [ ] koristiti computed signale za derived prikaze
- [ ] koristiti effect samo za side-effect scenarije

### 2.4 Testovi
- [ ] component testovi za kriticne UI dijelove
- [ ] service testovi za API i SignalR integracije
- [ ] smoke e2e scenariji za exam dashboard tok

## 3. DevOps i operativa
- [ ] docker-compose za postgres + mqtt + backend + frontend
- [ ] environment varijable i secrets politika
- [ ] CI koraci: build, test, lint, publish artefakata
- [ ] backup i retention strategija za logove i alarm istoriju

## 4. Prioriteti
- P0: ingestion, baza, osnovni dashboard, alerting
- P1: napredna lokalizacija i whitelist workflow
- P2: izvjestaji, tuning algoritama i operativni dashboard
