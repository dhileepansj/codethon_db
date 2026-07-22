import { useState, useEffect, useRef } from "react";
import { X, Plus, Upload, Trash2, Edit, Eye, Download, ChevronRight, FileSpreadsheet, ClipboardList } from "lucide-react";
import { toast } from "sonner";
import { mcqAdminService } from "@/services/mcqService";
import type { McqAssessment } from "@/types";

interface Props {
  onClose: () => void;
}

type View = "list" | "create" | "edit" | "questions" | "results";

export default function McqAssessmentPanel({ onClose }: Props) {
  const [view, setView] = useState<View>("list");
  const [assessments, setAssessments] = useState<McqAssessment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => { loadAssessments(); }, []);

  const loadAssessments = async () => {
    try {
      const data = await mcqAdminService.getAssessments();
      setAssessments(data);
    } catch { toast.error("Failed to load assessments"); }
    finally { setLoading(false); }
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Delete this assessment and all its questions?")) return;
    try {
      await mcqAdminService.deleteAssessment(id);
      toast.success("Assessment deleted");
      loadAssessments();
    } catch { toast.error("Failed to delete"); }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-900 rounded-2xl w-[90vw] max-w-5xl max-h-[85vh] flex flex-col shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b dark:border-gray-800">
          <div className="flex items-center gap-3">
            <ClipboardList className="h-5 w-5 text-indigo-500" />
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
              {view === "list" ? "MCQ Assessments" :
               view === "create" ? "Create Assessment" :
               view === "edit" ? "Edit Assessment" :
               view === "questions" ? "Question Bank" : "Results"}
            </h2>
          </div>
          <div className="flex items-center gap-2">
            {view !== "list" && (
              <button onClick={() => { setView("list"); setSelectedId(null); }} className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800 rounded-lg">
                ← Back
              </button>
            )}
            <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
              <X className="h-5 w-5 text-gray-500" />
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-6">
          {view === "list" && (
            <AssessmentList
              assessments={assessments}
              loading={loading}
              onCreateNew={() => setView("create")}
              onEdit={(id) => { setSelectedId(id); setView("edit"); }}
              onQuestions={(id) => { setSelectedId(id); setView("questions"); }}
              onResults={(id) => { setSelectedId(id); setView("results"); }}
              onDelete={handleDelete}
            />
          )}
          {view === "create" && (
            <AssessmentForm onSave={() => { loadAssessments(); setView("list"); }} />
          )}
          {view === "edit" && selectedId && (
            <AssessmentForm assessmentId={selectedId} onSave={() => { loadAssessments(); setView("list"); }} />
          )}
          {view === "questions" && selectedId && (
            <QuestionBankView assessmentId={selectedId} />
          )}
          {view === "results" && selectedId && (
            <ResultsView assessmentId={selectedId} />
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Assessment List ─────────────────────────────────────────────

function AssessmentList({ assessments, loading, onCreateNew, onEdit, onQuestions, onResults, onDelete }: {
  assessments: McqAssessment[]; loading: boolean;
  onCreateNew: () => void; onEdit: (id: number) => void;
  onQuestions: (id: number) => void; onResults: (id: number) => void;
  onDelete: (id: number) => void;
}) {
  if (loading) return <p className="text-gray-500 text-center py-8">Loading...</p>;

  return (
    <div>
      <div className="flex justify-end mb-4">
        <button onClick={onCreateNew} className="flex items-center gap-1.5 px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700">
          <Plus className="h-4 w-4" /> New Assessment
        </button>
      </div>

      {assessments.length === 0 ? (
        <p className="text-gray-400 text-center py-12">No assessments created yet</p>
      ) : (
        <div className="space-y-3">
          {assessments.map((a) => (
            <div key={a.id} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800 rounded-xl border dark:border-gray-700">
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <h3 className="font-medium text-gray-900 dark:text-white">{a.title}</h3>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${a.type === "MCQ" ? "bg-indigo-100 text-indigo-700" : "bg-teal-100 text-teal-700"}`}>{a.type}</span>
                  <span className="text-xs text-gray-400">{a.subType}</span>
                  {!a.isActive && <span className="text-xs text-red-500 font-medium">Inactive</span>}
                </div>
                <p className="text-xs text-gray-500 mt-1">
                  {a.totalQuestions} questions • {a.durationMinutes ?? "∞"} min • {a.maxMarks} marks • Bank: {a.questionBankCount} questions
                </p>
              </div>
              <div className="flex items-center gap-1">
                <button onClick={() => onQuestions(a.id)} className="p-2 hover:bg-indigo-50 rounded-lg text-indigo-600" title="Question Bank">
                  <FileSpreadsheet className="h-4 w-4" />
                </button>
                <button onClick={() => onResults(a.id)} className="p-2 hover:bg-green-50 rounded-lg text-green-600" title="Results">
                  <Eye className="h-4 w-4" />
                </button>
                <button onClick={() => onEdit(a.id)} className="p-2 hover:bg-amber-50 rounded-lg text-amber-600" title="Edit">
                  <Edit className="h-4 w-4" />
                </button>
                <button onClick={() => onDelete(a.id)} className="p-2 hover:bg-red-50 rounded-lg text-red-600" title="Delete">
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Assessment Form ─────────────────────────────────────────────

function AssessmentForm({ assessmentId, onSave }: { assessmentId?: number; onSave: () => void }) {
  const [form, setForm] = useState({
    title: "", type: "MCQ", subType: "", durationMinutes: 45, totalQuestions: 30, maxMarks: 45,
    simplePercentage: 60, mediumPercentage: 30, complexPercentage: 10,
    simpleMarks: 1, mediumMarks: 2, complexMarks: 3,
    negativeMarking: false, negativeMarkValue: 0,
    shuffleQuestions: true, shuffleOptions: true,
    showResultImmediately: false, passPercentage: 0,
    allowNavigation: true, allowReview: true,
    autoSubmitOnTimeout: true, oneQuestionPerPage: true,
    showComplexity: true, showMarks: true, isActive: true
  });

  useEffect(() => {
    if (assessmentId) {
      mcqAdminService.getAssessment(assessmentId).then((a) => {
        setForm({
          title: a.title, type: a.type, subType: a.subType,
          durationMinutes: a.durationMinutes ?? 45, totalQuestions: a.totalQuestions, maxMarks: a.maxMarks,
          simplePercentage: a.simplePercentage, mediumPercentage: a.mediumPercentage, complexPercentage: a.complexPercentage,
          simpleMarks: a.simpleMarks, mediumMarks: a.mediumMarks, complexMarks: a.complexMarks,
          negativeMarking: a.negativeMarking, negativeMarkValue: a.negativeMarkValue,
          shuffleQuestions: a.shuffleQuestions, shuffleOptions: a.shuffleOptions,
          showResultImmediately: a.showResultImmediately, passPercentage: a.passPercentage,
          allowNavigation: a.allowNavigation, allowReview: a.allowReview,
          autoSubmitOnTimeout: a.autoSubmitOnTimeout, oneQuestionPerPage: a.oneQuestionPerPage,
          showComplexity: (a as any).showComplexity ?? true, showMarks: (a as any).showMarks ?? true, isActive: a.isActive
        });
      });
    }
  }, [assessmentId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (assessmentId) {
        await mcqAdminService.updateAssessment(assessmentId, form as any);
        toast.success("Assessment updated");
      } else {
        await mcqAdminService.createAssessment(form as any);
        toast.success("Assessment created");
      }
      onSave();
    } catch (err: any) { toast.error(err.response?.data?.message || "Failed"); }
  };

  const set = (key: string, value: any) => setForm((f) => ({ ...f, [key]: value }));

  return (
    <form onSubmit={handleSubmit} className="space-y-6 max-w-2xl">
      {/* Basic Info */}
      <div className="grid grid-cols-2 gap-4">
        <div className="col-span-2">
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Title *</label>
          <input value={form.title} onChange={(e) => set("title", e.target.value)} required className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white" placeholder="e.g., Selenium MCQ Test" />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Type</label>
          <select value={form.type} onChange={(e) => set("type", e.target.value)} className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white">
            <option value="MCQ">MCQ</option>
            <option value="SQL">SQL</option>
          </select>
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Sub-Type *</label>
          <input value={form.subType} onChange={(e) => set("subType", e.target.value)} required className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white" placeholder="e.g., Selenium, Playwright" />
        </div>
      </div>

      {/* Test Config */}
      <div className="grid grid-cols-3 gap-4">
        <div>
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Duration (min)</label>
          <input type="number" value={form.durationMinutes} onChange={(e) => set("durationMinutes", +e.target.value)} className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white" />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Total Questions</label>
          <input type="number" value={form.totalQuestions} onChange={(e) => set("totalQuestions", +e.target.value)} className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white" />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Max Marks</label>
          <input type="number" value={form.maxMarks} onChange={(e) => set("maxMarks", +e.target.value)} className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white" />
        </div>
      </div>

      {/* Complexity Distribution */}
      <div>
        <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase">Complexity Distribution (%)</label>
        <div className="grid grid-cols-3 gap-4 mt-2">
          <div>
            <label className="text-xs text-green-600">Simple %</label>
            <input type="number" value={form.simplePercentage} onChange={(e) => set("simplePercentage", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
          <div>
            <label className="text-xs text-amber-600">Medium %</label>
            <input type="number" value={form.mediumPercentage} onChange={(e) => set("mediumPercentage", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
          <div>
            <label className="text-xs text-red-600">Complex %</label>
            <input type="number" value={form.complexPercentage} onChange={(e) => set("complexPercentage", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
        </div>
      </div>

      {/* Marks per Complexity */}
      <div>
        <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase">Marks per Complexity</label>
        <div className="grid grid-cols-3 gap-4 mt-2">
          <div>
            <label className="text-xs text-green-600">Simple</label>
            <input type="number" value={form.simpleMarks} onChange={(e) => set("simpleMarks", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
          <div>
            <label className="text-xs text-amber-600">Medium</label>
            <input type="number" value={form.mediumMarks} onChange={(e) => set("mediumMarks", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
          <div>
            <label className="text-xs text-red-600">Complex</label>
            <input type="number" value={form.complexMarks} onChange={(e) => set("complexMarks", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
        </div>
      </div>

      {/* Negative Marking */}
      <div className="grid grid-cols-2 gap-4">
        <div className="flex items-center gap-2">
          <input type="checkbox" checked={form.negativeMarking} onChange={(e) => set("negativeMarking", e.target.checked)} className="rounded" />
          <label className="text-sm text-gray-700 dark:text-gray-300">Negative Marking</label>
        </div>
        {form.negativeMarking && (
          <div>
            <label className="text-xs text-gray-600">Deduction per wrong answer</label>
            <input type="number" step="0.25" value={form.negativeMarkValue} onChange={(e) => set("negativeMarkValue", +e.target.value)} className="w-full mt-1 px-3 py-2 border rounded-lg text-sm dark:bg-gray-800 dark:border-gray-600 dark:text-white" />
          </div>
        )}
      </div>

      {/* Settings Toggles */}
      <div className="grid grid-cols-2 gap-3">
        {[
          ["shuffleQuestions", "Shuffle Questions"],
          ["shuffleOptions", "Shuffle Options"],
          ["showResultImmediately", "Show Result Immediately"],
          ["allowNavigation", "Allow Navigation"],
          ["allowReview", "Allow Flag/Review"],
          ["autoSubmitOnTimeout", "Auto-Submit on Timeout"],
          ["oneQuestionPerPage", "One Question Per Page"],
          ["showComplexity", "Show Complexity to Participant"],
          ["showMarks", "Show Marks to Participant"],
        ].map(([key, label]) => (
          <div key={key} className="flex items-center gap-2">
            <input type="checkbox" checked={(form as any)[key]} onChange={(e) => set(key, e.target.checked)} className="rounded" />
            <label className="text-sm text-gray-700 dark:text-gray-300">{label}</label>
          </div>
        ))}
      </div>

      {/* Pass Percentage */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400">Pass Percentage (0 = no pass/fail)</label>
          <input type="number" value={form.passPercentage} onChange={(e) => set("passPercentage", +e.target.value)} className="w-full mt-1 px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-800 dark:text-white" />
        </div>
        {assessmentId && (
          <div className="flex items-center gap-2 pt-5">
            <input type="checkbox" checked={form.isActive} onChange={(e) => set("isActive", e.target.checked)} className="rounded" />
            <label className="text-sm text-gray-700 dark:text-gray-300">Active</label>
          </div>
        )}
      </div>

      <div className="flex justify-end gap-2 pt-4 border-t dark:border-gray-700">
        <button type="submit" className="px-6 py-2.5 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700">
          {assessmentId ? "Update" : "Create"} Assessment
        </button>
      </div>
    </form>
  );
}

// ─── Question Bank View ──────────────────────────────────────────

function QuestionBankView({ assessmentId }: { assessmentId: number }) {
  const [questions, setQuestions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => { loadQuestions(); }, [assessmentId]);

  const loadQuestions = async () => {
    try {
      const data = await mcqAdminService.getQuestions(assessmentId);
      setQuestions(data);
    } catch { toast.error("Failed to load questions"); }
    finally { setLoading(false); }
  };

  const handleCsvUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = async (ev) => {
      const text = ev.target?.result as string;
      const lines = text.split("\n").filter((l) => l.trim());
      const startIdx = lines[0]?.toLowerCase().includes("question") ? 1 : 0;

      const parsed = lines.slice(startIdx).map((line, idx) => {
        // CSV format: SNo, Question, OptionA, OptionB, OptionC, OptionD, Answer, Complexity, Category
        const parts = parseCsvLine(line);
        return {
          sNo: parseInt(parts[0]) || idx + 1,
          question: parts[1] || "",
          optionA: parts[2] || "",
          optionB: parts[3] || "",
          optionC: parts[4] || "",
          optionD: parts[5] || "",
          correctAnswer: extractAnswerLetter(parts[6] || "A"),
          complexity: parts[7]?.trim() || "Simple",
          category: parts[8]?.trim() || null,
        };
      }).filter((q) => q.question && q.optionA);

      if (parsed.length === 0) {
        toast.error("No valid questions found in the file");
        return;
      }

      try {
        const result = await mcqAdminService.bulkUploadQuestions(assessmentId, parsed);
        toast.success(result.message || `${parsed.length} questions uploaded`);
        loadQuestions();
      } catch (err: any) {
        toast.error(err.response?.data?.message || "Upload failed");
      }
    };
    reader.readAsText(file);
    e.target.value = "";
  };

  const handleDelete = async (id: number) => {
    try {
      await mcqAdminService.deleteQuestion(id);
      toast.success("Question deleted");
      setQuestions((prev) => prev.filter((q) => q.id !== id));
    } catch { toast.error("Failed to delete"); }
  };

  const simpleCount = questions.filter((q) => q.complexity === "Simple" && q.isActive).length;
  const mediumCount = questions.filter((q) => q.complexity === "Medium" && q.isActive).length;
  const complexCount = questions.filter((q) => q.complexity === "Complex" && q.isActive).length;

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-500">
            Total: <strong>{questions.length}</strong> |
            <span className="text-green-600 ml-1">Simple: {simpleCount}</span> |
            <span className="text-amber-600 ml-1">Medium: {mediumCount}</span> |
            <span className="text-red-600 ml-1">Complex: {complexCount}</span>
          </span>
        </div>
        <div className="flex gap-2">
          <input ref={fileRef} type="file" accept=".csv,.txt" onChange={handleCsvUpload} className="hidden" />
          <button onClick={() => fileRef.current?.click()} className="flex items-center gap-1.5 px-3 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700">
            <Upload className="h-4 w-4" /> Upload CSV
          </button>
        </div>
      </div>

      <div className="text-xs text-gray-400 mb-4 bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
        <strong>CSV format:</strong> SNo, Question, OptionA, OptionB, OptionC, OptionD, Answer, Complexity, Category
        <br />Answer column can be just the letter (A/B/C/D) or full text like "A. Automating web browsers"
      </div>

      {loading ? (
        <p className="text-center text-gray-400 py-8">Loading...</p>
      ) : questions.length === 0 ? (
        <p className="text-center text-gray-400 py-12">No questions uploaded yet. Use CSV upload above.</p>
      ) : (
        <div className="space-y-2 max-h-[50vh] overflow-auto">
          {questions.map((q, idx) => (
            <div key={q.id} className="flex items-start gap-3 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border dark:border-gray-700 text-sm">
              <span className="text-xs font-mono text-gray-400 pt-0.5 w-6 text-right shrink-0">{q.sNo}</span>
              <div className="flex-1 min-w-0">
                <p className="text-gray-800 dark:text-gray-200 truncate">{q.question}</p>
                <div className="flex items-center gap-3 mt-1">
                  <span className={`text-xs px-1.5 py-0.5 rounded ${q.complexity === "Simple" ? "bg-green-100 text-green-700" : q.complexity === "Medium" ? "bg-amber-100 text-amber-700" : "bg-red-100 text-red-700"}`}>
                    {q.complexity}
                  </span>
                  <span className="text-xs text-gray-400">Ans: {q.correctAnswer}</span>
                  {q.category && <span className="text-xs text-gray-400">{q.category}</span>}
                </div>
              </div>
              <button onClick={() => handleDelete(q.id)} className="p-1 hover:bg-red-50 rounded text-red-400 hover:text-red-600 shrink-0">
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Results View ────────────────────────────────────────────────

function ResultsView({ assessmentId }: { assessmentId: number }) {
  const [results, setResults] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    mcqAdminService.getResults(assessmentId)
      .then(setResults)
      .catch(() => toast.error("Failed to load results"))
      .finally(() => setLoading(false));
  }, [assessmentId]);

  const handleDownload = async () => {
    try {
      const res = await fetch(
        `${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/mcq/assessments/${assessmentId}/results/download`,
        { headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}` } }
      );
      if (!res.ok) { toast.error("Download failed"); return; }
      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `MCQ_Results_${assessmentId}.csv`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    } catch { toast.error("Download failed"); }
  };

  if (loading) return <p className="text-center text-gray-400 py-8">Loading...</p>;
  if (results.length === 0) return <p className="text-center text-gray-400 py-12">No submissions yet</p>;

  return (
    <div className="overflow-auto">
      <div className="flex justify-end mb-3">
        <button onClick={handleDownload} className="flex items-center gap-1.5 px-3 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700">
          <Download className="h-4 w-4" /> Download CSV
        </button>
      </div>
      <table className="w-full text-sm">
        <thead className="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">User</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">Score</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">%</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">Correct</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">Wrong</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">Skipped</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">Time</th>
            <th className="px-3 py-2 text-left text-xs font-semibold text-gray-500 uppercase">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y dark:divide-gray-700">
          {results.map((r: any) => (
            <tr key={r.testId} className="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td className="px-3 py-2">
                <p className="font-medium text-gray-800 dark:text-white">{r.userID}</p>
                <p className="text-xs text-gray-400">{r.fullName || ""}</p>
              </td>
              <td className="px-3 py-2 font-mono">{r.score}/{r.maxScore}</td>
              <td className="px-3 py-2 font-mono">{r.percentage}%</td>
              <td className="px-3 py-2 text-green-600 font-mono">{r.correct}</td>
              <td className="px-3 py-2 text-red-600 font-mono">{r.wrong}</td>
              <td className="px-3 py-2 text-gray-400 font-mono">{r.skipped}</td>
              <td className="px-3 py-2 text-xs text-gray-500">{r.timeSpentSeconds ? `${Math.floor(r.timeSpentSeconds / 60)}m` : "—"}</td>
              <td className="px-3 py-2">
                {r.passed === true && <span className="text-xs px-2 py-0.5 rounded-full bg-green-100 text-green-700 font-medium">Pass</span>}
                {r.passed === false && <span className="text-xs px-2 py-0.5 rounded-full bg-red-100 text-red-700 font-medium">Fail</span>}
                {r.isAutoSubmitted && <span className="text-xs text-amber-600 ml-1">(auto)</span>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ─── CSV Parser Helper ───────────────────────────────────────────

function parseCsvLine(line: string): string[] {
  const result: string[] = [];
  let current = "";
  let inQuotes = false;

  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (ch === '"') {
      inQuotes = !inQuotes;
    } else if (ch === "," && !inQuotes) {
      result.push(current.trim());
      current = "";
    } else {
      current += ch;
    }
  }
  result.push(current.trim());
  return result;
}

function extractAnswerLetter(answer: string): string {
  const trimmed = answer.trim().toUpperCase();
  if (trimmed.length === 1 && "ABCD".includes(trimmed)) return trimmed;
  // Handle "A. Some text" format
  if (/^[A-D]\./.test(trimmed)) return trimmed[0];
  return "A";
}
