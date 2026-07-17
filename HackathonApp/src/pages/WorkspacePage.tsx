import { useState, useCallback, useEffect, useRef } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import { LogOut, Timer, Database, FolderTree, History, Play, Save, FilePlus, X, FileText, CheckCircle, BookOpen, Sparkles, Shield } from "lucide-react";
import { toast } from "sonner";
import Editor, { useMonaco } from "@monaco-editor/react";
import { logout } from "@/redux/slices/authSlice";
import { hackathonService } from "@/services/hackathonService";
import { fileService, isAiBlocked, getAiBlockMessage } from "@/services/fileService";
import SavingOverlay from "@/components/common/SavingOverlay";
import { startTabSwitchTracking, stopTabSwitchTracking } from "@/services/activityService";
import { startDevToolsProtection, stopDevToolsProtection, setOnDevToolsBlocked } from "@/services/devtoolsDetection";
import { registerInternalCopy, registerFileContent, validatePaste, setOnPasteBlocked } from "@/services/clipboardGuard";
import { useTheme } from "@/contexts/ThemeContext";
import ThemeToggle from "@/components/common/ThemeToggle";
import SchemaExplorer from "@/components/schema/SchemaExplorer";
import FileManager from "@/components/files/FileManager";
import ExecutionHistory from "@/components/history/ExecutionHistory";
import QuestionPanel from "@/components/common/QuestionPanel";
import HackathonGuidelines from "@/components/common/HackathonGuidelines";
import GuidedTour from "@/components/common/GuidedTour";
import type { TourStep } from "@/components/common/GuidedTour";
import SubmitPanel from "@/components/common/SubmitPanel";
import type { RootState, AppDispatch } from "@/redux/store";
import type { ExecuteResult, SessionInfo, FolderDto } from "@/types";

// ─── Editor Tab Model ─────────────────────────────────────────────

interface EditorTab {
  id: string;
  title: string;         // "Unsaved" or the file name
  content: string;
  savedContent: string;  // Content at last save (to detect changes)
  fileId: number | null;
  fileName: string | null;
  results: ExecuteResult | null;
}

let tabCounter = 1;

function createNewTab(): EditorTab {
  const id = `tab-${Date.now()}-${tabCounter}`;
  const title = `SQLQuery${tabCounter}`;
  tabCounter++;
  const content = "-- Write your SQL here\n";
  return {
    id,
    title,
    content,
    savedContent: content,
    fileId: null,
    fileName: null,
    results: null,
  };
}

// ─── Main Component ───────────────────────────────────────────────

export default function WorkspacePage() {
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const { user } = useSelector((s: RootState) => s.auth);
  const { isDark } = useTheme();
  const monaco = useMonaco();

  // Multi-tab state
  const [tabs, setTabs] = useState<EditorTab[]>([createNewTab()]);
  const [activeEditorTabId, setActiveEditorTabId] = useState(tabs[0].id);

  const [isExecuting, setIsExecuting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [sessionInfo, setSessionInfo] = useState<SessionInfo | null>(null);
  const [activeSection, setActiveSection] = useState<"editor" | "schema" | "files" | "history">("editor");
  const [showQuestionPanel, setShowQuestionPanel] = useState(true);
  const [showSaveModal, setShowSaveModal] = useState(false);
  const [showSubmitPanel, setShowSubmitPanel] = useState(false);
  const [pendingCloseTabId, setPendingCloseTabId] = useState<string | null>(null);

  const [showReactivatedModal, setShowReactivatedModal] = useState(false);
  const [reactivatedInfo, setReactivatedInfo] = useState<{ minutes?: number; expiresAt?: string } | null>(null);
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false);
  const [showSaveReminder, setShowSaveReminder] = useState(false);
  const [showGuidelines, setShowGuidelines] = useState(false);
  const [showResults, setShowResults] = useState(true);
  const [showTour, setShowTour] = useState(() => {
    // Show tour on first login (check sessionStorage)
    const tourSeen = sessionStorage.getItem("tour_completed");
    return !tourSeen;
  });
  const [devToolsBlocked, setDevToolsBlocked] = useState(false);
  const saveReminderRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const editorRef = useRef<any>(null);

  // Derived: current active tab
  const activeTabData = tabs.find((t) => t.id === activeEditorTabId) || tabs[0];

  // ─── Tab Helpers ────────────────────────────────────────────────

  const updateTab = useCallback((tabId: string, updates: Partial<EditorTab>) => {
    setTabs((prev) => prev.map((t) => (t.id === tabId ? { ...t, ...updates } : t)));
  }, []);

  const tabHasUnsavedChanges = (tab: EditorTab) => tab.content !== tab.savedContent;

  const switchToTab = useCallback((tabId: string) => {
    setActiveEditorTabId(tabId);
  }, []);

  const handleNewTab = useCallback(() => {
    const newTab = createNewTab();
    setTabs((prev) => [...prev, newTab]);
    setActiveEditorTabId(newTab.id);
    setActiveSection("editor");
  }, []);

  const handleCloseTab = useCallback((tabId: string, e?: React.MouseEvent) => {
    e?.stopPropagation();
    const tab = tabs.find((t) => t.id === tabId);
    if (!tab) return;

    if (tabHasUnsavedChanges(tab)) {
      setPendingCloseTabId(tabId);
      return;
    }

    doCloseTab(tabId);
  }, [tabs]);

  const doCloseTab = useCallback((tabId: string) => {
    setPendingCloseTabId(null);
    const remaining = tabs.filter((t) => t.id !== tabId);
    if (remaining.length === 0) {
      const newTab = createNewTab();
      setTabs([newTab]);
      setActiveEditorTabId(newTab.id);
    } else {
      setTabs(remaining);
      if (activeEditorTabId === tabId) {
        const closedIndex = tabs.findIndex((t) => t.id === tabId);
        const nextTab = remaining[Math.min(closedIndex, remaining.length - 1)];
        setActiveEditorTabId(nextTab.id);
      }
    }
  }, [tabs, activeEditorTabId]);

  // ─── Session ────────────────────────────────────────────────────

  useEffect(() => {
    hackathonService.getStatus().then(setSessionInfo).catch(() => {});
    startTabSwitchTracking();
    startDevToolsProtection();

    // Clipboard guard: show toast when external paste is blocked
    setOnPasteBlocked(() => {
      toast.error("External paste is not allowed. You can only paste content copied within the editor.", { duration: 5000 });
    });

    // DevTools detection: show blocking overlay instead of logout
    setOnDevToolsBlocked((blocked) => {
      setDevToolsBlocked(blocked);
    });

    return () => {
      stopTabSwitchTracking();
      stopDevToolsProtection();
    };
  }, []);

  useEffect(() => {
    if (sessionInfo && !sessionInfo.databaseCreated && !sessionInfo.isExpired && sessionInfo.isActive) {
      navigate("/create-database", { replace: true });
    }
  }, [sessionInfo, navigate]);

  useEffect(() => {
    const interval = setInterval(async () => {
      try {
        const status = await hackathonService.getStatus();
        setSessionInfo((prev) => {
          const wasDown = prev && (prev.isExpired || !prev.isActive);
          const isNowActive = status.isActive && !status.isExpired;
          if (wasDown && isNowActive) {
            setReactivatedInfo({ minutes: status.remainingMinutes ?? undefined, expiresAt: status.expiresAt ?? undefined });
            setShowReactivatedModal(true);
          }
          return status;
        });
      } catch (err: any) {
        if (err.response?.status === 403) {
          setSessionInfo((prev) => prev ? { ...prev, isActive: false, isExpired: false } : null);
        }
      }
    }, 10000);
    return () => clearInterval(interval);
  }, []);

  const isSessionExpired = sessionInfo?.isExpired ?? false;
  const isSessionDeactivated = sessionInfo !== null && !sessionInfo.isActive && !sessionInfo.isExpired;
  const isSubmitted = sessionInfo?.isSubmitted ?? false;

  // ─── Editor Change Handler ──────────────────────────────────────

  const activeTabIdRef = useRef(activeEditorTabId);
  activeTabIdRef.current = activeEditorTabId;

  const handleEditorChange = useCallback((value: string | undefined) => {
    const v = value || "";
    updateTab(activeTabIdRef.current, { content: v });
  }, [updateTab]);

  // ─── Execute ────────────────────────────────────────────────────

  const handleExecute = useCallback(async () => {
    // If user has selected text, execute only the selection (like SSMS)
    let content = "";
    if (editorRef.current) {
      const selection = editorRef.current.getSelection();
      const model = editorRef.current.getModel();
      if (selection && model && !selection.isEmpty()) {
        content = model.getValueInRange(selection);
      } else {
        content = editorRef.current.getValue();
      }
    } else {
      content = activeTabData.content;
    }

    if (!content.trim()) {
      toast.error("Nothing to execute");
      return;
    }
    setIsExecuting(true);
    try {
      const result = await hackathonService.execute({ sql: content, page: 1, pageSize: 25 });
      updateTab(activeEditorTabId, { results: result, content: editorRef.current?.getValue() || activeTabData.content });
      setShowResults(true); // Show results after execution
      const hasError = result.results.some((r) => r.type === "ERROR");
      if (hasError) toast.error("Execution completed with errors");
      else toast.success(`${result.executedBatches} batch(es) executed successfully`);
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Execution failed");
    } finally {
      setIsExecuting(false);
    }
  }, [activeEditorTabId, activeTabData.content, updateTab]);

  // ─── Save ───────────────────────────────────────────────────────

  const handleSave = useCallback(async () => {
    const content = editorRef.current?.getValue() || activeTabData.content;
    updateTab(activeEditorTabId, { content });

    if (activeTabData.fileId) {
      setIsSaving(true);
      try {
        await fileService.updateFile(activeTabData.fileId, { content });
        updateTab(activeEditorTabId, { savedContent: content });
        toast.success(`Saved: ${activeTabData.fileName}`);
      } catch (err: any) {
        if (isAiBlocked(err)) {
          toast.error(getAiBlockMessage(err), { duration: 8000 });
        } else {
          const msg = err.response?.data?.data?.message 
            || err.response?.data?.message 
            || err.response?.data?.errors?.[0]
            || err.message 
            || "Save failed";
          toast.error(msg);
        }
      } finally {
        setIsSaving(false);
      }
    } else {
      setShowSaveModal(true);
    }
  }, [activeEditorTabId, activeTabData, updateTab]);

  // ─── Keyboard Shortcuts ─────────────────────────────────────────

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        handleSave();
      }
      if (e.key === "F5") {
        e.preventDefault();
        handleExecute();
      }
      if ((e.ctrlKey || e.metaKey) && e.key === "e") {
        e.preventDefault();
        handleExecute();
      }
      if ((e.ctrlKey || e.metaKey) && e.key === "r") {
        e.preventDefault();
        setShowResults((prev) => !prev);
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [handleSave, handleExecute]);

  // ─── Warn on tab close / refresh if unsaved changes ─────────────

  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      const hasUnsaved = tabs.some((t) => t.content !== t.savedContent);
      if (hasUnsaved) {
        e.preventDefault();
        e.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [tabs]);

  // ─── Save Reminder (5 min of unsaved changes) ──────────────────

  useEffect(() => {
    const hasUnsaved = tabs.some((t) => t.content !== t.savedContent);

    if (hasUnsaved) {
      // Start a 5-minute timer if not already running
      if (!saveReminderRef.current) {
        saveReminderRef.current = setTimeout(() => {
          // Check again at trigger time — maybe user saved in the meantime
          setShowSaveReminder(true);
          saveReminderRef.current = null;
        }, 5 * 60 * 1000); // 5 minutes
      }
    } else {
      // All saved — clear the timer
      if (saveReminderRef.current) {
        clearTimeout(saveReminderRef.current);
        saveReminderRef.current = null;
      }
      setShowSaveReminder(false);
    }
  }, [tabs]);

  // ─── Open File (from FileManager) ──────────────────────────────

  const handleOpenFile = useCallback((content: string, fileName: string, fileId?: number) => {
    // Register file content so paste from this file is always allowed
    registerFileContent(content);

    // Check if file is already open in a tab
    const existingTab = tabs.find((t) => t.fileId === fileId && fileId != null);
    if (existingTab) {
      switchToTab(existingTab.id);
      setActiveSection("editor");
      toast.success(`Switched to: ${fileName}`);
      return;
    }

    // If current tab is empty/unsaved and untouched, reuse it
    if (!activeTabData.fileId && activeTabData.content === activeTabData.savedContent && activeTabData.content === "-- Write your SQL here\n") {
      updateTab(activeEditorTabId, {
        content,
        savedContent: content,
        fileId: fileId ?? null,
        fileName,
        title: fileName,
      });
      setActiveSection("editor");
      toast.success(`Opened: ${fileName}`);
      return;
    }

    // Open in new tab
    const newTab: EditorTab = {
      id: `tab-${Date.now()}-${tabCounter++}`,
      title: fileName,
      content,
      savedContent: content,
      fileId: fileId ?? null,
      fileName,
      results: null,
    };
    setTabs((prev) => [...prev, newTab]);
    setActiveEditorTabId(newTab.id);
    setActiveSection("editor");
    toast.success(`Opened: ${fileName}`);
  }, [tabs, activeTabData, switchToTab, updateTab]);

  // ─── Load Definition (from Schema Explorer) ────────────────────

  const handleLoadDefinition = useCallback((name: string, definition: string) => {
    const content = `-- ${name}\n${definition}`;
    const newTab: EditorTab = {
      id: `tab-${Date.now()}-${tabCounter++}`,
      title: name,
      content,
      savedContent: content,
      fileId: null,
      fileName: null,
      results: null,
    };
    setTabs((prev) => [...prev, newTab]);
    setActiveEditorTabId(newTab.id);
    setActiveSection("editor");
    toast.success(`Loaded definition: ${name}`);
  }, [activeEditorTabId, updateTab]);

  const handleLogout = () => {
    const hasUnsaved = tabs.some((t) => t.content !== t.savedContent);
    if (hasUnsaved) {
      setShowLogoutConfirm(true);
      return;
    }
    dispatch(logout());
    navigate("/login", { replace: true });
  };

  const confirmLogout = () => {
    setShowLogoutConfirm(false);
    dispatch(logout());
    navigate("/login", { replace: true });
  };

  // ─── Guided Tour Steps ──────────────────────────────────────────

  const tourSteps: TourStep[] = [
    {
      target: "[data-tour='header']",
      title: "Welcome!",
      content: "This is your workspace header. It shows your database name, session timer, and quick actions.",
      position: "bottom",
    },
    {
      target: "[data-tour='section-tabs']",
      title: "Navigation Tabs",
      content: "Switch between SQL Editor, Schema Explorer, File Manager, and Execution History from here.",
      position: "bottom",
    },
    {
      target: "[data-tour='question-btn']",
      title: "Question Paper",
      content: "Click here to toggle the question paper panel. It shows the hackathon tasks you need to complete.",
      position: "bottom",
    },
    {
      target: "[data-tour='submit-btn']",
      title: "Submit Your Work",
      content: "When you're done, click Submit to upload your final work. This is irreversible — make sure everything is saved!",
      position: "bottom",
    },
    {
      target: "[data-tour='guidelines-btn']",
      title: "DOs & DON'Ts",
      content: "Need a refresher on the rules? Click this icon anytime to review the hackathon guidelines.",
      position: "bottom",
    },
  ];

  const handleTourComplete = () => {
    setShowTour(false);
    sessionStorage.setItem("tour_completed", "true");
  };

  return (
    <div className="h-screen flex flex-col bg-background">
      <SavingOverlay visible={isSaving} />

      {/* DevTools Blocked Overlay */}
      {devToolsBlocked && (
        <div className="fixed inset-0 bg-red-950/95 flex items-center justify-center z-[200]" style={{ userSelect: "none" }}>
          <div className="text-center max-w-md px-6">
            <div className="bg-red-500/20 rounded-full p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center">
              <Shield className="h-12 w-12 text-red-400" />
            </div>
            <h2 className="text-2xl font-bold text-white mb-3">Developer Tools Detected</h2>
            <p className="text-red-200 mb-4">
              Developer Tools must be closed to continue using the application. 
              This activity has been logged.
            </p>
            <p className="text-red-300/70 text-sm">
              Close Developer Tools (F12) to resume your session.
            </p>
            <div className="mt-6 flex items-center justify-center gap-2">
              <span className="w-2 h-2 bg-red-400 rounded-full animate-pulse" />
              <span className="text-xs text-red-300">Waiting for DevTools to be closed...</span>
            </div>
          </div>
        </div>
      )}

      {/* Guided Tour */}
      {showTour && !isSessionDeactivated && !isSubmitted && (
        <GuidedTour
          steps={tourSteps}
          onComplete={handleTourComplete}
          onSkip={handleTourComplete}
        />
      )}

      {/* Session Deactivated Overlay */}
      {isSessionDeactivated && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-2xl p-8 max-w-md text-center shadow-2xl">
            <div className="bg-red-100 dark:bg-red-900/30 rounded-full p-4 w-16 h-16 mx-auto mb-4 flex items-center justify-center">
              <LogOut className="h-8 w-8 text-red-500" />
            </div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100 mb-2">Session Deactivated</h2>
            <p className="text-gray-600 dark:text-gray-400 mb-6">
              Your session has been deactivated by the administrator. Please contact them if you need access again.
            </p>
            <div className="flex gap-3 justify-center">
              <button onClick={() => { hackathonService.getStatus().then(setSessionInfo).catch(() => {}); }} className="px-4 py-2 text-sm font-medium text-teal-600 hover:bg-teal-50 dark:hover:bg-teal-900/20 rounded-lg transition-colors">
                Check Again
              </button>
              <button onClick={handleLogout} className="px-4 py-2 text-sm font-medium bg-gray-800 dark:bg-gray-600 text-white rounded-lg hover:bg-gray-900 transition-colors">
                Logout
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Submitted Overlay */}
      {isSubmitted && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-2xl p-8 max-w-md text-center shadow-2xl">
            <div className="bg-green-100 dark:bg-green-900/30 rounded-full p-4 w-16 h-16 mx-auto mb-4 flex items-center justify-center">
              <CheckCircle className="h-8 w-8 text-green-500" />
            </div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100 mb-2">Work Submitted</h2>
            <p className="text-gray-600 dark:text-gray-400 mb-6">
              You have successfully submitted your hackathon work. No further edits are allowed.
              {sessionInfo?.submittedAt && (
                <span className="block text-xs text-gray-400 mt-2">
                  Submitted at: {new Date(sessionInfo.submittedAt).toLocaleString()}
                </span>
              )}
            </p>
            <button
              onClick={handleLogout}
              className="px-4 py-2 text-sm font-medium bg-gray-800 dark:bg-gray-600 text-white rounded-lg hover:bg-gray-900 transition-colors"
            >
              Logout
            </button>
          </div>
        </div>
      )}

      {/* Header */}
      <header className="h-12 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center justify-between px-4 shrink-0" data-tour="header">
        <div className="flex items-center gap-3">
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5">
            <Database className="h-4 w-4 text-white" />
          </div>
          <span className="font-semibold text-teal-800 dark:text-teal-300">NovacCodeLab</span>
        </div>
        <div className="flex items-center gap-4">
          {isSessionExpired ? (
            <div className="flex items-center gap-1 text-sm text-red-600 font-medium">
              <Timer className="h-4 w-4" /><span>Expired</span>
            </div>
          ) : sessionInfo?.remainingMinutes != null ? (
            <LiveTimer expiresAt={sessionInfo.expiresAt} />
          ) : null}
          <span className="text-sm text-gray-600 dark:text-gray-300">{user?.fullName || user?.userID}</span>
          {!isSubmitted && !isSessionExpired && !isSessionDeactivated && (
            <button
              onClick={() => setShowSubmitPanel(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white rounded-md text-xs font-medium transition-colors"
              data-tour="submit-btn"
            >
              Submit
            </button>
          )}
          <button onClick={() => setShowGuidelines(true)} className="text-gray-400 hover:text-teal-500 transition-colors" title="Hackathon Guidelines" data-tour="guidelines-btn">
            <BookOpen className="h-4 w-4" />
          </button>
          <button onClick={() => setShowTour(true)} className="text-gray-400 hover:text-purple-500 transition-colors" title="Take a Tour">
            <Sparkles className="h-4 w-4" />
          </button>
          <ThemeToggle />
          <button onClick={handleLogout} className="text-gray-400 hover:text-red-500 transition-colors" title="Logout">
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </header>

      {/* Section Tabs */}
      <div className="h-10 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center px-4 gap-1 shrink-0" data-tour="section-tabs">
        {[
          { id: "editor" as const, label: "SQL Editor", icon: <Play className="h-3.5 w-3.5" /> },
          { id: "schema" as const, label: "Schema", icon: <FolderTree className="h-3.5 w-3.5" /> },
          { id: "files" as const, label: "Files", icon: <Save className="h-3.5 w-3.5" /> },
          { id: "history" as const, label: "History", icon: <History className="h-3.5 w-3.5" /> },
        ].map((tab) => (
          <button
            key={tab.id}
            onClick={() => {
              setActiveSection(tab.id);
            }}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
              activeSection === tab.id
                ? "bg-teal-100 text-teal-700 dark:bg-teal-900/40 dark:text-teal-300"
                : "text-gray-500 hover:text-gray-700 hover:bg-gray-100 dark:text-gray-400 dark:hover:text-gray-200 dark:hover:bg-gray-800"
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
        <span className="ml-auto" />
        <button
          onClick={() => setShowQuestionPanel(!showQuestionPanel)}
          data-tour="question-btn"
          className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
            showQuestionPanel
              ? "bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300"
              : "text-gray-500 hover:text-gray-700 hover:bg-gray-100 dark:text-gray-400 dark:hover:text-gray-200 dark:hover:bg-gray-800"
          }`}
          title="Toggle Question Paper"
        >
          <FileText className="h-3.5 w-3.5" />
          Question
        </button>
      </div>

      {/* Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Main content area */}
        <div className={`flex-1 flex flex-col overflow-hidden ${showQuestionPanel ? "border-r dark:border-gray-700" : ""}`}>
          {/* Session Expired Banner */}
          {isSessionExpired && !isSessionDeactivated && (
            <div className="px-4 py-2 bg-red-50 dark:bg-red-900/20 border-b border-red-200 dark:border-red-800 flex items-center gap-2 shrink-0">
              <Timer className="h-4 w-4 text-red-500" />
              <span className="text-sm font-medium text-red-700 dark:text-red-400">Session expired.</span>
              <span className="text-sm text-red-600 dark:text-red-400">You can still save your scripts, but query execution is disabled.</span>
            </div>
          )}

          {activeSection === "editor" && (
            <>
              {/* Editor Tabs Bar */}
              <div className="h-9 border-b bg-gray-100 dark:bg-gray-800 dark:border-gray-700 flex items-end px-1 gap-0.5 shrink-0 overflow-x-auto">
                {tabs.map((tab) => {
                  const isActive = tab.id === activeEditorTabId;
                  const hasChanges = tabHasUnsavedChanges(tab);
                  return (
                    <button
                      key={tab.id}
                      onClick={() => switchToTab(tab.id)}
                      className={`group flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-t-md border border-b-0 transition-colors max-w-[180px] ${
                        isActive
                          ? "bg-white dark:bg-gray-900 border-gray-300 dark:border-gray-600 text-gray-800 dark:text-gray-100"
                          : "bg-gray-200 dark:bg-gray-700 border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-600"
                      }`}
                    >
                      <span className="truncate">
                        {tab.title}
                      </span>
                      {hasChanges && (
                        <span className="w-2 h-2 rounded-full bg-orange-400 shrink-0" title="Unsaved changes" />
                      )}
                      {tabs.length > 1 && (
                        <span
                          onClick={(e) => handleCloseTab(tab.id, e)}
                          className="ml-1 p-0.5 rounded hover:bg-gray-300 dark:hover:bg-gray-500 opacity-0 group-hover:opacity-100 transition-opacity shrink-0"
                        >
                          <X className="h-3 w-3" />
                        </span>
                      )}
                    </button>
                  );
                })}
                {/* New Tab Button */}
                <button
                  onClick={handleNewTab}
                  className="flex items-center justify-center w-7 h-7 mb-0.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded transition-colors"
                  title="New Tab"
                >
                  <FilePlus className="h-3.5 w-3.5" />
                </button>
              </div>

              {/* Toolbar */}
              <div className="h-10 border-b bg-gray-50 dark:bg-gray-800 dark:border-gray-700 flex items-center px-4 gap-2 shrink-0">
                <button
                  onClick={handleExecute}
                  disabled={isExecuting || isSessionExpired}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-teal-600 hover:bg-teal-700 text-white rounded-md text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  title={isSessionExpired ? "Session expired — execution disabled" : ""}
                >
                  <Play className="h-3.5 w-3.5" />
                  {isExecuting ? "Executing..." : "Execute"}
                </button>
                <button
                  onClick={handleNewTab}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-200 rounded-md text-sm font-medium transition-colors"
                >
                  <FilePlus className="h-3.5 w-3.5" />
                  New
                </button>
                <button
                  onClick={handleSave}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-200 rounded-md text-sm font-medium transition-colors"
                >
                  <Save className="h-3.5 w-3.5" />
                  {activeTabData.fileId ? "Save" : "Save As"}
                </button>
              </div>

              {/* Quick Tips Bar */}
              <div className="px-4 py-1 bg-blue-50 dark:bg-blue-900/10 border-b dark:border-gray-700 flex items-center gap-4 shrink-0 overflow-x-auto">
                <span className="text-[10px] text-blue-600 dark:text-blue-400 font-medium shrink-0">Tips:</span>
                <span className="text-[10px] text-gray-500 dark:text-gray-400 shrink-0">F5 / Ctrl+E = Execute</span>
                <span className="text-[10px] text-gray-400 dark:text-gray-600">•</span>
                <span className="text-[10px] text-gray-500 dark:text-gray-400 shrink-0">Ctrl+S = Save</span>
                <span className="text-[10px] text-gray-400 dark:text-gray-600">•</span>
                <span className="text-[10px] text-gray-500 dark:text-gray-400 shrink-0">Ctrl+R = Toggle Results</span>
                <span className="text-[10px] text-gray-400 dark:text-gray-600">•</span>
                <span className="text-[10px] text-gray-500 dark:text-gray-400 shrink-0">Select text → Execute = runs only selection</span>
                <span className="text-[10px] text-gray-400 dark:text-gray-600">•</span>
                <span className="text-[10px] text-gray-500 dark:text-gray-400 shrink-0">Use <strong className="text-blue-600 dark:text-blue-400">GO</strong> to separate multiple batches</span>
              </div>

              {/* Editor + Results */}
              <div className="flex-1 flex flex-col overflow-hidden">
                <div className="flex-1 min-h-0">
                  <Editor
                    height="100%"
                    language="sql"
                    theme={isDark ? "vs-dark" : "vs-light"}
                    value={activeTabData.content}
                    onMount={(editor, monacoInstance) => {
                      editorRef.current = editor;
                      document.fonts.ready.then(() => { monacoInstance.editor.remeasureFonts(); });

                      // ─── Clipboard Guard ─────────────────────────────────

                      // Track internal copies via DOM events (these always fire)
                      const domNode = editor.getDomNode();
                      if (domNode) {
                        domNode.addEventListener("copy", () => {
                          const sel = editor.getSelection();
                          if (sel) {
                            const text = editor.getModel()?.getValueInRange(sel) || "";
                            if (text) registerInternalCopy(text);
                          }
                        }, true);

                        domNode.addEventListener("cut", () => {
                          const sel = editor.getSelection();
                          if (sel) {
                            const text = editor.getModel()?.getValueInRange(sel) || "";
                            if (text) registerInternalCopy(text);
                          }
                        }, true);

                        // Block drag-and-drop from external sources
                        domNode.addEventListener("drop", (e: Event) => {
                          e.preventDefault();
                          e.stopImmediatePropagation();
                          toast.error("Drag and drop is not allowed.", { duration: 3000 });
                        }, true);

                        domNode.addEventListener("dragover", (e: Event) => {
                          e.preventDefault();
                        }, true);
                      }

                      // Override all paste shortcuts — validate before allowing
                      const pasteHandler = () => {
                        navigator.clipboard.readText().then((text) => {
                          if (validatePaste(text)) {
                            const selection = editor.getSelection();
                            if (selection) {
                              editor.executeEdits("paste", [{
                                range: selection,
                                text: text,
                                forceMoveMarkers: true,
                              }]);
                            }
                          }
                        }).catch(() => {});
                      };

                      // Ctrl+V
                      editor.addCommand(monacoInstance.KeyMod.CtrlCmd | monacoInstance.KeyCode.KeyV, pasteHandler);
                      // Ctrl+Shift+V (paste without formatting)
                      editor.addCommand(monacoInstance.KeyMod.CtrlCmd | monacoInstance.KeyMod.Shift | monacoInstance.KeyCode.KeyV, pasteHandler);
                      // Shift+Insert (legacy paste)
                      editor.addCommand(monacoInstance.KeyMod.Shift | monacoInstance.KeyCode.Insert, pasteHandler);
                    }}
                    onChange={handleEditorChange}
                    options={{
                      minimap: { enabled: false },
                      fontSize: 14,
                      fontFamily: "'JetBrains Mono', Consolas, 'Courier New', monospace",
                      fontLigatures: false,
                      lineNumbers: "on",
                      wordWrap: "on",
                      scrollBeyondLastLine: false,
                      automaticLayout: true,
                      fixedOverflowWidgets: true,
                    }}
                  />
                </div>

                {/* Results Panel — SSMS style with Results/Messages tabs */}
                {activeTabData.results && showResults && (
                  <ResultsPanel results={activeTabData.results} />
                )}
              </div>
            </>
          )}

          {activeSection === "schema" && (
            <div className="flex-1 overflow-hidden">
              <SchemaExplorer onLoadDefinition={handleLoadDefinition} />
            </div>
          )}

          {activeSection === "files" && (
            <div className="flex-1 overflow-hidden relative">
              <FileManager
                currentEditorContent={activeTabData.content}
                onOpenFile={handleOpenFile}
              />
            </div>
          )}

          {activeSection === "history" && (
            <div className="flex-1 overflow-hidden">
              <ExecutionHistory onOpenInEditor={(content) => {
                const newTab: EditorTab = {
                  id: `tab-${Date.now()}-${tabCounter++}`,
                  title: "From History",
                  content,
                  savedContent: "",
                  fileId: null,
                  fileName: null,
                  results: null,
                };
                if (editorRef.current) {
                  updateTab(activeEditorTabId, { content: editorRef.current.getValue() });
                }
                setTabs((prev) => [...prev, newTab]);
                setActiveEditorTabId(newTab.id);
                setActiveSection("editor");
                toast.success("Query loaded into editor");
              }} />
            </div>
          )}
        </div>

        {/* Question Paper Split Panel */}
        {showQuestionPanel && (
          <div className="w-[45%] min-w-[300px] max-w-[600px] overflow-hidden">
            <QuestionPanel onClose={() => setShowQuestionPanel(false)} />
          </div>
        )}
      </div>

      {/* Save As Modal */}
      {showSaveModal && (
        <SaveAsModal
          onSave={async (fileName, fileType, folderId) => {
            const content = editorRef.current?.getValue() || activeTabData.content;
            try {
              const file = await fileService.createFile({ fileName, fileType, content, folderId });
              updateTab(activeEditorTabId, {
                fileId: file.fileId,
                fileName,
                title: fileName,
                savedContent: content,
                content,
              });
              setShowSaveModal(false);
              toast.success(`Saved: ${fileName}`);
            } catch (err: any) {
              const data = err.response?.data;
              const msg = data?.errors?.[0] || data?.Errors?.[0] || data?.message || "Save failed";
              toast.error(msg === "Bad Request" ? (data?.errors?.[0] || data?.Errors?.[0] || "Save failed") : msg);
            }
          }}
          onCancel={() => setShowSaveModal(false)}
        />
      )}

      {/* Logout Confirmation */}
      {showLogoutConfirm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[70]">
          <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-sm shadow-2xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="bg-amber-100 dark:bg-amber-900/30 rounded-full p-2">
                <Save className="h-5 w-5 text-amber-600" />
              </div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Unsaved Changes</h3>
            </div>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
              You have unsaved changes. Are you sure you want to logout? Your unsaved work will be lost.
            </p>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setShowLogoutConfirm(false)}
                className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={confirmLogout}
                className="px-4 py-2 text-sm font-medium bg-red-600 hover:bg-red-700 text-white rounded-lg transition-colors"
              >
                Logout Anyway
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Hackathon Guidelines */}
      {showGuidelines && (
        <HackathonGuidelines onClose={() => setShowGuidelines(false)} />
      )}

      {/* Save Reminder */}
      {showSaveReminder && (
        <div className="fixed bottom-6 right-6 z-[60] animate-in slide-in-from-bottom-4">
          <div className="bg-white dark:bg-gray-800 border border-amber-200 dark:border-amber-800 rounded-xl p-4 shadow-lg max-w-sm">
            <div className="flex items-start gap-3">
              <div className="bg-amber-100 dark:bg-amber-900/30 rounded-full p-1.5 shrink-0 mt-0.5">
                <Save className="h-4 w-4 text-amber-600" />
              </div>
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-800 dark:text-gray-100">Don't forget to save!</p>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                  You have unsaved changes for more than 5 minutes. Save your work to avoid losing it.
                </p>
                <div className="flex gap-2 mt-3">
                  <button
                    onClick={() => { setShowSaveReminder(false); handleSave(); }}
                    className="px-3 py-1.5 text-xs font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg transition-colors"
                  >
                    Save Now
                  </button>
                  <button
                    onClick={() => setShowSaveReminder(false)}
                    className="px-3 py-1.5 text-xs font-medium text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
                  >
                    Dismiss
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Submit Panel */}
      {showSubmitPanel && (
        <SubmitPanel
          onSubmitted={() => {
            setShowSubmitPanel(false);
            // Refresh session to get isSubmitted = true
            hackathonService.getStatus().then(setSessionInfo).catch(() => {});
          }}
          onCancel={() => setShowSubmitPanel(false)}
        />
      )}

      {/* Close Tab Unsaved Dialog */}
      {pendingCloseTabId && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60]">
          <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-sm shadow-2xl">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">Unsaved Changes</h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-5">
              &quot;{tabs.find(t => t.id === pendingCloseTabId)?.title}&quot; has unsaved changes. Close without saving?
            </p>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setPendingCloseTabId(null)}
                className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              >
                Cancel
              </button>
              <button
                onClick={() => doCloseTab(pendingCloseTabId)}
                className="px-4 py-2 text-sm font-medium bg-red-600 hover:bg-red-700 text-white rounded-lg"
              >
                Close Without Saving
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Session Reactivated Modal */}
      {showReactivatedModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-[70]">
          <div className="bg-white dark:bg-gray-800 rounded-2xl p-8 max-w-sm text-center shadow-2xl animate-in fade-in zoom-in-95">
            <div className="relative mb-6">
              <div className="bg-gradient-to-r from-teal-400 to-green-400 rounded-full p-4 w-20 h-20 mx-auto flex items-center justify-center shadow-lg shadow-teal-500/30">
                <Play className="h-10 w-10 text-white" />
              </div>
              <div className="absolute -top-1 -right-1 left-0 right-0 flex justify-center">
                <span className="relative flex h-4 w-4 -top-2 left-8">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-4 w-4 bg-green-500"></span>
                </span>
              </div>
            </div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100 mb-2">Session Activated! 🎉</h2>
            <p className="text-gray-600 dark:text-gray-400 mb-4">Your hackathon session has been reactivated by the administrator.</p>
            {reactivatedInfo?.minutes != null && reactivatedInfo.minutes > 0 ? (
              <div className="bg-teal-50 dark:bg-teal-900/20 rounded-xl p-4 mb-6">
                <p className="text-sm text-teal-700 dark:text-teal-300 font-medium mb-1">Time Remaining</p>
                <p className="text-2xl font-bold text-teal-800 dark:text-teal-200">{Math.floor(reactivatedInfo.minutes / 60)}h {reactivatedInfo.minutes % 60}m</p>
              </div>
            ) : (
              <div className="bg-teal-50 dark:bg-teal-900/20 rounded-xl p-4 mb-6">
                <p className="text-sm text-teal-700 dark:text-teal-300 font-medium mb-1">Duration</p>
                <p className="text-2xl font-bold text-teal-800 dark:text-teal-200">Unlimited</p>
              </div>
            )}
            <button onClick={() => setShowReactivatedModal(false)} className="w-full py-3 bg-gradient-to-r from-teal-600 to-green-600 hover:from-teal-700 hover:to-green-700 text-white font-semibold rounded-xl transition-all shadow-lg hover:shadow-xl">
              Let's Go!
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Live Timer ───────────────────────────────────────────────────

function LiveTimer({ expiresAt }: { expiresAt?: string }) {
  const [display, setDisplay] = useState("");
  const [isUrgent, setIsUrgent] = useState(false);

  useEffect(() => {
    const calcDisplay = () => {
      if (!expiresAt) return;
      const diff = new Date(expiresAt).getTime() - Date.now();
      if (diff <= 0) { setDisplay("00:00:00"); setIsUrgent(true); return; }
      const totalSec = Math.floor(diff / 1000);
      const h = Math.floor(totalSec / 3600);
      const m = Math.floor((totalSec % 3600) / 60);
      const s = totalSec % 60;
      setDisplay(`${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`);
      setIsUrgent(totalSec <= 300);
    };
    calcDisplay();
    const interval = setInterval(calcDisplay, 1000);
    return () => clearInterval(interval);
  }, [expiresAt]);

  return (
    <div className={`flex items-center gap-1 text-sm font-mono font-medium ${isUrgent ? "text-red-600 animate-pulse" : "text-orange-600"}`}>
      <Timer className="h-4 w-4" /><span>{display}</span>
    </div>
  );
}

// ─── Results Panel (SSMS-style) ───────────────────────────────────

function ResultsPanel({ results }: { results: ExecuteResult }) {
  const hasErrors = results.results.some((b) => b.type === "ERROR");
  const selectBatches = results.results.filter((b) => b.type === "SELECT" && b.columns && b.rows);

  // Auto-switch to Messages if there are errors and no result sets
  const [view, setView] = useState<"results" | "messages">(hasErrors && selectBatches.length === 0 ? "messages" : "results");
  const [panelHeight, setPanelHeight] = useState(250);
  const isResizing = useRef(false);
  const startY = useRef(0);
  const startHeight = useRef(0);

  // Update view when results change
  useEffect(() => {
    const errs = results.results.some((b) => b.type === "ERROR");
    const selects = results.results.filter((b) => b.type === "SELECT" && b.columns && b.rows);
    if (errs && selects.length === 0) setView("messages");
    else setView("results");
  }, [results]);

  const handleMouseDown = (e: React.MouseEvent) => {
    isResizing.current = true;
    startY.current = e.clientY;
    startHeight.current = panelHeight;
    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);
    e.preventDefault();
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (!isResizing.current) return;
    const diff = startY.current - e.clientY;
    const newHeight = Math.max(100, Math.min(600, startHeight.current + diff));
    setPanelHeight(newHeight);
  };

  const handleMouseUp = () => {
    isResizing.current = false;
    document.removeEventListener("mousemove", handleMouseMove);
    document.removeEventListener("mouseup", handleMouseUp);
  };

  return (
    <div style={{ height: panelHeight }} className="border-t dark:border-gray-700 flex flex-col bg-white dark:bg-gray-900 shrink-0">
      {/* Resize Handle */}
      <div
        onMouseDown={handleMouseDown}
        className="h-1 cursor-row-resize hover:bg-teal-400 dark:hover:bg-teal-600 bg-gray-200 dark:bg-gray-700 transition-colors"
      />

      {/* Results/Messages Tabs */}
      <div className="h-7 border-b dark:border-gray-700 flex items-center px-2 gap-0.5 bg-gray-50 dark:bg-gray-800 shrink-0">
        <button
          onClick={() => setView("results")}
          className={`px-3 py-1 text-xs font-medium rounded transition-colors ${
            view === "results"
              ? "bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-100 shadow-sm"
              : "text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
          }`}
        >
          Results
        </button>
        <button
          onClick={() => setView("messages")}
          className={`px-3 py-1 text-xs font-medium rounded transition-colors ${
            view === "messages"
              ? "bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-100 shadow-sm"
              : "text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
          }`}
        >
          Messages {hasErrors && <span className="ml-1 text-red-500">●</span>}
        </button>
        <span className="ml-auto text-[10px] text-gray-400 dark:text-gray-500">
          {results.executedBatches}/{results.totalBatches} batches • {results.results.reduce((sum, b) => sum + (b.durationMs || 0), 0)}ms total
        </span>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto">
        {view === "results" && (
          <div className="p-2">
            {selectBatches.length === 0 ? (
              <div className="text-xs text-gray-400 dark:text-gray-500 p-3 text-center italic">
                No result sets returned. Check the Messages tab.
              </div>
            ) : (
              selectBatches.map((batch, i) => (
                <div key={i} className="mb-3">
                  <div className="overflow-x-auto border dark:border-gray-700 rounded">
                    <table className="w-full text-xs border-collapse">
                      <thead className="bg-gray-100 dark:bg-gray-800 sticky top-0">
                        <tr>
                          <th className="px-2 py-1 text-left font-semibold text-gray-500 dark:text-gray-400 border-b dark:border-gray-700 border-r dark:border-gray-700 w-8">#</th>
                          {batch.columns!.map((col) => (
                            <th key={col} className="px-2 py-1 text-left font-semibold text-gray-700 dark:text-gray-300 border-b dark:border-gray-700 border-r dark:border-gray-700 last:border-r-0 whitespace-nowrap">{col}</th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {batch.rows!.map((row, ri) => (
                          <tr key={ri} className="border-b dark:border-gray-800 last:border-0 hover:bg-blue-50 dark:hover:bg-blue-900/10">
                            <td className="px-2 py-0.5 text-gray-400 dark:text-gray-500 border-r dark:border-gray-700 text-center font-mono">{ri + 1}</td>
                            {batch.columns!.map((col) => (
                              <td key={col} className="px-2 py-0.5 text-gray-700 dark:text-gray-300 font-mono border-r dark:border-gray-700 last:border-r-0 whitespace-nowrap">
                                {row[col] === null ? <span className="text-gray-400 dark:text-gray-600 italic">NULL</span> : String(row[col])}
                              </td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <div className="text-[10px] text-gray-500 dark:text-gray-400 mt-1 px-1">
                    {batch.totalRows} row(s) returned • {batch.durationMs}ms
                  </div>
                </div>
              ))
            )}
          </div>
        )}

        {view === "messages" && (
          <div className="p-2 font-mono text-xs space-y-0.5">
            {results.results.map((batch, i) => (
              <div key={i}>
                {batch.type === "ERROR" ? (
                  <div className="text-red-600 dark:text-red-400 py-0.5">
                    Msg: {batch.error}
                  </div>
                ) : batch.type === "SELECT" ? (
                  <div className="text-gray-600 dark:text-gray-400 py-0.5">
                    ({batch.totalRows} row(s) affected)
                  </div>
                ) : (
                  <div className="text-gray-600 dark:text-gray-400 py-0.5">
                    {batch.message || "Command(s) completed successfully."}
                  </div>
                )}
              </div>
            ))}
            <div className="text-gray-400 dark:text-gray-500 pt-2 border-t dark:border-gray-700 mt-2">
              Completion time: {new Date().toLocaleTimeString()} • Total execution time: {results.results.reduce((sum, b) => sum + (b.durationMs || 0), 0)}ms
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Save As Modal ────────────────────────────────────────────────

function SaveAsModal({ onSave, onCancel }: {
  onSave: (fileName: string, fileType: string, folderId?: number) => void;
  onCancel: () => void;
}) {
  const [fileName, setFileName] = useState("");
  const [fileType, setFileType] = useState("Script");
  const [folders, setFolders] = useState<FolderDto[]>([]);
  const [selectedFolderId, setSelectedFolderId] = useState<number | undefined>(undefined);

  useEffect(() => {
    fileService.getRoot().then((data) => setFolders(data.folders)).catch(() => {});
  }, []);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!fileName.trim()) { toast.error("File name is required"); return; }
    onSave(fileName.trim(), fileType, selectedFolderId);
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60]" onClick={onCancel}>
      <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-md shadow-2xl" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Save Script</h3>
          <button onClick={onCancel} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"><X className="h-4 w-4" /></button>
        </div>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1 block">File Name</label>
            <input value={fileName} onChange={(e) => setFileName(e.target.value)} placeholder="e.g., CreateOrdersTable.sql" autoFocus className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1 block">Type</label>
            <select value={fileType} onChange={(e) => setFileType(e.target.value)} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none">
              <option value="Script">Script</option>
              <option value="StoredProcedure">Stored Procedure</option>
              <option value="Function">Function</option>
              <option value="Trigger">Trigger</option>
              <option value="View">View</option>
              <option value="Other">Other</option>
            </select>
          </div>
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1 block">Folder</label>
            <select value={selectedFolderId ?? ""} onChange={(e) => setSelectedFolderId(e.target.value ? parseInt(e.target.value) : undefined)} className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none">
              <option value="">Root (no folder)</option>
              {folders.map((f) => (<option key={f.folderId} value={f.folderId}>{f.folderName}</option>))}
            </select>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" onClick={onCancel} className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Cancel</button>
            <button type="submit" className="px-4 py-2 text-sm font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg">Save</button>
          </div>
        </form>
      </div>
    </div>
  );
}
