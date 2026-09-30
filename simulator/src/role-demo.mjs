// Both demonstrations use the same traffic generator and the application's public API.
export async function prepareRoleDemo({ role, api, login, publishDevice, sleep, roomId, sessionId,
  assistantUsername, assistantPassword }) {
  let token;
  if (role === 'Assistant') {
    try { token = await login(assistantUsername, assistantPassword); }
    catch (error) {
      if (error.status !== 401) throw error;
      const professorToken = await login();
      await api('/api/staff/assistants', { method: 'POST', token: professorToken,
        body: { username: assistantUsername, password: assistantPassword } });
      token = await login(assistantUsername, assistantPassword);
    }
  } else token = await login();
  const me = await api('/api/staff/me', { token });
  if (me.role !== role) throw new Error(`Expected ${role}, but ${me.username} has role ${me.role}. Check simulator credentials.`);
  const personalDevice = role === 'Assistant' ? `SIM-ASSISTANT-${me.id}` : null;
  if (personalDevice) {
    const devices = await api('/api/staff/devices', { token });
    if (!devices.some(d => d.label === personalDevice)) {
      if (sessionId) throw new Error('First run demo-assistant without --session to register its simulated personal device. Stop active monitoring first.');
      const current = await api('/api/staff/scans/current', { token });
      if (current) throw new Error('Finish or cancel the existing registration on My devices before starting the demo.');
      const scan = await api('/api/staff/scans', { method: 'POST', token, body: { roomId } });
      try {
        await publishDevice(personalDevice, scan.id, token);
        let candidate;
        for (let attempt = 0; attempt < 30; attempt++) {
          const data = await api(`/api/staff/scans/${scan.id}`, { token });
          if (data.candidates.length > 1) throw new Error('Unexpected devices in demo registration. No devices were confirmed.');
          if (data.candidates.length === 1) { candidate = data.candidates[0]; break; }
          await sleep(1000);
        }
        if (!candidate) throw new Error('No registration reading received within 30 seconds. Check the ingestion worker.');
        await api(`/api/staff/scans/${scan.id}/confirm`, { method: 'POST', token,
          body: { devices: [{ deviceId: candidate.id, label: personalDevice }] } });
      } catch (error) {
        await api(`/api/staff/scans/${scan.id}/cancel`, { method: 'POST', token }).catch(() => {});
        throw error;
      }
    }
  }
  const session = sessionId
    ? await api(`/api/sessions/${sessionId}`, { token })
    : await api('/api/sessions/start-for-room', { method: 'POST', token,
      body: { roomId, name: `Demo ${role === 'Assistant' ? 'asistent' : 'profesor'} · ${me.username} · ${new Date().toISOString()}` } });
  if (session.status !== 'active') throw new Error('The supplied session must be active. Start it in the application first.');
  return { token, sessionId: session.id, personalDevice, username: me.username, roomId: session.roomId };
}
