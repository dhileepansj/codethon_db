// ─── Enums ────────────────────────────────────────────────────────────────

export enum SurveyStatus {
  Draft = 0,
  Active = 1,
  Closed = 2,
  Archived = 3,
}

export enum SurveyFieldType {
  ShortText = 0,
  LongText = 1,
  Number = 2,
  Email = 3,
  Phone = 4,
  Date = 5,
  DateTime = 6,
  Time = 7,
  Dropdown = 8,
  MultiSelect = 9,
  Radio = 10,
  Checkbox = 11,
  Rating = 12,
  Scale = 13,
  FileUpload = 14,
  Section = 15,
  Paragraph = 16,
  YesNo = 17,
  Matrix = 18,
}

export enum DependencyCondition {
  Equals = 0,
  NotEquals = 1,
  Contains = 2,
  GreaterThan = 3,
  LessThan = 4,
  IsEmpty = 5,
  IsNotEmpty = 6,
}

export enum DependencyAction {
  Show = 0,
  Hide = 1,
  Require = 2,
  SetOptions = 3,
}

export enum ParticipantStatus {
  Pending = 0,
  Sent = 1,
  Reminded = 2,
  Responded = 3,
  Declined = 4,
}

export enum DeclinedByType {
  ReportingManager = 0,
  VerticalHead = 1,
  Self = 2,
}

// ─── DTOs ─────────────────────────────────────────────────────────────────

export interface SurveyDto {
  id: string;
  title: string;
  description?: string;
  status: SurveyStatus;
  createdAt: string;
  updatedAt?: string;
  startsAt?: string;
  expiresAt?: string;
  allowMultiple: boolean;
  isAnonymous: boolean;
  thankYouMessage?: string;
  totalParticipants: number;
  totalResponses: number;
  fieldCount: number;
}

export interface SurveyDetailDto extends SurveyDto {
  fields: SurveyFieldDto[];
  emailSettings?: SurveyEmailSettingsDto;
}

export interface CreateSurveyDto {
  title: string;
  description?: string;
  startsAt?: string;
  expiresAt?: string;
  allowMultiple?: boolean;
  isAnonymous?: boolean;
  thankYouMessage?: string;
}

export interface UpdateSurveyDto {
  title?: string;
  description?: string;
  startsAt?: string;
  expiresAt?: string;
  allowMultiple?: boolean;
  isAnonymous?: boolean;
  thankYouMessage?: string;
}

// ─── Field DTOs ───────────────────────────────────────────────────────────

export interface SurveyFieldDto {
  id: string;
  surveyId: string;
  fieldType: SurveyFieldType;
  label: string;
  description?: string;
  placeholder?: string;
  isRequired: boolean;
  sortOrder: number;
  options?: string; // JSON
  validation?: string; // JSON
  sectionTitle?: string;
  defaultValue?: string;
  matrixRows?: string; // JSON
  matrixColumns?: string; // JSON
  dependencies: FieldDependencyDto[];
}

export interface CreateFieldDto {
  fieldType: SurveyFieldType;
  label: string;
  description?: string;
  placeholder?: string;
  isRequired?: boolean;
  sortOrder?: number;
  options?: string;
  validation?: string;
  sectionTitle?: string;
  defaultValue?: string;
  matrixRows?: string;
  matrixColumns?: string;
}

export interface UpdateFieldDto {
  fieldType?: SurveyFieldType;
  label?: string;
  description?: string;
  placeholder?: string;
  isRequired?: boolean;
  options?: string;
  validation?: string;
  sectionTitle?: string;
  defaultValue?: string;
  matrixRows?: string;
  matrixColumns?: string;
}

export interface ReorderFieldsDto {
  fields: { fieldId: string; sortOrder: number }[];
}

// ─── Dependency DTOs ──────────────────────────────────────────────────────

export interface FieldDependencyDto {
  id: string;
  fieldId: string;
  dependsOnFieldId: string;
  condition: DependencyCondition;
  value?: string;
  action: DependencyAction;
  optionMap?: string; // JSON
  logicGroupId?: string;
  logicOperator: string;
}

export interface CreateDependencyDto {
  dependsOnFieldId: string;
  condition: DependencyCondition;
  value?: string;
  action?: DependencyAction;
  optionMap?: string;
  logicGroupId?: string;
  logicOperator?: string;
}

// ─── Participant DTOs ─────────────────────────────────────────────────────

export interface SurveyParticipantDto {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeEmail: string;
  rmName?: string;
  rmEmail?: string;
  vhName?: string;
  vhEmail?: string;
  status: ParticipantStatus;
  uploadedAt: string;
  lastSentAt?: string;
  respondedAt?: string;
  reminderCount: number;
  declineInfo?: DeclineInfoDto;
}

export interface DeclineInfoDto {
  declinedBy?: DeclinedByType;
  reason?: string;
  attachmentPath?: string;
  declinedAt?: string;
  markedByUserName?: string;
}

export interface DeclineParticipantDto {
  declinedBy: DeclinedByType;
  reason: string;
}

export interface BulkUploadResultDto {
  totalRows: number;
  successCount: number;
  errorCount: number;
  errors: { row: number; field: string; message: string }[];
}

// ─── Email Settings ───────────────────────────────────────────────────────

export interface SurveyEmailSettingsDto {
  id: string;
  surveyId: string;
  includeRmByDefault: boolean;
  includeVhByDefault: boolean;
  emailMode: number;
  additionalCcEmails?: string;
  emailSubject?: string;
  emailBody?: string;
  reminderEnabled: boolean;
  reminderDays: number;
  maxReminders: number;
}

export interface UpdateEmailSettingsDto {
  includeRmByDefault: boolean;
  includeVhByDefault: boolean;
  emailMode: number;
  additionalCcEmails?: string;
  emailSubject?: string;
  emailBody?: string;
  reminderEnabled: boolean;
  reminderDays: number;
  maxReminders: number;
}

// ─── Response DTOs ────────────────────────────────────────────────────────

export interface SurveyResponseDto {
  id: string;
  surveyId: string;
  employeeId?: string;
  employeeName?: string;
  employeeEmail?: string;
  submittedAt: string;
  timeTakenSeconds?: number;
  answers: ResponseAnswerDto[];
}

export interface ResponseAnswerDto {
  fieldId: string;
  fieldLabel?: string;
  fieldType?: string;
  value?: string;
  fileUrl?: string;
}

export interface SubmitSurveyResponseDto {
  answers: { fieldId: string; value?: string; fileUrl?: string }[];
  timeTakenSeconds?: number;
}

// ─── Dashboard DTOs ───────────────────────────────────────────────────────

export interface SurveyDashboardDto {
  surveyId: string;
  title: string;
  totalParticipants: number;
  responded: number;
  pending: number;
  declined: number;
  reminded: number;
  notSent: number;
  responseRate: number;
}

export interface FieldAnalyticsDto {
  fieldId: string;
  label: string;
  fieldType: string;
  totalAnswers: number;
  optionBreakdown?: { option: string; count: number; percentage: number }[];
  averageValue?: number;
  textResponses?: string[];
}

// ─── Public Respond DTOs ──────────────────────────────────────────────────

export interface VerifyEmailResponse {
  isValid: boolean;
  message?: string;
  maskedEmail?: string;
}

export interface SendOtpResponse {
  success: boolean;
  message?: string;
  maskedEmail?: string;
  expiresInSeconds: number;
}

export interface VerifyOtpResponse {
  success: boolean;
  message?: string;
  sessionToken?: string;
  participantInfo?: ParticipantInfoDto;
}

export interface ParticipantInfoDto {
  employeeId: string;
  employeeName: string;
  employeeEmail: string;
}

export interface SurveyFormDto {
  surveyId: string;
  title: string;
  description?: string;
  participant: ParticipantInfoDto;
  fields: SurveyFieldDto[];
  thankYouMessage?: string;
}

// ─── Helpers ──────────────────────────────────────────────────────────────

export interface FieldOption {
  value: string;
  label: string;
}

export const FIELD_TYPE_LABELS: Record<SurveyFieldType, string> = {
  [SurveyFieldType.ShortText]: 'Short Answer',
  [SurveyFieldType.LongText]: 'Paragraph',
  [SurveyFieldType.Number]: 'Number',
  [SurveyFieldType.Email]: 'Email',
  [SurveyFieldType.Phone]: 'Phone',
  [SurveyFieldType.Date]: 'Date',
  [SurveyFieldType.DateTime]: 'Date & Time',
  [SurveyFieldType.Time]: 'Time',
  [SurveyFieldType.Dropdown]: 'Dropdown',
  [SurveyFieldType.MultiSelect]: 'Multi Select',
  [SurveyFieldType.Radio]: 'Multiple Choice',
  [SurveyFieldType.Checkbox]: 'Checkboxes',
  [SurveyFieldType.Rating]: 'Rating',
  [SurveyFieldType.Scale]: 'Linear Scale',
  [SurveyFieldType.FileUpload]: 'File Upload',
  [SurveyFieldType.Section]: 'Section Header',
  [SurveyFieldType.Paragraph]: 'Description Text',
  [SurveyFieldType.YesNo]: 'Yes / No',
  [SurveyFieldType.Matrix]: 'Matrix / Grid',
};

export const FIELD_TYPE_CATEGORIES = {
  text: [SurveyFieldType.ShortText, SurveyFieldType.LongText, SurveyFieldType.Number, SurveyFieldType.Email, SurveyFieldType.Phone],
  choice: [SurveyFieldType.Dropdown, SurveyFieldType.MultiSelect, SurveyFieldType.Radio, SurveyFieldType.Checkbox, SurveyFieldType.YesNo],
  dateTime: [SurveyFieldType.Date, SurveyFieldType.DateTime, SurveyFieldType.Time],
  advanced: [SurveyFieldType.Rating, SurveyFieldType.Scale, SurveyFieldType.FileUpload, SurveyFieldType.Matrix],
  layout: [SurveyFieldType.Section, SurveyFieldType.Paragraph],
};
