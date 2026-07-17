import { useEffect, useState } from "react";
import { Clock, Plus, Trash2, Save, XCircle, Timer, Coffee } from "lucide-react";
import { toast } from "sonner";
import httpClient from "@/services/httpClient";

interface BreakItem {
  id: number;
  title: string;
  startTime: string;
  endTime: string;
}

interface ScheduleData {
  configured: boolean;
  id?: number;
  sessionStartTime?: string;
  sessionEndTime?: string;
  extensionMinutes?: number;
  scheduleDate?: string;
  breaks?: BreakItem[];
}

interface SchedulePanelProps {
  onClose: () => void;
}

export default function SchedulePanel({ onClose }: SchedulePanelProps) {
  const [schedule, setSchedule] = useState<ScheduleData>({ configured: false });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  // Form fields
  const [startTime, setStartTime] = useState("10:00");
  const [endTime, setEndTime] = useState("18:00");
  const [scheduleDate, setScheduleDate] = useState("");

  // New break form
  const [showAddBreak, setShowAddBreak] = useState(false);
  const [breakTitle, setBreakTitle] = useState("Lunch Break");
  const [breakStart, setBreakStart] = useState("13:00");
  const [breakEnd, setBreakEnd] = useState("14:00");

  // Extend form
  const [extendMinutes, setExtendMinutes] = useState(30);

  // Alert config
  const [alerts, setAlerts] = useState<{ minutes: number; color: string }[]>([
    { minutes: 30, color: "#3b82f6" },
    { minutes: 15, color: "#f59e0b" },
    { minutes: 5, color: "#f97316" },
    { minutes: 1, color: "#ef4444" },
  ]);

  useEffect(() => { loadSchedule(); }, []);

  const loadSchedule = async () => {
    setLoading(true);
    try {
      const res = await httpClient.get("/api/admin/schedule");
      const data = res.data.data || res.data;
      setSchedule(data);
      if (data.configured) {
        setStartTime(data.sessionStartTime || "10:00");
        setEndTime(data.sessionEndTime || "18:00");
        setScheduleDate(data.scheduleDate ? data.scheduleDate.split("T")[0] : "");
        // Parse alert config
        if (data.alertConfig) {
          try {
            const parsed = JSON.parse(data.alertConfig);
            if (Array.isArray(parsed)) setAlerts(parsed);
          } catch {}
        }
      }
    } catch {
      toast.error("Failed to load schedule");
    } finally {
      setLoading(false);
    }
  };

  const handleSaveSchedule = async () => {
    setSaving(true);
    try {
      await httpClient.post("/api/admin/schedule", {
        sessionStartTime: startTime,
        sessionEndTime: endTime,
        scheduleDate: scheduleDate || null,
        alertConfig: JSON.stringify(alerts),
      });
      toast.success("Schedule saved");
      loadSchedule();
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  const handleAddBreak = async () => {
    if (!breakStart || !breakEnd) { toast.error("Start and end times required"); return; }
    try {
      await httpClient.post("/api/admin/schedule/breaks", {
        title: breakTitle.trim() || "Break",
        startTime: breakStart,
        endTime: breakEnd,
      });
      toast.success("Break added");
      setShowAddBreak(false);
      setBreakTitle("Break");
      setBreakStart("");
      setBreakEnd("");
      loadSchedule();
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || err.response?.data?.message || "Failed");
    }
  };

  const handleRemoveBreak = async (id: number) => {
    try {
      await httpClient.delete(`/api/admin/schedule/breaks/${id}`);
      toast.success("Break removed");
      loadSchedule();
    } catch {
      toast.error("Failed to remove");
    }
  };

  const handleExtend = async () => {
    if (!extendMinutes || extendMinutes <= 0) { toast.error("Enter valid minutes"); return; }
    try {
      await httpClient.post("/api/admin/schedule/extend", { minutes: extendMinutes });
      toast.success(`Extended by ${extendMinutes} minutes`);
      loadSchedule();
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Failed");
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-blue-500 to-cyan-500 rounded-lg p-2">
              <Clock className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Session Schedule</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">Define timing, breaks & extensions</p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <XCircle className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-6">
          {loading ? (
            <p className="text-center text-gray-400 py-12">Loading...</p>
          ) : (
            <>
              {/* Session Time */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3 flex items-center gap-2">
                  <Timer className="h-4 w-4" /> Session Timing
                </h3>
                <div className="grid grid-cols-3 gap-3">
                  <div>
                    <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">Start Time</label>
                    <input
                      type="time"
                      value={startTime}
                      onChange={(e) => setStartTime(e.target.value)}
                      className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>
                  <div>
                    <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">End Time</label>
                    <input
                      type="time"
                      value={endTime}
                      onChange={(e) => setEndTime(e.target.value)}
                      className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>
                  <div>
                    <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">Date (optional)</label>
                    <input
                      type="date"
                      value={scheduleDate}
                      onChange={(e) => setScheduleDate(e.target.value)}
                      className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                    />
                  </div>
                </div>
                <button
                  onClick={handleSaveSchedule}
                  disabled={saving}
                  className="mt-3 flex items-center gap-1.5 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium disabled:opacity-50"
                >
                  <Save className="h-3.5 w-3.5" /> {saving ? "Saving..." : "Save Schedule"}
                </button>
              </div>

              {/* Breaks */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3 flex items-center gap-2">
                  <Coffee className="h-4 w-4" /> Breaks
                </h3>

                {/* Existing breaks */}
                {(schedule.breaks || []).length > 0 ? (
                  <div className="space-y-2 mb-3">
                    {(schedule.breaks || []).map((b) => (
                      <div key={b.id} className="flex items-center justify-between px-3 py-2 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg">
                        <div className="flex items-center gap-3">
                          <Coffee className="h-4 w-4 text-amber-600" />
                          <div>
                            <p className="text-sm font-medium text-gray-800 dark:text-gray-100">{b.title}</p>
                            <p className="text-xs text-gray-500">{b.startTime} — {b.endTime}</p>
                          </div>
                        </div>
                        <button
                          onClick={() => handleRemoveBreak(b.id)}
                          className="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 rounded text-red-500"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-gray-400 mb-3">No breaks configured.</p>
                )}

                {/* Add break form */}
                {showAddBreak ? (
                  <div className="border dark:border-gray-700 rounded-lg p-3 space-y-3 bg-gray-50 dark:bg-gray-800">
                    <input
                      value={breakTitle}
                      onChange={(e) => setBreakTitle(e.target.value)}
                      placeholder="Break title (e.g., Lunch Break)"
                      className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                    />
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label className="text-xs text-gray-500 mb-1 block">Start</label>
                        <input
                          type="time"
                          value={breakStart}
                          onChange={(e) => setBreakStart(e.target.value)}
                          className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                        />
                      </div>
                      <div>
                        <label className="text-xs text-gray-500 mb-1 block">End</label>
                        <input
                          type="time"
                          value={breakEnd}
                          onChange={(e) => setBreakEnd(e.target.value)}
                          className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                        />
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <button onClick={handleAddBreak} className="px-3 py-1.5 bg-amber-600 hover:bg-amber-700 text-white rounded-lg text-xs font-medium">
                        Add Break
                      </button>
                      <button onClick={() => setShowAddBreak(false)} className="px-3 py-1.5 text-xs text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg">
                        Cancel
                      </button>
                    </div>
                  </div>
                ) : (
                  <button
                    onClick={() => setShowAddBreak(true)}
                    disabled={!schedule.configured}
                    className="flex items-center gap-1.5 px-3 py-2 border-2 border-dashed dark:border-gray-700 rounded-lg text-xs font-medium text-gray-500 hover:text-amber-600 hover:border-amber-400 transition-colors disabled:opacity-40 disabled:pointer-events-none"
                  >
                    <Plus className="h-3.5 w-3.5" /> Add Break
                  </button>
                )}
              </div>

              {/* Time Alerts */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3 flex items-center gap-2">
                  <Timer className="h-4 w-4" /> End-Time Alerts
                </h3>
                <p className="text-[10px] text-gray-400 mb-3">
                  Full-page alerts shown to participants before the session ends.
                </p>

                {/* Alert rows */}
                <div className="space-y-2 mb-3">
                  {alerts.map((alert, idx) => (
                    <div key={idx} className="flex items-center gap-3 bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2">
                      <input
                        type="color"
                        value={alert.color}
                        onChange={(e) => {
                          const updated = [...alerts];
                          updated[idx].color = e.target.value;
                          setAlerts(updated);
                        }}
                        className="w-8 h-8 rounded cursor-pointer border-0 p-0"
                        title="Pick color"
                      />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <input
                            type="number"
                            value={alert.minutes}
                            onChange={(e) => {
                              const updated = [...alerts];
                              updated[idx].minutes = parseInt(e.target.value) || 0;
                              setAlerts(updated);
                            }}
                            min={1}
                            max={120}
                            className="w-20 px-2 py-1 border dark:border-gray-600 rounded text-sm dark:bg-gray-700 dark:text-white text-center"
                          />
                          <span className="text-xs text-gray-500">minutes before end</span>
                        </div>
                      </div>
                      <div className="w-16 h-6 rounded" style={{ backgroundColor: alert.color }} title={alert.color} />
                      <button
                        onClick={() => setAlerts(alerts.filter((_, i) => i !== idx))}
                        className="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 rounded text-red-500"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  ))}
                </div>

                {/* Add new alert */}
                <button
                  onClick={() => setAlerts([...alerts, { minutes: 10, color: "#6366f1" }])}
                  className="flex items-center gap-1.5 px-3 py-1.5 border-2 border-dashed dark:border-gray-700 rounded-lg text-xs font-medium text-gray-500 hover:text-blue-600 hover:border-blue-400 transition-colors"
                >
                  <Plus className="h-3.5 w-3.5" /> Add Alert
                </button>
              </div>

              {/* Extension */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3 flex items-center gap-2">
                  <Clock className="h-4 w-4" /> Extend Session
                </h3>
                {schedule.extensionMinutes ? (
                  <p className="text-xs text-green-600 dark:text-green-400 mb-2">
                    Current extension: +{schedule.extensionMinutes} minutes
                  </p>
                ) : null}
                <div className="flex items-center gap-3">
                  <input
                    type="number"
                    value={extendMinutes}
                    onChange={(e) => setExtendMinutes(parseInt(e.target.value) || 0)}
                    min={1}
                    max={480}
                    placeholder="Minutes"
                    className="w-32 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                  />
                  <button
                    onClick={handleExtend}
                    disabled={!schedule.configured}
                    className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg text-sm font-medium disabled:opacity-40"
                  >
                    Extend
                  </button>
                  <span className="text-xs text-gray-400">Add extra time to the session end</span>
                </div>
              </div>

              {/* Summary */}
              {schedule.configured && (
                <div className="border-t dark:border-gray-800 pt-4">
                  <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">Current Schedule</h3>
                  <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 text-sm">
                    <p className="text-gray-700 dark:text-gray-200">
                      <strong>Session:</strong> {schedule.sessionStartTime} — {schedule.sessionEndTime}
                      {schedule.extensionMinutes ? <span className="text-green-600"> (+{schedule.extensionMinutes}min)</span> : null}
                    </p>
                    {(schedule.breaks || []).length > 0 && (
                      <p className="text-gray-500 dark:text-gray-400 mt-1">
                        <strong>Breaks:</strong> {(schedule.breaks || []).map(b => `${b.title} (${b.startTime}–${b.endTime})`).join(" · ")}
                      </p>
                    )}
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
