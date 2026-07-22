import { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { Clock, Flag, ChevronLeft, ChevronRight, Send, AlertTriangle, CheckCircle, LogOut, Database } from "lucide-react";
import { toast } from "sonner";
import { mcqTestService } from "@/services/mcqService";
import { logout } from "@/redux/slices/authSlice";
import ThemeToggle from "@/components/common/ThemeToggle";
import type { McqQuestionForTest, McqTestStatus, McqSubmitResult } from "@/types";
import type { AppDispatch } from "@/redux/store";

export default function McqTestPage() {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const [status, setStatus] = useState<McqTestStatus | null>(null);
  const [questions, setQuestions] = useState<McqQuestionForTest[]>([]);
  const [currentIndex, setCurrentIndex] = useState(1);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<McqSubmitResult | null>(null);
  const [showConfirmSubmit, setShowConfirmSubmit] = useState(false);
  const [remainingSeconds, setRemainingSeconds] = useState<number | null>(null);
  const [settings, setSettings] = useState({ allowNavigation: true, allowReview: true, oneQuestionPerPage: true });
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [s, q, info] = await Promise.all([
          mcqTestService.getStatus(),
          mcqTestService.getAllQuestions(),
          mcqTestService.getTestInfo()
        ]);
        if (s.isSubmitted) { navigate("/mcq-start", { replace: true }); return; }
        setStatus(s);
        setQuestions(q);
        setRemainingSeconds(s.remainingSeconds ?? null);
        setSettings({ allowNavigation: info.allowNavigation, allowReview: info.allowReview, oneQuestionPerPage: info.oneQuestionPerPage });
      } catch (err: any) {
        toast.error(err.response?.data?.errors?.[0] || "Failed to load test");
        navigate("/mcq-start", { replace: true });
      } finally { setLoading(false); }
    };
    load();
  }, [navigate]);

  // Timer
  useEffect(() => {
    if (remainingSeconds == null || remainingSeconds <= 0) return;
    timerRef.current = setInterval(() => {
      setRemainingSeconds((prev) => {
        if (prev == null || prev <= 1) { clearInterval(timerRef.current!); handleAutoSubmit(); return 0; }
        return prev - 1;
      });
    }, 1000);
    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [remainingSeconds != null]); // eslint-disable-line

  const handleAutoSubmit = async () => {
    try { const res = await mcqTestService.submitTest(true); setResult(res); } catch { toast.error("Auto-submit failed"); }
  };

  const currentQuestion = questions.find((q) => q.questionIndex === currentIndex);

  const handleSelectAnswer = useCallback(async (answer: string) => {
    if (!currentQuestion) return;
    const newAnswer = currentQuestion.selectedAnswer === answer ? null : answer;
    setQuestions((prev) => prev.map((q) => q.questionIndex === currentIndex ? { ...q, selectedAnswer: newAnswer ?? undefined } : q));
    try { await mcqTestService.saveAnswer({ questionId: currentQuestion.questionId, selectedAnswer: newAnswer }); }
    catch { setQuestions((prev) => prev.map((q) => q.questionIndex === currentIndex ? { ...q, selectedAnswer: currentQuestion.selectedAnswer } : q)); }
  }, [currentQuestion, currentIndex]);

  const handleFlag = useCallback(async () => {
    if (!currentQuestion) return;
    const newFlagged = !currentQuestion.isFlagged;
    setQuestions((prev) => prev.map((q) => q.questionIndex === currentIndex ? { ...q, isFlagged: newFlagged } : q));
    try { await mcqTestService.flagQuestion(currentQuestion.questionId, newFlagged); } catch {}
  }, [currentQuestion, currentIndex]);

  const handleSubmit = async () => {
    setShowConfirmSubmit(false);
    setSubmitting(true);
    try { const res = await mcqTestService.submitTest(false); setResult(res); if (timerRef.current) clearInterval(timerRef.current); }
    catch (err: any) { toast.error(err.response?.data?.errors?.[0] || "Submit failed"); }
    finally { setSubmitting(false); }
  };

  const handleLogout = () => { dispatch(logout()); navigate("/login", { replace: true }); };

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  };

  const answered = questions.filter((q) => q.selectedAnswer).length;
  const flagged = questions.filter((q) => q.isFlagged).length;
  const isTimeLow = remainingSeconds != null && remainingSeconds < 300;

  // ─── Result Screen ─────────────────────────────────────────────
  if (result) {
    return (
      <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-950">
        <header className="h-12 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center justify-between px-4 shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5"><Database className="h-4 w-4 text-white" /></div>
            <span className="font-semibold text-teal-800 dark:text-teal-300">NovacCodeLab</span>
          </div>
          <div className="flex items-center gap-3">
            <ThemeToggle />
            <button onClick={handleLogout} className="text-gray-400 hover:text-red-500 transition-colors" title="Logout"><LogOut className="h-4 w-4" /></button>
          </div>
        </header>
        <div className="flex-1 flex items-center justify-center p-4">
          <div className="max-w-sm w-full bg-white dark:bg-gray-900 rounded-xl shadow-sm border dark:border-gray-800 p-8 text-center">
            <CheckCircle className="h-12 w-12 text-green-500 mx-auto mb-4" />
            <h1 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">Test Submitted</h1>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">{result.message}</p>
            {result.showScores && (
              <div className="grid grid-cols-2 gap-2 mb-6 text-center">
                <Stat label="Score" value={`${result.score}/${result.maxScore}`} />
                <Stat label="Percentage" value={`${result.percentage}%`} />
                <Stat label="Correct" value={`${result.correct}`} cls="text-green-600" />
                <Stat label="Wrong" value={`${result.wrong}`} cls="text-red-600" />
              </div>
            )}
            <button onClick={handleLogout} className="w-full h-10 bg-teal-600 hover:bg-teal-700 text-white text-sm font-medium rounded-lg transition-colors">Done</button>
          </div>
        </div>
      </div>
    );
  }

  if (loading) return <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950"><Clock className="h-6 w-6 animate-spin text-teal-500" /></div>;

  // ─── Test Interface ────────────────────────────────────────────
  return (
    <div className="h-screen flex flex-col bg-gray-50 dark:bg-gray-950">
      {/* Header */}
      <header className="h-12 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center justify-between px-4 shrink-0">
        <div className="flex items-center gap-3">
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5"><Database className="h-4 w-4 text-white" /></div>
          <span className="font-semibold text-teal-800 dark:text-teal-300">NovacCodeLab</span>
          <span className="text-xs text-gray-400 dark:text-gray-500 hidden sm:inline">
            Q {currentIndex}/{questions.length}
            {currentQuestion?.complexity && <span className="ml-2 text-gray-500 dark:text-gray-400">{currentQuestion.complexity}{currentQuestion.marks > 0 ? ` • ${currentQuestion.marks}m` : ""}</span>}
          </span>
        </div>

        <div className="flex items-center gap-3">
          {/* Timer */}
          <div className={`flex items-center gap-1.5 px-2.5 py-1 rounded-md font-mono text-xs font-semibold ${isTimeLow ? "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" : "text-gray-600 dark:text-gray-300"}`}>
            <Clock className="h-3.5 w-3.5" />
            {remainingSeconds != null ? formatTime(remainingSeconds) : "∞"}
          </div>
          <span className="text-[11px] text-gray-400">{answered}/{questions.length}</span>
          {settings.allowReview && flagged > 0 && <span className="text-[11px] text-amber-600">{flagged} flagged</span>}
          <button onClick={() => setShowConfirmSubmit(true)} disabled={submitting} className="flex items-center gap-1.5 px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white rounded-md text-xs font-medium transition-colors">
            Submit
          </button>
          <ThemeToggle />
          <button onClick={handleLogout} className="text-gray-400 hover:text-red-500 transition-colors" title="Logout"><LogOut className="h-4 w-4" /></button>
        </div>
      </header>

      <div className="flex-1 flex overflow-hidden">
        {/* Main content */}
        <main className="flex-1 overflow-auto p-5">
          {settings.oneQuestionPerPage ? (
            currentQuestion && (
            <div className="max-w-2xl mx-auto">
              <div className="bg-white dark:bg-gray-900 rounded-lg border dark:border-gray-800 p-5 mb-5">
                <p className="text-sm text-gray-800 dark:text-gray-100 leading-relaxed whitespace-pre-wrap">{currentQuestion.question}</p>
              </div>
              <div className="space-y-2.5">
                {(["A", "B", "C", "D"] as const).map((opt) => {
                  const text = opt === "A" ? currentQuestion.optionA : opt === "B" ? currentQuestion.optionB : opt === "C" ? currentQuestion.optionC : currentQuestion.optionD;
                  const selected = currentQuestion.selectedAnswer === opt;
                  return (
                    <button key={opt} onClick={() => handleSelectAnswer(opt)}
                      className={`w-full text-left p-3.5 rounded-lg border transition-colors flex items-start gap-3 ${selected ? "border-teal-500 bg-teal-50 dark:bg-teal-900/20 dark:border-teal-400" : "border-gray-200 dark:border-gray-700 hover:border-teal-300 hover:bg-gray-50 dark:hover:bg-gray-800"}`}>
                      <span className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold shrink-0 ${selected ? "bg-teal-600 text-white" : "bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400"}`}>{opt}</span>
                      <span className="text-sm text-gray-700 dark:text-gray-200 pt-0.5">{text}</span>
                    </button>
                  );
                })}
              </div>
              <div className="flex items-center justify-between mt-5">
                {settings.allowReview ? (
                  <button onClick={handleFlag} className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${currentQuestion.isFlagged ? "bg-amber-100 text-amber-700 dark:bg-amber-900/20 dark:text-amber-400" : "text-gray-500 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/10"}`}>
                    <Flag className="h-3.5 w-3.5" />{currentQuestion.isFlagged ? "Flagged" : "Flag"}
                  </button>
                ) : <div />}
                <div className="flex gap-2">
                  {settings.allowNavigation && (
                    <button onClick={() => setCurrentIndex((i) => Math.max(1, i - 1))} disabled={currentIndex <= 1} className="px-3 py-1.5 rounded-md text-xs font-medium bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-40 flex items-center gap-1">
                      <ChevronLeft className="h-3.5 w-3.5" /> Prev
                    </button>
                  )}
                  <button onClick={() => setCurrentIndex((i) => Math.min(questions.length, i + 1))} disabled={currentIndex >= questions.length} className="px-3 py-1.5 rounded-md text-xs font-medium bg-teal-50 dark:bg-teal-900/20 text-teal-700 dark:text-teal-300 hover:bg-teal-100 dark:hover:bg-teal-900/30 disabled:opacity-40 flex items-center gap-1">
                    Next <ChevronRight className="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>
            </div>)
          ) : (
            <div className="max-w-2xl mx-auto space-y-5">
              {questions.map((q) => (
                <div key={q.questionId} className="bg-white dark:bg-gray-900 rounded-lg border dark:border-gray-800 p-5">
                  <div className="flex items-center gap-2 mb-3">
                    <span className="text-[11px] font-semibold text-teal-600 dark:text-teal-400">Q{q.questionIndex}</span>
                    {q.complexity && <span className="text-[10px] text-gray-400">{q.complexity}{q.marks > 0 ? ` • ${q.marks}m` : ""}</span>}
                  </div>
                  <p className="text-sm text-gray-800 dark:text-gray-100 leading-relaxed whitespace-pre-wrap mb-3">{q.question}</p>
                  <div className="space-y-2">
                    {(["A", "B", "C", "D"] as const).map((opt) => {
                      const text = opt === "A" ? q.optionA : opt === "B" ? q.optionB : opt === "C" ? q.optionC : q.optionD;
                      const selected = q.selectedAnswer === opt;
                      return (
                        <button key={opt} onClick={() => {
                          const newAns = q.selectedAnswer === opt ? null : opt;
                          setQuestions((prev) => prev.map((x) => x.questionId === q.questionId ? { ...x, selectedAnswer: newAns ?? undefined } : x));
                          mcqTestService.saveAnswer({ questionId: q.questionId, selectedAnswer: newAns }).catch(() => { setQuestions((prev) => prev.map((x) => x.questionId === q.questionId ? { ...x, selectedAnswer: q.selectedAnswer } : x)); });
                        }}
                          className={`w-full text-left p-3 rounded-md border transition-colors flex items-start gap-2.5 ${selected ? "border-teal-500 bg-teal-50 dark:bg-teal-900/20" : "border-gray-200 dark:border-gray-700 hover:border-teal-300"}`}>
                          <span className={`w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-bold shrink-0 ${selected ? "bg-teal-600 text-white" : "bg-gray-100 dark:bg-gray-800 text-gray-500"}`}>{opt}</span>
                          <span className="text-sm text-gray-700 dark:text-gray-200">{text}</span>
                        </button>
                      );
                    })}
                  </div>
                  {settings.allowReview && (
                    <button onClick={() => { const f = !q.isFlagged; setQuestions((p) => p.map((x) => x.questionId === q.questionId ? { ...x, isFlagged: f } : x)); mcqTestService.flagQuestion(q.questionId, f).catch(() => {}); }}
                      className={`mt-2 flex items-center gap-1 text-[11px] px-2 py-1 rounded transition-colors ${q.isFlagged ? "text-amber-700 bg-amber-100 dark:bg-amber-900/20 dark:text-amber-400" : "text-gray-400 hover:text-amber-600"}`}>
                      <Flag className="h-3 w-3" />{q.isFlagged ? "Flagged" : "Flag"}
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </main>

        {/* Sidebar nav */}
        {settings.allowNavigation && (
          <aside className="w-56 bg-white dark:bg-gray-900 border-l dark:border-gray-800 p-3 overflow-auto shrink-0">
            <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Navigation</p>
            <div className="grid grid-cols-5 gap-1.5">
              {questions.map((q) => (
                <button key={q.questionIndex} onClick={() => setCurrentIndex(q.questionIndex)}
                  className={`w-8 h-8 rounded text-[10px] font-bold flex items-center justify-center relative transition-colors ${
                    q.questionIndex === currentIndex ? "ring-2 ring-teal-500 bg-teal-50 text-teal-700 dark:bg-teal-900/30 dark:text-teal-300" :
                    q.selectedAnswer ? "bg-green-100 text-green-700 dark:bg-green-900/20 dark:text-green-400" :
                    "bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400 hover:bg-gray-200"
                  }`}>
                  {q.questionIndex}
                  {q.isFlagged && <span className="absolute -top-0.5 -right-0.5 w-2 h-2 bg-amber-400 rounded-full" />}
                </button>
              ))}
            </div>
            <div className="mt-4 space-y-1.5 text-[10px] text-gray-400">
              <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-green-100 border border-green-300" />Answered</div>
              <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-gray-100 border border-gray-300" />Unanswered</div>
              {settings.allowReview && <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded bg-gray-100 border border-gray-300 relative"><span className="absolute -top-px -right-px w-1.5 h-1.5 bg-amber-400 rounded-full" /></span>Flagged</div>}
            </div>
          </aside>
        )}
      </div>

      {/* Confirm Submit */}
      {showConfirmSubmit && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-800 p-6 max-w-xs w-full mx-4 shadow-lg">
            <div className="flex items-start gap-3 mb-4">
              <AlertTriangle className="h-5 w-5 text-amber-500 shrink-0 mt-0.5" />
              <div>
                <h3 className="text-sm font-semibold text-gray-900 dark:text-white">Submit Test?</h3>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  {questions.length - answered} unanswered.
                  {settings.allowReview && flagged > 0 && ` ${flagged} flagged.`}
                  {" "}Cannot be undone.
                </p>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button onClick={() => setShowConfirmSubmit(false)} className="px-3 py-1.5 text-xs text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">Cancel</button>
              <button onClick={handleSubmit} className="px-3 py-1.5 text-xs font-medium bg-green-600 text-white rounded-md hover:bg-green-700">Submit</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Stat({ label, value, cls }: { label: string; value: string; cls?: string }) {
  return (
    <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-2.5">
      <p className={`text-base font-bold ${cls || "text-gray-900 dark:text-white"}`}>{value}</p>
      <p className="text-[10px] text-gray-500 dark:text-gray-400">{label}</p>
    </div>
  );
}
