import httpClient from "./httpClient";
import type {
  McqAssessment,
  McqTestInfo,
  McqStartResult,
  McqTestStatus,
  McqQuestionForTest,
  McqSubmitResult,
} from "@/types";

// ─── Admin APIs ──────────────────────────────────────────────────

export const mcqAdminService = {
  getAssessments: async (): Promise<McqAssessment[]> => {
    const res = await httpClient.get("/api/mcq/assessments");
    return res.data.data;
  },

  getAssessment: async (id: number): Promise<McqAssessment> => {
    const res = await httpClient.get(`/api/mcq/assessments/${id}`);
    return res.data.data;
  },

  createAssessment: async (data: Partial<McqAssessment>) => {
    const res = await httpClient.post("/api/mcq/assessments", data);
    return res.data.data;
  },

  updateAssessment: async (id: number, data: Partial<McqAssessment>) => {
    const res = await httpClient.put(`/api/mcq/assessments/${id}`, data);
    return res.data;
  },

  deleteAssessment: async (id: number) => {
    const res = await httpClient.delete(`/api/mcq/assessments/${id}`);
    return res.data;
  },

  getQuestions: async (assessmentId: number) => {
    const res = await httpClient.get(`/api/mcq/assessments/${assessmentId}/questions`);
    return res.data.data;
  },

  addQuestion: async (assessmentId: number, data: any) => {
    const res = await httpClient.post(`/api/mcq/assessments/${assessmentId}/questions`, data);
    return res.data.data;
  },

  bulkUploadQuestions: async (assessmentId: number, questions: any[]) => {
    const res = await httpClient.post(`/api/mcq/assessments/${assessmentId}/questions/bulk`, questions);
    return res.data;
  },

  updateQuestion: async (questionId: number, data: any) => {
    const res = await httpClient.put(`/api/mcq/questions/${questionId}`, data);
    return res.data;
  },

  deleteQuestion: async (questionId: number) => {
    const res = await httpClient.delete(`/api/mcq/questions/${questionId}`);
    return res.data;
  },

  getResults: async (assessmentId: number) => {
    const res = await httpClient.get(`/api/mcq/assessments/${assessmentId}/results`);
    return res.data.data;
  },
};

// ─── Participant APIs ────────────────────────────────────────────

export const mcqTestService = {
  getTestInfo: async (): Promise<McqTestInfo> => {
    const res = await httpClient.get("/api/mcq/test/info");
    return res.data.data;
  },

  startTest: async (): Promise<McqStartResult> => {
    const res = await httpClient.post("/api/mcq/test/start");
    return res.data.data;
  },

  getStatus: async (): Promise<McqTestStatus> => {
    const res = await httpClient.get("/api/mcq/test/status");
    return res.data.data;
  },

  getQuestion: async (questionIndex: number): Promise<McqQuestionForTest> => {
    const res = await httpClient.get(`/api/mcq/test/questions/${questionIndex}`);
    return res.data.data;
  },

  getAllQuestions: async (): Promise<McqQuestionForTest[]> => {
    const res = await httpClient.get("/api/mcq/test/questions");
    return res.data.data;
  },

  saveAnswer: async (data: { questionId: number; selectedAnswer?: string | null; timeTakenSeconds?: number; isFlagged?: boolean }) => {
    const res = await httpClient.post("/api/mcq/test/answer", data);
    return res.data;
  },

  flagQuestion: async (questionId: number, isFlagged: boolean) => {
    const res = await httpClient.post("/api/mcq/test/flag", { questionId, isFlagged });
    return res.data;
  },

  submitTest: async (isAutoSubmit = false): Promise<McqSubmitResult> => {
    const res = await httpClient.post("/api/mcq/test/submit", { isAutoSubmit });
    return res.data.data;
  },
};
