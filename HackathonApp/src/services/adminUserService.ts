import httpClient from "./httpClient";

export interface AdminUserData {
  id: number;
  userID: string;
  fullName?: string;
  email?: string;
  role: string;
  isActive: boolean;
  permissions: AdminPermissionsData;
  createdDate: string;
  lastLoginAt?: string;
}

export interface AdminPermissionsData {
  canManageUsers: boolean;
  canManageSessions: boolean;
  canViewMonitoring: boolean;
  canManageAssessments: boolean;
  canViewResults: boolean;
  canManageHackathonSetup: boolean;
  canManageServerConfig: boolean;
  canManageScaffoldScripts: boolean;
  canManageSecuritySettings: boolean;
  canManageAiDetection: boolean;
  canExportData: boolean;
  canResetDatabase: boolean;
  canDeleteUsers: boolean;
}

export const adminUserService = {
  getAll: async (): Promise<AdminUserData[]> => {
    const res = await httpClient.get("/api/admin-users");
    return res.data.data;
  },

  create: async (data: { UserID: string; Password: string; FullName?: string; Email?: string; Permissions: AdminPermissionsData }) => {
    const res = await httpClient.post("/api/admin-users", data);
    return res.data.data;
  },

  update: async (id: number, data: { FullName?: string; Email?: string; IsActive?: boolean; Permissions?: AdminPermissionsData }) => {
    const res = await httpClient.put(`/api/admin-users/${id}`, data);
    return res.data;
  },

  changePassword: async (id: number, newPassword: string) => {
    const res = await httpClient.put(`/api/admin-users/${id}/password`, { newPassword });
    return res.data;
  },

  delete: async (id: number) => {
    const res = await httpClient.delete(`/api/admin-users/${id}`);
    return res.data;
  },
};
