import { useState, useEffect, useCallback, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { LogOut, Plus, Trash2, ChevronRight, Database, BookOpen, Clock, Save } from "lucide-react";
import { toast } from "sonner";
import { logout } from "@/redux/slices/authSlice";
import { manualTestService } from "@/services/manualTestService";
import ThemeToggle from "@/components/common/ThemeToggle";
import SecurityShield from "@/components/common/SecurityShield";
import { useTheme } from "@/contexts/ThemeContext";
import type { AppDispatch, RootState } from "@/redux/store";
import type { ScenarioDto, TestCaseDto, ManualTestWorkspace } from "@/services/manualTestService";

export default function ManualTestWorkspacePage() {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useSelector((s: RootState) => s.auth);
  const { isDark } = useTheme();
  const [workspace, setWorkspace] = useState<ManualTestWorkspace | null>(null);
  const [scenarios, setScenarios] = useState<ScenarioDto[]>([]);
  const [selectedScenario, setSelectedScenario] = useState<ScenarioDto | null>(null);
  const [testCases, setTestCases] = useState<TestCaseDto[]>([]);
  const [showUseCase, setShowUseCase] = useState(true);
  const [showSubmitConfirm, setShowSubmitConfirm] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [loading, setLoading] = useState(true);

  // Build dark-mode-aware HTML for the Use Case iframe
  const useCaseIframeHtml = useMemo(() => {
    if (!workspace?.useCaseHtml) return "";
    if (!isDark) return workspace.useCaseHtml;
    // Inject dark mode overrides before </head> or prepend if no </head>
    const darkStyles = `<style>
      body { background: #111827 !important; color: #e5e7eb !important; }
      h1 { color: #2dd4bf !important; border-bottom-color: #2dd4bf !important; }
      h2 { color: #93c5fd !important; }
      h3 { color: #d1d5db !important; }
      table { border-color: #374151 !important; }
      th, td { border-color: #374151 !important; color: #e5e7eb !important; }
      th { background: #1f2937 !important; }
      .rule { background: rgba(245,158,11,0.1) !important; border-left-color: #f59e0b !important; color: #fde68a !important; }
      .note { background: rgba(59,130,246,0.1) !important; border-left-color: #3b82f6 !important; color: #bfdbfe !important; }
      code { background: #1f2937 !important; color: #e5e7eb !important; }
    </style>`;
    if (workspace.useCaseHtml.includes("</head>")) {
      return workspace.useCaseHtml.replace("</head>", darkStyles + "</head>");
    }
    return darkStyles + workspace.useCaseHtml;
  }, [workspace?.useCaseHtml, isDark]);

  useEffect(() => {
    manualTestService.getWorkspace()
      .then(async (ws) => {
        setWorkspace(ws);
        setScenarios(ws.scenarios);
        if (ws.scenarios.length > 0) {
          const first = ws.scenarios[0];
          setSelectedScenario(first);
          manualTestService.getTestCases(first.id).then(setTestCases).catch(() => {});
        }
        // Check submission status
        try {
          const status = await manualTestService.getSubmissionStatus();
          if (status.isSubmitted) setIsSubmitted(true);
        } catch {}
      })
      .catch((err) => { toast.error(err.response?.data?.errors?.[0] || "Failed to load"); navigate("/", { replace: true }); })
      .finally(() => setLoading(false));
  }, [navigate]);

  const loadTestCases = useCallback(async (scenario: ScenarioDto) => {
    setSelectedScenario(scenario);
    try { const cases = await manualTestService.getTestCases(scenario.id); setTestCases(cases); }
    catch { toast.error("Failed to load test cases"); }
  }, []);

  // ─── Scenario actions ──────────────────────────────────────────

  const addScenario = async () => {
    const nextNum = scenarios.length + 1;
    const newId = `SC_${String(nextNum).padStart(3, "0")}`;
    try {
      const res = await manualTestService.saveScenario({ scenarioId: newId, sortOrder: nextNum });
      const newScenario: ScenarioDto = { id: res.id, sNo: nextNum, scenarioId: newId, sortOrder: nextNum, testCaseCount: 0 };
      setScenarios((prev) => [...prev, newScenario]);
      setSelectedScenario(newScenario);
      setTestCases([]);
    } catch { toast.error("Failed to add scenario"); }
  };

  const updateScenario = async (field: string, value: string) => {
    if (!selectedScenario) return;
    const updated = { ...selectedScenario, [field]: value };
    setSelectedScenario(updated);
    setScenarios((prev) => prev.map((s) => s.id === updated.id ? updated : s));
    try { await manualTestService.saveScenario(updated); }
    catch { toast.error("Failed to save"); }
  };

  const deleteScenario = async (id: number) => {
    try {
      await manualTestService.deleteScenario(id);
      setScenarios((prev) => prev.filter((s) => s.id !== id));
      if (selectedScenario?.id === id) { setSelectedScenario(null); setTestCases([]); }
      toast.success("Scenario deleted");
    } catch { toast.error("Failed to delete"); }
  };

  // ─── Test case actions ─────────────────────────────────────────

  const addTestCase = async () => {
    if (!selectedScenario) return;
    const nextStep = testCases.length + 1;
    const stepNo = String(nextStep).padStart(3, "0");
    const tcId = `${selectedScenario.scenarioId}-${stepNo}`;
    try {
      const res = await manualTestService.saveTestCase({ scenarioDbId: selectedScenario.id, testCaseId: tcId, stepNo, sortOrder: nextStep });
      setTestCases((prev) => [...prev, { id: res.id, scenarioDbId: selectedScenario.id, sNo: nextStep, testCaseId: tcId, stepNo, sortOrder: nextStep }]);
    } catch { toast.error("Failed to add test case"); }
  };

  const updateTestCase = async (tcId: number, field: string, value: string) => {
    const tc = testCases.find((c) => c.id === tcId);
    if (!tc) return;
    const updated = { ...tc, [field]: value };
    setTestCases((prev) => prev.map((c) => c.id === tcId ? updated : c));
    try { await manualTestService.saveTestCase({ ...updated, scenarioDbId: tc.scenarioDbId }); }
    catch { /* silent — auto-save */ }
  };

  const deleteTestCase = async (id: number) => {
    try { await manualTestService.deleteTestCase(id); setTestCases((prev) => prev.filter((c) => c.id !== id)); }
    catch { toast.error("Failed to delete"); }
  };

  const handleLogout = () => { dispatch(logout()); navigate("/login", { replace: true }); };

  const handleSubmit = async () => {
    setShowSubmitConfirm(false);
    try {
      await manualTestService.submit();
      setIsSubmitted(true);
      toast.success("Work submitted successfully");
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || err.response?.data?.message || "Submit failed");
    }
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950"><Clock className="h-6 w-6 animate-spin text-teal-500" /></div>;

  return (
    <SecurityShield tabSwitch devTools clipboardGuard>
      <div className="h-screen flex flex-col bg-gray-50 dark:bg-gray-950">
        {/* Header */}
        <header className="h-12 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center justify-between px-4 shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5"><Database className="h-4 w-4 text-white" /></div>
            <span className="font-semibold text-teal-800 dark:text-teal-300">NovacCodeLab</span>
            <span className="text-xs text-gray-400 hidden sm:inline">— {workspace?.title}</span>
          </div>
          <div className="flex items-center gap-3">
            <button onClick={() => setShowUseCase(!showUseCase)} className={`flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium transition-colors ${showUseCase ? "bg-teal-50 text-teal-700 dark:bg-teal-900/20 dark:text-teal-300" : "text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800"}`}>
              <BookOpen className="h-3.5 w-3.5" /> Use Case
            </button>
            <button onClick={() => setShowSubmitConfirm(true)} className="flex items-center gap-1.5 px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white rounded-md text-xs font-medium transition-colors">
              Submit
            </button>
            <ThemeToggle />
            <button onClick={handleLogout} className="text-gray-400 hover:text-red-500 transition-colors" title="Logout"><LogOut className="h-4 w-4" /></button>
          </div>
        </header>

        {/* Main content */}
        <div className="flex-1 flex overflow-hidden">
          {/* Left: Scenario & Test Case Editor */}
          <div className={`flex-1 flex flex-col overflow-hidden ${showUseCase ? "border-r dark:border-gray-700" : ""}`}>
            {/* Scenario list */}
            <div className="h-10 border-b dark:border-gray-800 bg-white dark:bg-gray-900 flex items-center px-3 gap-2 shrink-0">
              <span className="text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase">Scenarios</span>
              <div className="flex-1 flex items-center gap-1 overflow-x-auto">
                {scenarios.map((s) => (
                  <button key={s.id} onClick={() => loadTestCases(s)}
                    className={`px-2.5 py-1 rounded text-[11px] font-medium whitespace-nowrap transition-colors ${selectedScenario?.id === s.id ? "bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-300" : "text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"}`}>
                    {s.scenarioId}
                  </button>
                ))}
              </div>
              <button onClick={addScenario} className="p-1 text-teal-600 hover:bg-teal-50 dark:hover:bg-teal-900/20 rounded" title="Add scenario"><Plus className="h-3.5 w-3.5" /></button>
            </div>

            {/* Scenario detail + Test cases */}
            <div className="flex-1 overflow-auto p-4">
              {selectedScenario ? (
                <div className="max-w-4xl space-y-4">
                  {/* Scenario fields */}
                  <div className="bg-white dark:bg-gray-900 rounded-lg border dark:border-gray-800 p-4">
                    <div className="flex items-center justify-between mb-3">
                      <span className="text-xs font-semibold text-teal-600 dark:text-teal-400">{selectedScenario.scenarioId}</span>
                      <button onClick={() => deleteScenario(selectedScenario.id)} className="text-red-400 hover:text-red-600 p-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20"><Trash2 className="h-3.5 w-3.5" /></button>
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label className="text-[10px] font-medium text-gray-500 dark:text-gray-400">Scenario Title</label>
                        <input value={selectedScenario.scenario || ""} onChange={(e) => updateScenario("scenario", e.target.value)} placeholder="e.g., Login validation"
                          className="w-full mt-0.5 px-2.5 py-1.5 border dark:border-gray-700 rounded-md text-xs dark:bg-gray-800 dark:text-white" />
                      </div>
                      <div>
                        <label className="text-[10px] font-medium text-gray-500 dark:text-gray-400">Must Test</label>
                        <select value={selectedScenario.mustTest || ""} onChange={(e) => updateScenario("mustTest", e.target.value)}
                          className="w-full mt-0.5 px-2.5 py-1.5 border dark:border-gray-700 rounded-md text-xs dark:bg-gray-800 dark:text-white">
                          <option value="">—</option><option value="Yes">Yes</option><option value="No">No</option>
                        </select>
                      </div>
                      <div className="col-span-2">
                        <label className="text-[10px] font-medium text-gray-500 dark:text-gray-400">Description</label>
                        <textarea value={selectedScenario.description || ""} onChange={(e) => updateScenario("description", e.target.value)} rows={2} placeholder="Describe the scenario..."
                          className="w-full mt-0.5 px-2.5 py-1.5 border dark:border-gray-700 rounded-md text-xs dark:bg-gray-800 dark:text-white resize-none" />
                      </div>
                    </div>
                  </div>

                  {/* Test Cases Table */}
                  <div className="bg-white dark:bg-gray-900 rounded-lg border dark:border-gray-800">
                    <div className="flex items-center justify-between px-4 py-2.5 border-b dark:border-gray-800">
                      <span className="text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase">Test Cases</span>
                      <button onClick={addTestCase} className="flex items-center gap-1 px-2 py-1 text-[11px] text-teal-600 hover:bg-teal-50 dark:hover:bg-teal-900/20 rounded font-medium">
                        <Plus className="h-3 w-3" /> Add Step
                      </button>
                    </div>

                    {testCases.length === 0 ? (
                      <p className="text-xs text-gray-400 text-center py-8">No test cases yet. Click "+ Add Step" above.</p>
                    ) : (
                      <div className="overflow-x-auto">
                        <table className="w-full text-xs">
                          <thead>
                            <tr className="bg-gray-50 dark:bg-gray-800 text-left">
                              <th className="px-3 py-2 text-[10px] font-semibold text-gray-500 uppercase w-12">S.No</th>
                              <th className="px-3 py-2 text-[10px] font-semibold text-gray-500 uppercase w-16">Step</th>
                              <th className="px-3 py-2 text-[10px] font-semibold text-gray-500 uppercase w-40">Test Case Description</th>
                              <th className="px-3 py-2 text-[10px] font-semibold text-gray-500 uppercase">Test Step / Input Specification</th>
                              <th className="px-3 py-2 text-[10px] font-semibold text-gray-500 uppercase w-36">Input/Test Data</th>
                              <th className="px-3 py-2 text-[10px] font-semibold text-gray-500 uppercase">Expected Result</th>
                              <th className="px-3 py-2 w-8"></th>
                            </tr>
                          </thead>
                          <tbody className="divide-y dark:divide-gray-800">
                            {testCases.map((tc, idx) => (
                              <tr key={tc.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                                <td className="px-3 py-1.5">
                                  <span className="text-[10px] font-mono text-gray-400">{idx + 1}</span>
                                </td>
                                <td className="px-3 py-1.5">
                                  <span className="text-[10px] font-mono text-gray-400">{tc.stepNo}</span>
                                </td>
                                <td className="px-3 py-1.5">
                                  <input value={(tc as any).testCaseDescription || ""} onChange={(e) => updateTestCase(tc.id, "testCaseDescription", e.target.value)} placeholder="Test case description"
                                    className="w-full px-2 py-1 border dark:border-gray-700 rounded text-xs dark:bg-gray-800 dark:text-white" />
                                </td>
                                <td className="px-3 py-1.5">
                                  <input value={tc.inputSpecification || ""} onChange={(e) => updateTestCase(tc.id, "inputSpecification", e.target.value)} placeholder="Test step / input specification"
                                    className="w-full px-2 py-1 border dark:border-gray-700 rounded text-xs dark:bg-gray-800 dark:text-white" />
                                </td>
                                <td className="px-3 py-1.5">
                                  <input value={tc.inputTestData || ""} onChange={(e) => updateTestCase(tc.id, "inputTestData", e.target.value)} placeholder="Input / test data"
                                    className="w-full px-2 py-1 border dark:border-gray-700 rounded text-xs dark:bg-gray-800 dark:text-white" />
                                </td>
                                <td className="px-3 py-1.5">
                                  <input value={tc.expectedResult || ""} onChange={(e) => updateTestCase(tc.id, "expectedResult", e.target.value)} placeholder="Expected result"
                                    className="w-full px-2 py-1 border dark:border-gray-700 rounded text-xs dark:bg-gray-800 dark:text-white" />
                                </td>
                                <td className="px-2 py-1.5">
                                  <button onClick={() => deleteTestCase(tc.id)} className="text-red-400 hover:text-red-600 p-0.5 rounded"><Trash2 className="h-3 w-3" /></button>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <Plus className="h-8 w-8 text-gray-300 dark:text-gray-600 mx-auto mb-2" />
                    <p className="text-sm text-gray-400 dark:text-gray-500">No scenarios yet</p>
                    <button onClick={addScenario} className="mt-3 px-4 py-2 bg-teal-600 text-white text-xs font-medium rounded-lg hover:bg-teal-700">
                      + Create First Scenario
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Right: Use Case Panel — same pattern as QuestionPanel in WorkspacePage */}
          {showUseCase && useCaseIframeHtml && (
            <div className="w-[45%] min-w-[300px] max-w-[600px] overflow-hidden">
              <div className="h-full flex flex-col bg-white dark:bg-gray-900">
                <div className="h-10 border-b dark:border-gray-700 flex items-center justify-between px-3 shrink-0 bg-gray-50 dark:bg-gray-800">
                  <div className="flex items-center gap-2">
                    <BookOpen className="h-4 w-4 text-teal-600" />
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-200 truncate max-w-[200px]">
                      Use Case
                    </span>
                  </div>
                  <button onClick={() => setShowUseCase(false)} className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded">
                    <span className="text-sm">✕</span>
                  </button>
                </div>
                <div className="flex-1 overflow-auto">
                  <iframe
                    srcDoc={useCaseIframeHtml}
                    title="Use Case"
                    className="w-full h-full border-0"
                    sandbox="allow-same-origin"
                    style={{ minHeight: "100%" }}
                  />
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Submit Confirmation */}
      {showSubmitConfirm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-800 p-6 max-w-xs w-full mx-4 shadow-lg">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-2">Submit Work?</h3>
            <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
              You have {scenarios.length} scenario(s). Once submitted, you cannot make further changes.
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">This cannot be undone.</p>
            <div className="flex justify-end gap-2">
              <button onClick={() => setShowSubmitConfirm(false)} className="px-3 py-1.5 text-xs text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">Cancel</button>
              <button onClick={handleSubmit} className="px-3 py-1.5 text-xs font-medium bg-green-600 text-white rounded-md hover:bg-green-700">Submit</button>
            </div>
          </div>
        </div>
      )}

      {/* Submitted overlay */}
      {isSubmitted && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-800 p-8 max-w-sm w-full mx-4 shadow-lg text-center">
            <div className="w-12 h-12 bg-green-100 dark:bg-green-900/30 rounded-full flex items-center justify-center mx-auto mb-3">
              <Save className="h-6 w-6 text-green-600" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">Work Submitted</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">Your test scenarios and cases have been submitted successfully.</p>
            <button onClick={handleLogout} className="w-full py-2.5 bg-teal-600 text-white text-sm font-medium rounded-lg hover:bg-teal-700">Done</button>
          </div>
        </div>
      )}
    </SecurityShield>
  );
}
