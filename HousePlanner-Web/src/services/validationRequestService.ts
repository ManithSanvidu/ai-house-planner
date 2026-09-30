import apiClient from './apiClient';

const API_BASE_URL = 'http://localhost:5265/api/validation-requests';

export const validationRequestService = {
 getAll: async (statuses?: string[], page: number = 1, pageSize: number = 50) => {
  let url = API_BASE_URL;
  const params = new URLSearchParams();
  if (statuses && statuses.length > 0) {
   statuses.forEach(s => params.append('status', s));
  }
  params.append('page', page.toString());
  params.append('pageSize', pageSize.toString());
  
  url = `${API_BASE_URL}?${params.toString()}`;
  
  const response = await apiClient.get(url);
  return response.data;
 },

 getSummary: async (): Promise<{ pending: number; underReview: number; approved: number; rejected: number }> => {
  const response = await apiClient.get(`${API_BASE_URL}/summary`);
  return response.data;
 },

 getById: async (id: string, signal?: AbortSignal) => {
  const response = await apiClient.get(`${API_BASE_URL}/${id}`, { signal });
  return response.data;
 },

 approve: async (id: string, review?: string) => {
  const response = await apiClient.patch(`${API_BASE_URL}/${id}/approve`, { review });
  return response.data;
 },

 reject: async (id: string, review?: string) => {
  const response = await apiClient.patch(`${API_BASE_URL}/${id}/reject`, { review });
  return response.data;
 },

 create: async (workflowStateId: string) => {
  const response = await apiClient.post(`${API_BASE_URL}`, { workflowStateId });
  return response.data;
 }
};
