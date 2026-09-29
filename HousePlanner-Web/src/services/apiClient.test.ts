import { beforeEach, expect, test, vi } from 'vitest';

const { getSession, refreshSession } = vi.hoisted(() => ({
 getSession: vi.fn(),
 refreshSession: vi.fn(),
}));

vi.mock('../lib/supabase', () => ({
 supabase: { auth: { getSession, refreshSession } },
}));

import apiClient from './apiClient';

beforeEach(() => {
 getSession.mockReset();
 refreshSession.mockReset();
});

test('attaches the current Supabase JWT to visualization requests', async () => {
 getSession.mockResolvedValue({
  data: { session: { access_token: 'current-jwt', expires_at: Math.floor(Date.now() / 1000) + 3600 } },
 });
 let authorization: unknown;
 apiClient.defaults.adapter = async config => {
  authorization = config.headers?.Authorization;
  return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
 };

 await apiClient.get('/design/00000000-0000-0000-0000-000000000001/visualization');

 expect(authorization).toBe('Bearer current-jwt');
 expect(refreshSession).not.toHaveBeenCalled();
});

test('refreshes an expiring JWT before sending the request', async () => {
 getSession.mockResolvedValue({
  data: { session: { access_token: 'stale-jwt', expires_at: Math.floor(Date.now() / 1000) + 10 } },
 });
 refreshSession.mockResolvedValue({ data: { session: { access_token: 'fresh-jwt' } } });
 let authorization: unknown;
 apiClient.defaults.adapter = async config => {
  authorization = config.headers?.Authorization;
  return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
 };

 await apiClient.get('/design/00000000-0000-0000-0000-000000000001/visualization');
 expect(authorization).toBe('Bearer fresh-jwt');
});

test('removes the bearer token from rejected Axios request metadata', async () => {
 getSession.mockResolvedValue({
  data: { session: { access_token: 'sensitive-jwt', expires_at: Math.floor(Date.now() / 1000) + 3600 } },
 });
 apiClient.defaults.adapter = async config => {
  const error = Object.assign(new Error('request failed'), { config });
  throw error;
 };

 const error = await apiClient.get('/protected').catch(value => value);

 expect(error.config.headers.Authorization).toBeUndefined();
});
