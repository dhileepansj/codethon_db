// Auth
export interface LoginRequest {
  UserID: string;
  Password: string;
}

export interface LoginResponse {
  token: string;
  userID: string;
  role: "SuperAdmin" | "Admin" | "Participant";
  fullName?: string;
  mustChangePassword: boolean;
  dbEnginePreference?: "SqlServer" | "Oracle";
  assessmentType?: "SQL" | "MCQ";
  assessmentSubType?: string;
  assessmentId?: number;
  permissions?: AdminPermissions;
  session?: SessionInfo;
}

export interface AdminPermissions {
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
  canManageManualTesting: boolean;
  canExportData: boolean;
  canResetDatabase: boolean;
  canDeleteUsers: boolean;
}

export interface SessionInfo {
  isActive: boolean;
  isExpired: boolean;
  databaseCreated: boolean;
  isSubmitted: boolean;
  submittedAt?: string;
  databaseName?: string;
  expiresAt?: string;
  remainingMinutes?: number;
  schedule?: ScheduleInfo;
}

export interface ScheduleInfo {
  sessionStartTime: string;
  sessionEndTime: string;
  extensionMinutes: number;
  isInBreak: boolean;
  currentBreakTitle?: string;
  breakEndsAt?: string;
  isWrongDate: boolean;
  isBeforeStart: boolean;
  isAfterEnd: boolean;
  scheduleDate?: string;
  alerts: AlertConfig[];
  breaks: BreakInfo[];
}

export interface AlertConfig {
  minutes: number;
  color: string;
}

export interface BreakInfo {
  title: string;
  startTime: string;
  endTime: string;
}

// Hackathon
export interface ExecuteRequest {
  sql: string;
  page: number;
  pageSize: number;
}

export interface ExecuteResult {
  results: BatchResult[];
  totalBatches: number;
  executedBatches: number;
}

export interface BatchResult {
  batchIndex: number;
  type: "DDL" | "DML" | "SELECT" | "ERROR";
  message?: string;
  rowsAffected?: number;
  durationMs: number;
  columns?: string[];
  rows?: Record<string, unknown>[];
  totalRows?: number;
  page?: number;
  pageSize?: number;
  error?: string;
}

// Schema
export interface DatabaseOverview {
  databaseName: string;
  tableCount: number;
  viewCount: number;
  procedureCount: number;
  functionCount: number;
  triggerCount: number;
  sizeMB?: number;
}

export interface TableInfo {
  tableName: string;
  schema: string;
  columnCount: number;
  rowCount: number;
  createDate?: string;
}

export interface ColumnInfo {
  columnName: string;
  dataType: string;
  maxLength?: number;
  isNullable: boolean;
  isPrimaryKey: boolean;
  isIdentity: boolean;
  isForeignKey: boolean;
  foreignKeyTable?: string;
  defaultValue?: string;
  ordinalPosition: number;
}

export interface DbObject {
  name: string;
  schema: string;
  type: string;
  createDate?: string;
  modifyDate?: string;
}

// File Manager
export interface FolderDto {
  folderId: number;
  parentFolderId?: number;
  folderName: string;
  createdDate: string;
  fileCount: number;
  subFolderCount: number;
}

export interface FileListItem {
  fileId: number;
  folderId?: number;
  folderPath?: string;
  fileName: string;
  fileType: string;
  createdDate: string;
  modifiedDate?: string;
}

export interface FileDetail {
  fileId: number;
  folderId?: number;
  fileName: string;
  fileType: string;
  content?: string;
  createdDate: string;
  modifiedDate?: string;
}

// Admin
export interface UserDto {
  id: number;
  userID: string;
  fullName?: string;
  email?: string;
  role: string;
  isActive: boolean;
  mustChangePassword: boolean;
  passwordResetRequested: boolean;
  createdDate: string;
  lastLoginAt?: string;
  loginCount: number;
  dbEnginePreference?: "SqlServer" | "Oracle";
  assessmentType?: "SQL" | "MCQ";
  assessmentTitle?: string;
  assessmentSubType?: string;
  assessmentId?: number;
  mcqProgress?: {
    status: "NotStarted" | "InProgress" | "Submitted";
    totalQuestions: number;
    answered: number;
    score?: number;
    maxScore?: number;
    percentage?: number;
    passed?: boolean;
    submittedAt?: string;
  };
  session?: {
    isActive: boolean;
    isExpired: boolean;
    databaseCreated: boolean;
    isSubmitted: boolean;
    databaseName?: string;
    startedAt?: string;
    expiresAt?: string;
  };
}

export interface DashboardStats {
  totalUsers: number;
  activeSessions: number;
  databasesCreated: number;
  queriesToday: number;
}

// API Response wrapper
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

// ─── MCQ Types ───────────────────────────────────────────────────

export interface McqAssessment {
  id: number;
  title: string;
  type: "SQL" | "MCQ";
  subType: string;
  durationMinutes?: number;
  totalQuestions: number;
  maxMarks: number;
  simplePercentage: number;
  mediumPercentage: number;
  complexPercentage: number;
  simpleMarks: number;
  mediumMarks: number;
  complexMarks: number;
  negativeMarking: boolean;
  negativeMarkValue: number;
  shuffleQuestions: boolean;
  shuffleOptions: boolean;
  showResultImmediately: boolean;
  passPercentage: number;
  allowNavigation: boolean;
  allowReview: boolean;
  autoSubmitOnTimeout: boolean;
  oneQuestionPerPage: boolean;
  showComplexity: boolean;
  showMarks: boolean;
  isActive: boolean;
  questionBankCount: number;
  createdDate: string;
}

export interface McqTestInfo {
  assessmentId: number;
  title: string;
  totalQuestions: number;
  durationMinutes: number;
  maxMarks: number;
  negativeMarking: boolean;
  negativeMarkValue: number;
  allowNavigation: boolean;
  allowReview: boolean;
  oneQuestionPerPage: boolean;
  showComplexity: boolean;
  showMarks: boolean;
  simpleCount: number;
  mediumCount: number;
  complexCount: number;
  simpleMarks: number;
  mediumMarks: number;
  complexMarks: number;
  hasExistingTest: boolean;
  existingTestId?: number;
  isAlreadySubmitted: boolean;
  submittedScore?: number;
  submittedMaxScore?: number;
  submittedPercentage?: number;
  submittedPassed?: boolean;
}

export interface McqStartResult {
  testId: number;
  totalQuestions: number;
  startedAt: string;
  expiresAt: string;
  durationMinutes: number;
}

export interface McqTestStatus {
  testId: number;
  isInProgress: boolean;
  isSubmitted: boolean;
  startedAt?: string;
  expiresAt?: string;
  remainingSeconds?: number;
  totalQuestions: number;
  answered: number;
  flagged: number;
  navigationPanel: McqNavItem[];
}

export interface McqNavItem {
  questionIndex: number;
  questionId: number;
  isAnswered: boolean;
  isFlagged: boolean;
  complexity?: string;
}

export interface McqQuestionForTest {
  questionId: number;
  questionIndex: number;
  question: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  complexity: string;
  marks: number;
  category?: string;
  selectedAnswer?: string;
  isFlagged: boolean;
}

export interface McqSubmitResult {
  score: number;
  maxScore: number;
  percentage: number;
  correct: number;
  wrong: number;
  skipped: number;
  totalQuestions: number;
  passed?: boolean;
  timeSpentSeconds?: number;
  message: string;
  showScores: boolean;
  detailedResults?: McqAnswerReview[];
}

export interface McqAnswerReview {
  questionIndex: number;
  question: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  correctAnswer: string;
  selectedAnswer?: string;
  isCorrect: boolean;
  marksAwarded: number;
  explanation?: string;
}
