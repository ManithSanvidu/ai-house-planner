import { beforeEach, describe, expect, it, vi } from 'vitest';
import apiClient from './apiClient';
import { validationRequestService } from './validationRequestService';

vi.mock('./apiClient', () => ({
 default: { get: vi.fn(), patch: vi.fn() },
}));

describe('validationRequestService', () => {
 beforeEach(() => vi.clearAllMocks());

 it('uses configured API-client-relative architect routes for dashboard, list, and details', async () => {
  vi.mocked(apiClient.get).mockResolvedValue({ data: {} } as never);
  await validationRequestService.getSummary();
  await validationRequestService.getAll(['Pending', 'Under Review'], 2, 25);
  await validationRequestService.getById('request-1');

  expect(apiClient.get).toHaveBeenNthCalledWith(1, '/architect/validation-requests/summary');
  expect(apiClient.get).toHaveBeenNthCalledWith(
   2,
   '/architect/validation-requests?status=Pending&status=Under+Review&page=2&pageSize=25',
  );
  expect(apiClient.get).toHaveBeenNthCalledWith(
   3,
   '/architect/validation-requests/request-1',
   { signal: undefined },
  );
 });

 it('uses focused architect decision routes', async () => {
  vi.mocked(apiClient.patch).mockResolvedValue({ data: {} } as never);
  await validationRequestService.approve('request-1', 'Approved');
  await validationRequestService.reject('request-2', 'Rejected');
  await validationRequestService.requestRevision('request-3', 'Revise circulation');

  expect(apiClient.patch).toHaveBeenNthCalledWith(
   1, '/architect/validation-requests/request-1/approve', { review: 'Approved' },
  );
  expect(apiClient.patch).toHaveBeenNthCalledWith(
   2, '/architect/validation-requests/request-2/reject', { review: 'Rejected' },
  );
  expect(apiClient.patch).toHaveBeenNthCalledWith(
   3, '/architect/validation-requests/request-3/request-revision', { review: 'Revise circulation' },
  );
 });
});
