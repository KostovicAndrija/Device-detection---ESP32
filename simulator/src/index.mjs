import mqtt from 'mqtt';
import { randomUUID } from 'node:crypto';
import { prepareRoleDemo } from './role-demo.mjs';

const MQTT_URL = process.env.MQTT_URL ?? 'mqtt://localhost:1883';
const API_URL = (process.env.API_URL ?? 'http://localhost:7108').replace(/\/$/, '');
const USERNAME = process.env.APP_USERNAME ?? 'admin';
const PASSWORD = process.env.APP_PASSWORD ?? 'admin';
const args = process.argv.slice(2);
const command = args[0] ?? 'help';

const option = (name, fallback) => {
  const index = args.indexOf(`--${name}`);
  return index >= 0 && args[index + 1] ? args[index + 1] : fallback;
};
const numberOption = (name, fallback) => {
  const value = Number(option(name, fallback));
  if (!Number.isFinite(value) || value <= 0) throw new Error(`--${name} must be a positive number`);
  return value;
};
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));
const iso = (offsetMs = 0) => new Date(Date.now() + offsetMs).toISOString();
const eventId = prefix => `${prefix}-${Date.now()}-${randomUUID().slice(0, 8)}`;

function connectMqtt() {
  return new Promise((resolve, reject) => {
    const client = mqtt.connect(MQTT_URL, {
      clientId: `sensor-simulator-${randomUUID().slice(0, 8)}`,
      reconnectPeriod: 1000,
      connectTimeout: 10_000
    });
    const timer = setTimeout(() => {
      client.end(true);
      reject(new Error(`MQTT connection timed out: ${MQTT_URL}`));
    }, 15_000);
    client.once('connect', () => {
      clearTimeout(timer);
      console.log(`Connected to ${MQTT_URL}`);
      resolve(client);
    });
    client.once('error', error => {
      clearTimeout(timer);
      reject(error);
    });
  });
}

function publish(client, sensorId, payload, qos = 1) {
  const topic = `sensors/${sensorId}/rssi`;
  const body = typeof payload === 'string' ? payload : JSON.stringify(payload);
  return new Promise((resolve, reject) => {
    client.publish(topic, body, { qos }, error => error ? reject(error) : resolve());
  });
}

const reading = ({ device, sensor = 'S1', sessionId = null, signal = 'wifi', rssi = -70,
  timestamp = iso(), id = eventId(sensor) }) => ({
  eventId: id,
  deviceIdentifier: device,
  sensorId: sensor,
  sessionId,
  signalType: signal,
  rssi,
  timestamp
});

async function api(path, { method = 'GET', token, body } = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    method,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(body ? { 'Content-Type': 'application/json' } : {})
    },
    body: body ? JSON.stringify(body) : undefined
  });
  const text = await response.text();
  const data = text ? (() => { try { return JSON.parse(text); } catch { return text; } })() : null;
  if (!response.ok) {
    const error = new Error(`${method} ${path} returned ${response.status}: ${text}`);
    error.status = response.status;
    throw error;
  }
  return data;
}

async function login(username = USERNAME, password = PASSWORD) {
  const login = await api('/api/auth/login', {
    method: 'POST', body: { username, password }
  });
  return login.accessToken;
}

async function prepareSession(token) {
  const session = await api('/api/sessions', {
    method: 'POST', token,
    body: {
      name: `Simulator ${new Date().toLocaleString('sv-SE')}`,
      roomId: 'UC-101',
      startsAt: iso()
    }
  });
  await api(`/api/sessions/${session.id}/start`, { method: 'POST', token });
  console.log(`Created and started session ${session.id}`);
  return { token, sessionId: session.id };
}

async function addWhitelist(token, sessionId, device, studentRef) {
  await api(`/api/sessions/${sessionId}/whitelist`, {
    method: 'POST', token,
    body: {
      studentRef,
      deviceIdentifier: device,
      validFrom: iso(-60_000),
      validTo: iso(60 * 60_000)
    }
  });
}

async function normal(client, sessionId) {
  console.log('Scenario: normal traffic');
  for (let i = 0; i < 8; i++) {
    await publish(client, `S${i % 3 + 1}`, reading({
      device: '02:00:00:00:10:01', sensor: `S${i % 3 + 1}`, sessionId,
      signal: i % 2 ? 'ble' : 'wifi', rssi: -88 + (i % 4)
    }));
    await sleep(80);
  }
}

async function highRisk(client, sessionId) {
  console.log('Scenario: high-risk Bluetooth pairing');
  await publish(client, 'S2', reading({
    device: '02:00:00:00:20:01', sensor: 'S2', sessionId,
    signal: 'bluetooth_pairing', rssi: -42
  }));
}

async function whitelisted(client, token, sessionId) {
  console.log('Scenario: whitelisted device');
  const device = '02:00:00:00:30:01';
  await addWhitelist(token, sessionId, device, 'SIM-STUDENT-1');
  await publish(client, 'S1', reading({ device, sessionId, rssi: -35 }));
}

const sensors = [
  { id: 'S1', x: 0, y: 0 },
  { id: 'S2', x: 8, y: 0 },
  { id: 'S3', x: 4, y: 6 }
];
const path = [
  { x: 1, y: 1 }, { x: 2.5, y: 2 }, { x: 4, y: 3 }, { x: 5.5, y: 2 }, { x: 7, y: 1 }
];
const noise = [0, -1.2, 0.8, -0.5, 1.1, -0.7, 0.4];

function rssiAt(point, sensor, sampleIndex) {
  const distance = Math.max(1, Math.hypot(point.x - sensor.x, point.y - sensor.y));
  return Math.max(-120, Math.min(0, Math.round((-45 - 27 * Math.log10(distance) + noise[sampleIndex % noise.length]) * 10) / 10));
}

async function localization(client, token, sessionId) {
  console.log('Scenario: movement/localization through S1, S2 and S3');
  const device = '02:00:00:00:40:01';
  await addWhitelist(token, sessionId, device, 'SIM-LOCALIZATION');
  for (let p = 0; p < path.length; p++) {
    for (let s = 0; s < sensors.length; s++) {
      const sensor = sensors[s];
      await publish(client, sensor.id, reading({
        device, sensor: sensor.id, sessionId,
        rssi: rssiAt(path[p], sensor, p * sensors.length + s)
      }));
    }
    console.log(`  point ${p + 1}/${path.length}: (${path[p].x}, ${path[p].y})`);
    await sleep(250);
  }
}

async function edgeCases(client, sessionId) {
  console.log('Scenario: edge cases (errors below are expected in worker logs)');
  const duplicateId = eventId('duplicate');
  const duplicate = reading({ device: '02:00:00:00:50:01', sessionId, rssi: -100, id: duplicateId });
  await publish(client, 'S1', duplicate);
  await sleep(250);
  await publish(client, 'S1', duplicate);

  await publish(client, 'S1', reading({ device: '02:00:00:00:50:02', sessionId, rssi: -120 }));
  await publish(client, 'S1', reading({ device: '02:00:00:00:50:03', sessionId, rssi: 0 }));
  await publish(client, 'UNKNOWN', reading({
    device: '02:00:00:00:50:04', sensor: 'UNKNOWN', sessionId, rssi: -70
  }));
  await publish(client, 'S1', reading({
    device: '02:00:00:00:50:05', sessionId, rssi: -70, timestamp: iso(-24 * 60 * 60_000)
  }));
  await publish(client, 'S1', reading({
    device: '02:00:00:00:50:06', sessionId, rssi: -70, timestamp: iso(24 * 60 * 60_000)
  }));
  const noOptionalFields = reading({ device: '02:00:00:00:50:07', sessionId, rssi: -75 });
  delete noOptionalFields.eventId;
  delete noOptionalFields.timestamp;
  await publish(client, 'S1', noOptionalFields);
  await publish(client, 'S1', '{not-valid-json');
  await publish(client, 'S1', JSON.stringify({ deviceIdentifier: '02:00:00:00:50:08', sensorId: 'S1', rssi: -70 }));
  await publish(client, 'S1', reading({ device: '02:00:00:00:50:09', sessionId, rssi: -121 }));
  await publish(client, 'S1', reading({ device: '02:00:00:00:50:10', sessionId, rssi: 1 }));
  await publish(client, 'blank', reading({ device: '02:00:00:00:50:11', sensor: ' ', sessionId, rssi: -70 }));
}

async function load(client, sessionId) {
  const devices = Math.floor(numberOption('devices', 100));
  const rate = numberOption('rate', 20);
  const count = Math.floor(numberOption('count', devices * 5));
  console.log(`Scenario: load (${count} messages, ${devices} devices, ${rate}/s)`);
  for (let i = 0; i < count; i++) {
    const n = i % devices;
    const sensor = `S${i % 3 + 1}`;
    await publish(client, sensor, reading({
      device: `SIM-LOAD-${String(n).padStart(5, '0')}`,
      sensor, sessionId, signal: i % 4 === 0 ? 'ble' : 'wifi', rssi: -95 + (i % 35)
    }));
    await sleep(1000 / rate);
  }
}

async function demo(client, token, sessionId, personalDevice = null) {
  const duration = numberOption('duration', 900);
  const intervalSeconds = numberOption('interval', 2);
  const deviceCount = Math.max(1, Math.min(5, Math.floor(numberOption('devices', 5))));
  const zones = [
    { x: 1.2, y: 1.4 },
    { x: 4.0, y: 1.5 },
    { x: 6.8, y: 1.4 },
    { x: 2.1, y: 4.5 },
    { x: 5.9, y: 4.5 }
  ];
  const demoDevices = Array.from({ length: deviceCount }, (_, index) => ({
    id: index === 0 && personalDevice ? personalDevice : `SIM-DEMO-${String(index + 1).padStart(2, '0')}`,
    joinsAt: index * 3,
    phase: index * 5,
    zone: zones[index]
  }));
  if (!personalDevice) await addWhitelist(token, sessionId, demoDevices[0].id, 'DEMO-WHITELIST-1');

  // Isti serijski boot/status format koji ispisuje stvarni ESP32 sniffer.
  // MQTT poruke ispod ostaju JSON jer ih takve očekuje ingestion worker.
  console.log('ets Jul 29 2019 12:21:46');
  const started = Date.now();
  let tick = 0;
  while ((Date.now() - started) / 1000 < duration) {
    const elapsed = (Date.now() - started) / 1000;
    const channel = tick % 13 + 1;
    console.log(`Sniffing channel ${channel}`);
    for (let index = 0; index < demoDevices.length; index++) {
      const device = demoDevices[index];
      if (elapsed < device.joinsAt) continue;
      // Uređaji su prisutni većinu vremena. Kratka pauza simulira telefon koji
      // utihne, ali je kraća od vremena zadržavanja markera na mapi.
      if ((elapsed - device.joinsAt + device.phase) % 90 >= 75) continue;
      const angle = elapsed * 0.12 + index * 1.25;
      const point = {
        x: device.zone.x + Math.cos(angle) * 0.35,
        y: device.zone.y + Math.sin(angle * 0.85) * 0.3
      };
      for (let sensorIndex = 0; sensorIndex < sensors.length; sensorIndex++) {
        const sensor = sensors[sensorIndex];
        await publish(client, sensor.id, reading({
          device: device.id,
          sensor: sensor.id,
          sessionId,
          signal: index === 1 ? 'bluetooth_pairing' : (index % 3 === 0 ? 'ble' : 'wifi'),
          rssi: index === 1 ? Math.max(-48, rssiAt(point, sensor, tick + sensorIndex + index)) : rssiAt(point, sensor, tick + sensorIndex + index)
        }));
      }
    }
    tick++;
    await sleep(intervalSeconds * 1000);
  }
}

async function verify(token, sessionId) {
  const deadline = Date.now() + 10_000;
  let alerts = [];
  let positions = [];
  let devices = [];
  let threeSensorPosition = false;
  do {
    [alerts, positions, devices] = await Promise.all([
      api(`/api/sessions/${sessionId}/alerts`, { token }),
      api(`/api/sessions/${sessionId}/positions`, { token }),
      api('/api/devices/active?windowMinutes=10', { token })
    ]);
    threeSensorPosition = positions.some(position => position.sensorCount === 3);
    if (alerts.length >= 2 && threeSensorPosition && devices.length >= 8) break;
    await sleep(500);
  } while (Date.now() < deadline);
  console.log('\nVerification');
  console.log(`  ${alerts.length >= 2 ? 'PASS' : 'FAIL'} alerts created: ${alerts.length} (expected at least 2)`);
  console.log(`  ${threeSensorPosition ? 'PASS' : 'FAIL'} three-sensor position available`);
  console.log(`  ${devices.length >= 8 ? 'PASS' : 'FAIL'} active devices: ${devices.length} (expected at least 8)`);
  console.log(`  Session: ${sessionId}`);
  if (alerts.length < 2 || !threeSensorPosition || devices.length < 8) process.exitCode = 2;
}

function help() {
  console.log(`ESP32 sensor simulator

Usage:
  node src/index.mjs <scenario> [options]

Scenarios:
  suite          Run normal, whitelist, high-risk, localization and edge cases
  normal         Valid low-risk Wi-Fi/BLE traffic
  high-risk      Strong Bluetooth pairing event that should create an alert
  whitelist      Strong signal from a whitelisted device
  localization   Deterministic movement observed by S1, S2 and S3
  edge-cases     Duplicates, boundaries, bad JSON, missing fields and bad values
  load           Configurable traffic volume
  demo           Live classroom traffic with devices appearing and fading
  demo-professor Professor-owned demo (default room UC-101)
  demo-assistant Assistant-owned demo with personal-device registration (UC-202)

Options:
  --room <id>        Room for demo-professor/demo-assistant
  --session <id>     Use an existing session instead of creating one
  --devices <n>      Device count (demo default and maximum 5, load default 100)
  --count <n>        Message count for load (default devices * 5)
  --rate <n>         Messages per second for load (default 20)
  --duration <sec>   Demo duration (default 900)
  --interval <sec>   Demo publish interval (default 2)`);
}

async function main() {
  if (command === 'help' || command === '--help' || command === '-h') return help();
  if (command === 'demo-professor' || command === 'demo-assistant') {
    // Validate traffic options before creating accounts, scans or sessions.
    numberOption('duration', 900); numberOption('interval', 2); numberOption('devices', 5);
    const role = command === 'demo-assistant' ? 'Assistant' : 'Professor';
    const client = await connectMqtt();
    try {
      const prepared = await prepareRoleDemo({ role, api, login, sleep,
        roomId: option('room', role === 'Assistant' ? 'UC-202' : 'UC-101'),
        sessionId: option('session', null),
        assistantUsername: process.env.ASSISTANT_USERNAME ?? 'demo-asistent',
        assistantPassword: process.env.ASSISTANT_PASSWORD ?? 'AsistentDemo2026!',
        publishDevice: (device, sessionId) => publish(client, 'S1', reading({ device, sessionId, rssi: -40 })) });
      console.log(`Demo ${role}: ${prepared.username}; room ${prepared.roomId}; session ${prepared.sessionId}`);
      console.log(`Open http://localhost:4200/floor-map?room=${prepared.roomId}&session=${prepared.sessionId}`);
      await demo(client, prepared.token, prepared.sessionId, prepared.personalDevice);
    } finally { await new Promise(resolve => client.end(false, {}, resolve)); }
    return;
  }
  const suppliedSession = option('session', null);
  const requiresToken = !suppliedSession || ['suite', 'whitelist', 'localization', 'demo'].includes(command);
  const token = requiresToken ? await login() : null;
  const prepared = suppliedSession
    ? { token, sessionId: suppliedSession }
    : await prepareSession(token);
  const client = await connectMqtt();
  try {
    if (command === 'suite') {
      await normal(client, prepared.sessionId);
      await whitelisted(client, prepared.token, prepared.sessionId);
      await highRisk(client, prepared.sessionId);
      await localization(client, prepared.token, prepared.sessionId);
      await edgeCases(client, prepared.sessionId);
      await verify(prepared.token, prepared.sessionId);
    } else if (command === 'normal') await normal(client, prepared.sessionId);
    else if (command === 'high-risk') await highRisk(client, prepared.sessionId);
    else if (command === 'whitelist') await whitelisted(client, prepared.token, prepared.sessionId);
    else if (command === 'localization') await localization(client, prepared.token, prepared.sessionId);
    else if (command === 'edge-cases') await edgeCases(client, prepared.sessionId);
    else if (command === 'load') await load(client, prepared.sessionId);
    else if (command === 'demo') await demo(client, prepared.token, prepared.sessionId);
    else throw new Error(`Unknown scenario: ${command}`);
    await sleep(300);
  } finally {
    await new Promise(resolve => client.end(false, {}, resolve));
  }
}

main().catch(error => {
  console.error(`Simulator failed: ${error.stack ?? error.message}`);
  process.exitCode = 1;
});
