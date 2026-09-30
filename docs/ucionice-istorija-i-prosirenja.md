# Učionice, istorija i proširivanje obrade

Plan prostorije je centralni deo stranice **Učionice**. Zelena dugmad sa znakom plus služe za dodavanje. Podaci i dimenzije učionice, klupe/računari i senzori uređuju se kroz dijaloge. Klik na objekat na planu ili u bočnoj listi otvara njegov dijalog. **Odustani** ne menja sačuvane podatke. Sačuvane promene se odmah prikazuju na planu.

Klupa ima širinu, dubinu i koordinate centra. Cela njena površina mora biti unutar prostorije. Vrsta opreme razlikuje običnu klupu od računara na klupi. Stari zapisi bez dimenzija zadržavaju prethodnu veličinu prikaza. Nova prazna učionica može se sačuvati pre dodavanja senzora. Bez senzora nema procene položaja. [Učionica A-0-15](classrooms/A-0-15.md) dokumentuje unos korisnikovog PDF plana i predložene položaje senzora.

Profesor u navigaciji otvara **Učionice**. Može dodati novu učionicu ili izmeniti postojeću: oznaku, naziv, opis, širinu i dužinu u metrima, računare/klupe sa oznakama i koordinatama, senzore sa oznakama, modelom, koordinatama i parametrima RSSI modela. Koordinatni početak je gornji levi ugao prikaza. Server odbija duple oznake, koordinate izvan prostorije, nevažeće dimenzije i parametre kalibracije. Za stabilnu trilateraciju potrebna su najmanje tri nekolinearna senzora. Postojeći algoritam sa manje senzora zadržava grublju procenu težištem.

Početna migracija prenosi postojeća tri prikazana rasporeda u bazu. Senzori imaju dosadašnje oznake S1, S2 i S3 i koordinate (0,0), (8,0), (4,6). Za nove sesije raspored se čita iz baze, a ne iz globalne liste senzora u appsettings. `Localization:WindowSeconds` i dalje određuje prozor grupisanja očitavanja. Promene istog rasporeda štiti optimistička konkurencija preko broja revizije.

## Zamena senzora

- Ako novi senzor šalje postojeći JSON ugovor, promeniti model, oznaku, koordinate i kalibraciju u učionici. Poslovna obrada i interfejs ostaju isti.
- Ako šalje drugi format, implementirati `ISensorMessageDecoder` i registrovati adapter u zavisnostima. MQTT Worker prevodi poruku u zajednički `RssiIngressMessage` pre poslovne obrade. HTTP ulaz takođe koristi taj zajednički model.
- Oznake senzora vezuju se za raspored sesije. Identifikator sesije ostaje obavezan deo povezivanja očitavanja sa učionicom. Sam naziv modela ne instalira firmware niti menja radio-mogućnosti fizičke ploče.
- Parametri `ReferenceRssi` i `PathLossExponent` važe pojedinačno po senzoru. Zamena RSSI tehnologije merenjem vremena leta ili ugla zahteva proširenje zajedničkog modela i odgovarajući algoritam. Te merne veličine ne treba predstavljati kao RSSI.

## Istorija i reprodukcija

Pri pokretanju sesije trajno se čuva kopija rasporeda prostorije, računara, senzora i kalibracije. Kasnije uređivanje učionice utiče na naredna pokretanja, bez menjanja kopije aktivne ili završene sesije. Planska sesija pre početka dobija tadašnji aktuelni raspored.

Očitavanja redovnih sesija se više ne brišu automatski nakon 24 časa. Baza i dalje koristi postojeći Docker volumen. Čišćenje nakon konfigurisanog roka ostaje za registraciona skeniranja ličnih uređaja. Zbog trajnog čuvanja redovnih sesija potrebno je planirati kapacitet diska i rezervne kopije baze.

Na **Mapi učionice** izabrati sesiju iz istorije. Završena sesija sa podacima automatski otvara reprodukciju. Dostupni su vremenski klizač, puštanje, pauza i brzine 1×, 2×, 5× i 10×. Reprodukcija ne šalje MQTT poruke, ne menja sesiju i ne generiše nove alarme.

Prikaz je ponovni proračun iz sačuvanih očitavanja, a ne video-snimak nekadašnjeg ekrana. Uživo i istorija koriste isti servis i ograničen prozor od 60 sekundi za inicijalizaciju filtra. Stanje filtra se ne deli između sesija i redosled korisnikovog pomeranja klizača ne menja rezultat. Promena buduće implementacije algoritma može promeniti ponovni proračun. Odgovor API-ja navodi verziju algoritma. Status dozvole uređaja računa se iz raspoloživih whitelist zapisa i njihovog vremenskog važenja. Ranije ručno obrisana dozvola se ne može rekonstruisati.

Starije sesije bez snimljenog rasporeda jasno su označene i koriste raspoloživi raspored učionice. Očitavanja već obrisana prethodnim mehanizmom čuvanja ne mogu se vratiti. Ne prikazuje se izmišljena istorija.

API za ovlašćenog korisnika:

- `GET /api/classrooms`, `POST /api/classrooms`, `PUT /api/classrooms/{id}`. Pisanje je dozvoljeno samo profesoru.
- `GET /api/sessions/{id}/layout`. Raspored i oznaka da li postoji istorijska kopija.
- `GET /api/sessions/{id}/history`. Broj očitavanja, prvo/poslednje vreme i verzija proračuna.
- `GET /api/sessions/{id}/history/frame?at=2026-09-18T12:00:00Z`. Položaji u izabranom trenutku.
- `GET /api/sessions/{id}/history/observations?page=0&pageSize=1000`. Stranični JSON izvoz sa `schemaVersion`, rasporedom i pseudonimizovanim identifikatorima. Za stabilan izvoz koristiti završenu sesiju. `hasMore` određuje da li treba preuzeti narednu stranicu.

Profesor ima pristup svim sesijama, a asistent svojim. Iste provere važe za istoriju, raspored i izvoz.

## Priprema za AI

`IPositionEstimator` je zamenjivi asinhroni interfejs koji prima raspored i normalizovana senzorska očitavanja, a vraća procenjene koordinate i pouzdanost. Podrazumevani `RssiPositionEstimator` zadržava računanje udaljenosti i ponderisane najmanje kvadrate. Nova lokalna implementacija ili klijent odabranog AI servisa registruje se preko istog interfejsa. U testovima je proverena zamena algoritma bez promene prijema i baze.

Ne poziva se nijedan spoljašnji AI servis, ne šalju se podaci van sistema i ne zahtevaju se API ključevi. Stranični izvoz omogućava naknadnu analizu i pripremu podataka za treniranje. Koordinate računara same po sebi nisu oznake stvarnog položaja telefona. Za nadgledano treniranje potrebna su zasebna poznata referentna merenja.

## Oznake i simulatori

Kružići prikazuju Wi-Fi, Bluetooth ili Wi-Fi/BT za mešovita očitavanja; oznake `ble` i `bluetooth_pairing` pripadaju Bluetooth prikazu. Kratki identifikator uređaja ostaje ispod kružića, a detalji u opisu.

Oba demo simulatora koriste raspored iz sesije: dimenzije, senzore i kalibraciju. Postojeće komande ostaju važeće. Osnovni kontrolni scenariji `normal`, `high-risk` i drugi i dalje služe proveri izvornog S1/S2/S3 ugovora.
