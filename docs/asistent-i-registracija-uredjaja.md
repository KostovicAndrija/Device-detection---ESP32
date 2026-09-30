# Asistent i registracija ličnih uređaja

Korisničke uloge su `Professor` i `Assistant`. `Worker` je tehnička uloga za slanje događaja iz servisa za obradu očitavanja. Profesor kreira nalog asistenta na stranici „Asistenti”. Lozinka novog naloga mora imati najmanje 12 znakova.

Profesor vidi sve sesije, upravlja praćenjem, alarmima, izveštajima i ručnom listom dozvoljenih uređaja. Asistent samostalno kreira, pokreće i zaustavlja svoje sesije, vidi njihove uređaje, pozicije, alarme i izveštaje i potvrđuje njihove alarme. Ne menja ručnu listu dozvoljenih uređaja i nema pristup tuđim sesijama. Stare sesije bez vlasnika dostupne su profesoru.

## Postupak

1. Asistent otvara „Mapa učionice”, bira slobodnu učionicu i pokreće registraciono skeniranje dok je sam.
2. Senzori šalju očitavanja sa identifikatorom te registracione sesije. Prikupljanje traje najviše dva minuta. Tokom registracije nema alarma.
3. Asistent bira pronađene uređaje koji su njegovi i upisuje nazive. Za lakše prepoznavanje može registrovati jedan po jedan uređaj. Samo prisustvo u praznoj učionici nije dokaz vlasništva. Uređaji se ne prisvajaju automatski na osnovu jačine signala.
4. Potvrda završava skeniranje. Izbor se može potvrditi još deset minuta nakon isteka prikupljanja. Nepotvrđeni kandidati nisu na listi dozvoljenih uređaja. Posle napuštanja stranice aktuelno skeniranje se ponovo prikazuje pri povratku.
5. Svako naredno pokretanje sesije automatski kopira registrovane uređaje vlasnika u listu dozvoljenih uređaja te sesije. Asistent mora imati najmanje jedan registrovan uređaj. Njegovi lični uređaji imaju rizik 0 i ne stvaraju alarme. Za ostale uređaje ostaje uobičajeno računanje rizika.

Registracija prihvata samo uređaje koje je server stvarno zabeležio u tom skeniranju. Ne prihvata proizvoljan identifikator iz pregledača. Isti identifikator ne može biti registrovan na dva naloga. Promena registracije zahteva da korisnik prethodno zaustavi svoje aktivno praćenje. Promena identifikatora uređaja zahteva novu registraciju. Wi-Fi i Bluetooth identifikatori istog fizičkog uređaja mogu zahtevati odvojene stavke.

## Pokretanje i simulacija

Migracija `AddAssistantDeviceRegistration` dodaje vlasnika sesije, registraciona skeniranja i lične uređaje. API je primenjuje pri pokretanju. Potrebno je ponovo izgraditi i pokrenuti API, Worker i frontend.

U repozitorijumu nema ESP32 firmware-a koji prima komandu iz aplikacije. Dugme otvara sesiju za prikupljanje očitavanja. Senzor ili simulator mora slati njen `sessionId`. Za lokalnu proveru, u roku od dva minuta nakon pokretanja skeniranja:

```powershell
cd simulator
node src/index.mjs normal --session ID_REGISTRACIONOG_SKENIRANJA
```

Izaberite pronađeni simulirani uređaj i sačuvajte ga. Zatim pokrenite praćenje kao asistent i ponovite komandu sa identifikatorom nove sesije. Scenario `normal` koristi isti identifikator uređaja. Za proveru alarma drugog uređaja koristite scenario `high-risk` sa identifikatorom te sesije. Ovo proverava softverski tok, ne predstavlja fizičko merenje.

## Provere

Integracioni testovi proveravaju registraciju, izostanak alarma za lični uređaj, alarm za nepoznati uređaj, zabranu proizvoljnog izbora uređaja i pristupa tuđoj sesiji, istek skeniranja i konkurentne izmene registracije. Koriste EF InMemory. Ne zamenjuju proveru migracije na PostgreSQL-u i rad sa fizičkim senzorima.

SignalR događaji se dostavljaju grupi profesora i vlasniku sesije. Odjava prekida vezu i briše lokalno zapamćene događaje pre prijave drugog korisnika.
