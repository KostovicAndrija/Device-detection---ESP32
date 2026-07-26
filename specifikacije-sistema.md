# Specifikacije sistema

## 1. Naziv i svrha

Sistem je predlozen za pasivnu detekciju Wi-Fi i Bluetooth aktivnosti u ucionici tokom ispita, sa ciljem da profesor dobije upozorenja o potencijalnom prepisivanju ili prisustvu neovlascenih uredjaja.

Sistem ne presrece sadrzaj komunikacije. Obradjuju se samo metapodaci kao sto su MAC adrese, RSSI vrijednosti, tip uredjaja i vrijeme detekcije.

## 2. Glavni ciljevi

- detekcija nepoznatih uredjaja u ispitnom prostoru
- procjena lokacije uredjaja na osnovu RSSI signala
- identifikacija sumnjivih obrazaca kretanja i blizine
- pracenje Bluetooth pairing pokusaja i Bluetooth/BLE aktivnosti
- prikaz real-time obavjestenja profesoru

## 3. Hardverska arhitektura

Sistem se zasniva na tri ESP32 senzora rasporedjena po ucionici.

### Raspored senzora

- S1: prednji lijevi ugao
- S2: prednji desni ugao
- S3: sredina zadnjeg zida

Ovakav raspored formira trougao koji pokriva veci dio prostora i smanjuje gresku trilateracije.

### Mogucnosti ESP32

- Wi-Fi 802.11 b/g/n
- Bluetooth Classic
- Bluetooth Low Energy (BLE)
- promiskuitetni Wi-Fi mod za pasivno osluskuvanje okvira

## 4. Tipovi skeniranja

### 4.1 Wi-Fi passive scanning

ESP32 u promiskuitetnom modu hvata Probe Request okvire i biljezi:

- MAC adresu uredjaja
- RSSI vrijednost
- SSID listu koju uredjaj trazi
- vendor/OUI informacije kada su dostupne

### 4.2 Bluetooth Classic discovery

Koristi se za otkrivanje uredjaja koji su discoverable. Biljeze se:

- BD_ADDR
- RSSI
- naziv uredjaja
- class of device

### 4.3 BLE scanning

BLE uredjaji se prate kroz advertisement pakete. Biljeze se:

- BLE adresa
- RSSI
- advertisement podaci

## 5. Vremenska multiplikacija skeniranja

ESP32 ne izvrsava Wi-Fi i Bluetooth skeniranje potpuno paralelno, vec se koristi vremenska smjena:

- Wi-Fi sken: 500 ms
- BLE sken: 200 ms
- Bluetooth Classic inquiry: oko 10 s, periodicki na 2 minute

Preporuceni ciklus je:

1. Wi-Fi
2. BLE
3. Wi-Fi
4. povremeni BT inquiry

## 6. Lokalizacija i obrada RSSI signala

### 6.1 RSSI do udaljenosti

Za procjenu udaljenosti koristi se Log-Distance Path Loss model.

Parametar okoline n zavisi od prostora:

- slobodan prostor: 2.0
- tipicna kancelarija: 2.5 do 3.0
- prostor sa preprekama: 3.0 do 3.5

Kalibracija se radi mjerenjem RSSI na udaljenosti od 1 metar.

### 6.2 Trilateracija

Sa tri senzora se procjenjuje pozicija uredjaja rjesavanjem sistema krivih iz RSSI udaljenosti.

### 6.3 Weighted Least Squares

Zbog suma se koristi Weighted Least Squares, pri cemu senzori sa jacim signalom dobijaju vecu tezinu.

### 6.4 Kalman filter

Svaki uredjaj moze imati sopstveni Kalman filter za zagladjivanje RSSI vrijednosti kroz vrijeme.

Predlozeni parametri:

- process noise Q: 0.01
- measurement noise R: 2.0
- pocetna konvergencija: 5 do 10 uzoraka

## 7. Detekcija anomalija

Sistem racuna risk score od 0 do 100 po uredjaju.

Upozorenje se prikazuje kada score predje prag od 70.

Moguci signali za uzbunu:

- nepoznat uredjaj u ucionici
- prevelika blizina izmedju uredjaja
- Bluetooth pairing pokusaj tokom ispita
- uredjaj koji se pojavljuje van ocekivane zone

## 8. Arhitektura softvera

### 8.1 Firmware na ESP32

- Wi-Fi promiskuitetni mod
- Bluetooth discovery i BLE scanning
- slanje podataka prema backendu preko MQTT-a

### 8.2 Backend

Backend moze biti implementiran u Spring Boot ili .NET.

Klucni servisi:

- MqttSubscriberService
- RssiAggregatorService
- TrilatService
- AnomalyDetectionService
- WhitelistService
- WebSocketPushService
- ExamSessionService

### 8.3 Frontend

Predlozen je Angular frontend sa sljedecim komponentama:

- ExamDashboardComponent
- FloorMapComponent
- DeviceListComponent
- AlertPanelComponent
- WhitelistManagerComponent
- ExamReportComponent

### 8.4 Baza podataka

Sistem treba da cuva:

- uredjaje i njihove hashirane identifikatore
- RSSI mjerenja po senzoru
- konfiguraciju ucionice
- ispitne sesije
- alarme i istoriju detekcija
- whitelist studenata i dozvoljenih uredjaja

## 9. Privatnost i uskladjenost

Zbog zastite privatnosti preporucuje se:

- hashiranje MAC adresa prije cuvanja
- zadrzavanje logova ograniciti na 24 sata nakon ispita
- obavjestavanje studenata o sistemu
- da sistem bude savjetodavan, a ne automatski kaznjavajuci
- ne snimati sadrzaj paketa, nego samo metapodatke

## 10. Glavni tehnicki izazovi

- randomizovane MAC adrese
- RSSI varijacije zbog zidova i ljudi u prostoriji
- potreba za kalibracijom po svakoj ucionici
- sinhronizacija vremena izmedju senzora
- detekcija uredjaja koji se nalaze van ucionice

## 11. Preporuceni implementacioni koraci

1. napraviti proof-of-concept sa jednim ESP32 senzorom
2. implementirati prijem podataka na backendu
3. dodati prikaz uredjaja u realnom vremenu
4. uvesti tri senzora i trilateraciju
5. dodati anomaly scoring i alert panel
6. implementirati whitelist i izvjestaje

## 12. Zakljucak

Predlozeni sistem je pasivno, tehnicki izvodljivo rjesenje za pracenje Wi-Fi i Bluetooth aktivnosti tokom ispita. Najveci fokus treba biti na kvalitetu RSSI obrade, privatnosti i pravilnoj kalibraciji ucionice.