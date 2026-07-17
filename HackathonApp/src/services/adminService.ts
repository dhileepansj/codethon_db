import httpClient from "./httpClient";
import type { UserDto, DashboardStats } from "@/types";

export const adminService = {
  getDashboard: async (): Promise<DashboardStats> => {
    const res = await httpClient.get("/api/admin/dashboard");
    return res.data.data;
  },

  getUsers: async (): Promise<UserDto[]> => {
    const res = await httpClient.get("/api/admin/users");
    return res.data.data;
  },

  createUser: async (data: { UserID: string; Password: string; FullName?: string; Email?: string }) => {
    const res = await httpClient.post("/api/admin/users", data);
    return res.data;
  },

  bulkCreateUsers: async (users: { UserID: string; Password: string; FullName?: string; Email?: string }[]) => {
    const res = await httpClient.post("/api/admin/users/bulk", users);
    return res.data;
  },

  activateSession: async (userId: string, durationMinutes?: number) => {
    const body = durationMinutes && durationMinutes > 0 ? { durationMinutes } : {};
    const res = await httpClient.post(`/api/admin/sessions/${userId}/activate`, body);
    return res.data;
  },

  deactivateSession: async (userId: string) => {
    const res = await httpClient.post(`/api/admin/sessions/${userId}/deactivate`);
    return res.data;
  },

  extendSession: async (userId: string, additionalMinutes: number) => {
    const res = await httpClient.post(`/api/admin/sessions/${userId}/extend`, { additionalMinutes });
    return res.data;
  },

  resetDatabase: async (userId: string) => {
    const res = await httpClient.post(`/api/admin/users/${userId}/reset-db`);
    return res.data;
  },

  changeUserPassword: async (userId: string, newPassword: string) => {
    const res = await httpClient.post(`/api/admin/users/${userId}/change-password`, { newPassword });
    return res.data;
  },

  getUserFiles: async (userId: string) => {
    const res = await httpClient.get(`/api/admin/users/${userId}/files`);
    return res.data.data;
  },

  getUserHistory: async (userId: string, page = 1, pageSize = 20) => {
    const res = await httpClient.get(`/api/history/${userId}`, { params: { page, pageSize } });
    return res.data.data;
  },

  exportUser: async (userId: string) => {
    try {
      const res = await httpClient.get(`/api/admin/export/${userId}`, { responseType: "blob" });
      downloadBlob(res.data, `NovacCodeLab_Export_${userId}.zip`);
    } catch (err: any) {
      // Axios error with blob response — parse the error blob
      if (err.response?.data instanceof Blob) {
        const text = await err.response.data.text();
        try {
          const json = JSON.parse(text);
          throw new Error(json.errors?.[0] || json.message || "Nothing to export");
        } catch (parseErr: any) {
          if (parseErr.message !== "Nothing to export" && !parseErr.message.includes("No files")) throw new Error("Export failed");
          throw parseErr;
        }
      }
      throw new Error(err.response?.data?.errors?.[0] || "Export failed");
    }
  },

  exportAll: async () => {
    try {
      const res = await httpClient.get("/api/admin/export/all", { responseType: "blob" });
      downloadBlob(res.data, `NovacCodeLab_Export_All.zip`);
    } catch (err: any) {
      if (err.response?.data instanceof Blob) {
        const text = await err.response.data.text();
        try {
          const json = JSON.parse(text);
          throw new Error(json.errors?.[0] || json.message || "Nothing to export");
        } catch (parseErr: any) {
          if (parseErr.message !== "Nothing to export" && !parseErr.message.includes("No files") && !parseErr.message.includes("No participants")) throw new Error("Export failed");
          throw parseErr;
        }
      }
      throw new Error(err.response?.data?.errors?.[0] || "Export failed");
    }
  },

  configureServer: async (data: { ServerName: string; AdminUserId: string; AdminPassword: string; DbPrefix?: string }) => {
    const res = await httpClient.post("/api/admin/config/hackathon-server", data);
    return res.data;
  },

  getServerConfig: async () => {
    const res = await httpClient.get("/api/admin/config/hackathon-server");
    return res.data.data;
  },

  // ─── Question Paper ────────────────────────────────────────────

  getQuestionPaper: async () => {
    const res = await httpClient.get("/api/admin/question-paper");
    return res.data.data;
  },

  saveQuestionPaper: async (data: {
    title: string;
    htmlContent: string;
    scheduledDate?: string;
    startTime?: string;
    endTime?: string;
    durationMinutes?: number;
  }) => {
    const res = await httpClient.post("/api/admin/question-paper", data);
    return res.data;
  },

  uploadQuestionHtml: async (file: File): Promise<{ htmlContent: string; fileName: string }> => {
    const formData = new FormData();
    formData.append("file", file);
    const res = await httpClient.post("/api/admin/question-paper/upload-html", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return res.data.data;
  },
};

function downloadBlob(data: Blob, filename: string) {
  const url = window.URL.createObjectURL(data);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  window.URL.revokeObjectURL(url);
}
