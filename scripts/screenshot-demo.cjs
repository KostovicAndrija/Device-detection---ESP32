// Real MQTT traffic for a repeatable presentation; never changes existing rooms/sessions.
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { randomUUID } = require('node:crypto');
const mqtt = require('../simulator/node_modules/mqtt');
const root = path.resolve(__dirname, '..');
const env = Object.fromEntries(fs.readFileSync(path.join(root,'.env'),'utf8').split(/\r?\n/).filter(x=>x.includes('=')).map(x=>{const i=x.indexOf('=');return [x.slice(0,i),x.slice(i+1)];}));
const out = path.join(root,'screenshots','2026-10-20');
const delay = ms => new Promise(r=>setTimeout(r,ms));
let token, client, room, session, lastWarning=0;
const requestedRoom=process.argv[2]??'DEMO-20';
const controlPort=requestedRoom==='A-0-15'?7120:7119;
async function api(route, method='GET', body) {
  const r = await fetch('http://localhost:7108'+route,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})},body:body?JSON.stringify(body):undefined});
  const text=await r.text();if(!r.ok)throw Error(`${method} ${route}: ${r.status} ${text}`);return text?JSON.parse(text):null;
}
function send(topic, value){return new Promise((resolve,reject)=>client.publish(topic,JSON.stringify(value),{qos:1},e=>e?reject(e):resolve()));}
async function heartbeat(layout=room){for(const s of layout.sensors)await send(`sensors/${s.id}/status`,{sensorId:s.id,classroomId:layout.id,timestamp:new Date().toISOString(),firmwareVersion:'presentation-demo',isSimulated:true});}
const devices=[
  {id:'DEMO-PRESENTATION-CRITICAL',signal:'bluetooth_pairing',x:1.55,y:3.1},
  {id:'DEMO-PRESENTATION-WARNING',signal:'wifi',x:6.45,y:3.1},
  {id:'DEMO-PRESENTATION-ALLOWED',signal:'wifi',x:4,y:1.65}
];
async function emit(device, at=Date.now(), target=session.id, layout=room){
  for(const s of layout.sensors){
    const distance=Math.max(.3,Math.hypot(device.x-s.x,device.y-s.y));
    await send(`sensors/${s.id}/rssi`,{eventId:'presentation-'+randomUUID(),deviceIdentifier:device.id,sensorId:s.id,sessionId:target,signalType:device.signal,rssi:s.referenceRssi-10*s.pathLossExponent*Math.log10(distance),timestamp:new Date(at).toISOString()});
  }
}
async function ensureRoom(id,name,sensorPrefix){
  const rooms=await api('/api/classrooms');let found=rooms.find(r=>r.id===id);if(found)return found;
  const computers=[];for(let row=0;row<3;row++)for(let col=0;col<3;col++)computers.push({id:'K-'+String(row*3+col+1).padStart(2,'0'),x:[1.55,4,6.45][col],y:[1.65,3.1,4.55][row],width:1.5,height:.65,kind:'desk',computerCount:2});
  computers.push({id:'KATEDRA',x:4,y:.55,width:1.7,height:.55,kind:'desk',computerCount:1,isTeacherDesk:true});
  return api('/api/classrooms','POST',{id,name,description:'Demonstracija lokalizacije: tri senzora, devet klupa i kontrolisani radio-signali.',width:8,height:6,revision:1,computers,
    sensors:[{id:sensorPrefix+'1',x:.2,y:.2,model:'ESP32',referenceRssi:-45,pathLossExponent:2.7},{id:sensorPrefix+'2',x:7.8,y:.2,model:'ESP32',referenceRssi:-45,pathLossExponent:2.7},{id:sensorPrefix+'3',x:4,y:5.8,model:'ESP32',referenceRssi:-45,pathLossExponent:2.7}],boards:[{id:'TABLA',x:4,y:.06,width:3,height:.12}]});
}
async function main(){
  fs.mkdirSync(out,{recursive:true});
  token=(await api('/api/auth/login','POST',{username:env.APP_USERNAME||'admin',password:env.APP_PASSWORD||'admin123'})).accessToken;
  client=await mqtt.connectAsync('mqtt://localhost:1883');
  room=requestedRoom==='A-0-15'?(await api('/api/classrooms')).find(r=>r.id===requestedRoom):await ensureRoom('DEMO-20','Demo učionica · 20. oktobar','D20-S');
  if(!room)throw Error('Requested classroom does not exist');
  let registrationRoom=await ensureRoom('DEMO-REG','Demo · registracija i pokretanje','DR-S');
  let offlineRoom=await ensureRoom('DEMO-OFF','Demo · senzori nisu aktivni','DO-S');
  let criticalDesk='K-04',warningDesk='K-06';
  if(requestedRoom==='A-0-15'){
    criticalDesk='K-03';warningDesk='K-08';
    const desks=[room.computers.find(d=>d.id===criticalDesk),room.computers.find(d=>d.id===warningDesk),room.computers.find(d=>d.isTeacherDesk)??room.computers[0]];
    devices.forEach((device,i)=>{device.x=desks[i].x;device.y=desks[i].y;});
    for(const [aux,prefix,suffix] of [[registrationRoom,'DR-S','registracija'],[offlineRoom,'DO-S','provera senzora']]){
      const updated=await api('/api/classrooms/'+aux.id,'PUT',{...room,id:aux.id,name:'Učionica A-0-15 · '+suffix,revision:aux.revision,sensors:room.sensors.map((s,i)=>({...s,id:prefix+(i+1)}))});
      if(prefix==='DR-S')registrationRoom=updated;else offlineRoom=updated;
    }
  }
  await heartbeat();await heartbeat(registrationRoom);await delay(1500);
  const sessions=await api('/api/sessions');session=sessions.find(s=>s.roomId===room.id&&s.status==='active');
  if(!session)session=await api('/api/sessions/start-for-room','POST',{roomId:room.id,name:'Demonstracija · kritična i sumnjiva klupa'});
  const entries=await api(`/api/sessions/${session.id}/whitelist`);
  if(!entries.some(e=>e.studentRef==='Telefon nastavnika'))await api(`/api/sessions/${session.id}/whitelist`,'POST',{studentRef:'Telefon nastavnika',deviceIdentifier:devices[2].id,validFrom:new Date(Date.now()-3600000).toISOString(),validTo:new Date(Date.now()+86400000).toISOString()});
  for(const age of [60,40,20,0])await emit(devices[0],Date.now()-age*1000);
  await emit(devices[1],Date.now()-40000);await emit(devices[1]);lastWarning=Date.now();await emit(devices[2]);
  const state={roomId:room.id,roomName:room.name,sessionId:session.id,registrationRoomId:registrationRoom.id,offlineRoomId:offlineRoom.id,url:`http://localhost:4200/floor-map?room=${room.id}&session=${session.id}`,criticalDesk,warningDesk,deviceCount:3,controlPort,startedAt:new Date().toISOString()};
  fs.writeFileSync(path.join(out,'demo-state.json'),JSON.stringify(state,null,2));
  http.createServer(async(req,res)=>{
    try{
      if(req.url==='/pulse')await emit(devices[0]);
      if(req.url==='/registration'){
        const scans=await api('/api/staff/scans/current');if(!scans)throw Error('No professor registration scan');
        await emit({...devices[2],id:'DEMO-PROFESSOR-PERSONAL'},Date.now(),scans.id,registrationRoom);
      }
      res.end(JSON.stringify(state));
    }catch(e){res.statusCode=500;res.end(String(e));}
  }).listen(controlPort,'127.0.0.1');
  console.log('DEMO_READY '+JSON.stringify(state));
  const started=Date.now();
  while(Date.now()-started<3600000){
    await heartbeat();await heartbeat(registrationRoom);await emit(devices[0]);await emit(devices[2]);
    if(Date.now()-lastWarning>=40000){await emit(devices[1]);lastWarning=Date.now();}
    await delay(2000);
  }
  await client.endAsync();process.exit(0);
}
main().catch(e=>{console.error(e);process.exit(1);});
