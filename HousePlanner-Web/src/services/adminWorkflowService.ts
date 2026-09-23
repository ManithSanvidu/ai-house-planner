import apiClient from './apiClient';

export interface AdminWorkflowSummary {
  workflowId: string;
  clientName: string;
  clientEmail: string;
  status: string;
  approvalStatus: string;
  createdAt: string;
}

export const adminWorkflowService = {
  getAllWorkflows: async (): Promise<AdminWorkflowSummary[]> => {
    const response = await apiClient.get('/workflows/admin/all');
    return response.data;
  },

  deleteWorkflow: async (id: string): Promise<void> => {
    await apiClient.delete(`/workflows/admin/${id}`);
  }
};
