import { test } from 'node:test';
import assert from 'node:assert/strict';
import { prepareRoleDemo } from './role-demo.mjs';

function fixture(role = 'Assistant', registered = false, candidates = [{ id: 'device' }]) {
  const calls = [];
  const api = async (path, options = {}) => {
    calls.push({ path, ...options });
    if (path === '/api/staff/me') return { id: 'user', username: 'assistant', role };
    if (path === '/api/staff/devices') return registered ? [{ label: 'SIM-ASSISTANT-user' }] : [];
    if (path === '/api/staff/scans/current') return null;
    if (path === '/api/staff/scans') return { id: 'scan' };
    if (path === '/api/staff/scans/scan') return { candidates };
    if (path === '/api/sessions/start-for-room') return { id: 'session', roomId: options.body.roomId, status: 'active' };
  };
  return { calls, options: { role, api, login: async username => username ? 'assistant-token' : 'professor-token',
    publishDevice: async () => {}, sleep: async () => {}, roomId: 'UC-202',
    assistantUsername: 'assistant', assistantPassword: 'long-demo-password' } };
}

test('assistant confirms observed device before starting its own session', async () => {
  const f = fixture();
  const result = await prepareRoleDemo(f.options);
  assert.equal(result.personalDevice, 'SIM-ASSISTANT-user');
  const confirmation = f.calls.find(c => c.path.endsWith('/confirm'));
  assert.deepEqual(confirmation.body.devices, [{ deviceId: 'device', label: result.personalDevice }]);
  assert.equal(f.calls.at(-1).path, '/api/sessions/start-for-room');
  assert.ok(f.calls.every(c => c.token === 'assistant-token'));
});
test('registered assistant skips scanning on subsequent runs', async () => {
  const f = fixture('Assistant', true);
  await prepareRoleDemo(f.options);
  assert.ok(!f.calls.some(c => c.path.includes('/scans')));
});
test('professor skips personal registration', async () => {
  const f = fixture('Professor');
  const result = await prepareRoleDemo(f.options);
  assert.equal(result.personalDevice, null);
  assert.ok(f.calls.every(c => c.token === 'professor-token'));
});
test('ambiguous registration cancels without confirming or starting monitoring', async () => {
  const f = fixture('Assistant', false, [{ id: 'one' }, { id: 'two' }]);
  await assert.rejects(prepareRoleDemo(f.options), /Unexpected devices/);
  assert.equal(f.calls.at(-1).path, '/api/staff/scans/scan/cancel');
  assert.ok(!f.calls.some(c => c.path.endsWith('/confirm') || c.path.includes('start-for-room')));
});
test('missing assistant uses professor only to create account', async () => {
  const f = fixture('Assistant', true); let attempts = 0;
  f.options.login = async username => {
    if (!username) return 'professor-token';
    if (attempts++ === 0) throw Object.assign(new Error('Unauthorized'), { status: 401 });
    return 'assistant-token';
  };
  await prepareRoleDemo(f.options);
  const privileged = f.calls.filter(c => c.token === 'professor-token');
  assert.equal(privileged.length, 1);
  assert.equal(privileged[0].path, '/api/staff/assistants');
});
test('wrong account role never starts monitoring', async () => {
  const f = fixture('Professor'); f.options.role = 'Assistant';
  await assert.rejects(prepareRoleDemo(f.options), /Expected Assistant/);
  assert.ok(!f.calls.some(c => c.path.includes('start-for-room')));
});
