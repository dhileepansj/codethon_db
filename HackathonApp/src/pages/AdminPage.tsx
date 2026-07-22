import { useEffect, useState, useCallback } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import {
  LogOut, Users, Play, Square, RotateCcw, Download, Plus, Upload,
  Clock, Settings, Shield, ShieldAlert, Calendar, ChevronRight,
  Search, MoreHorizontal, AlertTriangle, CheckCircle, XCircle,
  Key, File, History, Eye, Unlock, FileSpreadsheet, Database, ArrowLeftRight, Code, Trash2, ClipboardList
} from "lucide-react";
import { toast } from "sonner";
import { logout } from "@/redux/slices/authSlice";
import { adminService } from "@/services/adminService";
import { activityAdminService } from "@/services/activityService";
import ThemeToggle from "@/components/common/ThemeToggle";
import { ConfirmDialog, InputDialog } from "@/components/common/CustomDialog";
import BulkUserImport from "@/components/admin/BulkUserImport";
import HackathonSetupPanel from "@/components/admin/HackathonSetupPanel";
import AiDetectionPanel from "@/components/admin/AiDetectionPanel";
import TabSwitchLogsPanel from "@/components/admin/TabSwitchLogsPanel";
import SecuritySettingsPanel from "@/components/admin/SecuritySettingsPanel";
import ScaffoldScriptsPanel from "@/components/admin/ScaffoldScriptsPanel";
import SchedulePanel from "@/components/admin/SchedulePanel";
import McqAssessmentPanel from "@/components/admin/McqAssessmentPanel";
import AdminUsersPanel from "@/components/admin/AdminUsersPanel";
import { mcqAdminService } from "@/services/mcqService";
import type { AppDispatch, RootState } from "@/redux/store";
import type { UserDto, DashboardStats } from "@/types";

type AdminView = "participants" | "setup" | "config";
type DialogState =
  | { type: "none" }
  | { type: "activate"; userId: string }
  | { type: "activateAll" }
  | { type: "activateAllConfirm"; duration?: number }
  | { type: "deactivateAll" }
  | { type: "extend"; userId: string }
  | { type: "resetDb"; userId: string }
  | { type: "deleteUser"; userId: string };

export default function AdminPage() {
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const { user } = useSelector((s: RootState) => s.auth);

  // Permissions helper — SuperAdmin has everything, Admin uses permissions object
  const isSuperAdmin = user?.role === "SuperAdmin";
  const can = (perm: string) => isSuperAdmin || (user?.permissions as any)?.[perm] === true;

  const [view, setView] = useState<AdminView>("participants");
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [users, setUsers] = useState<UserDto[]>([]);
  const [activityMap, setActivityMap] = useState<Record<string, { switches: number; suspicious: boolean }>>({});
  const [devtoolsMap, setDevtoolsMap] = useState<Record<string, number>>({});
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [dialog, setDialog] = useState<DialogState>({ type: "none" });

  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showBulkImport, setShowBulkImport] = useState(false);
  const [showHackathonSetup, setShowHackathonSetup] = useState(false);
  const [showServerConfig, setShowServerConfig] = useState(false);
  const [showAiDetection, setShowAiDetection] = useState(false);
  const [showTabSwitchLogs, setShowTabSwitchLogs] = useState(false);
  const [showSecuritySettings, setShowSecuritySettings] = useState(false);
  const [showScaffoldScripts, setShowScaffoldScripts] = useState(false);
  const [showSchedule, setShowSchedule] = useState(false);
  const [showMcqPanel, setShowMcqPanel] = useState(false);
  const [showAdminUsers, setShowAdminUsers] = useState(false);

  const [newUser, setNewUser] = useState({ UserID: "", Password: "", FullName: "", Email: "", DbEnginePreference: "SqlServer", AssessmentId: "" });
  const [serverConfig, setServerConfig] = useState({ ServerName: "", AdminUserId: "", AdminPassword: "", DbPrefix: "Hackathon_", DbEngineType: "SqlServer", OracleServiceName: "", Port: "1521" });
  const [assessmentList, setAssessmentList] = useState<{ id: number; title: string; type: string; subType: string }[]>([]);

  const loadData = useCallback(async () => {
    try {
      const [s, u] = await Promise.all([adminService.getDashboard(), adminService.getUsers()]);
      setStats(s);
      setUsers(u);
      try {
        const activity = await activityAdminService.getOverview();
        const map: Record<string, { switches: number; suspicious: boolean }> = {};
        for (const a of activity) map[a.userId] = { switches: a.switchesInLastHour, suspicious: a.isSuspicious };
        setActivityMap(map);
      } catch {}
      try {
        const res = await fetch(
          `${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/activity/devtools`,
          { headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}`, "Content-Type": "application/json" } }
        );
        if (res.ok) {
          const data = await res.json();
          const devMap: Record<string, number> = {};
          for (const d of (data.data || data)) devMap[d.userId] = d.totalAttempts;
          setDevtoolsMap(devMap);
        }
      } catch {}
    } catch { toast.error("Failed to load data"); }
    // Load assessments for user creation
    try { const a = await mcqAdminService.getAssessments(); setAssessmentList(a.map((x: any) => ({ id: x.id, title: x.title, type: x.type, subType: x.subType }))); } catch {}
  }, []);

  useEffect(() => { loadData(); }, [loadData]);
  useEffect(() => { const i = setInterval(loadData, 30000); return () => clearInterval(i); }, [loadData]);

  const filteredUsers = users.filter((u) =>
    !searchQuery || u.userID.toLowerCase().includes(searchQuery.toLowerCase()) ||
    u.fullName?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // ─── Actions ────────────────────────────────────────────────────

  const doActivate = async (userId: string, minutesStr: string) => {
    const duration = minutesStr.trim() ? parseInt(minutesStr) : undefined;
    setDialog({ type: "none" });
    try { await adminService.activateSession(userId, duration); toast.success(`Activated: ${userId}`); loadData(); } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const doDeactivate = async (userId: string) => {
    try { await adminService.deactivateSession(userId); toast.success("Deactivated"); loadData(); } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const doActivateAll = async (minutesStr: string) => {
    const duration = minutesStr.trim() ? parseInt(minutesStr) : undefined;
    setDialog({ type: "activateAllConfirm", duration });
  };

  const doActivateAllConfirmed = async (duration?: number) => {
    setDialog({ type: "none" });
    try {
      let count = 0;
      for (const u of users) { if (!u.session?.isActive) { await adminService.activateSession(u.userID, duration); count++; } }
      toast.success(`${count} sessions activated`);
      loadData();
    } catch { toast.error("Some activations failed"); loadData(); }
  };

  const doDeactivateAll = async () => {
    setDialog({ type: "none" });
    try {
      let count = 0;
      for (const u of users) { if (u.session?.isActive) { await adminService.deactivateSession(u.userID); count++; } }
      toast.success(`${count} sessions deactivated`);
      loadData();
    } catch { toast.error("Some failed"); loadData(); }
  };

  const doExtend = async (userId: string, minutesStr: string) => {
    setDialog({ type: "none" });
    if (!minutesStr.trim() || isNaN(parseInt(minutesStr))) return;
    try { await adminService.extendSession(userId, parseInt(minutesStr)); toast.success(`Extended by ${minutesStr}m`); loadData(); } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const doResetDb = async (userId: string) => {
    setDialog({ type: "none" });
    try { await adminService.resetDatabase(userId); toast.success("Database reset"); loadData(); } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const doDeleteUser = async (userId: string) => {
    setDialog({ type: "none" });
    try { await adminService.deleteUser(userId); toast.success(`User "${userId}" permanently deleted`); setSelectedUser(null); loadData(); } catch (err: any) { toast.error(err.response?.data?.message || "Failed to delete user"); }
  };

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const payload: any = { UserID: newUser.UserID, Password: newUser.Password, FullName: newUser.FullName || undefined, Email: newUser.Email || undefined };
      if (newUser.AssessmentId) {
        payload.AssessmentId = parseInt(newUser.AssessmentId);
      } else {
        payload.DbEnginePreference = newUser.DbEnginePreference;
      }
      await adminService.createUser(payload);
      toast.success("User created");
      setShowCreateModal(false);
      setNewUser({ UserID: "", Password: "", FullName: "", Email: "", DbEnginePreference: "SqlServer", AssessmentId: "" });
      loadData();
    } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const handleConfigureServer = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await adminService.configureServer({
        ServerName: serverConfig.ServerName,
        AdminUserId: serverConfig.AdminUserId,
        AdminPassword: serverConfig.AdminPassword,
        DbPrefix: serverConfig.DbPrefix || undefined,
        DbEngineType: serverConfig.DbEngineType,
        OracleServiceName: serverConfig.DbEngineType === "Oracle" ? serverConfig.OracleServiceName : undefined,
        Port: serverConfig.DbEngineType === "Oracle" && serverConfig.Port ? parseInt(serverConfig.Port) : undefined,
      });
      toast.success(`${serverConfig.DbEngineType === "Oracle" ? "Oracle" : "SQL Server"} configured`);
      setShowServerConfig(false);
    } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const handleLogout = () => { dispatch(logout()); navigate("/login", { replace: true }); };

  // ─── Render ─────────────────────────────────────────────────────

  return (
    <div className="h-screen flex bg-gray-50 dark:bg-gray-950">
      {/* Sidebar */}
      <aside className="w-60 bg-white dark:bg-gray-900 border-r dark:border-gray-800 flex flex-col shrink-0">
        <div className="h-14 flex items-center gap-2.5 px-5 border-b dark:border-gray-800">
          <div className="bg-gradient-to-br from-teal-500 to-teal-700 rounded-lg p-1.5">
            <Database className="h-4 w-4 text-white" />
          </div>
          <div>
            <span className="text-sm font-semibold text-gray-800 dark:text-gray-100">NovacCodeLab</span>
            <span className="block text-[10px] text-gray-400 -mt-0.5">Admin Console</span>
          </div>
        </div>

        <nav className="flex-1 py-4 px-3 space-y-1">
          {can("canManageUsers") && <SidebarItem icon={<Users className="h-4 w-4" />} label="Participants" active={view === "participants"} onClick={() => setView("participants")} />}
          {can("canManageAiDetection") && <SidebarItem icon={<Shield className="h-4 w-4" />} label="AI Detection" active={false} onClick={() => setShowAiDetection(true)} />}
          {can("canViewMonitoring") && <SidebarItem icon={<ArrowLeftRight className="h-4 w-4" />} label="Tab Switch Logs" active={false} onClick={() => setShowTabSwitchLogs(true)} />}
          {can("canManageSecuritySettings") && <SidebarItem icon={<Key className="h-4 w-4" />} label="Security Settings" active={false} onClick={() => setShowSecuritySettings(true)} />}
          {can("canManageScaffoldScripts") && <SidebarItem icon={<Code className="h-4 w-4" />} label="Scaffold Scripts" active={false} onClick={() => setShowScaffoldScripts(true)} />}
          {can("canManageSessions") && <SidebarItem icon={<Clock className="h-4 w-4" />} label="Session Schedule" active={false} onClick={() => setShowSchedule(true)} />}
          {can("canManageHackathonSetup") && <SidebarItem icon={<Calendar className="h-4 w-4" />} label="Hackathon Setup" active={false} onClick={() => setShowHackathonSetup(true)} />}
          {can("canManageAssessments") && <SidebarItem icon={<ClipboardList className="h-4 w-4" />} label="MCQ Assessments" active={false} onClick={() => setShowMcqPanel(true)} />}
          {can("canManageServerConfig") && <SidebarItem icon={<Settings className="h-4 w-4" />} label="Server Config" active={false} onClick={() => setShowServerConfig(true)} />}
          {isSuperAdmin && <SidebarItem icon={<Shield className="h-4 w-4" />} label="Admin Users" active={false} onClick={() => setShowAdminUsers(true)} />}
        </nav>

        {/* Stats at bottom of sidebar */}
        {stats && (
          <div className="px-4 py-4 border-t dark:border-gray-800 space-y-2">
            <MiniStat label="Users" value={stats.totalUsers} />
            <MiniStat label="Active" value={stats.activeSessions} color="green" />
            <MiniStat label="DBs" value={stats.databasesCreated} color="blue" />
            <MiniStat label="Queries today" value={stats.queriesToday} color="orange" />
          </div>
        )}

        <div className="px-3 py-3 border-t dark:border-gray-800">
          <div className="flex items-center justify-between">
            <ThemeToggle />
            <button onClick={handleLogout} className="flex items-center gap-1.5 text-xs text-gray-500 hover:text-red-500 transition-colors">
              <LogOut className="h-3.5 w-3.5" /> Logout
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 flex flex-col overflow-hidden">
        {/* Top bar */}
        <header className="h-14 bg-white dark:bg-gray-900 border-b dark:border-gray-800 flex items-center justify-between px-6 shrink-0">
          <div className="flex items-center gap-4">
            <h1 className="text-base font-semibold text-gray-800 dark:text-gray-100">Participants</h1>
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" />
              <input
                type="search"
                name="participant-search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search users..."
                autoComplete="off"
                className="pl-8 pr-3 py-1.5 w-56 text-sm border dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 dark:text-gray-200 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500"
              />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <ActionBtn icon={<Play />} label="Activate All" color="green" onClick={() => setDialog({ type: "activateAll" })} />
            <ActionBtn icon={<Square />} label="Stop All" color="red" onClick={() => setDialog({ type: "deactivateAll" })} />
            <ActionBtn icon={<Plus />} label="Create" color="teal" onClick={() => setShowCreateModal(true)} />
            <ActionBtn icon={<Upload />} label="Bulk Import" color="blue" onClick={() => setShowBulkImport(true)} />
            <ActionBtn icon={<Download />} label="Export All" color="orange" onClick={async () => { try { await adminService.exportAll(); toast.success("Downloaded"); } catch (err: any) { toast.error(err.message || "Nothing to export"); } }} />
          </div>
        </header>

        {/* Table */}
        <div className="flex-1 overflow-auto">
          <table className="w-full">
            <thead className="bg-gray-50 dark:bg-gray-900 sticky top-0 z-10">
              <tr className="text-left">
                <th className="px-5 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">User</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Type</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Session</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Time</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Progress</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Monitoring</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Result</th>
                <th className="px-3 py-3 text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-40">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y dark:divide-gray-800">
              {filteredUsers.map((u) => {
                const activity = activityMap[u.userID];
                const devtools = devtoolsMap[u.userID] || 0;
                const isMcq = u.assessmentType === "MCQ";
                return (
                  <tr
                    key={u.id}
                    className={`group hover:bg-teal-50/40 dark:hover:bg-teal-900/5 transition-colors cursor-pointer ${selectedUser?.id === u.id ? "bg-teal-50 dark:bg-teal-900/10" : "bg-white dark:bg-gray-900"}`}
                    onClick={() => { setSelectedUser(u); }}
                  >
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-3">
                        <div className={`w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold ${isMcq ? "bg-gradient-to-br from-indigo-400 to-purple-600" : "bg-gradient-to-br from-teal-400 to-teal-600"}`}>
                          {u.userID.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-800 dark:text-gray-100">
                            {u.userID}
                            {u.passwordResetRequested && (
                              <span className="ml-1.5 inline-flex px-1.5 py-0.5 text-[9px] font-semibold rounded bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
                                🔑 Reset
                              </span>
                            )}
                          </p>
                          <p className="text-xs text-gray-400">{u.fullName || "—"}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      <span className={`text-[10px] px-2 py-0.5 rounded-full font-semibold ${
                        isMcq ? "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300" :
                        u.dbEnginePreference === "Oracle" ? "bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300" :
                        "bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-300"
                      }`}>
                        {isMcq ? (u.assessmentSubType || "MCQ") : u.dbEnginePreference === "Oracle" ? "Oracle" : "SQL Server"}
                      </span>
                    </td>
                    <td className="px-3 py-3">
                      <StatusBadge status={u.session?.isExpired ? "expired" : u.session?.isActive ? "active" : "inactive"} />
                    </td>
                    <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400 font-mono">
                      {u.session?.isExpired ? <span className="text-red-500">Expired</span> : u.session?.expiresAt ? formatTimeRemaining(u.session.expiresAt) : u.session?.isActive ? "∞" : "—"}
                    </td>
                    <td className="px-3 py-3">
                      {isMcq ? (
                        u.mcqProgress?.status === "Submitted" ? (
                          <span className="text-xs font-mono text-indigo-600 dark:text-indigo-400">{u.mcqProgress.answered}/{u.mcqProgress.totalQuestions} done</span>
                        ) : u.mcqProgress?.status === "InProgress" ? (
                          <span className="text-xs font-mono text-amber-600">{u.mcqProgress.answered}/{u.mcqProgress.totalQuestions} answering</span>
                        ) : (
                          <span className="text-xs text-gray-400">Not started</span>
                        )
                      ) : (
                        u.session?.databaseCreated ? (
                          <span className="text-xs font-mono text-teal-600 dark:text-teal-400">{u.session.databaseName}</span>
                        ) : (
                          <span className="text-xs text-gray-400">—</span>
                        )
                      )}
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-2">
                        {!activity || activity.switches === 0 ? (
                          <span className="text-xs text-green-600">●</span>
                        ) : activity.suspicious ? (
                          <span className="flex items-center gap-0.5 text-xs text-red-600 font-medium"><AlertTriangle className="h-3 w-3" />{activity.switches}</span>
                        ) : (
                          <span className="text-xs text-gray-500">{activity.switches}sw</span>
                        )}
                        {devtools > 0 && (
                          <span className="flex items-center gap-0.5 text-xs text-red-600 font-medium"><ShieldAlert className="h-3 w-3" />{devtools}</span>
                        )}
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      {isMcq ? (
                        u.mcqProgress?.status === "Submitted" ? (
                          <span className={`text-xs font-mono font-semibold ${u.mcqProgress.passed === false ? "text-red-600" : "text-green-600"}`}>
                            {u.mcqProgress.score}/{u.mcqProgress.maxScore} ({u.mcqProgress.percentage}%)
                          </span>
                        ) : (
                          <span className="text-xs text-gray-400">—</span>
                        )
                      ) : (
                        (u.session as any)?.isSubmitted ? (
                          <CheckCircle className="h-4 w-4 text-green-500" />
                        ) : (
                          <span className="text-xs text-gray-400">—</span>
                        )
                      )}
                    </td>
                    <td className="px-3 py-3" onClick={(e) => e.stopPropagation()}>
                      <div className="flex items-center gap-0.5">
                        {!u.session?.isActive ? (
                          <IconBtn icon={<Play />} title="Activate" color="green" onClick={() => setDialog({ type: "activate", userId: u.userID })} />
                        ) : (
                          <IconBtn icon={<Square />} title="Deactivate" color="red" onClick={() => doDeactivate(u.userID)} />
                        )}
                        {u.session?.isActive && <IconBtn icon={<Clock />} title="Extend" color="amber" onClick={() => setDialog({ type: "extend", userId: u.userID })} />}
                        {!isMcq && u.session?.databaseCreated && <IconBtn icon={<RotateCcw />} title="Reset DB" color="orange" onClick={() => setDialog({ type: "resetDb", userId: u.userID })} />}
                        <IconBtn icon={<Download />} title="Export" color="blue" onClick={async () => { try { await adminService.exportUser(u.userID); } catch (err: any) { toast.error(err.message || "Nothing to export"); } }} />
                        <IconBtn icon={<Trash2 />} title="Delete User" color="red" onClick={() => setDialog({ type: "deleteUser", userId: u.userID })} />
                      </div>
                    </td>
                  </tr>
                );
              })}
              {filteredUsers.length === 0 && (
                <tr><td colSpan={8} className="px-5 py-12 text-center text-gray-400 text-sm">No participants found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </main>

      {/* Detail Panel (slide-in from right) */}
      {selectedUser && (
        <DetailPanel
          user={selectedUser}
          devtoolsCount={devtoolsMap[selectedUser.userID] || 0}
          onClose={() => setSelectedUser(null)}
          onRefresh={loadData}
        />
      )}

      {/* Modals */}
      {showCreateModal && (
        <Modal title="Create Participant" onClose={() => setShowCreateModal(false)}>
          <form onSubmit={handleCreateUser} className="space-y-3">
            <input placeholder="User ID *" value={newUser.UserID} onChange={(e) => setNewUser({ ...newUser, UserID: e.target.value })} required className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <input placeholder="Password *" value={newUser.Password} onChange={(e) => setNewUser({ ...newUser, Password: e.target.value })} required className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <input placeholder="Full Name" value={newUser.FullName} onChange={(e) => setNewUser({ ...newUser, FullName: e.target.value })} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <input placeholder="Email" value={newUser.Email} onChange={(e) => setNewUser({ ...newUser, Email: e.target.value })} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Assessment</label>
              <select value={newUser.AssessmentId} onChange={(e) => setNewUser({ ...newUser, AssessmentId: e.target.value })} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none">
                <option value="">— SQL Hackathon (use DB Engine below) —</option>
                {assessmentList.map((a) => (
                  <option key={a.id} value={a.id}>{a.title} ({a.type} - {a.subType})</option>
                ))}
              </select>
            </div>
            {!newUser.AssessmentId && (
              <div>
                <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Database Engine</label>
                <select value={newUser.DbEnginePreference} onChange={(e) => setNewUser({ ...newUser, DbEnginePreference: e.target.value })} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none">
                  <option value="SqlServer">SQL Server</option>
                  <option value="Oracle">Oracle</option>
                </select>
              </div>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" onClick={() => setShowCreateModal(false)} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Cancel</button>
              <button type="submit" className="px-4 py-2 text-sm font-medium bg-teal-600 text-white rounded-lg hover:bg-teal-700">Create</button>
            </div>
          </form>
        </Modal>
      )}

      {showBulkImport && <BulkUserImport onClose={() => setShowBulkImport(false)} onComplete={() => { setShowBulkImport(false); loadData(); }} />}
      {showHackathonSetup && <HackathonSetupPanel onClose={() => setShowHackathonSetup(false)} />}
      {showAiDetection && <AiDetectionPanel users={users} onClose={() => setShowAiDetection(false)} />}
      {showTabSwitchLogs && <TabSwitchLogsPanel onClose={() => setShowTabSwitchLogs(false)} />}
      {showSecuritySettings && <SecuritySettingsPanel onClose={() => setShowSecuritySettings(false)} />}
      {showScaffoldScripts && <ScaffoldScriptsPanel onClose={() => setShowScaffoldScripts(false)} />}
      {showSchedule && <SchedulePanel onClose={() => setShowSchedule(false)} />}
      {showMcqPanel && <McqAssessmentPanel onClose={() => setShowMcqPanel(false)} />}
      {showAdminUsers && <AdminUsersPanel onClose={() => setShowAdminUsers(false)} />}

      {showServerConfig && (
        <Modal title="Server Configuration" onClose={() => setShowServerConfig(false)}>
          {/* Engine type tabs */}
          <div className="flex gap-1 mb-4 bg-gray-100 dark:bg-gray-800 p-1 rounded-lg">
            <button
              type="button"
              onClick={() => setServerConfig({ ...serverConfig, DbEngineType: "SqlServer", DbPrefix: "Hackathon_" })}
              className={`flex-1 px-3 py-2 text-sm font-medium rounded-md transition-colors ${serverConfig.DbEngineType === "SqlServer" ? "bg-white dark:bg-gray-700 text-teal-700 dark:text-teal-300 shadow-sm" : "text-gray-500 hover:text-gray-700 dark:text-gray-400"}`}
            >
              SQL Server
            </button>
            <button
              type="button"
              onClick={() => setServerConfig({ ...serverConfig, DbEngineType: "Oracle", DbPrefix: "HACK_" })}
              className={`flex-1 px-3 py-2 text-sm font-medium rounded-md transition-colors ${serverConfig.DbEngineType === "Oracle" ? "bg-white dark:bg-gray-700 text-teal-700 dark:text-teal-300 shadow-sm" : "text-gray-500 hover:text-gray-700 dark:text-gray-400"}`}
            >
              Oracle
            </button>
          </div>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
            {serverConfig.DbEngineType === "Oracle"
              ? "Oracle server where participant schemas are created."
              : "SQL Server where participant databases are created."}
          </p>
          <form onSubmit={handleConfigureServer} className="space-y-3">
            <input placeholder={serverConfig.DbEngineType === "Oracle" ? "Host Name / IP" : "Server Name"} value={serverConfig.ServerName} onChange={(e) => setServerConfig({ ...serverConfig, ServerName: e.target.value })} required className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <input placeholder={serverConfig.DbEngineType === "Oracle" ? "Admin User (e.g., SYSTEM)" : "Admin User ID"} value={serverConfig.AdminUserId} onChange={(e) => setServerConfig({ ...serverConfig, AdminUserId: e.target.value })} required className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <input type="password" placeholder="Admin Password" value={serverConfig.AdminPassword} onChange={(e) => setServerConfig({ ...serverConfig, AdminPassword: e.target.value })} required className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            <input placeholder={serverConfig.DbEngineType === "Oracle" ? "Schema Prefix (default: HACK_)" : "DB Prefix (default: Hackathon_)"} value={serverConfig.DbPrefix} onChange={(e) => setServerConfig({ ...serverConfig, DbPrefix: e.target.value })} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
            {serverConfig.DbEngineType === "Oracle" && (
              <>
                <input placeholder="Service Name (e.g., XEPDB1, ORCL)" value={serverConfig.OracleServiceName} onChange={(e) => setServerConfig({ ...serverConfig, OracleServiceName: e.target.value })} required className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
                <input placeholder="Port (default: 1521)" value={serverConfig.Port} onChange={(e) => setServerConfig({ ...serverConfig, Port: e.target.value })} type="number" className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
              </>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" onClick={() => setShowServerConfig(false)} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Cancel</button>
              <button type="submit" className="px-4 py-2 text-sm font-medium bg-teal-600 text-white rounded-lg hover:bg-teal-700">Save</button>
            </div>
          </form>
        </Modal>
      )}

      {/* Dialogs */}
      {dialog.type === "activate" && <InputDialog title="Activate Session" message={`Duration for "${dialog.userId}". Leave empty for unlimited.`} inputLabel="Minutes" inputPlaceholder="e.g., 180" inputType="number" confirmLabel="Activate" allowEmpty onConfirm={(v) => doActivate(dialog.userId, v)} onCancel={() => setDialog({ type: "none" })} />}
      {dialog.type === "activateAll" && <InputDialog title="Activate All" message={`Duration for all ${users.filter(u => !u.session?.isActive).length} inactive users.`} inputLabel="Minutes" inputPlaceholder="e.g., 180" inputType="number" confirmLabel="Next" allowEmpty onConfirm={doActivateAll} onCancel={() => setDialog({ type: "none" })} />}
      {dialog.type === "activateAllConfirm" && <ConfirmDialog title="Confirm" message={`Activate ${users.filter(u => !u.session?.isActive).length} sessions${dialog.duration ? ` (${dialog.duration}m)` : " (unlimited)"}?`} confirmLabel="Activate All" confirmVariant="primary" onConfirm={() => doActivateAllConfirmed(dialog.duration)} onCancel={() => setDialog({ type: "none" })} />}
      {dialog.type === "deactivateAll" && <ConfirmDialog title="Stop All Sessions" message={`Deactivate ${users.filter(u => u.session?.isActive).length} active sessions?`} confirmLabel="Deactivate All" confirmVariant="danger" onConfirm={doDeactivateAll} onCancel={() => setDialog({ type: "none" })} />}
      {dialog.type === "extend" && <InputDialog title="Extend Session" message={`Add time to "${dialog.userId}".`} inputLabel="Additional minutes" inputPlaceholder="e.g., 30" inputType="number" confirmLabel="Extend" allowEmpty={false} onConfirm={(v) => doExtend(dialog.userId, v)} onCancel={() => setDialog({ type: "none" })} />}
      {dialog.type === "resetDb" && <ConfirmDialog title="Reset Database" message={`DROP database for "${dialog.userId}"? This is irreversible.`} confirmLabel="Drop Database" confirmVariant="danger" onConfirm={() => doResetDb(dialog.userId)} onCancel={() => setDialog({ type: "none" })} />}
      {dialog.type === "deleteUser" && <ConfirmDialog title="Delete User Permanently" message={`Permanently delete "${dialog.userId}"? This will DROP their database/schema, delete all their files, execution history, logs, and remove the user. This action cannot be undone.`} confirmLabel="Delete Permanently" confirmVariant="danger" onConfirm={() => doDeleteUser(dialog.userId)} onCancel={() => setDialog({ type: "none" })} />}
    </div>
  );
}

// ─── Detail Panel ─────────────────────────────────────────────────

function DetailPanel({ user, devtoolsCount, onClose, onRefresh }: { user: UserDto; devtoolsCount: number; onClose: () => void; onRefresh: () => void }) {
  const [tab, setTab] = useState<"overview" | "files" | "history" | "security">("overview");
  const [files, setFiles] = useState<any[]>([]);
  const [history, setHistory] = useState<any[]>([]);
  const [historyCount, setHistoryCount] = useState(0);
  const [devtoolsLogs, setDevtoolsLogs] = useState<any[]>([]);
  const [submissionFiles, setSubmissionFiles] = useState<any[]>([]);
  const [newPassword, setNewPassword] = useState("");

  useEffect(() => {
    if (tab === "files") { adminService.getUserFiles(user.userID).then(setFiles).catch(() => setFiles([])); }
    if (tab === "history") { adminService.getUserHistory(user.userID).then((d: any) => { setHistory(d?.items || []); setHistoryCount(d?.totalCount || 0); }).catch(() => {}); }
    if (tab === "security") {
      fetch(`${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/activity/${user.userID}/logs`, { headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}` } })
        .then(r => r.json()).then(d => setDevtoolsLogs((d.data?.logs || []).filter((l: any) => l.eventType.startsWith("devtools_")))).catch(() => {});
      fetch(`${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/admin/submissions/${user.userID}/files`, { headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}` } })
        .then(r => r.json()).then(d => setSubmissionFiles(d.data || [])).catch(() => {});
    }
  }, [tab, user.userID]);

  const handleChangePassword = async () => {
    if (!newPassword || newPassword.length < 6) { toast.error("Min 6 characters"); return; }
    try { await adminService.changeUserPassword(user.userID, newPassword); toast.success("Password changed"); setNewPassword(""); } catch (err: any) { const errors = err.response?.data?.errors; toast.error(errors?.length ? errors.join(" ") : err.response?.data?.message || "Failed"); }
  };

  const handleReleaseSubmission = async () => {
    try {
      await fetch(`${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/admin/submissions/${user.userID}/release`, { method: "POST", headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}`, "Content-Type": "application/json" } });
      toast.success("Submission released");
      onRefresh();
    } catch { toast.error("Failed"); }
  };

  return (
    <div className="w-96 bg-white dark:bg-gray-900 border-l dark:border-gray-800 flex flex-col shrink-0 overflow-hidden">
      {/* Header */}
      <div className="px-5 py-4 border-b dark:border-gray-800 flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2">
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-teal-400 to-teal-600 flex items-center justify-center text-white text-sm font-bold">{user.userID.charAt(0).toUpperCase()}</div>
            <div>
              <p className="text-sm font-semibold text-gray-800 dark:text-gray-100">{user.userID}</p>
              <p className="text-xs text-gray-400">{user.fullName || "No name"}</p>
            </div>
          </div>
        </div>
        <button onClick={onClose} className="p-1 hover:bg-gray-100 dark:hover:bg-gray-800 rounded"><XCircle className="h-4 w-4 text-gray-400" /></button>
      </div>

      {/* Tabs */}
      <div className="flex border-b dark:border-gray-800 px-2">
        {(["overview", "files", "history", "security"] as const).map((t) => (
          <button key={t} onClick={() => setTab(t)} className={`px-3 py-2 text-xs font-medium capitalize border-b-2 transition-colors ${tab === t ? "border-teal-500 text-teal-600" : "border-transparent text-gray-500 hover:text-gray-700"}`}>
            {t}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto">
        {tab === "overview" && (
          <div className="p-5 space-y-4">
            <dl className="grid grid-cols-2 gap-y-3 gap-x-4 text-sm">
              <dt className="text-gray-500">Status</dt>
              <dd><StatusBadge status={user.session?.isExpired ? "expired" : user.session?.isActive ? "active" : "inactive"} /></dd>
              <dt className="text-gray-500">Database</dt>
              <dd className="font-mono text-xs text-teal-600 dark:text-teal-400">{user.session?.databaseName || "—"}</dd>
              <dt className="text-gray-500">Email</dt>
              <dd className="text-gray-700 dark:text-gray-200">{user.email || "—"}</dd>
              <dt className="text-gray-500">Created</dt>
              <dd className="text-gray-700 dark:text-gray-200">{new Date(user.createdDate).toLocaleDateString()}</dd>
              <dt className="text-gray-500">Last Login</dt>
              <dd className="text-gray-700 dark:text-gray-200">{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : "Never"}</dd>
              <dt className="text-gray-500">Login Count</dt>
              <dd className="text-gray-700 dark:text-gray-200">{user.loginCount}</dd>
              <dt className="text-gray-500">DevTools</dt>
              <dd>{devtoolsCount > 0 ? <span className="text-red-600 font-medium">{devtoolsCount} attempts</span> : <span className="text-green-600">Clean</span>}</dd>
              <dt className="text-gray-500">Submitted</dt>
              <dd>{(user.session as any)?.isSubmitted ? <span className="text-green-600 font-medium">Yes</span> : "No"}</dd>
            </dl>

            {(user.session as any)?.isSubmitted && (
              <button onClick={handleReleaseSubmission} className="w-full flex items-center justify-center gap-2 px-3 py-2 text-sm font-medium bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-400 rounded-lg hover:bg-amber-100 dark:hover:bg-amber-900/40 transition-colors">
                <Unlock className="h-4 w-4" /> Release Submission
              </button>
            )}

            <div className="pt-3 border-t dark:border-gray-800">
              <label className="text-xs font-medium text-gray-500 mb-1.5 block">Reset Password</label>
              {/* Hidden fields to prevent browser autofill from targeting the search input */}
              <input type="text" autoComplete="username" style={{ display: "none" }} tabIndex={-1} />
              <div className="flex gap-2">
                <input type="password" autoComplete="new-password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} placeholder="New password" className="flex-1 px-3 py-2 text-sm border dark:border-gray-700 rounded-lg dark:bg-gray-800 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
                <button onClick={handleChangePassword} className="px-3 py-2 text-sm font-medium bg-teal-600 text-white rounded-lg hover:bg-teal-700"><Key className="h-4 w-4" /></button>
              </div>
            </div>
          </div>
        )}

        {tab === "files" && (
          <div className="p-4 space-y-1">
            {files.length === 0 ? <p className="text-center text-gray-400 py-8 text-sm">No files</p> : files.map((f: any) => (
              <div key={f.fileId} className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800">
                <File className="h-4 w-4 text-gray-400 shrink-0" />
                <span className="text-sm text-gray-700 dark:text-gray-200 truncate flex-1">{f.fileName}</span>
                <span className="text-[10px] px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-gray-500">{f.fileType}</span>
              </div>
            ))}
          </div>
        )}

        {tab === "history" && (
          <div className="p-4 space-y-1">
            {history.length === 0 ? <p className="text-center text-gray-400 py-8 text-sm">No history</p> : history.map((h: any) => (
              <div key={h.id} className="px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 border-b dark:border-gray-800 last:border-0">
                <div className="flex items-center gap-2 mb-0.5">
                  {h.status === "Success" ? <CheckCircle className="h-3 w-3 text-green-500" /> : <XCircle className="h-3 w-3 text-red-500" />}
                  <span className="text-[10px] font-medium text-gray-500">{h.queryType}</span>
                  <span className="text-[10px] text-gray-400 ml-auto">{h.durationMs}ms</span>
                </div>
                <code className="text-xs text-gray-600 dark:text-gray-300 font-mono line-clamp-2">{h.queryPreview}</code>
              </div>
            ))}
            {historyCount > 0 && <p className="text-xs text-gray-400 text-center pt-2">{historyCount} total</p>}
          </div>
        )}

        {tab === "security" && (
          <div className="p-4 space-y-5">
            <div>
              <h4 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">DevTools Attempts</h4>
              {devtoolsLogs.length === 0 ? <p className="text-sm text-gray-400">No attempts detected</p> : (
                <div className="space-y-1 max-h-40 overflow-y-auto">
                  {devtoolsLogs.slice(0, 20).map((l: any, i: number) => (
                    <div key={i} className="flex items-center justify-between text-xs px-2 py-1 rounded bg-red-50 dark:bg-red-900/10">
                      <span className="text-red-700 dark:text-red-400 font-mono">{l.eventType}</span>
                      <span className="text-gray-400">{new Date(l.eventTime).toLocaleTimeString()}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div>
              <h4 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">Submission Files</h4>
              {submissionFiles.length === 0 ? <p className="text-sm text-gray-400">No files uploaded</p> : (
                <div className="space-y-1">
                  {submissionFiles.map((f: any) => (
                    <div key={f.id} className="flex items-center gap-2 px-2 py-1.5 rounded bg-gray-50 dark:bg-gray-800">
                      <FileSpreadsheet className="h-4 w-4 text-green-600 shrink-0" />
                      <span className="text-xs text-gray-700 dark:text-gray-200 truncate flex-1">{f.fileName}</span>
                      <span className="text-[10px] text-gray-400">{(f.fileSizeBytes / 1024).toFixed(0)}KB</span>
                      <a href={`${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/admin/submissions/files/${f.id}/download`} className="text-teal-600 hover:text-teal-700"><Download className="h-3.5 w-3.5" /></a>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Sub-components ───────────────────────────────────────────────

function SidebarItem({ icon, label, active, onClick }: { icon: React.ReactNode; label: string; active: boolean; onClick: () => void }) {
  return (
    <button onClick={onClick} className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${active ? "bg-teal-50 dark:bg-teal-900/20 text-teal-700 dark:text-teal-300" : "text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200"}`}>
      {icon}<span>{label}</span>
    </button>
  );
}

function MiniStat({ label, value, color }: { label: string; value: number; color?: string }) {
  const c = color === "green" ? "text-green-600" : color === "blue" ? "text-blue-600" : color === "orange" ? "text-orange-600" : "text-gray-800 dark:text-gray-100";
  return (
    <div className="flex items-center justify-between">
      <span className="text-xs text-gray-500">{label}</span>
      <span className={`text-sm font-semibold ${c}`}>{value}</span>
    </div>
  );
}

function StatusBadge({ status }: { status: "active" | "inactive" | "expired" }) {
  const styles = { active: "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400", inactive: "bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400", expired: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" };
  return <span className={`inline-flex px-2 py-0.5 text-[10px] font-semibold rounded-full capitalize ${styles[status]}`}>{status}</span>;
}

function ActionBtn({ icon, label, color, onClick }: { icon: React.ReactElement; label: string; color: string; onClick: () => void }) {
  const colors: Record<string, string> = { green: "bg-green-600 hover:bg-green-700", red: "bg-red-600 hover:bg-red-700", teal: "bg-teal-600 hover:bg-teal-700", blue: "bg-blue-600 hover:bg-blue-700", orange: "bg-orange-500 hover:bg-orange-600" };
  return (
    <button onClick={onClick} className={`flex items-center gap-1.5 px-2.5 py-1.5 text-white rounded-md text-xs font-medium transition-colors ${colors[color]}`}>
      <span className="h-3.5 w-3.5 [&>svg]:h-3.5 [&>svg]:w-3.5">{icon}</span>{label}
    </button>
  );
}

function IconBtn({ icon, title, color, onClick }: { icon: React.ReactElement; title: string; color: string; onClick: () => void }) {
  const colors: Record<string, string> = { green: "text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20", red: "text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20", amber: "text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20", orange: "text-orange-600 hover:bg-orange-50 dark:hover:bg-orange-900/20", blue: "text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20" };
  return (
    <button onClick={onClick} title={title} className={`p-1.5 rounded transition-colors ${colors[color]}`}>
      <span className="h-3.5 w-3.5 [&>svg]:h-3.5 [&>svg]:w-3.5">{icon}</span>
    </button>
  );
}

function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) {
  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={onClose}>
      <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-md shadow-2xl" onClick={(e) => e.stopPropagation()}>
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">{title}</h3>
        {children}
      </div>
    </div>
  );
}

function formatTimeRemaining(expiresAt: string): string {
  const diff = new Date(expiresAt).getTime() - Date.now();
  if (diff <= 0) return "Expired";
  const totalSec = Math.floor(diff / 1000);
  const h = Math.floor(totalSec / 3600);
  const m = Math.floor((totalSec % 3600) / 60);
  return `${h}h ${m}m`;
}
