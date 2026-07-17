import { useEffect, useState } from "react";
import { X, File, Clock, Key, CheckCircle, XCircle, AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import { adminService } from "@/services/adminService";
import type { UserDto, FileListItem } from "@/types";

interface HistoryItem {
  id: number;
  userID?: string;
  databaseName?: string;
  queryPreview: string;
  queryType?: string;
  status: string;
  errorMessage?: string;
  rowsAffected?: number;
  durationMs?: number;
  executedAt: string;
}

interface UserDetailPanelProps {
  user: UserDto;
  onClose: () => void;
  onRefresh: () => void;
}

export default function UserDetailPanel({ user, onClose, onRefresh }: UserDetailPanelProps) {
  const [activeTab, setActiveTab] = useState<"files" | "history" | "settings">("files");
  const [files, setFiles] = useState<FileListItem[]>([]);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [historyCount, setHistoryCount] = useState(0);
  const [newPassword, setNewPassword] = useState("");
  const [loadingFiles, setLoadingFiles] = useState(false);
  const [loadingHistory, setLoadingHistory] = useState(false);

  useEffect(() => {
    if (activeTab === "files") loadFiles();
    if (activeTab === "history") loadHistory();
  }, [activeTab]);

  const loadFiles = async () => {
    setLoadingFiles(true);
    try {
      const data = await adminService.getUserFiles(user.userID);
      setFiles(data || []);
    } catch {
      setFiles([]);
    } finally {
      setLoadingFiles(false);
    }
  };

  const loadHistory = async () => {
    setLoadingHistory(true);
    try {
      const data = await adminService.getUserHistory(user.userID);
      setHistory(data?.items || []);
      setHistoryCount(data?.totalCount || 0);
    } catch {
      setHistory([]);
    } finally {
      setLoadingHistory(false);
    }
  };

  const handleChangePassword = async () => {
    if (!newPassword || newPassword.length < 6) {
      toast.error("Password must be at least 6 characters");
      return;
    }
    try {
      await adminService.changeUserPassword(user.userID, newPassword);
      toast.success(`Password changed for ${user.userID}`);
      setNewPassword("");
    } catch (err: any) {
      const errors = err.response?.data?.errors;
      const message = errors?.length ? errors.join(" ") : err.response?.data?.message || "Failed";
      toast.error(message);
    }
  };

  const statusIcon = (status: string) => {
    switch (status) {
      case "Success": return <CheckCircle className="h-3 w-3 text-green-500" />;
      case "Failed": return <XCircle className="h-3 w-3 text-red-500" />;
      case "Timeout": return <AlertTriangle className="h-3 w-3 text-orange-500" />;
      default: return <Clock className="h-3 w-3 text-gray-400" />;
    }
  };

  const fileTypeColors: Record<string, string> = {
    Script: "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
    StoredProcedure: "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400",
    Function: "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400",
    Trigger: "bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
    View: "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400",
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{user.userID}</h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">{user.fullName || "No name"} • {user.email || "No email"}</p>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <X className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Info bar */}
        <div className="px-6 py-2 bg-gray-50 dark:bg-gray-800/50 border-b dark:border-gray-800 flex items-center gap-4 text-sm shrink-0">
          <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full ${
            user.session?.isActive ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400" : "bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400"
          }`}>
            {user.session?.isActive ? "Session Active" : "Session Inactive"}
          </span>
          {user.session?.databaseName && (
            <span className="font-mono text-xs text-teal-600 dark:text-teal-400">{user.session.databaseName}</span>
          )}
        </div>

        {/* Tabs */}
        <div className="flex border-b dark:border-gray-800 px-6 shrink-0">
          {([
            { id: "files" as const, label: "Files", icon: <File className="h-3.5 w-3.5" /> },
            { id: "history" as const, label: "History", icon: <Clock className="h-3.5 w-3.5" /> },
            { id: "settings" as const, label: "Settings", icon: <Key className="h-3.5 w-3.5" /> },
          ]).map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? "border-teal-500 text-teal-600 dark:text-teal-400"
                  : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400"
              }`}
            >
              {tab.icon}
              {tab.label}
            </button>
          ))}
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {/* Files Tab */}
          {activeTab === "files" && (
            <div className="p-4">
              {loadingFiles ? (
                <p className="text-center text-gray-400 py-8">Loading...</p>
              ) : files.length === 0 ? (
                <p className="text-center text-gray-400 py-8">No files saved</p>
              ) : (
                <div className="space-y-1">
                  {files.map((f) => (
                    <div key={f.fileId} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800">
                      <File className="h-4 w-4 text-gray-400" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-700 dark:text-gray-200 truncate">{f.fileName}</p>
                        {f.folderPath && <p className="text-xs text-gray-400">{f.folderPath}</p>}
                      </div>
                      <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${fileTypeColors[f.fileType] || "bg-gray-100 text-gray-700"}`}>
                        {f.fileType}
                      </span>
                    </div>
                  ))}
                  <p className="text-xs text-gray-400 text-center pt-2">{files.length} file(s) total</p>
                </div>
              )}
            </div>
          )}

          {/* History Tab */}
          {activeTab === "history" && (
            <div className="p-4">
              {loadingHistory ? (
                <p className="text-center text-gray-400 py-8">Loading...</p>
              ) : history.length === 0 ? (
                <p className="text-center text-gray-400 py-8">No execution history</p>
              ) : (
                <div className="space-y-1">
                  {history.map((h) => (
                    <div key={h.id} className="px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 border-b dark:border-gray-800 last:border-0">
                      <div className="flex items-center gap-2 mb-1">
                        {statusIcon(h.status)}
                        <span className="text-xs font-medium text-gray-500 dark:text-gray-400">{h.queryType}</span>
                        <span className="text-xs text-gray-400 ml-auto">{h.durationMs}ms</span>
                        <span className="text-xs text-gray-400">
                          {new Date(h.executedAt).toLocaleString("en-IN", { hour12: false, hour: "2-digit", minute: "2-digit", day: "2-digit", month: "short" })}
                        </span>
                      </div>
                      <code className="text-xs text-gray-600 dark:text-gray-300 font-mono line-clamp-2">{h.queryPreview}</code>
                      {h.errorMessage && <p className="text-xs text-red-500 mt-1">{h.errorMessage}</p>}
                    </div>
                  ))}
                  <p className="text-xs text-gray-400 text-center pt-2">{historyCount} total queries</p>
                </div>
              )}
            </div>
          )}

          {/* Settings Tab */}
          {activeTab === "settings" && (
            <div className="p-6 space-y-6">
              <div>
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3">Change Password</h3>
                <div className="flex gap-2">
                  <input
                    type="password"
                    placeholder="New password (min 6 chars)"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="flex-1 px-3 py-2 border dark:border-gray-700 rounded-lg text-sm dark:bg-gray-800 dark:text-white"
                  />
                  <button
                    onClick={handleChangePassword}
                    className="px-4 py-2 bg-teal-600 text-white rounded-lg text-sm font-medium hover:bg-teal-700"
                  >
                    Change
                  </button>
                </div>
              </div>

              <div className="border-t dark:border-gray-800 pt-4">
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">User Info</h3>
                <dl className="grid grid-cols-2 gap-2 text-sm">
                  <dt className="text-gray-500">User ID</dt>
                  <dd className="text-gray-800 dark:text-gray-200 font-mono">{user.userID}</dd>
                  <dt className="text-gray-500">Full Name</dt>
                  <dd className="text-gray-800 dark:text-gray-200">{user.fullName || "—"}</dd>
                  <dt className="text-gray-500">Email</dt>
                  <dd className="text-gray-800 dark:text-gray-200">{user.email || "—"}</dd>
                  <dt className="text-gray-500">Created</dt>
                  <dd className="text-gray-800 dark:text-gray-200">{new Date(user.createdDate).toLocaleDateString()}</dd>
                  <dt className="text-gray-500">Active</dt>
                  <dd className="text-gray-800 dark:text-gray-200">{user.isActive ? "Yes" : "No"}</dd>
                  <dt className="text-gray-500">Database</dt>
                  <dd className="text-gray-800 dark:text-gray-200 font-mono">{user.session?.databaseName || "Not created"}</dd>
                </dl>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
