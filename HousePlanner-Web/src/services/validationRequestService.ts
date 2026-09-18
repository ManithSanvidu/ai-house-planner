import apiClient from './apiClient';

const API_BASE_URL = 'http://localhost:5265/api/validation-requests';

export const validationRequestService = {
  getAll: async (status?: string) => {
    const url = status ? `${API_BASE_URL}?status=${status}` : API_BASE_URL;
    const response = await apiClient.get(url);
    return response.data;
  },

  getById: async (id: string) => {
    const response = await apiClient.get(`${API_BASE_URL}/${id}`);
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
