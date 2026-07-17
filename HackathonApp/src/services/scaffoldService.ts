import httpClient from "./httpClient";

export interface ScaffoldScript {
  id: number;
  title: string;
  fileName: string;
  sqlContent: string;
  executionOrder: number;
  isActive: boolean;
  createdDate?: string;
  createdBy?: string;
}

export const scaffoldService = {
  getAll: async (): Promise<ScaffoldScript[]> => {
    const res = await httpClient.get("/api/admin/scaffold-scripts");
    return res.data.data || res.data;
  },

  create: async (data: { title: string; fileName?: string; sqlContent: string; executionOrder: number }): Promise<ScaffoldScript> => {
    const res = await httpClient.post("/api/admin/scaffold-scripts", data);
    return res.data.data || res.data;
  },

  update: async (id: number, data: { title?: string; fileName?: string; sqlContent?: string; executionOrder?: number; isActive?: boolean }): Promise<void> => {
    await httpClient.put(`/api/admin/scaffold-scripts/${id}`, data);
  },

  delete: async (id: number): Promise<void> => {
    await httpClient.delete(`/api/admin/scaffold-scripts/${id}`);
  },
};
