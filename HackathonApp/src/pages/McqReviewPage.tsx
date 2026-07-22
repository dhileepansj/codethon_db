import { useState, useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { ChevronLeft, ChevronRight, Search, ArrowLeft, CheckCircle, XCircle, MinusCircle, Database } from "lucide-react";
import { toast } from "sonner";
import httpClient from "@/services/httpClient";
import ThemeToggle from "@/components/common/ThemeToggle";

interface Respondent {
  userLoginId: string;
  fullName?: string;
  score: number;
  maxScore: number;
  percentage: number;
}

interface ReviewData {
  userLoginId: string;
  fullName?: string;
  score: number;
  maxScore: number;
  percentage: number;
  correct: number;
  wrong: number;
  skipped: number;
  totalQuestions: number;
  timeSpentSeconds?: number;
  submittedAt?: string;
  passed?: boolean;
  questions: ReviewQuestion[];
}

interface ReviewQuestion {
  questionIndex: number;
  question: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  correctAnswer: string;
  selectedAnswer?: string;
  isCorrect?: boolean;
  marksAwarded: number;
  complexity: string;
  category?: string;
  explanation?: string;
}

export default function McqReviewPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const assessmentId = searchParams.get("assessmentId");
  const initialUser = searchParams.get("user");

  const [respondents, setRespondents] = useState<Respondent[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [review, setReview] = useState<ReviewData | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [filter, setFilter] = useState<"all" | "correct" | "wrong" | "skipped">("all");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!assessmentId) { navigate("/admin", { replace: true }); return; }
    httpClient.get(`/api/mcq/assessments/${assessmentId}/respondents`)
      .then((res) => {
        const list = res.data.data || res.data;
        setRespondents(list);
        if (initialUser) {
          const idx = list.findIndex((r: Respondent) => r.userLoginId === initialUser.toUpperCase());
          if (idx >= 0) setCurrentIndex(idx);
        }
      })
      .catch(() => toast.error("Failed to load respondents"))
      .finally(() => setLoading(false));
  }, [assessmentId, initialUser, navigate]);

  useEffect(() => {
    if (respondents.length === 0 || !assessmentId) return;
    const user = respondents[currentIndex];
    if (!user) return;
    setReview(null);
    httpClient.get(`/api/mcq/assessments/${assessmentId}/review/${user.userLoginId}`)
      .then((res) => setReview(res.data.data || res.data))
      .catch(() => toast.error("Failed to load review"));
  }, [currentIndex, respondents, assessmentId]);

  const goToUser = (userId: string) => {
    const idx = respondents.findIndex((r) => r.userLoginId.toUpperCase() === userId.toUpperCase());
    if (idx >= 0) { setCurrentIndex(idx); setSearchQuery(""); }
    else toast.error("User not found");
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) goToUser(searchQuery.trim());
  };

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}m ${s}s`;
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950"><p className="text-gray-400">Loading...</p></div>;
  if (respondents.length === 0) return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950">
      <div className="text-center">
        <p className="text-gray-400 mb-4">No submissions yet</p>
        <button onClick={() => navigate("/admin")} className="text-teal-600 text-sm hover:underline">← Back to Admin</button>
      </div>
    </div>
  );

  const current = respondents[currentIndex];

  return (
    <div className="h-screen bg-gray-50 dark:bg-gray-950 flex flex-col">
      {/* Header — sticky */}
      <header className="h-12 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center justify-between px-4 shrink-0">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate("/admin")} className="flex items-center gap-1 text-gray-500 hover:text-teal-600 text-xs"><ArrowLeft className="h-3.5 w-3.5" /> Admin</button>
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5"><Database className="h-4 w-4 text-white" /></div>
          <span className="font-semibold text-teal-800 dark:text-teal-300 text-sm">MCQ Review</span>
        </div>
        <div className="flex items-center gap-3">
          <ThemeToggle />
        </div>
      </header>

      {/* Navigation bar — sticky */}
      <div className="border-b bg-white dark:bg-gray-900 dark:border-gray-700 px-4 py-3 flex items-center justify-between shrink-0">
        <div className="flex items-center gap-4">
          <button onClick={() => setCurrentIndex((i) => Math.max(0, i - 1))} disabled={currentIndex <= 0} className="p-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-30">
            <ChevronLeft className="h-4 w-4 text-gray-600 dark:text-gray-300" />
          </button>
          <div className="text-center min-w-[200px]">
            <p className="text-sm font-semibold text-gray-900 dark:text-white">{current?.userLoginId}</p>
            <p className="text-[11px] text-gray-500">{current?.fullName || "—"} · {currentIndex + 1} of {respondents.length}</p>
          </div>
          <button onClick={() => setCurrentIndex((i) => Math.min(respondents.length - 1, i + 1))} disabled={currentIndex >= respondents.length - 1} className="p-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-30">
            <ChevronRight className="h-4 w-4 text-gray-600 dark:text-gray-300" />
          </button>
        </div>

        {/* Score summary */}
        {review && (
          <div className="flex items-center gap-4 text-xs text-gray-600 dark:text-gray-400">
            <span className="font-mono font-semibold text-gray-900 dark:text-white">{review.score}/{review.maxScore} ({review.percentage}%)</span>
            <span className="text-green-600">✓ {review.correct}</span>
            <span className="text-red-600">✗ {review.wrong}</span>
            <span className="text-gray-400">— {review.skipped}</span>
            {review.timeSpentSeconds && <span>{formatTime(review.timeSpentSeconds)}</span>}
          </div>
        )}

        {/* Filters + Search */}
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-0.5 bg-gray-100 dark:bg-gray-800 rounded-md p-0.5">
            {([["all", "All"], ["correct", "✓"], ["wrong", "✗"], ["skipped", "—"]] as const).map(([key, label]) => (
              <button key={key} onClick={() => setFilter(key)} className={`px-2 py-1 text-[10px] font-medium rounded transition-colors ${filter === key ? "bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm" : "text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"}`}>
                {label}
              </button>
            ))}
          </div>
          <form onSubmit={handleSearch} className="flex items-center gap-1">
            <input value={searchQuery} onChange={(e) => setSearchQuery(e.target.value.toUpperCase())} placeholder="User ID" className="w-28 px-2.5 py-1.5 border dark:border-gray-700 rounded-md text-xs dark:bg-gray-800 dark:text-white uppercase" />
            <button type="submit" className="p-1.5 text-gray-500 hover:text-teal-600 hover:bg-teal-50 dark:hover:bg-teal-900/20 rounded-md"><Search className="h-3.5 w-3.5" /></button>
          </form>
        </div>
      </div>

      {/* Questions */}
      <main className="flex-1 overflow-auto p-5">
        {!review ? (
          <p className="text-center text-gray-400 py-12">Loading answers...</p>
        ) : (
          <div className="max-w-3xl mx-auto space-y-4">
            {review.questions
              .filter((q) => filter === "all" ? true : filter === "correct" ? q.isCorrect === true : filter === "wrong" ? q.isCorrect === false : q.isCorrect == null)
              .map((q) => (
              <div key={q.questionIndex} className={`bg-white dark:bg-gray-900 rounded-lg border p-5 ${q.isCorrect === true ? "border-green-200 dark:border-green-800/40" : q.isCorrect === false ? "border-red-200 dark:border-red-800/40" : "border-gray-200 dark:border-gray-800"}`}>
                {/* Question header */}
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-bold text-gray-500 dark:text-gray-400">Q{q.questionIndex}</span>
                    {q.complexity && <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium ${q.complexity === "Simple" ? "bg-green-100 text-green-700" : q.complexity === "Medium" ? "bg-amber-100 text-amber-700" : "bg-red-100 text-red-700"}`}>{q.complexity}</span>}
                    {q.category && <span className="text-[10px] text-gray-400">{q.category}</span>}
                  </div>
                  <div className="flex items-center gap-1.5">
                    {q.isCorrect === true && <CheckCircle className="h-4 w-4 text-green-500" />}
                    {q.isCorrect === false && <XCircle className="h-4 w-4 text-red-500" />}
                    {q.isCorrect == null && <MinusCircle className="h-4 w-4 text-gray-400" />}
                    <span className="text-[10px] font-mono text-gray-500">{q.marksAwarded > 0 ? `+${q.marksAwarded}` : q.marksAwarded < 0 ? `${q.marksAwarded}` : "0"}</span>
                  </div>
                </div>

                {/* Question text */}
                <p className="text-sm text-gray-800 dark:text-gray-100 leading-relaxed whitespace-pre-wrap mb-3">{q.question}</p>

                {/* Options */}
                <div className="space-y-1.5">
                  {(["A", "B", "C", "D"] as const).map((opt) => {
                    const text = opt === "A" ? q.optionA : opt === "B" ? q.optionB : opt === "C" ? q.optionC : q.optionD;
                    const isSelected = q.selectedAnswer === opt;
                    const isCorrect = q.correctAnswer === opt;

                    let borderClass = "border-gray-200 dark:border-gray-700";
                    let bgClass = "";
                    if (isCorrect) { borderClass = "border-green-400 dark:border-green-600"; bgClass = "bg-green-50 dark:bg-green-900/10"; }
                    if (isSelected && !isCorrect) { borderClass = "border-red-400 dark:border-red-600"; bgClass = "bg-red-50 dark:bg-red-900/10"; }
                    if (isSelected && isCorrect) { borderClass = "border-green-500 dark:border-green-500"; bgClass = "bg-green-50 dark:bg-green-900/20"; }

                    return (
                      <div key={opt} className={`flex items-start gap-2.5 p-2.5 rounded-md border ${borderClass} ${bgClass}`}>
                        <span className={`w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-bold shrink-0 ${
                          isCorrect ? "bg-green-500 text-white" : isSelected ? "bg-red-500 text-white" : "bg-gray-100 dark:bg-gray-800 text-gray-500"
                        }`}>{opt}</span>
                        <span className="text-xs text-gray-700 dark:text-gray-200 pt-0.5 flex-1">{text}</span>
                        {isCorrect && <span className="text-[9px] text-green-600 font-medium shrink-0">Correct</span>}
                        {isSelected && !isCorrect && <span className="text-[9px] text-red-600 font-medium shrink-0">Selected</span>}
                      </div>
                    );
                  })}
                </div>

                {/* Explanation */}
                {q.explanation && (
                  <div className="mt-3 px-3 py-2 bg-blue-50 dark:bg-blue-900/10 border border-blue-200 dark:border-blue-800/30 rounded-md">
                    <p className="text-[11px] text-blue-700 dark:text-blue-300">{q.explanation}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
