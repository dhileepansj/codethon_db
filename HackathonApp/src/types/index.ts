// Auth
export interface LoginRequest {
  UserID: string;
  Password: string;
}

export interface LoginResponse {
  token: string;
  userID: string;
  role: "SuperAdmin" | "Participant";
  fullName?: string;
  mustChangePassword: boolean;
  session?: SessionInfo;
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
  session?: {
    isActive: boolean;
    isExpired: boolean;
    databaseCreated: boolean;
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
