import { useEffect, useState } from "react";
import { ArrowLeftRight, XCircle, Clock, User, AlertTriangle, Eye } from "lucide-react";
import { toast } from "sonner";
import httpClient from "@/services/httpClient";

interface TabSwitchLogEntry {
  eventType: string;
  eventTime: string;
  awayDurationSeconds?: number;
}

interface UserTabSwitchData {
  userId: string;
  fullName?: string;
  switchesInLastHour: number;
  isSuspicious: boolean;
  logs?: TabSwitchLogEntry[];
}

interface TabSwitchLogsPanelProps {
  onClose: () => void;
}

export default function TabSwitchLogsPanel({ onClose }: TabSwitchLogsPanelProps) {
  const [overview, setOverview] = useState<UserTabSwitchData[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedUser, setExpandedUser] = useState<string | null>(null);
  const [userLogs, setUserLogs] = useState<Record<string, TabSwitchLogEntry[]>>({});
  const [loadingLogs, setLoadingLogs] = useState<string | null>(null);

  useEffect(() => {
    loadOverview();
    const interval = setInterval(loadOverview, 15000);
    return () => clearInterval(interval);
  }, []);

  const loadOverview = async () => {
    try {
      const res = await httpClient.get("/api/activity/overview");
      const data = res.data.data || res.data || [];
      setOverview(data);
    } catch {
      toast.error("Failed to load tab switch data");
    } finally {
      setLoading(false);
    }
  };

  const loadUserLogs = async (userId: string) => {
    if (userLogs[userId]) return;
    setLoadingLogs(userId);
    try {
      const res = await httpClient.get(`/api/activity/${userId}/logs`);
      const data = res.data.data || res.data;
      setUserLogs((prev) => ({ ...prev, [userId]: data.logs || [] }));
    } catch {
      toast.error(`Failed to load logs for ${userId}`);
    } finally {
      setLoadingLogs(null);
    }
  };

  const handleExpand = (userId: string) => {
    if (expandedUser === userId) {
      setExpandedUser(null);
    } else {
      setExpandedUser(userId);
      loadUserLogs(userId);
    }
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

  const getEventLabel = (eventType: string) => {
    const labels: Record<string, string> = {
      tab_hidden: "Tab Hidden (switched away)",
      tab_visible: "Tab Visible (came back)",
      window_blur: "Window Blur (Alt+Tab)",
      window_focus: "Window Focus (returned)",
    };
    return labels[eventType] || eventType;
  };

  const getEventColor = (eventType: string) => {
    if (eventType === "tab_hidden" || eventType === "window_blur") return "text-orange-600 bg-orange-50 dark:bg-orange-900/20";
    return "text-green-600 bg-green-50 dark:bg-green-900/20";
  };

  const suspiciousUsers = overview.filter((u) => u.isSuspicious);
  const totalSwitches = overview.reduce((sum, u) => sum + u.switchesInLastHour, 0);

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-2xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-amber-500 to-orange-500 rounded-lg p-2">
              <ArrowLeftRight className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Tab Switch Logs</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Activity in the last hour · Auto-refreshes every 15s
              </p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <XCircle className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Summary Bar */}
        {overview.length > 0 && (
          <div className="px-6 py-3 bg-amber-50 dark:bg-amber-900/10 border-b dark:border-gray-800 flex items-center gap-6">
            <div className="flex items-center gap-2">
              <ArrowLeftRight className="h-4 w-4 text-amber-600" />
              <span className="text-sm font-medium text-amber-700 dark:text-amber-400">
                {totalSwitches} switches (last hour)
              </span>
            </div>
            {suspiciousUsers.length > 0 && (
              <div className="flex items-center gap-2">
                <AlertTriangle className="h-4 w-4 text-red-500" />
                <span className="text-sm font-medium text-red-700 dark:text-red-400">
                  {suspiciousUsers.length} suspicious user(s)
                </span>
              </div>
            )}
            <div className="flex items-center gap-2">
              <User className="h-4 w-4 text-gray-500" />
              <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                {overview.filter((u) => u.switchesInLastHour > 0).length} active switcher(s)
              </span>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <p className="text-center text-gray-400 py-12">Loading...</p>
          ) : overview.filter((u) => u.switchesInLastHour > 0).length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20">
              <Eye className="h-16 w-16 text-green-300 mb-4" />
              <p className="text-lg font-medium text-gray-600 dark:text-gray-300">All participants are focused</p>
              <p className="text-sm text-gray-400 mt-1">No tab switches detected in the last hour.</p>
            </div>
          ) : (
            <div className="p-4 space-y-3">
              {overview
                .filter((u) => u.switchesInLastHour > 0)
                .sort((a, b) => b.switchesInLastHour - a.switchesInLastHour)
                .map((user) => (
                  <div key={user.userId} className="border dark:border-gray-700 rounded-lg overflow-hidden">
                    {/* User Header */}
                    <div
                      className="px-4 py-3 flex items-center gap-3 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
                      onClick={() => handleExpand(user.userId)}
                    >
                      <div className={`rounded-full p-2 ${user.isSuspicious ? "bg-red-100 dark:bg-red-900/30" : "bg-amber-100 dark:bg-amber-900/30"}`}>
                        <User className={`h-4 w-4 ${user.isSuspicious ? "text-red-600 dark:text-red-400" : "text-amber-600 dark:text-amber-400"}`} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-semibold text-gray-800 dark:text-gray-100">{user.userId}</span>
                          {user.fullName && <span className="text-xs text-gray-400">({user.fullName})</span>}
                          {user.isSuspicious && (
                            <span className="flex items-center gap-1 text-[10px] px-1.5 py-0.5 rounded-full bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400 font-medium">
                              <AlertTriangle className="h-2.5 w-2.5" /> Suspicious
                            </span>
                          )}
                        </div>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          {user.switchesInLastHour} switch(es) in the last hour
                        </p>
                      </div>
                      <div className="text-center">
                        <p className={`text-lg font-bold ${user.isSuspicious ? "text-red-600 dark:text-red-400" : "text-amber-600 dark:text-amber-400"}`}>
                          {user.switchesInLastHour}
                        </p>
                        <p className="text-[10px] text-gray-400">switches</p>
                      </div>
                    </div>

                    {/* Expanded: Log Details */}
                    {expandedUser === user.userId && (
                      <div className="border-t dark:border-gray-700 bg-gray-50 dark:bg-gray-800/30 px-4 py-3 max-h-80 overflow-y-auto">
                        {loadingLogs === user.userId ? (
                          <p className="text-center text-gray-400 py-4 text-sm">Loading logs...</p>
                        ) : (userLogs[user.userId] || []).length === 0 ? (
                          <p className="text-center text-gray-400 py-4 text-sm">No detailed logs available</p>
                        ) : (
                          <div className="space-y-1.5">
                            {(userLogs[user.userId] || []).slice(0, 50).map((log, idx) => (
                              <div
                                key={idx}
                                className={`flex items-center gap-3 px-3 py-2 rounded-lg ${getEventColor(log.eventType)}`}
                              >
                                <ArrowLeftRight className="h-3.5 w-3.5 shrink-0" />
                                <div className="flex-1 min-w-0">
                                  <p className="text-xs font-medium">{getEventLabel(log.eventType)}</p>
                                  {log.awayDurationSeconds != null && log.awayDurationSeconds > 0 && (
                                    <p className="text-[10px] text-gray-500">Away for {log.awayDurationSeconds}s</p>
                                  )}
                                </div>
                                <div className="flex items-center gap-1 text-[10px] text-gray-500 shrink-0">
                                  <Clock className="h-3 w-3" />
                                  {formatTime(log.eventTime)}
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
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
