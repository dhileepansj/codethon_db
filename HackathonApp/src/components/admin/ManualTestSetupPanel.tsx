import { useState, useEffect, useRef } from "react";
import { X, Plus, Trash2, Edit, Download, FileText, Eye } from "lucide-react";
import { toast } from "sonner";
import { mcqAdminService } from "@/services/mcqService";
import httpClient from "@/services/httpClient";
import type { McqAssessment } from "@/types";

interface Props {
  onClose: () => void;
}

type View = "list" | "create" | "edit" | "submissions";

export default function ManualTestSetupPanel({ onClose }: Props) {
  const [view, setView] = useState<View>("list");
  const [assessments, setAssessments] = useState<McqAssessment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => { loadAssessments(); }, []);

  const loadAssessments = async () => {
    try {
      const all = await mcqAdminService.getAssessments();
      setAssessments(all.filter((a) => a.type === "ManualTesting"));
    } catch { toast.error("Failed to load"); }
    finally { setLoading(false); }
  };

  const handleDelete = async (id: number) => {
    setDeleteTarget(id);
  };

  const [deleteTarget, setDeleteTarget] = useState<number | null>(null);

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try { await mcqAdminService.deleteAssessment(deleteTarget); toast.success("Deleted"); loadAssessments(); }
    catch { toast.error("Failed"); }
    finally { setDeleteTarget(null); }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b dark:border-gray-800">
          <div className="flex items-center gap-2">
            <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5">
              <FileText className="h-4 w-4 text-white" />
            </div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
              {view === "list" ? "Manual Testing" : view === "create" ? "Create Assessment" : view === "edit" ? "Edit Assessment" : "Submissions"}
            </h2>
          </div>
          <div className="flex items-center gap-2">
            {view !== "list" && <button onClick={() => { setView("list"); setSelectedId(null); }} className="px-3 py-1.5 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">← Back</button>}
            <button onClick={onClose} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"><X className="h-5 w-5" /></button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {view === "list" && (
            <div>
              <div className="flex justify-end mb-4">
                <button onClick={() => setView("create")} className="flex items-center gap-1.5 px-3 py-1.5 bg-teal-600 text-white text-xs font-medium rounded-md hover:bg-teal-700">
                  <Plus className="h-3.5 w-3.5" /> New Assessment
                </button>
              </div>
              {loading ? <p className="text-sm text-gray-400 text-center py-8">Loading...</p> :
              assessments.length === 0 ? <p className="text-sm text-gray-400 text-center py-10">No manual testing assessments yet</p> : (
                <div className="space-y-2">
                  {assessments.map((a) => (
                    <div key={a.id} className="flex items-center justify-between p-3.5 bg-gray-50 dark:bg-gray-800 rounded-lg border dark:border-gray-700">
                      <div>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">{a.title}</p>
                        <p className="text-xs text-gray-500 mt-0.5">{a.subType} · {a.durationMinutes ?? "∞"} min</p>
                      </div>
                      <div className="flex items-center gap-1">
                        <button onClick={() => { setSelectedId(a.id); setView("submissions"); }} className="p-1.5 hover:bg-green-50 dark:hover:bg-green-900/20 rounded text-green-600" title="Submissions"><Eye className="h-3.5 w-3.5" /></button>
                        <button onClick={() => { setSelectedId(a.id); setView("edit"); }} className="p-1.5 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded text-amber-600" title="Edit"><Edit className="h-3.5 w-3.5" /></button>
                        <button onClick={() => handleDelete(a.id)} className="p-1.5 hover:bg-red-50 dark:hover:bg-red-900/20 rounded text-red-500" title="Delete"><Trash2 className="h-3.5 w-3.5" /></button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {(view === "create" || view === "edit") && (
            <ManualTestForm assessmentId={view === "edit" ? selectedId : undefined} onSave={() => { loadAssessments(); setView("list"); }} />
          )}

          {view === "submissions" && selectedId && (
            <SubmissionsView assessmentId={selectedId} />
          )}
        </div>
      </div>

      {/* Delete confirm */}
      {deleteTarget && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-[60]">
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-800 p-6 max-w-xs w-full mx-4 shadow-lg">
            <div className="flex items-start gap-3 mb-4">
              <Trash2 className="h-5 w-5 text-red-500 shrink-0 mt-0.5" />
              <div>
                <h3 className="text-sm font-semibold text-gray-900 dark:text-white">Delete Assessment</h3>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">This will delete the assessment. Cannot be undone.</p>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button onClick={() => setDeleteTarget(null)} className="px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">Cancel</button>
              <button onClick={confirmDelete} className="px-3 py-1.5 text-xs font-medium bg-red-600 text-white rounded-md hover:bg-red-700">Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Form ────────────────────────────────────────────────────────

function ManualTestForm({ assessmentId, onSave }: { assessmentId?: number | null; onSave: () => void }) {
  const [form, setForm] = useState({
    title: "", subType: "TestScenario", durationMinutes: 120,
  });
  const [useCaseHtml, setUseCaseHtml] = useState("");
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (assessmentId) {
      mcqAdminService.getAssessment(assessmentId).then((a) => {
        setForm({ title: a.title, subType: a.subType, durationMinutes: a.durationMinutes ?? 120 });
      });
      httpClient.get(`/api/manual-test/assessments/${assessmentId}/usecase`)
        .then((res) => setUseCaseHtml(res.data.data?.useCaseHtml || res.data.useCaseHtml || ""))
        .catch(() => {});
    }
  }, [assessmentId]);

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.name.endsWith(".html") && !file.name.endsWith(".htm")) {
      toast.error("Only .html or .htm files are allowed"); return;
    }
    const reader = new FileReader();
    reader.onload = (ev) => { setUseCaseHtml(ev.target?.result as string); setUploadedFileName(file.name); toast.success(`Uploaded: ${file.name}`); };
    reader.readAsText(file);
    e.target.value = "";
  };

  const handleSubmit = async () => {
    if (!form.title.trim()) { toast.error("Title is required"); return; }
    setIsSaving(true);
    try {
      if (assessmentId) {
        await mcqAdminService.updateAssessment(assessmentId, { ...form, type: "ManualTesting", totalQuestions: 0, maxMarks: 0 } as any);
        if (useCaseHtml) await httpClient.post(`/api/manual-test/assessments/${assessmentId}/usecase`, { htmlContent: useCaseHtml });
        toast.success("Assessment updated");
      } else {
        const created = await mcqAdminService.createAssessment({ ...form, type: "ManualTesting", totalQuestions: 0, maxMarks: 0 } as any);
        if (useCaseHtml && created?.id) await httpClient.post(`/api/manual-test/assessments/${created.id}/usecase`, { htmlContent: useCaseHtml });
        toast.success("Assessment created");
      }
      onSave();
    } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
    finally { setIsSaving(false); }
  };

  return (
    <div className="space-y-6">
      {/* Title */}
      <div>
        <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Assessment Title *</label>
        <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder="e.g., Student Info — Manual Testing"
          className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
      </div>

      {/* Sub-type + Duration */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Sub-Type</label>
          <input value={form.subType} onChange={(e) => setForm({ ...form, subType: e.target.value })} placeholder="e.g., TestScenario, Functional"
            className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
        </div>
        <div>
          <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Duration (minutes)</label>
          <input type="number" value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: +e.target.value })}
            className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none" />
          <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">Time limit for participants to complete the test.</p>
        </div>
      </div>

      {/* Use Case Upload */}
      <div>
        <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Use Case (HTML) *</label>
        <div className="border-2 border-dashed dark:border-gray-600 rounded-lg p-6 text-center hover:border-teal-400 dark:hover:border-teal-600 transition-colors">
          <input ref={fileRef} type="file" accept=".html,.htm" onChange={handleFileUpload} className="hidden" id="usecase-upload" />
          <label htmlFor="usecase-upload" className="cursor-pointer">
            <FileText className="h-8 w-8 text-gray-400 mx-auto mb-2" />
            <p className="text-sm text-gray-600 dark:text-gray-400">Click to upload an HTML file</p>
            <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">.html or .htm files only — use case / requirements document</p>
          </label>
        </div>

        {/* Upload status */}
        {(useCaseHtml || uploadedFileName) && (
          <div className="mt-3 flex items-center gap-2 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg px-3 py-2">
            <FileText className="h-4 w-4 text-green-600" />
            <span className="text-sm text-green-700 dark:text-green-400">
              {uploadedFileName || "Use case loaded"}
              <span className="text-xs text-green-600 dark:text-green-500 ml-2">({(useCaseHtml.length / 1024).toFixed(1)} KB)</span>
            </span>
          </div>
        )}

        {/* Preview */}
        {useCaseHtml && (
          <div className="mt-3">
            <p className="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Preview:</p>
            <div className="border dark:border-gray-600 rounded-lg overflow-hidden h-48">
              <iframe srcDoc={useCaseHtml} title="Preview" className="w-full h-full border-0" sandbox="allow-same-origin" />
            </div>
          </div>
        )}
      </div>

      {/* Save button */}
      <div className="flex justify-end pt-2">
        <button onClick={handleSubmit} disabled={isSaving}
          className="px-5 py-2.5 bg-teal-600 hover:bg-teal-700 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-50 flex items-center gap-2">
          {isSaving ? "Saving..." : assessmentId ? "Save Changes" : "Create Assessment"}
        </button>
      </div>
    </div>
  );
}

// ─── Submissions ─────────────────────────────────────────────────

function SubmissionsView({ assessmentId }: { assessmentId: number }) {
  const [submissions, setSubmissions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    httpClient.get(`/api/manual-test/submissions/${assessmentId}`)
      .then((res) => setSubmissions(res.data.data || []))
      .catch(() => toast.error("Failed to load"))
      .finally(() => setLoading(false));
  }, [assessmentId]);

  const handleExport = async () => {
    try {
      const res = await fetch(
        `${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/manual-test/submissions/${assessmentId}/export`,
        { headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}` } }
      );
      if (!res.ok) { toast.error("Export failed"); return; }
      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url; a.download = `ManualTest_Export.csv`;
      document.body.appendChild(a); a.click(); document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    } catch { toast.error("Export failed"); }
  };

  if (loading) return <p className="text-sm text-gray-400 text-center py-8">Loading...</p>;
  if (submissions.length === 0) return <p className="text-sm text-gray-400 text-center py-10">No submissions yet</p>;

  return (
    <div>
      <div className="flex justify-end mb-3">
        <button onClick={handleExport} className="flex items-center gap-1.5 px-3 py-1.5 bg-green-600 text-white text-xs font-medium rounded-md hover:bg-green-700">
          <Download className="h-3.5 w-3.5" /> Export CSV
        </button>
      </div>
      <table className="w-full text-xs">
        <thead className="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th className="px-3 py-2 text-left text-[10px] font-semibold text-gray-500 uppercase">User</th>
            <th className="px-3 py-2 text-left text-[10px] font-semibold text-gray-500 uppercase">Scenarios</th>
            <th className="px-3 py-2 text-left text-[10px] font-semibold text-gray-500 uppercase">Test Cases</th>
          </tr>
        </thead>
        <tbody className="divide-y dark:divide-gray-700">
          {submissions.map((s: any) => (
            <tr key={s.userId} className="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td className="px-3 py-2">
                <p className="font-medium text-gray-800 dark:text-white">{s.userLoginId}</p>
                <p className="text-[10px] text-gray-400">{s.fullName || ""}</p>
              </td>
              <td className="px-3 py-2 font-mono">{s.scenarioCount}</td>
              <td className="px-3 py-2 font-mono">{s.testCaseCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
