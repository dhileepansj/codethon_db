import httpClient from "./httpClient";
import type { FolderDto, FileListItem, FileDetail } from "@/types";

export interface AiBlockedResponse {
  message: string;
  blocked: true;
  confidenceScore: number;
  detectionResult: string;
  reasoning: string;
  blockedSaveId: number;
}

export const fileService = {
  getRoot: async (): Promise<{ folders: FolderDto[]; files: FileListItem[] }> => {
    const res = await httpClient.get("/api/files");
    return res.data.data;
  },

  getFolderContents: async (folderId: number): Promise<{ folders: FolderDto[]; files: FileListItem[] }> => {
    const res = await httpClient.get(`/api/files/folder/${folderId}`);
    return res.data.data;
  },

  createFolder: async (folderName: string, parentFolderId?: number): Promise<FolderDto> => {
    const res = await httpClient.post("/api/files/folder", { folderName, parentFolderId });
    return res.data.data;
  },

  renameFolder: async (folderId: number, folderName: string) => {
    return httpClient.put(`/api/files/folder/${folderId}`, { folderName });
  },

  deleteFolder: async (folderId: number) => {
    return httpClient.delete(`/api/files/folder/${folderId}`);
  },

  createFile: async (data: { fileName: string; fileType?: string; content?: string; folderId?: number }): Promise<FileListItem> => {
    const res = await httpClient.post("/api/files", data);
    return res.data.data;
  },

  getFile: async (fileId: number): Promise<FileDetail> => {
    const res = await httpClient.get(`/api/files/${fileId}`);
    return res.data.data;
  },

  updateFile: async (fileId: number, data: { fileName?: string; fileType?: string; content?: string }) => {
    return httpClient.put(`/api/files/${fileId}`, data);
  },

  deleteFile: async (fileId: number) => {
    return httpClient.delete(`/api/files/${fileId}`);
  },

  moveFile: async (fileId: number, targetFolderId?: number) => {
    return httpClient.put(`/api/files/${fileId}/move`, { targetFolderId });
  },
};

/**
 * Check if an axios error response is an AI detection block.
 */
export function isAiBlocked(error: any): boolean {
  // Response wrapper nests the actual response inside .data.data
  return error?.response?.status === 403 && error?.response?.data?.data?.blocked === true;
}

/**
 * Get the AI block message from the error response.
 */
export function getAiBlockMessage(error: any): string {
  return error?.response?.data?.data?.message || "Save blocked due to suspected AI-generated content.";
}
