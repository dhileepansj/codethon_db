import { useState, useEffect } from "react";
import { X, Plus, Trash2, Edit, Shield, KeyRound } from "lucide-react";
import { toast } from "sonner";
import { adminUserService } from "@/services/adminUserService";
import type { AdminUserData, AdminPermissionsData } from "@/services/adminUserService";

interface Props {
  onClose: () => void;
}

type View = "list" | "create" | "edit";

const PERMISSION_LABELS: { key: keyof AdminPermissionsData; label: string; description: string }[] = [
  { key: "canManageUsers", label: "Manage Users", description: "Create, edit, deactivate participants" },
  { key: "canManageSessions", label: "Manage Sessions", description: "Activate, deactivate, extend sessions" },
  { key: "canViewMonitoring", label: "View Monitoring", description: "Tab-switch logs, DevTools attempts" },
  { key: "canManageAssessments", label: "Manage Assessments", description: "MCQ CRUD, question bank upload" },
  { key: "canViewResults", label: "View Results", description: "MCQ scores, download CSV" },
  { key: "canManageHackathonSetup", label: "Hackathon Setup", description: "Question paper, schedule, breaks" },
  { key: "canManageServerConfig", label: "Server Config", description: "SQL Server / Oracle connections" },
  { key: "canManageScaffoldScripts", label: "Scaffold Scripts", description: "Upload/edit starter scripts" },
  { key: "canManageSecuritySettings", label: "Security Settings", description: "Password policies, lockout" },
  { key: "canManageAiDetection", label: "AI Detection", description: "Configure AI plagiarism settings" },
  { key: "canExportData", label: "Export Data", description: "Export participant submissions" },
  { key: "canResetDatabase", label: "Reset Database", description: "Drop and reset participant DBs" },
  { key: "canDeleteUsers", label: "Delete Users", description: "Permanently delete users" },
];

const DEFAULT_PERMISSIONS: AdminPermissionsData = {
  canManageUsers: false, canManageSessions: false, canViewMonitoring: false,
  canManageAssessments: false, canViewResults: false, canManageHackathonSetup: false,
  canManageServerConfig: false, canManageScaffoldScripts: false, canManageSecuritySettings: false,
  canManageAiDetection: false, canExportData: false, canResetDatabase: false, canDeleteUsers: false,
};

export default function AdminUsersPanel({ onClose }: Props) {
  const [view, setView] = useState<View>("list");
  const [admins, setAdmins] = useState<AdminUserData[]>([]);
  const [selectedAdmin, setSelectedAdmin] = useState<AdminUserData | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState<AdminUserData | null>(null);

  useEffect(() => { loadAdmins(); }, []);

  const loadAdmins = async () => {
    try { const data = await adminUserService.getAll(); setAdmins(data); }
    catch { toast.error("Failed to load admins"); }
    finally { setLoading(false); }
  };

  const handleDelete = async (admin: AdminUserData) => {
    setDeleteTarget(admin);
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try { await adminUserService.delete(deleteTarget.id); toast.success("Admin deleted"); loadAdmins(); }
    catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
    finally { setDeleteTarget(null); }
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-900 rounded-xl w-[90vw] max-w-3xl max-h-[80vh] flex flex-col shadow-lg border dark:border-gray-800">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b dark:border-gray-800">
          <div className="flex items-center gap-2">
            <Shield className="h-4 w-4 text-teal-500" />
            <h2 className="text-sm font-semibold text-gray-900 dark:text-white">
              {view === "list" ? "Admin Users" : view === "create" ? "Create Admin" : "Edit Admin"}
            </h2>
          </div>
          <div className="flex items-center gap-2">
            {view !== "list" && (
              <button onClick={() => { setView("list"); setSelectedAdmin(null); }} className="px-2.5 py-1 text-xs text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">← Back</button>
            )}
            <button onClick={onClose} className="p-1.5 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md"><X className="h-4 w-4 text-gray-500" /></button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-5">
          {view === "list" && <AdminList admins={admins} loading={loading} onCreate={() => setView("create")} onEdit={(a) => { setSelectedAdmin(a); setView("edit"); }} onDelete={handleDelete} />}
          {view === "create" && <AdminForm onSave={() => { loadAdmins(); setView("list"); }} />}
          {view === "edit" && selectedAdmin && <AdminForm admin={selectedAdmin} onSave={() => { loadAdmins(); setView("list"); }} />}
        </div>
      </div>

      {/* Delete Confirmation */}
      {deleteTarget && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-[60]">
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-800 p-6 max-w-xs w-full mx-4 shadow-lg">
            <div className="flex items-start gap-3 mb-4">
              <Trash2 className="h-5 w-5 text-red-500 shrink-0 mt-0.5" />
              <div>
                <h3 className="text-sm font-semibold text-gray-900 dark:text-white">Delete Admin</h3>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  Delete "{deleteTarget.userID}"? This cannot be undone.
                </p>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button onClick={() => setDeleteTarget(null)} className="px-3 py-1.5 text-xs text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">Cancel</button>
              <button onClick={confirmDelete} className="px-3 py-1.5 text-xs font-medium bg-red-600 text-white rounded-md hover:bg-red-700">Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── List ────────────────────────────────────────────────────────

function AdminList({ admins, loading, onCreate, onEdit, onDelete }: {
  admins: AdminUserData[]; loading: boolean;
  onCreate: () => void; onEdit: (a: AdminUserData) => void; onDelete: (a: AdminUserData) => void;
}) {
  if (loading) return <p className="text-sm text-gray-400 text-center py-8">Loading...</p>;

  return (
    <div>
      <div className="flex justify-end mb-4">
        <button onClick={onCreate} className="flex items-center gap-1.5 px-3 py-1.5 bg-teal-600 text-white text-xs font-medium rounded-md hover:bg-teal-700">
          <Plus className="h-3.5 w-3.5" /> New Admin
        </button>
      </div>

      {admins.length === 0 ? (
        <p className="text-sm text-gray-400 text-center py-10">No admin users created yet</p>
      ) : (
        <div className="space-y-2">
          {admins.map((a) => {
            const permCount = Object.values(a.permissions).filter(Boolean).length;
            return (
              <div key={a.id} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border dark:border-gray-700">
                <div>
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-medium text-gray-900 dark:text-white">{a.userID}</p>
                    {!a.isActive && <span className="text-[10px] text-red-500 font-medium">Inactive</span>}
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {a.fullName || "—"} · {permCount}/13 permissions
                    {a.lastLoginAt && <span className="ml-2">Last login: {new Date(a.lastLoginAt).toLocaleDateString()}</span>}
                  </p>
                </div>
                <div className="flex items-center gap-1">
                  <button onClick={() => onEdit(a)} className="p-1.5 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded text-amber-600" title="Edit"><Edit className="h-3.5 w-3.5" /></button>
                  <button onClick={() => onDelete(a)} className="p-1.5 hover:bg-red-50 dark:hover:bg-red-900/20 rounded text-red-500" title="Delete"><Trash2 className="h-3.5 w-3.5" /></button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ─── Form ────────────────────────────────────────────────────────

function AdminForm({ admin, onSave }: { admin?: AdminUserData; onSave: () => void }) {
  const [form, setForm] = useState({
    UserID: admin?.userID || "",
    Password: "",
    FullName: admin?.fullName || "",
    Email: admin?.email || "",
    IsActive: admin?.isActive ?? true,
  });
  const [permissions, setPermissions] = useState<AdminPermissionsData>(admin?.permissions || DEFAULT_PERMISSIONS);
  const [newPassword, setNewPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (admin) {
        await adminUserService.update(admin.id, {
          FullName: form.FullName || undefined,
          Email: form.Email || undefined,
          IsActive: form.IsActive,
          Permissions: permissions,
        });
        toast.success("Admin updated");
      } else {
        if (!form.UserID || !form.Password) { toast.error("User ID and Password required"); return; }
        await adminUserService.create({
          UserID: form.UserID,
          Password: form.Password,
          FullName: form.FullName || undefined,
          Email: form.Email || undefined,
          Permissions: permissions,
        });
        toast.success("Admin created");
      }
      onSave();
    } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const handleChangePassword = async () => {
    if (!admin || !newPassword) { toast.error("Enter a password"); return; }
    try { await adminUserService.changePassword(admin.id, newPassword); toast.success("Password changed"); setNewPassword(""); }
    catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const toggleAll = (val: boolean) => {
    const p = { ...permissions };
    for (const key of Object.keys(p)) (p as any)[key] = val;
    setPermissions(p);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5 max-w-lg">
      {/* Basic info */}
      <div className="grid grid-cols-2 gap-3">
        <div className={admin ? "col-span-2" : ""}>
          <label className="text-[11px] font-medium text-gray-500 dark:text-gray-400">User ID {!admin && "*"}</label>
          <input value={form.UserID} onChange={(e) => setForm({ ...form, UserID: e.target.value })} disabled={!!admin} required={!admin}
            className="w-full mt-1 px-3 py-2 border dark:border-gray-700 rounded-md text-sm dark:bg-gray-800 dark:text-white disabled:opacity-50" />
        </div>
        {!admin && (
          <div>
            <label className="text-[11px] font-medium text-gray-500 dark:text-gray-400">Password *</label>
            <input type="password" value={form.Password} onChange={(e) => setForm({ ...form, Password: e.target.value })} required
              className="w-full mt-1 px-3 py-2 border dark:border-gray-700 rounded-md text-sm dark:bg-gray-800 dark:text-white" />
          </div>
        )}
        <div>
          <label className="text-[11px] font-medium text-gray-500 dark:text-gray-400">Full Name</label>
          <input value={form.FullName} onChange={(e) => setForm({ ...form, FullName: e.target.value })}
            className="w-full mt-1 px-3 py-2 border dark:border-gray-700 rounded-md text-sm dark:bg-gray-800 dark:text-white" />
        </div>
        <div>
          <label className="text-[11px] font-medium text-gray-500 dark:text-gray-400">Email</label>
          <input value={form.Email} onChange={(e) => setForm({ ...form, Email: e.target.value })}
            className="w-full mt-1 px-3 py-2 border dark:border-gray-700 rounded-md text-sm dark:bg-gray-800 dark:text-white" />
        </div>
      </div>

      {admin && (
        <div className="flex items-center gap-2">
          <input type="checkbox" checked={form.IsActive} onChange={(e) => setForm({ ...form, IsActive: e.target.checked })} className="rounded" />
          <label className="text-sm text-gray-700 dark:text-gray-300">Active</label>
        </div>
      )}

      {/* Permissions */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <p className="text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Permissions</p>
          <div className="flex gap-2">
            <button type="button" onClick={() => toggleAll(true)} className="text-[10px] text-teal-600 hover:underline">Select All</button>
            <button type="button" onClick={() => toggleAll(false)} className="text-[10px] text-gray-400 hover:underline">Clear All</button>
          </div>
        </div>
        <div className="grid grid-cols-1 gap-1.5 max-h-60 overflow-auto border dark:border-gray-700 rounded-lg p-3">
          {PERMISSION_LABELS.map(({ key, label, description }) => (
            <label key={key} className="flex items-start gap-2.5 py-1.5 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 rounded px-1.5 -mx-1.5">
              <input type="checkbox" checked={permissions[key]} onChange={(e) => setPermissions({ ...permissions, [key]: e.target.checked })}
                className="rounded mt-0.5 shrink-0" />
              <div>
                <p className="text-xs font-medium text-gray-800 dark:text-gray-200">{label}</p>
                <p className="text-[10px] text-gray-400">{description}</p>
              </div>
            </label>
          ))}
        </div>
      </div>

      {/* Change password (edit mode) */}
      {admin && (
        <div className="border-t dark:border-gray-700 pt-4">
          <p className="text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Change Password</p>
          <div className="flex gap-2">
            <input type="password" placeholder="New password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
              className="flex-1 px-3 py-2 border dark:border-gray-700 rounded-md text-sm dark:bg-gray-800 dark:text-white" />
            <button type="button" onClick={handleChangePassword} className="px-3 py-2 bg-amber-500 text-white text-xs font-medium rounded-md hover:bg-amber-600">
              <KeyRound className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      )}

      <div className="flex justify-end pt-2">
        <button type="submit" className="px-5 py-2 bg-teal-600 text-white text-xs font-medium rounded-md hover:bg-teal-700">
          {admin ? "Update Admin" : "Create Admin"}
        </button>
      </div>
    </form>
  );
}
