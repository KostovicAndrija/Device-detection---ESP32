# Best practices za kodiranje (Backend + Frontend)

## 1. Opsta pravila
- pisati male i fokusirane commit-e sa jasnom porukom
- koristiti konzistentan naming i folder organizaciju
- izbjegavati dupliranje logike (DRY)
- svaka nova funkcionalnost treba imati test ili jasno opravdanje zasto nema test
- ne miksati refaktor i novu funkcionalnost u istom PR-u

## 2. Backend best practices (.NET)

### 2.1 Arhitektura i granice
- `Domain` ne smije zavisiti od `Infrastructure`
- use-case logiku drzati u `Application` sloju
- `Api` treba biti tanak sloj (validacija, mapiranje, delegiranje)
- spoljne integracije (MQTT, baza, cache) izolovati u `Infrastructure`

### 2.2 Kod i dizajn
- koristiti async/await end-to-end za I/O
- koristiti cancellation token gdje god je moguce
- validaciju ulaza raditi na granici sistema (controller/handler)
- domain pravila enkapsulirati u domen modelima ili domain servisima
- izbjegavati static mutable state

### 2.3 Podaci i baza
- koristiti migracije i ne mijenjati shemu rucno u produkciji
- obavezno indexirati kolone koje se filtriraju po vremenu/sesiji/device-u
- koristiti transakcije za vise povezanih upisa
- cuvati hashirane identifikatore uredjaja, ne originalne adrese

### 2.4 Observability i sigurnost
- strukturisano logovanje (Serilog ili slicno)
- metrici ingestion throughput, processing latency, error rate
- ne logovati osjetljive podatke i tajne
- JWT sa kratkim expiry i refresh strategijom

### 2.5 Testiranje
- unit testovi za cistu poslovnu logiku
- integration testovi za DB i API kontrakte
- test fixture-i i test data builder-i za citljivost testova

## 3. Frontend best practices (Angular 20 + signals)

### 3.1 Struktura i organizacija
- koristiti standalone komponente i feature folder strukturu
- core sloj koristiti za shared servise i infrastrukturu
- ne stavljati poslovnu logiku u template

### 3.2 Signals pristup
- koristiti `signal` za lokalno stanje komponente
- koristiti `computed` za derived stanje umjesto manuelnog racunanja
- `effect` koristiti samo za side-effect (pozivi servisa, log, sync), ne za derivaciju podataka
- minimizovati nepotrebne subscribe obrasce kada signal moze rijesiti stanje

### 3.3 UI i performanse
- koristiti `OnPush` strategiju gdje je moguce
- koristiti track by funkcije za velike liste
- debouncovati filtere/pretragu da se smanji broj poziva
- fallback prikazi za loading, empty i error stanje na svakom feature ekranu

### 3.4 API i real-time integracija
- centralizovati HTTP i SignalR logiku u servisima
- mapirati DTO u view model prije renderovanja
- implementirati retry i reconnect strategiju za SignalR
- prikazati status konekcije korisniku

### 3.5 Testiranje
- component testovi za kriticne interakcije
- service testovi za API mapiranje i greske
- e2e smoke scenariji za dashboard, alerts i whitelist tok

## 4. Definition of Done (DoD)
- kod prolazi lint/build/test pipeline
- dokumentacija endpointa i feature-a je azurirana
- dodani su logovi i metrike za novu funkcionalnost
- bez regresije na postojecim testovima
- sigurnosni i privatnosni zahtjevi ispostovani
