const fs=require('node:fs'),path=require('node:path'),{randomUUID}=require('node:crypto');
const {chromium}=require(process.argv[2]);const mqtt=require('../simulator/node_modules/mqtt');
const out=path.resolve(__dirname,'../screenshots/2026-10-20'),state=JSON.parse(fs.readFileSync(path.join(out,'demo-state.json'),'utf8'));
const pause=ms=>new Promise(r=>setTimeout(r,ms));let browser,page,context,client;
async function login(user='admin',password='admin123'){
  const r=await fetch('http://localhost:7108/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({username:user,password})});if(!r.ok)throw Error('Login failed');
  const auth=await r.json();context=await browser.newContext({viewport:{width:1600,height:1100},deviceScaleFactor:1.5,locale:'sr-Latn-RS',timezoneId:'Europe/Belgrade',reducedMotion:'reduce'});
  await context.addInitScript(auth=>localStorage.setItem('device-detection-auth',JSON.stringify(auth)),auth);page=await context.newPage();
}
async function api(route,method='GET',body){const token=await page.evaluate(()=>JSON.parse(localStorage.getItem('device-detection-auth')).accessToken);const r=await fetch('http://localhost:7108'+route,{method,headers:{Authorization:'Bearer '+token,'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});const t=await r.text();if(!r.ok)throw Error(route+' '+r.status+' '+t);return t?JSON.parse(t):null;}
async function goto(route){await page.goto('http://localhost:4200'+route,{waitUntil:'networkidle'});await pause(500);}
async function snap(name,scope=page){await page.evaluate(()=>document.fonts.ready);await pause(200);await scope.screenshot({path:path.join(out,name+'.png'),fullPage:scope===page,animations:'disabled'});console.log('UPDATED '+name);}
async function fresh(){await page.waitForFunction(()=>document.querySelectorAll('.room-shell .device-marker.live').length===3&&document.querySelectorAll('.room-shell .risk-warning').length===1&&document.querySelectorAll('.room-shell .risk-critical').length===1,null,{timeout:60000});}
async function emit(sessionId,id,signal='wifi',deskId='K-04'){
  const rooms=await api('/api/classrooms'),room=rooms.find(r=>r.id===state.registrationRoomId),desk=room.computers.find(d=>d.id===deskId),timestamp=new Date().toISOString();
  for(const sensor of room.sensors){const d=Math.max(.3,Math.hypot(desk.x-sensor.x,desk.y-sensor.y));await new Promise((ok,fail)=>client.publish(`sensors/${sensor.id}/rssi`,JSON.stringify({eventId:'a015-capture-'+randomUUID(),sessionId,sensorId:sensor.id,deviceIdentifier:id,signalType:signal,rssi:sensor.referenceRssi-10*sensor.pathLossExponent*Math.log10(d),timestamp}),{qos:1},e=>e?fail(e):ok()));}
}
(async()=>{
  browser=await chromium.launch({channel:'chrome',headless:true,args:['--disk-cache-size=1048576','--disable-gpu-shader-disk-cache']});client=await mqtt.connectAsync('mqtt://localhost:1883');await login();
  const professor=context;
  if(!process.argv.includes('--finish')){
  await goto('/dashboard');await snap('03-profesor-pregled');
  await goto(state.url.replace('http://localhost:4200',''));await fresh();await snap('04-mapa-uzivo');await snap('05-mapa-detalj',page.locator('.room-shell'));
  await page.getByRole('button',{name:'Uvećaj mapu',exact:true}).click();await snap('06-mapa-uvecana');await page.keyboard.press('Escape');
  await page.locator('summary.settings-heading').click();await page.locator('.settings-fields input').nth(1).fill('10');await page.getByRole('button',{name:'Primeni na prikaz'}).click();await pause(800);await snap('09-primenjena-podesavanja');await page.getByRole('button',{name:'Vrati početna podešavanja'}).click();await page.locator('summary.settings-heading').click();
  await page.getByRole('button',{name:'Reprodukcija',exact:true}).click();await page.locator('.replay-controls').waitFor();await page.getByRole('slider').press('End');await pause(1000);await snap('11-reprodukcija-sesije');
  await goto('/classrooms');await page.getByRole('button',{name:'Učionica A-0-15',exact:true}).click();await snap('12-ucionica-raspored');
  for(const [button,file] of [['Nova učionica','13-nova-ucionica'],['Dimenzije i podaci','14-podaci-ucionice']]){await page.getByRole('button',{name:button,exact:true}).click();await snap(file);await page.getByRole('button',{name:'Zatvori dijalog'}).click();}
  await page.locator('.equipment-row').filter({has:page.getByText(state.criticalDesk,{exact:true})}).click();await snap('15-izmena-klupe');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.getByRole('button',{name:'Dodaj klupu / računar',exact:true}).click();await snap('16-dodavanje-klupe');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.locator('.equipment-row').filter({has:page.getByText('S1',{exact:true})}).click();await snap('17-kalibracija-senzora');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await page.getByRole('button',{name:'Dodaj senzor',exact:true}).click();await snap('18-dodavanje-senzora');await page.getByRole('button',{name:'Zatvori dijalog'}).click();
  await goto(`/floor-map?room=${state.offlineRoomId}`);await page.getByRole('button',{name:'Počni očitavanje'}).click();await page.locator('.preflight-alert').waitFor();await snap('28-blokada-neaktivni-senzori');
  await login('demo-asistent-20261020','DemoAsistent2026!');await goto('/dashboard');await snap('30-asistent-pregled');
  await goto(`/floor-map?room=${state.registrationRoomId}`);const panel=page.locator('app-device-registration');await panel.getByRole('button',{name:'Skeniraj moje uređaje'}).click();await panel.locator('.scan-workspace').waitFor();await snap('31-asistent-registracija-01-skeniranje',panel);
  const scan=await api('/api/staff/scans/current');await emit(scan.id,'DEMO-ASSISTANT-A015');await panel.locator('.candidate').first().waitFor({timeout:20000});await panel.locator('input[type=checkbox]').first().check();await panel.getByRole('textbox',{name:'Naziv uređaja'}).fill('Telefon asistenta · A015');await snap('31-asistent-registracija-02-izbor-uredjaja',panel);
  await panel.getByRole('button',{name:'Potvrdi moje uređaje'}).click();await panel.locator('.scan-workspace').waitFor({state:'hidden'});await snap('31-asistent-registracija-03-sacuvan-uredjaj',panel);
  await page.getByRole('button',{name:'Počni očitavanje'}).click();await page.getByRole('button',{name:'Zaustavi očitavanje'}).waitFor();const session=(await api('/api/sessions')).find(s=>s.roomId===state.registrationRoomId&&s.status==='active');
  await emit(session.id,'DEMO-ASSISTANT-A015');await emit(session.id,'DEMO-ASSISTANT-EXAM-PHONE','bluetooth_pairing','K-03');await pause(3500);await snap('32-asistent-aktivna-sesija');
  await goto('/whitelist');await page.locator('select').selectOption(session.id);await pause(500);await snap('33-asistent-automatski-dozvoljen-uredjaj');
  await goto(`/floor-map?room=${state.registrationRoomId}&session=${session.id}`);await page.getByRole('button',{name:'Zaustavi očitavanje'}).click();await page.getByRole('button',{name:'Počni očitavanje'}).waitFor();await snap('34-asistent-zavrsena-sesija');
  }
  page=await professor.newPage();await goto(state.url.replace('http://localhost:4200',''));await page.locator('.risk-warning').first().waitFor({timeout:60000});
  for(let i=0;i<65;i++){await fetch('http://127.0.0.1:'+state.controlPort+'/pulse');if(await page.locator('.alert-toast').count())break;await pause(2000);}
  if(!(await page.locator('.alert-toast').count()))throw Error('Expected a real alert toast');await snap('36-toast-obavestenje');await page.locator('.risk-warning').first().waitFor({timeout:60000});await snap('37-zavrsna-mapa');
  const manifest=JSON.parse(fs.readFileSync(path.join(out,'manifest.json'),'utf8'));manifest.find(x=>x.file==='04-mapa-uzivo.png').title=`Mapa A-0-15: crvena ${state.criticalDesk}, narandžasta ${state.warningDesk} i tri uređaja`;fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify(manifest,null,2));
  let readme=fs.readFileSync(path.join(out,'PROCITAJ.md'),'utf8').replaceAll('K-04',state.criticalDesk).replaceAll('K-06',state.warningDesk).replace(/http:\/\/localhost:4200\/floor-map\?room=[^\s]+/,state.url);readme+='\n\nScreenshotovi učionice ponovo su snimljeni na rasporedu A-0-15. Registracija i provera senzora koriste zasebne demo učionice sa istim rasporedom A-0-15.\n';fs.writeFileSync(path.join(out,'PROCITAJ.md'),readme);
  console.log('A015 SCREENSHOTS COMPLETE');await client.endAsync();await browser.close();
})().catch(async e=>{console.error(e.stack);try{await browser.close();}catch{}process.exit(1);});
