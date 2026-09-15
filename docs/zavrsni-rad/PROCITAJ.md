# Dokument o ESP32 projektu

- `Zavrsni_rad_Andrija_Kostovic_51_2021_ESP32.docx` — Word verzija za uređivanje, sa povezanim sadržajem i automatskim poljem za sadržaj.
- `Zavrsni_rad_Andrija_Kostovic_51_2021_ESP32_Prosireni.pdf`. Aktuelna verzija od 61 strane za čitanje i štampu, sa povezanim sadržajem i obeleživačima poglavlja. PDF bez dodatka „Prosireni” je ranija verzija.
- `Zavrsni_rad_ESP32.md` — tekstualni izvor rada.
- `assets/` — dijagrami i grb sa naslovne strane dostavljenog uzora.

Uneti su Andrija Kostović, indeks 51/2021, i mentorka dr Ana Kaplarević-Mališić. Naziv ustanove i grb preuzeti su iz dostavljenog uzora: Institut za matematiku i informatiku, Prirodno-matematički fakultet Univerziteta u Kragujevcu. Oblik imena mentorke proveren je na [zvaničnoj stranici Instituta](https://imi.pmf.kg.ac.rs/nastavno-osoblje).

U ažuriranoj verziji engleski izrazi su u kurzivu, uključujući naslove, sadržaj, tehničke primere i dijagrame. Dokument nema tabele i sadrži 62 stavke sa nabrajanjem. Numerisani naslovi tabela su uklonjeni. Rečenične crte zamenjene su tačkama. Spojnice u nazivima, putanjama i prezimenu, kao i matematički znaci, sačuvani su. Prethodne verzije nalaze se u rezervnim folderima.

Ostavljeno je 16 označenih mesta za slike, uz postojeća četiri tehnička dijagrama. U Wordu zamenite pasus „МЕСТО ЗА СЛИКУ” odgovarajućom slikom i uklonite njegov okvir i prazan prostor. Natpis ispod slike prilagodite stvarnom prikazu. Mesta za fizičku opremu i grafikone merenja popunite tek nakon stvarnog izvođenja. Broj 61 odnosi se na provereni PDF. Word ima nezavisan prelom, koji može zavisiti od verzije programa i naknadno dodatih slika.

Poglavlje 5.11 opisuje implementirane uloge profesora i asistenta i samostalnu registraciju ličnih uređaja. U radu je navedeno ograničenje da još nema firmvera koji prima komandu za pokretanje skeniranja.

Pre predaje dopuniti označena polja u biografiji i prilagoditi mesec predaje ako nije septembar 2026. Na početku dokumenta objašnjeno je da su fizički senzorski sloj i terenski rezultati van potvrđenog obima dostupnog repozitorijuma.

Word i PDF napravljeni su iz istog teksta, uz zaseban prelom. U Wordu je sadržaj već popunjen povezanim naslovima. Za Wordove brojeve stranica i posle izmena: desni klik na sadržaj → Update Field / Ažuriraj polje → Update entire table / Ažuriraj ceo sadržaj. PDF već ima izračunate brojeve stranica. Ako menjate Word, novu PDF verziju izvezite iz te izmenjene datoteke.

## Ponovno generisanje iz tekstualnog izvora

Potrebni su Python, python-docx, Pillow, PyMuPDF i ReportLab, kao i Windows fontovi Times New Roman, Arial i Consolas. Pokrenuti iz korena projekta:

```powershell
python docs/zavrsni-rad/izradi_dokument.py
python docs/zavrsni-rad/izradi_pdf.py
```

Ponovno generisanje prepisuje izlazne DOCX/PDF datoteke. Prethodno sačuvati eventualne ručne izmene. Lokalno su provereni softverski testovi, dok rezultati fizičkih merenja preciznosti i performansi nisu izmišljeni niti dodati. Sažetak provere nalazi se u `../provera-asistenta-2026-09-10.md`.
