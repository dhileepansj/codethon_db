import { useEffect, useState } from "react";
import { Shield, ShieldAlert, ShieldCheck, ShieldX, Settings, Eye, CheckCircle, XCircle, Clock, AlertTriangle, User } from "lucide-react";
import { toast } from "sonner";
import { aiDetectionService } from "@/services/aiDetectionService";
import type { AiDetectionSettings, BlockedSave, AiDetectionLog, UserOverride } from "@/services/aiDetectionService";
import type { UserDto } from "@/types";

interface AiDetectionPanelProps {
  users: UserDto[];
  onClose: () => void;
}

type TabId = "blocked" | "logs" | "settings";

export default function AiDetectionPanel({ users, onClose }: AiDetectionPanelProps) {
  const [activeTab, setActiveTab] = useState<TabId>("blocked");
  const [settings, setSettings] = useState<AiDetectionSettings | null>(null);
  const [blockedSaves, setBlockedSaves] = useState<BlockedSave[]>([]);
  const [logs, setLogs] = useState<AiDetectionLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [showSettingsEdit, setShowSettingsEdit] = useState(false);
  const [editMode, setEditMode] = useState("AllowAndMark");
  const [editThreshold, setEditThreshold] = useState(70);
  const [expandedBlockId, setExpandedBlockId] = useState<number | null>(null);
  const [showUserOverride, setShowUserOverride] = useState(false);
  const [overrideUserId, setOverrideUserId] = useState("");
  const [overrideMode, setOverrideMode] = useState("Block");
  const [overrideThreshold, setOverrideThreshold] = useState(70);

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = async () => {
    setLoading(true);
    try {
      const s = await aiDetectionService.getSettings();
      setSettings(s);
      setEditMode(s.mode);
      setEditThreshold(s.blockThreshold);

      if (activeTab === "blocked") {
        const blocked = await aiDetectionService.getBlockedSaves();
        setBlockedSaves(blocked);
      } else if (activeTab === "logs") {
        const flagged = await aiDetectionService.getFlaggedLogs(30);
        setLogs(flagged);
      }
    } catch {
      toast.error("Failed to load AI detection data");
    } finally {
      setLoading(false);
    }
  };

  const handleSaveSettings = async () => {
    try {
      await aiDetectionService.updateSettings({ mode: editMode, blockThreshold: editThreshold });
      toast.success("AI detection settings updated");
      setShowSettingsEdit(false);
      loadData();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Failed to update settings");
    }
  };

  const handleApprove = async (id: number) => {
    try {
      await aiDetectionService.approveBlockedSave(id);
      toast.success("Save approved — file content saved for user");
      loadData();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Failed");
    }
  };

  const handleReject = async (id: number) => {
    try {
      await aiDetectionService.rejectBlockedSave(id);
      toast.success("Save rejected");
      loadData();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Failed");
    }
  };

  const handleSetUserOverride = async () => {
    if (!overrideUserId) return;
    try {
      await aiDetectionService.setUserOverride(overrideUserId, { mode: overrideMode, blockThreshold: overrideThreshold });
      toast.success(`Override set for ${overrideUserId}`);
      setShowUserOverride(false);
      setOverrideUserId("");
      loadData();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Failed");
    }
  };

  const handleRemoveOverride = async (userId: string) => {
    try {
      await aiDetectionService.removeUserOverride(userId);
      toast.success("Override removed");
      loadData();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Failed");
    }
  };

  const modeColors: Record<string, string> = {
    Block: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
    AllowAndMark: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
    Disabled: "bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400",
  };

  const scoreColor = (score: number) => {
    if (score >= 70) return "text-red-600 dark:text-red-400";
    if (score >= 50) return "text-amber-600 dark:text-amber-400";
    return "text-green-600 dark:text-green-400";
  };

  const pendingCount = blockedSaves.filter(b => b.status === "Pending").length;

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-3xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-purple-500 to-pink-500 rounded-lg p-2">
              <Shield className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">AI Detection</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Mode: <span className={`inline-flex px-1.5 py-0.5 rounded text-[10px] font-medium ${modeColors[settings?.mode || "Disabled"]}`}>
                  {settings?.mode || "Loading..."}
                </span>
                {" · "}Threshold: {settings?.blockThreshold ?? "..."}%
                {pendingCount > 0 && (
                  <span className="ml-2 inline-flex px-1.5 py-0.5 rounded bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400 text-[10px] font-bold">
                    {pendingCount} pending
                  </span>
                )}
              </p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <XCircle className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Tabs */}
        <div className="flex border-b dark:border-gray-800 px-6 shrink-0">
          {([
            { id: "blocked" as TabId, label: "Blocked Saves", icon: <ShieldAlert className="h-3.5 w-3.5" />, badge: pendingCount },
            { id: "logs" as TabId, label: "Detection Logs", icon: <Eye className="h-3.5 w-3.5" /> },
            { id: "settings" as TabId, label: "Settings", icon: <Settings className="h-3.5 w-3.5" /> },
          ]).map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? "border-purple-500 text-purple-600 dark:text-purple-400"
                  : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400"
              }`}
            >
              {tab.icon}
              {tab.label}
              {tab.badge ? (
                <span className="ml-1 px-1.5 py-0.5 text-[10px] font-bold rounded-full bg-red-500 text-white">{tab.badge}</span>
              ) : null}
            </button>
          ))}
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <p className="text-center text-gray-400 py-12">Loading...</p>
          ) : (
            <>
              {/* ─── Blocked Saves Tab ──────────────────────── */}
              {activeTab === "blocked" && (
                <div className="p-4 space-y-2">
                  {blockedSaves.length === 0 ? (
                    <div className="text-center py-12">
                      <ShieldCheck className="h-12 w-12 text-green-400 mx-auto mb-3" />
                      <p className="text-gray-500 dark:text-gray-400">No blocked saves</p>
                    </div>
                  ) : (
                    blockedSaves.map((b) => (
                      <div key={b.id} className="border dark:border-gray-700 rounded-lg overflow-hidden">
                        <div
                          className="px-4 py-3 flex items-center gap-3 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
                          onClick={() => setExpandedBlockId(expandedBlockId === b.id ? null : b.id)}
                        >
                          {b.status === "Pending" ? (
                            <Clock className="h-4 w-4 text-amber-500 shrink-0" />
                          ) : b.status === "Approved" ? (
                            <CheckCircle className="h-4 w-4 text-green-500 shrink-0" />
                          ) : (
                            <XCircle className="h-4 w-4 text-red-500 shrink-0" />
                          )}
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-medium text-gray-800 dark:text-gray-100">{b.loginId || `User #${b.userId}`}</span>
                              <span className="text-xs text-gray-400">·</span>
                              <span className="text-xs text-gray-500 dark:text-gray-400 font-mono truncate">{b.fileName}</span>
                            </div>
                            <p className="text-xs text-gray-400 mt-0.5">
                              {new Date(b.blockedDate).toLocaleString("en-IN", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", hour12: false })}
                              {b.reasoning && ` — ${b.reasoning.substring(0, 80)}${b.reasoning.length > 80 ? "..." : ""}`}
                            </p>
                          </div>
                          <span className={`text-sm font-bold ${scoreColor(b.confidenceScore)}`}>{b.confidenceScore}%</span>
                          <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium ${
                            b.status === "Pending" ? "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400" :
                            b.status === "Approved" ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400" :
                            "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400"
                          }`}>
                            {b.status}
                          </span>
                        </div>

                        {expandedBlockId === b.id && (
                          <div className="px-4 pb-4 border-t dark:border-gray-700 bg-gray-50 dark:bg-gray-800/30">
                            <div className="mt-3 space-y-2">
                              {b.reasoning && (
                                <p className="text-xs text-gray-600 dark:text-gray-300">
                                  <strong>AI Reasoning:</strong> {b.reasoning}
                                </p>
                              )}
                              {b.attemptedContent && (
                                <details className="text-xs">
                                  <summary className="cursor-pointer text-gray-500 hover:text-gray-700 dark:text-gray-400">View attempted content ({b.attemptedContent.length} chars)</summary>
                                  <pre className="mt-1 p-2 bg-gray-100 dark:bg-gray-900 rounded text-[11px] max-h-48 overflow-auto whitespace-pre-wrap font-mono">{b.attemptedContent}</pre>
                                </details>
                              )}
                              {b.reviewedBy && (
                                <p className="text-xs text-gray-500">
                                  Reviewed by <strong>{b.reviewedBy}</strong> on {new Date(b.reviewedDate!).toLocaleString()}
                                  {b.adminRemarks && ` — "${b.adminRemarks}"`}
                                </p>
                              )}
                              {b.status === "Pending" && (
                                <div className="flex gap-2 pt-2">
                                  <button
                                    onClick={() => handleApprove(b.id)}
                                    className="flex items-center gap-1 px-3 py-1.5 bg-green-600 text-white rounded-lg text-xs font-medium hover:bg-green-700"
                                  >
                                    <CheckCircle className="h-3 w-3" /> Approve & Save
                                  </button>
                                  <button
                                    onClick={() => handleReject(b.id)}
                                    className="flex items-center gap-1 px-3 py-1.5 bg-red-600 text-white rounded-lg text-xs font-medium hover:bg-red-700"
                                  >
                                    <XCircle className="h-3 w-3" /> Reject
                                  </button>
                                </div>
                              )}
                            </div>
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </div>
              )}

              {/* ─── Detection Logs Tab ─────────────────────── */}
              {activeTab === "logs" && (
                <div className="p-4">
                  {logs.length === 0 ? (
                    <p className="text-center text-gray-400 py-12">No detection logs yet</p>
                  ) : (
                    <div className="space-y-1">
                      {logs.map((l) => (
                        <div key={l.id} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 border-b dark:border-gray-800 last:border-0">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-medium text-gray-800 dark:text-gray-100">{l.loginId || `User #${l.userId}`}</span>
                              <span className="text-xs text-gray-400 font-mono truncate">{l.fileName}</span>
                              {l.tabSwitchBeforeSave && (
                                <span className="text-[10px] px-1 py-0.5 bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400 rounded" title="Tab switch detected before save">
                                  ⚠ Tab Switch
                                </span>
                              )}
                            </div>
                            <p className="text-xs text-gray-400 mt-0.5">
                              {new Date(l.analyzedDate).toLocaleString("en-IN", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", hour12: false })}
                              {" · "}{l.contentLength} chars (+{l.contentDelta})
                              {l.processingTimeMs && ` · ${l.processingTimeMs}ms`}
                              {l.reasoning && ` — ${l.reasoning.substring(0, 60)}...`}
                            </p>
                          </div>
                          <span className={`text-sm font-bold ${scoreColor(l.confidenceScore)}`}>{l.confidenceScore}%</span>
                          <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium ${
                            l.detectionResult === "AI" ? "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" :
                            l.detectionResult === "Human" ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400" :
                            "bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400"
                          }`}>
                            {l.detectionResult}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* ─── Settings Tab ──────────────────────────── */}
              {activeTab === "settings" && settings && (
                <div className="p-6 space-y-6">
                  {/* Global Settings */}
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3 flex items-center gap-2">
                      <Settings className="h-4 w-4" /> Global Settings
                    </h3>

                    {!showSettingsEdit ? (
                      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 space-y-2">
                        <div className="flex items-center justify-between">
                          <span className="text-sm text-gray-600 dark:text-gray-300">Detection Mode</span>
                          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${modeColors[settings.mode]}`}>{settings.mode}</span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-sm text-gray-600 dark:text-gray-300">Block Threshold</span>
                          <span className="text-sm font-mono font-bold text-gray-800 dark:text-gray-100">{settings.blockThreshold}%</span>
                        </div>
                        {settings.modifiedBy && (
                          <p className="text-xs text-gray-400">Last modified by {settings.modifiedBy} on {new Date(settings.modifiedDate!).toLocaleString()}</p>
                        )}
                        <button onClick={() => setShowSettingsEdit(true)} className="mt-2 px-3 py-1.5 bg-purple-600 text-white rounded-lg text-xs font-medium hover:bg-purple-700">
                          Edit Settings
                        </button>
                      </div>
                    ) : (
                      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 space-y-3">
                        <div>
                          <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">Detection Mode</label>
                          <select
                            value={editMode}
                            onChange={(e) => setEditMode(e.target.value)}
                            className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                          >
                            <option value="Block">Block — Reject save if AI detected</option>
                            <option value="AllowAndMark">Allow & Mark — Save but flag for admin</option>
                            <option value="Disabled">Disabled — No AI detection</option>
                          </select>
                        </div>
                        <div>
                          <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">
                            Block Threshold: <strong>{editThreshold}%</strong>
                          </label>
                          <input
                            type="range"
                            min="0"
                            max="100"
                            value={editThreshold}
                            onChange={(e) => setEditThreshold(parseInt(e.target.value))}
                            className="w-full"
                          />
                          <div className="flex justify-between text-[10px] text-gray-400">
                            <span>0 (block everything)</span>
                            <span>100 (block only obvious AI)</span>
                          </div>
                        </div>
                        <div className="flex gap-2 pt-1">
                          <button onClick={handleSaveSettings} className="px-3 py-1.5 bg-purple-600 text-white rounded-lg text-xs font-medium hover:bg-purple-700">
                            Save
                          </button>
                          <button onClick={() => setShowSettingsEdit(false)} className="px-3 py-1.5 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg text-xs">
                            Cancel
                          </button>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* User Overrides */}
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3 flex items-center gap-2">
                      <User className="h-4 w-4" /> Per-User Overrides
                    </h3>

                    {settings.userOverrides.length === 0 ? (
                      <p className="text-xs text-gray-400 mb-3">No per-user overrides configured. All users follow global settings.</p>
                    ) : (
                      <div className="space-y-1 mb-3">
                        {settings.userOverrides.map((o) => (
                          <div key={o.userId} className="flex items-center justify-between px-3 py-2 bg-gray-50 dark:bg-gray-800 rounded-lg">
                            <div>
                              <span className="text-sm font-medium text-gray-800 dark:text-gray-100">{o.loginId || `User #${o.userId}`}</span>
                              <span className="text-xs text-gray-400 ml-2">{o.fullName}</span>
                            </div>
                            <div className="flex items-center gap-2">
                              <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium ${modeColors[o.mode || "Disabled"]}`}>{o.mode}</span>
                              <span className="text-xs font-mono text-gray-600 dark:text-gray-300">{o.blockThreshold}%</span>
                              <button
                                onClick={() => handleRemoveOverride(o.loginId!)}
                                className="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 rounded text-red-500"
                                title="Remove override"
                              >
                                <XCircle className="h-3.5 w-3.5" />
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}

                    {!showUserOverride ? (
                      <button onClick={() => setShowUserOverride(true)} className="px-3 py-1.5 bg-teal-600 text-white rounded-lg text-xs font-medium hover:bg-teal-700">
                        + Add User Override
                      </button>
                    ) : (
                      <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4 space-y-3 border border-blue-200 dark:border-blue-800">
                        <div>
                          <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">User</label>
                          <select
                            value={overrideUserId}
                            onChange={(e) => setOverrideUserId(e.target.value)}
                            className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                          >
                            <option value="">Select user...</option>
                            {users.filter(u => u.role !== "SuperAdmin").map(u => (
                              <option key={u.userID} value={u.userID}>{u.userID} — {u.fullName || "No name"}</option>
                            ))}
                          </select>
                        </div>
                        <div>
                          <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">Mode</label>
                          <select
                            value={overrideMode}
                            onChange={(e) => setOverrideMode(e.target.value)}
                            className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                          >
                            <option value="Block">Block</option>
                            <option value="AllowAndMark">Allow & Mark</option>
                            <option value="Disabled">Disabled</option>
                          </select>
                        </div>
                        <div>
                          <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">Threshold: {overrideThreshold}%</label>
                          <input
                            type="range"
                            min="0"
                            max="100"
                            value={overrideThreshold}
                            onChange={(e) => setOverrideThreshold(parseInt(e.target.value))}
                            className="w-full"
                          />
                        </div>
                        <div className="flex gap-2">
                          <button onClick={handleSetUserOverride} className="px-3 py-1.5 bg-teal-600 text-white rounded-lg text-xs font-medium hover:bg-teal-700">
                            Set Override
                          </button>
                          <button onClick={() => setShowUserOverride(false)} className="px-3 py-1.5 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg text-xs">
                            Cancel
                          </button>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* How it works */}
                  <div className="border-t dark:border-gray-800 pt-4">
                    <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">How It Works</h3>
                    <ul className="text-xs text-gray-500 dark:text-gray-400 space-y-1 list-disc pl-4">
                      <li><strong>Block mode:</strong> When AI score ≥ threshold, the save is rejected. User sees a message. Admin reviews and can approve/reject.</li>
                      <li><strong>Allow & Mark mode:</strong> Save is always allowed, but flagged in detection logs for admin review.</li>
                      <li><strong>Disabled:</strong> No AI detection. Files save normally.</li>
                      <li><strong>Per-user override:</strong> Takes priority over global setting. Use to unblock a specific user or apply stricter rules.</li>
                      <li><strong>Fail-open:</strong> If Ollama is unreachable, saves are allowed automatically.</li>
                    </ul>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
