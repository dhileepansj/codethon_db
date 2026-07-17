import { useEffect, useState } from "react";
import { Shield, Lock, Clock, Users, Key, XCircle, CheckCircle } from "lucide-react";
import { toast } from "sonner";
import { securityService } from "@/services/securityService";
import type { SecuritySettings, PasswordChangeLogEntry } from "@/services/securityService";

interface SecuritySettingsPanelProps {
  onClose: () => void;
}

type TabId = "policy" | "logs";

export default function SecuritySettingsPanel({ onClose }: SecuritySettingsPanelProps) {
  const [tab, setTab] = useState<TabId>("policy");
  const [settings, setSettings] = useState<SecuritySettings | null>(null);
  const [logs, setLogs] = useState<PasswordChangeLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  // Editable fields
  const [form, setForm] = useState({
    minLength: 8,
    maxLength: 64,
    requireUppercase: true,
    requireLowercase: true,
    requireDigit: true,
    requireSpecialChar: true,
    passwordHistoryCount: 5,
    maxFailedLoginAttempts: 5,
    lockoutDurationMinutes: 15,
    passwordExpiryDays: 0,
    maxConcurrentSessions: 1,
  });

  useEffect(() => {
    loadData();
  }, [tab]);

  const loadData = async () => {
    setLoading(true);
    try {
      if (tab === "policy") {
        const s = await securityService.getSettings();
        setSettings(s);
        setForm({
          minLength: s.minLength,
          maxLength: s.maxLength,
          requireUppercase: s.requireUppercase,
          requireLowercase: s.requireLowercase,
          requireDigit: s.requireDigit,
          requireSpecialChar: s.requireSpecialChar,
          passwordHistoryCount: s.passwordHistoryCount,
          maxFailedLoginAttempts: s.maxFailedLoginAttempts,
          lockoutDurationMinutes: s.lockoutDurationMinutes,
          passwordExpiryDays: s.passwordExpiryDays,
          maxConcurrentSessions: s.maxConcurrentSessions,
        });
      } else {
        const l = await securityService.getPasswordChangeLogs();
        setLogs(l);
      }
    } catch {
      toast.error("Failed to load security data");
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await securityService.updateSettings(form);
      toast.success("Security settings saved");
      loadData();
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-2xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-indigo-500 to-purple-500 rounded-lg p-2">
              <Shield className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Security Settings</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">Password policy & audit logs</p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <XCircle className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Tabs */}
        <div className="flex border-b dark:border-gray-800 px-6 shrink-0">
          {([
            { id: "policy" as TabId, label: "Password Policy", icon: <Lock className="h-3.5 w-3.5" /> },
            { id: "logs" as TabId, label: "Change Logs", icon: <Clock className="h-3.5 w-3.5" /> },
          ]).map((t) => (
            <button
              key={t.id}
              onClick={() => setTab(t.id)}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                tab === t.id
                  ? "border-indigo-500 text-indigo-600 dark:text-indigo-400"
                  : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400"
              }`}
            >
              {t.icon}
              {t.label}
            </button>
          ))}
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <p className="text-center text-gray-400 py-12">Loading...</p>
          ) : tab === "policy" ? (
            <div className="p-6 space-y-6">
              {/* Password Complexity */}
              <Section title="Password Complexity" icon={<Key className="h-4 w-4" />}>
                <NumberField label="Minimum Length" value={form.minLength} onChange={(v) => setForm({ ...form, minLength: v })} min={4} max={32} />
                <NumberField label="Maximum Length (0 = no limit)" value={form.maxLength} onChange={(v) => setForm({ ...form, maxLength: v })} min={0} max={128} />
                <ToggleField label="Require Uppercase (A-Z)" checked={form.requireUppercase} onChange={(v) => setForm({ ...form, requireUppercase: v })} />
                <ToggleField label="Require Lowercase (a-z)" checked={form.requireLowercase} onChange={(v) => setForm({ ...form, requireLowercase: v })} />
                <ToggleField label="Require Digit (0-9)" checked={form.requireDigit} onChange={(v) => setForm({ ...form, requireDigit: v })} />
                <ToggleField label="Require Special Character (!@#$...)" checked={form.requireSpecialChar} onChange={(v) => setForm({ ...form, requireSpecialChar: v })} />
              </Section>

              {/* Password History */}
              <Section title="Password History" icon={<Clock className="h-4 w-4" />}>
                <NumberField label="Passwords remembered (cannot reuse last N)" value={form.passwordHistoryCount} onChange={(v) => setForm({ ...form, passwordHistoryCount: v })} min={0} max={24} />
              </Section>

              {/* Account Lockout */}
              <Section title="Account Lockout" icon={<Shield className="h-4 w-4" />}>
                <NumberField label="Max failed login attempts (0 = disabled)" value={form.maxFailedLoginAttempts} onChange={(v) => setForm({ ...form, maxFailedLoginAttempts: v })} min={0} max={20} />
                <NumberField label="Lockout duration (minutes, 0 = permanent)" value={form.lockoutDurationMinutes} onChange={(v) => setForm({ ...form, lockoutDurationMinutes: v })} min={0} max={1440} />
              </Section>

              {/* Session Security */}
              <Section title="Session & Expiry" icon={<Users className="h-4 w-4" />}>
                <NumberField label="Password expires after (days, 0 = never)" value={form.passwordExpiryDays} onChange={(v) => setForm({ ...form, passwordExpiryDays: v })} min={0} max={365} />
                <NumberField label="Max concurrent sessions per user (0 = unlimited)" value={form.maxConcurrentSessions} onChange={(v) => setForm({ ...form, maxConcurrentSessions: v })} min={0} max={10} />
              </Section>

              {/* Last modified */}
              {settings?.modifiedBy && (
                <p className="text-xs text-gray-400 text-center">
                  Last modified by <strong>{settings.modifiedBy}</strong> on {new Date(settings.modifiedDate!).toLocaleString()}
                </p>
              )}

              {/* Save Button */}
              <div className="flex justify-end pt-2">
                <button
                  onClick={handleSave}
                  disabled={saving}
                  className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-sm font-medium transition-colors disabled:opacity-50"
                >
                  {saving ? "Saving..." : "Save Settings"}
                </button>
              </div>
            </div>
          ) : (
            <div className="p-4">
              {logs.length === 0 ? (
                <p className="text-center text-gray-400 py-12">No password change logs</p>
              ) : (
                <div className="space-y-1">
                  {logs.map((log) => (
                    <div key={log.id} className="flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 border-b dark:border-gray-800 last:border-0">
                      <div className={`rounded-full p-1.5 ${log.changedBy === "Admin" ? "bg-purple-100 dark:bg-purple-900/30" : "bg-teal-100 dark:bg-teal-900/30"}`}>
                        {log.changedBy === "Admin" ? (
                          <Shield className="h-3.5 w-3.5 text-purple-600 dark:text-purple-400" />
                        ) : (
                          <Key className="h-3.5 w-3.5 text-teal-600 dark:text-teal-400" />
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium text-gray-800 dark:text-gray-100">{log.loginId}</span>
                          {log.fullName && <span className="text-xs text-gray-400">({log.fullName})</span>}
                        </div>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          {log.changedBy === "Admin" ? `Reset by ${log.changedByUserId}` : "Self-changed"}
                          {log.ipAddress && ` · IP: ${log.ipAddress}`}
                        </p>
                      </div>
                      <span className="text-xs text-gray-400 shrink-0">
                        {new Date(log.changedAt).toLocaleString("en-IN", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", hour12: false })}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Sub-components ───────────────────────────────────────────────

function Section({ title, icon, children }: { title: string; icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <div>
      <h3 className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3">
        {icon} {title}
      </h3>
      <div className="space-y-3 pl-6">
        {children}
      </div>
    </div>
  );
}

function NumberField({ label, value, onChange, min, max }: { label: string; value: number; onChange: (v: number) => void; min: number; max: number }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-sm text-gray-600 dark:text-gray-300">{label}</span>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(parseInt(e.target.value) || 0)}
        min={min}
        max={max}
        className="w-20 px-2 py-1.5 text-sm text-center border dark:border-gray-700 rounded-lg dark:bg-gray-800 dark:text-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 outline-none"
      />
    </div>
  );
}

function ToggleField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-sm text-gray-600 dark:text-gray-300">{label}</span>
      <button
        onClick={() => onChange(!checked)}
        className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${checked ? "bg-indigo-600" : "bg-gray-300 dark:bg-gray-600"}`}
      >
        <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${checked ? "translate-x-6" : "translate-x-1"}`} />
      </button>
    </div>
  );
}
