const fs=require('node:fs');
const path=require('node:path');
const {randomUUID}=require('node:crypto');
const {chromium}=require(process.argv[2]);
const mqtt=require('../simulator/node_modules/mqtt');
const root=path.resolve(__dirname,'..');
const out=path.join(root,'screenshots','2026-10-20');
const state=JSON.parse(fs.readFileSync(path.join(out,'demo-state.json'),'utf8'));
const resume=process.argv.includes('--resume');
const manifest=resume?JSON.parse(fs.readFileSync(path.join(out,'manifest.json'),'utf8')):[];
const pause=ms=>new Promise(r=>setTimeout(r,ms));
let browser,context,page,client;
const username='demo-asistent-20261020',password='DemoAsistent2026!';
async function snap(name,title,scope=page){
  await page.evaluate(()=>document.fonts.ready);await pause(350);
  await scope.screenshot({path:path.join(out,name+'.png'),fullPage:scope===page,animations:'disabled'});
  manifest.push({file:name+'.png',title});console.log('CAPTURED '+name);
  fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify(manifest,null,2));
}
async function goto(route){await page.goto('http://localhost:4200'+route,{waitUntil:'networkidle'});await pause(600);}
async function login(user,pass){await page.locator('input[name=username]').fill(user);await page.locator('input[name=password]').fill(pass);await page.getByRole('button',{name:'Prijavi se'}).click();await page.waitForURL('**/dashboard');await pause(800);}
async function api(route,method='GET',body){
  const token=await page.evaluate(()=>JSON.parse(localStorage.getItem('device-detection-auth')).accessToken);
  const r=await fetch('http://localhost:7108'+route,{method,headers:{Authorization:'Bearer '+token,'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});
  const text=await r.text();if(!r.ok)throw Error(`${route}: ${r.status} ${text}`);return text?JSON.parse(text):null;
}
async function publishDevice(scanId,deviceId,point={x:4,y:3.1},roomId=state.registrationRoomId,signal='wifi'){
  const rooms=await api('/api/classrooms');const room=rooms.find(x=>x.id===roomId);
  for(const s of room.sensors){const d=Math.max(.3,Math.hypot(point.x-s.x,point.y-s.y));await new Promise((resolve,reject)=>client.publish(`sensors/${s.id}/rssi`,JSON.stringify({eventId:'screens-'+randomUUID(),sensorId:s.id,deviceIdentifier:deviceId,sessionId:scanId,signalType:signal,rssi:s.referenceRssi-10*s.pathLossExponent*Math.log10(d),timestamp:new Date().toISOString()}),{qos:1},e=>e?reject(e):resolve()));}
}
async function registration(prefix,deviceId,label){
  const panel=page.locator('app-device-registration');
  await panel.getByRole('button',{name:'Skeniraj moje uređaje'}).click();
  await panel.locator('.scan-workspace').waitFor();
  await snap(prefix+'-01-skeniranje','Pokretanje registracije ličnog uređaja',panel);
  const scan=await api('/api/staff/scans/current');
  await publishDevice(scan.id,deviceId);
  await panel.locator('.candidate').first().waitFor({timeout:20000});
  await panel.locator('input[type=checkbox]').first().check();
  await panel.getByRole('textbox',{name:'Naziv uređaja'}).fill(label);
  await snap(prefix+'-02-izbor-uredjaja','Izbor i imenovanje detektovanog uređaja',panel);
  await panel.getByRole('button',{name:'Potvrdi moje uređaje'}).click();
  await panel.locator('.scan-workspace').waitFor({state:'hidden'});
  await snap(prefix+'-03-sacuvan-uredjaj','Uspešno registrovan lični uređaj',panel);
}
async function main(){
  browser=await chromium.launch({channel:'chrome',headless:true,args:['--disk-cache-size=1048576','--media-cache-size=1048576','--disable-gpu-shader-disk-cache']});
  context=await browser.newContext({viewport:{width:1600,height:1100},deviceScaleFactor:1.5,locale:'sr-Latn-RS',timezoneId:'Europe/Belgrade',reducedMotion:'reduce'});
  page=await context.newPage();
  client=await mqtt.connectAsync('mqtt://localhost:1883');
  page.on('pageerror',e=>console.error('PAGE_ERROR '+e.message));
  if(!resume){
  await goto('/login');await snap('01-prijava','Prijava korisnika');
  await page.locator('input[name=username]').fill('admin');await page.locator('input[name=password]').fill('pogresna-lozinka');await page.getByRole('button',{name:'Prijavi se'}).click();await page.locator('.error').waitFor();await snap('02-prijava-greska','Pogrešni podaci za prijavu');
  await login('admin','admin123');
  await snap('03-profesor-pregled','Pregled učionica i istorije sesija');
  await goto(`/floor-map?room=${state.roomId}&session=${state.sessionId}`);
  await page.locator('.risk-warning').first().waitFor({timeout:55000});await page.locator('.risk-critical').first().waitFor();
  await snap('04-mapa-uzivo','Mapa uživo: crvena K-04, narandžasta K-06 i tri uređaja');
  await snap('05-mapa-detalj','Detalj učionice sa kritičnom i sumnjivom klupom',page.locator('.room-shell'));
  await page.getByRole('button',{name:'Uvećaj mapu',exact:true}).click();await snap('06-mapa-uvecana','Uvećana mapa učionice');await page.keyboard.press('Escape');
  await page.locator('summary.settings-heading').click();await snap('07-formula-i-podesavanja','Formula, aktivni intervali i pragovi',page.locator('.detection-settings'));
  await page.locator('.settings-fields input').nth(1).fill('10');await snap('08-promena-vremenskog-prozora','Primer uticaja produženja prozora na 150 sekundi',page.locator('.detection-settings'));
  await page.getByRole('button',{name:'Primeni na prikaz'}).click();await pause(900);await snap('09-primenjena-podesavanja','Primenjeni novi kriterijumi na proračun klupa');
  await page.getByRole('button',{name:'Vrati početna podešavanja'}).click();
  await page.getByText('Kako najbolje iskoristiti sistem?',{exact:true}).click();await snap('10-uputstvo-za-koriscenje','Uputstvo za testiranje i tumačenje rezultata',page.locator('.detection-settings'));
  await page.locator('summary.settings-heading').click();
  await page.getByRole('button',{name:'Reprodukcija',exact:true}).click();await page.locator('.replay-controls').waitFor();await snap('11-reprodukcija-sesije','Reprodukcija sačuvanih očitavanja');
  await goto('/classrooms');await page.getByRole('button',{name:'Demo učionica · 20. oktobar',exact:true}).click();await snap('12-ucionica-raspored','Raspored klupa, računara i senzora');
  await page.getByRole('button',{name:'Nova učionica',exact:true}).click();await snap('13-nova-ucionica','Dijalog dodavanja učionice');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.getByRole('button',{name:'Dimenzije i podaci',exact:true}).click();await snap('14-podaci-ucionice','Dimenzije i opis učionice');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.locator('.equipment-row').filter({has:page.getByText('K-04',{exact:true})}).click();await snap('15-izmena-klupe','Koordinate, dimenzije i broj računara na klupi');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.getByRole('button',{name:'Dodaj klupu / računar',exact:true}).click();await snap('16-dodavanje-klupe','Dijalog dodavanja klupe');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.locator('.equipment-row').filter({has:page.getByText('D20-S1',{exact:true})}).click();await snap('17-kalibracija-senzora','Pozicija senzora i RSSI kalibracija');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.getByRole('button',{name:'Dodaj senzor',exact:true}).click();await snap('18-dodavanje-senzora','Dijalog dodavanja senzora');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await goto('/devices');await snap('19-aktivni-uredjaji','Tabela detektovanih uređaja');
  await page.getByPlaceholder('Hash ili tip').fill('wifi');await snap('20-pretraga-uredjaja','Filtriranje uređaja po tipu signala');
  await goto('/alerts');await page.locator('select').selectOption(state.sessionId);await pause(700);await snap('21-alarmi','Alarmi sa vrstom signala i statusom');
  const acknowledge=page.getByRole('button',{name:'Potvrdi',exact:true});if(await acknowledge.count()){await acknowledge.first().click();await pause(600);await snap('22-obradjen-alarm','Potvrđen i obrađen alarm');}
  await goto('/whitelist');await page.locator('select').selectOption(state.sessionId);await pause(700);await snap('23-dozvoljeni-uredjaji','Dozvoljeni telefon nastavnika i obrazac za dodavanje');
  await page.locator('input[name=student]').fill('Demonstracija · odobren telefon');await page.locator('input[name=device]').fill('DEMO-WHITELIST-EXAMPLE');await snap('24-dodavanje-dozvoljenog-uredjaja','Popunjen obrazac odobravanja uređaja');
  await goto('/reports');await page.locator('select').selectOption(state.sessionId);await pause(700);await snap('25-izvestaj-sesije','Izveštaj sa brojem merenja, uređaja i alarma');
  const dl=page.waitForEvent('download');await page.getByRole('button',{name:'Izvezi CSV'}).click();await (await dl).saveAs(path.join(out,'izvestaj-demo.csv'));
  await goto('/assistants');await snap('26-novi-asistent','Kreiranje naloga asistenta');
  await page.locator('input[name=username]').fill(username);await page.locator('input[name=password]').fill(password);await page.getByRole('button',{name:'Kreiraj asistenta'}).click();await pause(700);await snap('27-nalog-asistenta','Rezultat kreiranja demo naloga asistenta');
  await goto(`/floor-map?room=${state.offlineRoomId}`);await page.getByRole('button',{name:'Počni očitavanje'}).click();await page.locator('.preflight-alert').waitFor();await snap('28-blokada-neaktivni-senzori','Praćenje je blokirano ako sva tri senzora nisu aktivna');
  }else{await goto('/login');await login('admin','admin123');}
  await goto(`/floor-map?room=${state.registrationRoomId}`);
  await page.locator('app-device-registration').getByRole('button',{name:'Skeniraj moje uređaje'}).click();
  await page.locator('app-device-registration [role=alert]').waitFor();
  await snap('29-registracija-blokirana-tokom-pracenja','Registracija ličnih uređaja je blokirana dok korisnik ima aktivno praćenje',page.locator('app-device-registration'));
  const professorContext=context;
  context=await browser.newContext({viewport:{width:1600,height:1100},deviceScaleFactor:1.5,locale:'sr-Latn-RS',timezoneId:'Europe/Belgrade',reducedMotion:'reduce'});page=await context.newPage();
  await goto('/login');await login(username,password);await snap('30-asistent-pregled','Pregled aplikacije iz uloge asistenta');
  await goto(`/floor-map?room=${state.registrationRoomId}`);await registration('31-asistent-registracija','DEMO-ASSISTANT-PERSONAL','Telefon asistenta');
  await page.getByRole('button',{name:'Počni očitavanje'}).click();await page.getByRole('button',{name:'Zaustavi očitavanje'}).waitFor();
  const current=(await api('/api/sessions')).find(s=>s.roomId===state.registrationRoomId&&s.status==='active');
  await publishDevice(current.id,'DEMO-ASSISTANT-PERSONAL');await publishDevice(current.id,'DEMO-ASSISTANT-EXAM-PHONE',{x:1.55,y:3.1},state.registrationRoomId,'bluetooth_pairing');await pause(3500);
  await snap('32-asistent-aktivna-sesija','Asistent pokreće sopstvenu sesiju; njegov telefon je dozvoljen');
  await goto('/whitelist');await page.locator('select').selectOption(current.id);await pause(500);await snap('33-asistent-automatski-dozvoljen-uredjaj','Lični uređaj asistenta automatski je dozvoljen u novoj sesiji');
  await goto(`/floor-map?room=${state.registrationRoomId}&session=${current.id}`);await page.getByRole('button',{name:'Zaustavi očitavanje'}).click();await page.getByRole('button',{name:'Počni očitavanje'}).waitFor();await snap('34-asistent-zavrsena-sesija','Zaustavljanje očitavanja i sačuvana istorija');
  await page.getByRole('button',{name:'Odjava',exact:true}).click();await page.waitForURL('**/login');await snap('35-odjava','Odjava i povratak na ekran prijave');
  page=await professorContext.newPage();await goto(state.url.replace('http://localhost:4200',''));
  await page.locator('.risk-warning').first().waitFor({timeout:55000});
  const alertCount=(await api(`/api/sessions/${state.sessionId}/alerts`)).length;
  for(let i=0;i<65;i++){await fetch('http://127.0.0.1:7119/pulse');if(await page.locator('.alert-toast').count())break;await pause(2000);}
  if(await page.locator('.alert-toast').count())await snap('36-toast-obavestenje','Toast notifikacija za novi Bluetooth signal');
  await page.locator('.risk-warning').first().waitFor({timeout:55000});
  await snap('37-zavrsna-mapa','Završni prikaz: kritična i sumnjiva klupa sa tri uređaja');
  fs.writeFileSync(path.join(out,'PROCITAJ.md'),'# Screenshotovi aplikacije\n\nSnimljeno 20.10.2026. iz stvarno pokrenute aplikacije, uz simulirane MQTT poruke.\n\nDemo: jedna crvena klupa K-04, jedna narandžasta K-06, dva nepoznata uređaja i jedan dozvoljen telefon nastavnika. Narandžasti uređaj emituje na 40 sekundi; klupa povremeno kratko postaje normalna kada staro očitavanje izađe iz prozora.\n\nAktivna demonstracija: '+state.url+'\n\n'+manifest.map(x=>'- ['+x.title+']('+x.file+')').join('\n')+'\n\nPromene sadržaja prilikom snimanja: dodate su samo demo učionice, demo sesije, dozvoljeni demo uređaji i nalog asistenta. Postojeći podaci nisu obrisani.\n');
  console.log('FINISHED '+manifest.length+' screenshots');
  await client.endAsync();await browser.close();
}
main().catch(async e=>{console.error(e.stack);try{await page.screenshot({path:path.join(out,'debug-failure.png'),fullPage:true});}catch{}try{await browser.close();}catch{}process.exit(1);});
