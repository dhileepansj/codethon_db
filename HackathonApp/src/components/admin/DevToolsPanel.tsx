import { useEffect, useState } from "react";
import { ShieldAlert, Monitor, Keyboard, Mouse, Eye, XCircle, Clock, User, AlertTriangle, LogOut } from "lucide-react";
import { toast } from "sonner";
import httpClient from "@/services/httpClient";

interface DevToolsAttempt {
  eventType: string;
  eventTime: string;
  method: string;
}

interface UserDevToolsData {
  userId: string;
  fullName?: string;
  totalAttempts: number;
  logoutCount: number;
  lastAttempt: string;
  attempts: DevToolsAttempt[];
}

interface DevToolsPanelProps {
  onClose: () => void;
}

export default function DevToolsPanel({ onClose }: DevToolsPanelProps) {
  const [data, setData] = useState<UserDevToolsData[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedUser, setExpandedUser] = useState<string | null>(null);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 15000); // Auto-refresh every 15s
    return () => clearInterval(interval);
  }, []);

  const loadData = async () => {
    try {
      const res = await httpClient.get("/api/activity/devtools");
      setData(res.data.data || []);
    } catch {
      toast.error("Failed to load devtools data");
    } finally {
      setLoading(false);
    }
  };

  const getMethodIcon = (method: string) => {
    if (method.includes("keyboard") || method.includes("f12")) return <Keyboard className="h-3.5 w-3.5" />;
    if (method.includes("right_click")) return <Mouse className="h-3.5 w-3.5" />;
    if (method.includes("size")) return <Monitor className="h-3.5 w-3.5" />;
    if (method.includes("debugger")) return <Eye className="h-3.5 w-3.5" />;
    if (method.includes("logout")) return <LogOut className="h-3.5 w-3.5" />;
    return <ShieldAlert className="h-3.5 w-3.5" />;
  };

  const getMethodLabel = (method: string) => {
    const labels: Record<string, string> = {
      keyboard_f12: "F12 key pressed",
      keyboard_ctrl_shift_i: "Ctrl+Shift+I pressed",
      keyboard_ctrl_shift_j: "Ctrl+Shift+J pressed",
      keyboard_ctrl_shift_c: "Ctrl+Shift+C pressed",
      keyboard_ctrl_u: "Ctrl+U pressed",
      right_click: "Right-click attempted",
      size_detection: "Window size anomaly (DevTools docked)",
      debugger_timing: "Debugger timing detected",
    };
    if (method.includes("logout")) return "⚠ FORCED LOGOUT — " + labels[method.replace("logout_", "")] || method;
    return labels[method] || method;
  };

  const getMethodColor = (eventType: string) => {
    if (eventType.includes("logout")) return "text-red-500 bg-red-50 dark:bg-red-900/20";
    if (eventType.includes("size") || eventType.includes("debugger")) return "text-orange-500 bg-orange-50 dark:bg-orange-900/20";
    return "text-amber-600 bg-amber-50 dark:bg-amber-900/20";
  };

  const formatTime = (dateStr: string) => {
    return new Date(dateStr).toLocaleString("en-IN", {
      day: "2-digit",
      month: "short",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: false,
    });
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-2xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-red-500 to-orange-500 rounded-lg p-2">
              <ShieldAlert className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">DevTools Detection</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                {data.length} user(s) with detection events · Auto-refreshes every 15s
              </p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <XCircle className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Summary Bar */}
        {data.length > 0 && (
          <div className="px-6 py-3 bg-red-50 dark:bg-red-900/10 border-b dark:border-gray-800 flex items-center gap-6">
            <div className="flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-red-500" />
              <span className="text-sm font-medium text-red-700 dark:text-red-400">
                {data.reduce((sum, d) => sum + d.totalAttempts, 0)} total attempts
              </span>
            </div>
            <div className="flex items-center gap-2">
              <LogOut className="h-4 w-4 text-red-500" />
              <span className="text-sm font-medium text-red-700 dark:text-red-400">
                {data.reduce((sum, d) => sum + d.logoutCount, 0)} forced logouts
              </span>
            </div>
            <div className="flex items-center gap-2">
              <User className="h-4 w-4 text-orange-500" />
              <span className="text-sm font-medium text-orange-700 dark:text-orange-400">
                {data.length} user(s) flagged
              </span>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <p className="text-center text-gray-400 py-12">Loading...</p>
          ) : data.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20">
              <ShieldAlert className="h-16 w-16 text-green-300 mb-4" />
              <p className="text-lg font-medium text-gray-600 dark:text-gray-300">No DevTools attempts detected</p>
              <p className="text-sm text-gray-400 mt-1">All participants are following the rules.</p>
            </div>
          ) : (
            <div className="p-4 space-y-3">
              {data.map((user) => (
                <div key={user.userId} className="border dark:border-gray-700 rounded-lg overflow-hidden">
                  {/* User Header */}
                  <div
                    className="px-4 py-3 flex items-center gap-3 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
                    onClick={() => setExpandedUser(expandedUser === user.userId ? null : user.userId)}
                  >
                    <div className="bg-red-100 dark:bg-red-900/30 rounded-full p-2">
                      <User className="h-4 w-4 text-red-600 dark:text-red-400" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold text-gray-800 dark:text-gray-100">{user.userId}</span>
                        {user.fullName && <span className="text-xs text-gray-400">({user.fullName})</span>}
                      </div>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        Last attempt: {formatTime(user.lastAttempt)}
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="text-center">
                        <p className="text-lg font-bold text-red-600 dark:text-red-400">{user.totalAttempts}</p>
                        <p className="text-[10px] text-gray-400">attempts</p>
                      </div>
                      {user.logoutCount > 0 && (
                        <div className="text-center">
                          <p className="text-lg font-bold text-orange-600 dark:text-orange-400">{user.logoutCount}</p>
                          <p className="text-[10px] text-gray-400">logouts</p>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Expanded: Attempt Details */}
                  {expandedUser === user.userId && (
                    <div className="border-t dark:border-gray-700 bg-gray-50 dark:bg-gray-800/30 px-4 py-3 max-h-80 overflow-y-auto">
                      <div className="space-y-1.5">
                        {user.attempts.map((attempt, idx) => (
                          <div
                            key={idx}
                            className={`flex items-center gap-3 px-3 py-2 rounded-lg ${getMethodColor(attempt.eventType)}`}
                          >
                            <span className="shrink-0">{getMethodIcon(attempt.method)}</span>
                            <div className="flex-1 min-w-0">
                              <p className="text-xs font-medium">{getMethodLabel(attempt.method)}</p>
                            </div>
                            <div className="flex items-center gap-1 text-[10px] text-gray-500 shrink-0">
                              <Clock className="h-3 w-3" />
                              {formatTime(attempt.eventTime)}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
