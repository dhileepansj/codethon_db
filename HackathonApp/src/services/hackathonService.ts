import httpClient from "./httpClient";
import type { ExecuteRequest, ExecuteResult, SessionInfo } from "@/types";

export const hackathonService = {
  getStatus: async (): Promise<SessionInfo> => {
    const res = await httpClient.get("/api/hackathon/status");
    return res.data.data;
  },

  createDatabase: async () => {
    const res = await httpClient.post("/api/hackathon/create-database");
    return res.data.data;
  },

  execute: async (request: ExecuteRequest): Promise<ExecuteResult> => {
    const res = await httpClient.post("/api/hackathon/execute", request);
    return res.data.data;
  },

  getQuestionPaper: async (): Promise<{ title: string; htmlContent: string; scheduledDate?: string; startTime?: string; endTime?: string; durationMinutes?: number } | null> => {
    try {
      const res = await httpClient.get("/api/hackathon/question-paper");
      return res.data.data;
    } catch {
      return null;
    }
  },

  submit: async () => {
    const res = await httpClient.post("/api/hackathon/submit");
    return res.data;
  },

  uploadSubmissionFiles: async (files: File[]) => {
    const formData = new FormData();
    files.forEach((f) => formData.append("files", f));
    const res = await httpClient.post("/api/hackathon/submission-files", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return res.data.data;
  },

  getSubmissionFiles: async (): Promise<{ id: number; fileName: string; contentType: string; fileSizeBytes: number; uploadedAt: string }[]> => {
    const res = await httpClient.get("/api/hackathon/submission-files");
    return res.data.data;
  },

  deleteSubmissionFile: async (fileId: number) => {
    const res = await httpClient.delete(`/api/hackathon/submission-files/${fileId}`);
    return res.data;
  },
};
