import axios from 'axios';
import type {
  SurveyDto,
  SurveyDetailDto,
  CreateSurveyDto,
  UpdateSurveyDto,
  SurveyFieldDto,
  CreateFieldDto,
  UpdateFieldDto,
  ReorderFieldsDto,
  FieldDependencyDto,
  CreateDependencyDto,
  SurveyParticipantDto,
  DeclineParticipantDto,
  BulkUploadResultDto,
  SurveyEmailSettingsDto,
  UpdateEmailSettingsDto,
  SurveyDashboardDto,
  SurveyResponseDto,
  FieldAnalyticsDto,
  VerifyEmailResponse,
  SendOtpResponse,
  VerifyOtpResponse,
  SurveyFormDto,
  SubmitSurveyResponseDto,
} from '../types/survey';
import { SurveyStatus } from '../types/survey';

const API_BASE = import.meta.env.VITE_API_BASE_URL || '';

const api = axios.create({
  baseURL: `${API_BASE}/hackathonapi`,
});

// Attach JWT token for admin requests
api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Unwrap response wrapper: { success, data, message, errors } → data
api.interceptors.response.use((response) => {
  if (response.data && typeof response.data === 'object' && 'success' in response.data && 'data' in response.data) {
    response.data = response.data.data;
  }
  return response;
});

// ─── Survey CRUD ──────────────────────────────────────────────────────────

export const surveyApi = {
  getAll: () => api.get<SurveyDto[]>('/api/surveys').then(r => r.data),

  getById: (id: string) => api.get<SurveyDetailDto>(`/api/surveys/${id}`).then(r => r.data),

  create: (dto: CreateSurveyDto) => api.post<SurveyDto>('/api/surveys', dto).then(r => r.data),

  update: (id: string, dto: UpdateSurveyDto) => api.put<SurveyDto>(`/api/surveys/${id}`, dto).then(r => r.data),

  delete: (id: string) => api.delete(`/api/surveys/${id}`).then(r => r.data),

  updateStatus: (id: string, status: SurveyStatus) =>
    api.put<SurveyDto>(`/api/surveys/${id}/status`, { status }).then(r => r.data),

  clone: (id: string) => api.post<SurveyDto>(`/api/surveys/${id}/clone`).then(r => r.data),
};

// ─── Form Builder ─────────────────────────────────────────────────────────

export const fieldApi = {
  getAll: (surveyId: string) =>
    api.get<SurveyFieldDto[]>(`/api/surveys/${surveyId}/fields`).then(r => r.data),

  create: (surveyId: string, dto: CreateFieldDto) =>
    api.post<SurveyFieldDto>(`/api/surveys/${surveyId}/fields`, dto).then(r => r.data),

  update: (surveyId: string, fieldId: string, dto: UpdateFieldDto) =>
    api.put<SurveyFieldDto>(`/api/surveys/${surveyId}/fields/${fieldId}`, dto).then(r => r.data),

  delete: (surveyId: string, fieldId: string) =>
    api.delete(`/api/surveys/${surveyId}/fields/${fieldId}`).then(r => r.data),

  reorder: (surveyId: string, dto: ReorderFieldsDto) =>
    api.put(`/api/surveys/${surveyId}/fields/reorder`, dto).then(r => r.data),

  // Dependencies
  createDependency: (surveyId: string, fieldId: string, dto: CreateDependencyDto) =>
    api.post<FieldDependencyDto>(`/api/surveys/${surveyId}/fields/${fieldId}/dependencies`, dto).then(r => r.data),

  deleteDependency: (surveyId: string, depId: string) =>
    api.delete(`/api/surveys/${surveyId}/fields/dependencies/${depId}`).then(r => r.data),

  getAllDependencies: (surveyId: string) =>
    api.get<FieldDependencyDto[]>(`/api/surveys/${surveyId}/fields/dependencies`).then(r => r.data),

  validateDependencies: (surveyId: string) =>
    api.get(`/api/surveys/${surveyId}/fields/dependencies/validate`).then(r => r.data),
};

// ─── Participants ─────────────────────────────────────────────────────────

export const participantApi = {
  getAll: (surveyId: string) =>
    api.get<SurveyParticipantDto[]>(`/api/surveys/${surveyId}/participants`).then(r => r.data),

  getPending: (surveyId: string) =>
    api.get<SurveyParticipantDto[]>(`/api/surveys/${surveyId}/participants/pending`).then(r => r.data),

  upload: (surveyId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post<BulkUploadResultDto>(`/api/surveys/${surveyId}/participants/upload`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  delete: (surveyId: string, participantId: string) =>
    api.delete(`/api/surveys/${surveyId}/participants/${participantId}`).then(r => r.data),

  decline: (surveyId: string, participantId: string, dto: DeclineParticipantDto, attachment?: File) => {
    const formData = new FormData();
    formData.append('declinedBy', dto.declinedBy.toString());
    formData.append('reason', dto.reason);
    if (attachment) formData.append('attachment', attachment);
    return api.put(`/api/surveys/${surveyId}/participants/${participantId}/decline`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  resetStatus: (surveyId: string, participantId: string) =>
    api.put(`/api/surveys/${surveyId}/participants/${participantId}/reset`).then(r => r.data),

  downloadTemplate: (surveyId: string) =>
    api.get(`/api/surveys/${surveyId}/participants/template`, { responseType: 'blob' }).then(r => r.data),
};

// ─── Distribution ─────────────────────────────────────────────────────────

export const distributionApi = {
  getEmailSettings: (surveyId: string) =>
    api.get<SurveyEmailSettingsDto>(`/api/surveys/${surveyId}/distribution/email-settings`).then(r => r.data),

  updateEmailSettings: (surveyId: string, dto: UpdateEmailSettingsDto) =>
    api.put<SurveyEmailSettingsDto>(`/api/surveys/${surveyId}/distribution/email-settings`, dto).then(r => r.data),

  send: (surveyId: string) =>
    api.post(`/api/surveys/${surveyId}/distribution/send`).then(r => r.data),

  remind: (surveyId: string, participantIds: string[]) =>
    api.post(`/api/surveys/${surveyId}/distribution/remind`, { participantIds }).then(r => r.data),
};

// ─── Dashboard ────────────────────────────────────────────────────────────

export const dashboardApi = {
  getSummary: (surveyId: string) =>
    api.get<SurveyDashboardDto>(`/api/surveys/${surveyId}/dashboard`).then(r => r.data),

  getResponses: (surveyId: string, page = 1, pageSize = 50) =>
    api.get<SurveyResponseDto[]>(`/api/surveys/${surveyId}/dashboard/responses`, { params: { page, pageSize } }).then(r => r.data),

  getResponseDetail: (surveyId: string, responseId: string) =>
    api.get<SurveyResponseDto>(`/api/surveys/${surveyId}/dashboard/responses/${responseId}`).then(r => r.data),

  getAnalytics: (surveyId: string) =>
    api.get<FieldAnalyticsDto[]>(`/api/surveys/${surveyId}/dashboard/analytics`).then(r => r.data),

  export: (surveyId: string) =>
    api.get(`/api/surveys/${surveyId}/dashboard/export`, { responseType: 'blob' }).then(r => r.data),
};

// ─── Public Survey Respond (No Auth) ─────────────────────────────────────

const publicApi = axios.create({
  baseURL: `${API_BASE}/hackathonapi`,
});

// Unwrap response wrapper for public API too
publicApi.interceptors.response.use((response) => {
  if (response.data && typeof response.data === 'object' && 'success' in response.data && 'data' in response.data) {
    response.data = response.data.data;
  }
  return response;
});

export const respondApi = {
  verifyEmail: (token: string, email: string) =>
    publicApi.post<VerifyEmailResponse>(`/api/survey-respond/${token}/verify-email`, { email }).then(r => r.data),

  sendOtp: (token: string, email: string) =>
    publicApi.post<SendOtpResponse>(`/api/survey-respond/${token}/send-otp`, { email }).then(r => r.data),

  verifyOtp: (token: string, email: string, otp: string) =>
    publicApi.post<VerifyOtpResponse>(`/api/survey-respond/${token}/verify-otp`, { email, otp }).then(r => r.data),

  getForm: (token: string, sessionToken: string) =>
    publicApi.get<SurveyFormDto>(`/api/survey-respond/${token}/form`, {
      headers: { 'X-Session-Token': sessionToken },
    }).then(r => r.data),

  submit: (token: string, sessionToken: string, dto: SubmitSurveyResponseDto) =>
    publicApi.post(`/api/survey-respond/${token}/submit`, dto, {
      headers: { 'X-Session-Token': sessionToken },
    }).then(r => r.data),
};
