import httpClient from "./httpClient";

export interface SecuritySettings {
  id: number;
  minLength: number;
  maxLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireSpecialChar: boolean;
  passwordHistoryCount: number;
  maxFailedLoginAttempts: number;
  lockoutDurationMinutes: number;
  passwordExpiryDays: number;
  maxConcurrentSessions: number;
  modifiedDate?: string;
  modifiedBy?: string;
}

export interface PasswordChangeLogEntry {
  id: number;
  userId: number;
  loginId: string;
  fullName?: string;
  changedBy: string;
  changedByUserId?: string;
  changedAt: string;
  ipAddress?: string;
}

export const securityService = {
  getSettings: async (): Promise<SecuritySettings> => {
    const res = await httpClient.get("/api/admin/security-settings");
    return res.data.data;
  },

  updateSettings: async (data: Omit<SecuritySettings, "id" | "modifiedDate" | "modifiedBy">): Promise<void> => {
    await httpClient.put("/api/admin/security-settings", data);
  },

  getPasswordChangeLogs: async (userId?: string): Promise<PasswordChangeLogEntry[]> => {
    const params = userId ? { userId } : {};
    const res = await httpClient.get("/api/admin/password-change-logs", { params });
    return res.data.data || res.data;
  },
};
