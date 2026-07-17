import httpClient from "./httpClient";

export interface AiDetectionSettings {
  mode: string;
  blockThreshold: number;
  modifiedDate?: string;
  modifiedBy?: string;
  userOverrides: UserOverride[];
}

export interface UserOverride {
  userId: number;
  loginId?: string;
  fullName?: string;
  mode?: string;
  blockThreshold?: number;
  modifiedDate: string;
  modifiedBy?: string;
}

export interface BlockedSave {
  id: number;
  userId: number;
  loginId?: string;
  fullName?: string;
  fileId: number;
  fileName: string;
  attemptedContent?: string;
  confidenceScore: number;
  reasoning?: string;
  status: string;
  reviewedBy?: string;
  reviewedDate?: string;
  adminRemarks?: string;
  blockedDate: string;
}

export interface AiDetectionLog {
  id: number;
  userId: number;
  loginId?: string;
  fullName?: string;
  fileId: number;
  fileName: string;
  confidenceScore: number;
  detectionResult: string;
  reasoning?: string;
  contentLength: number;
  contentDelta: number;
  tabSwitchBeforeSave: boolean;
  modelUsed?: string;
  processingTimeMs?: number;
  analyzedDate: string;
}

export const aiDetectionService = {
  // ─── Settings ─────────────────────────────────────────────

  getSettings: async (): Promise<AiDetectionSettings> => {
    const res = await httpClient.get("/api/admin/ai-detection/settings");
    return res.data.data;
  },

  updateSettings: async (data: { mode: string; blockThreshold: number }) => {
    const res = await httpClient.put("/api/admin/ai-detection/settings", {
      Mode: data.mode,
      BlockThreshold: data.blockThreshold,
    });
    return res.data;
  },

  setUserOverride: async (userId: string, data: { mode?: string; blockThreshold?: number }) => {
    const res = await httpClient.put(`/api/admin/ai-detection/settings/user/${userId}`, {
      Mode: data.mode,
      BlockThreshold: data.blockThreshold,
    });
    return res.data;
  },

  removeUserOverride: async (userId: string) => {
    const res = await httpClient.delete(`/api/admin/ai-detection/settings/user/${userId}`);
    return res.data;
  },

  // ─── Blocked Saves ────────────────────────────────────────

  getBlockedSaves: async (status?: string): Promise<BlockedSave[]> => {
    const params = status ? { status } : {};
    const res = await httpClient.get("/api/admin/ai-detection/blocked", { params });
    return res.data.data;
  },

  getUserBlockedSaves: async (userId: string): Promise<BlockedSave[]> => {
    const res = await httpClient.get(`/api/admin/ai-detection/blocked/user/${userId}`);
    return res.data.data;
  },

  approveBlockedSave: async (id: number, remarks?: string) => {
    const res = await httpClient.post(`/api/admin/ai-detection/blocked/${id}/approve`, { Remarks: remarks });
    return res.data;
  },

  rejectBlockedSave: async (id: number, remarks?: string) => {
    const res = await httpClient.post(`/api/admin/ai-detection/blocked/${id}/reject`, { Remarks: remarks });
    return res.data;
  },

  // ─── Logs ─────────────────────────────────────────────────

  getFlaggedLogs: async (minScore = 60): Promise<AiDetectionLog[]> => {
    const res = await httpClient.get("/api/admin/ai-detection/flagged", { params: { minScore } });
    return res.data.data;
  },

  getUserLogs: async (userId: string): Promise<AiDetectionLog[]> => {
    const res = await httpClient.get(`/api/admin/ai-detection/user/${userId}`);
    return res.data.data;
  },
};
