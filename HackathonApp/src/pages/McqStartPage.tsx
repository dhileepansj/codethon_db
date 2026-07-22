import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { ClipboardList, Loader2, LogOut, Clock, Award, AlertTriangle, CheckCircle, Database } from "lucide-react";
import { toast } from "sonner";
import { mcqTestService } from "@/services/mcqService";
import { logout } from "@/redux/slices/authSlice";
import { useTheme } from "@/contexts/ThemeContext";
import ThemeToggle from "@/components/common/ThemeToggle";
import type { AppDispatch } from "@/redux/store";
import type { McqTestInfo } from "@/types";

export default function McqStartPage() {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const [info, setInfo] = useState<McqTestInfo | null>(null);
  const [isStarting, setIsStarting] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    mcqTestService.getTestInfo()
      .then(setInfo)
      .catch((err) => toast.error(err.response?.data?.errors?.[0] || "Failed to load test info"))
      .finally(() => setLoading(false));
  }, []);

  const handleStart = async () => {
    setIsStarting(true);
    try {
      await mcqTestService.startTest();
      navigate("/mcq-test", { replace: true });
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || err.response?.data?.message || "Failed to start test");
    } finally {
      setIsStarting(false);
    }
  };

  const handleResume = () => navigate("/mcq-test", { replace: true });

  const handleLogout = () => {
    dispatch(logout());
    navigate("/login", { replace: true });
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950">
        <Loader2 className="h-6 w-6 animate-spin text-teal-500" />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-950">
      {/* Header — matches app standard */}
      <header className="h-12 border-b bg-white dark:bg-gray-900 dark:border-gray-700 flex items-center justify-between px-4 shrink-0">
        <div className="flex items-center gap-3">
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5">
            <Database className="h-4 w-4 text-white" />
          </div>
          <span className="font-semibold text-teal-800 dark:text-teal-300">NovacCodeLab</span>
        </div>
        <div className="flex items-center gap-3">
          <ThemeToggle />
          <button onClick={handleLogout} className="text-gray-400 hover:text-red-500 transition-colors" title="Logout">
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </header>

      {/* Content */}
      <div className="flex-1 flex items-center justify-center p-4">
        <div className="max-w-lg w-full bg-white dark:bg-gray-900 rounded-xl shadow-sm border dark:border-gray-800 p-8">
          {/* Title */}
          <div className="text-center mb-6">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-lg bg-teal-50 dark:bg-teal-900/20 mb-3">
              <ClipboardList className="h-6 w-6 text-teal-600 dark:text-teal-400" />
            </div>
            <h1 className="text-xl font-semibold text-gray-900 dark:text-white">{info?.title || "MCQ Test"}</h1>
          </div>

          {info && (
            <div className="space-y-4 mb-6">
              {/* Info row */}
              <div className={`grid ${info.showMarks ? "grid-cols-3" : "grid-cols-2"} gap-3`}>
                <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 text-center">
                  <p className="text-lg font-bold text-gray-900 dark:text-white">{info.totalQuestions}</p>
                  <p className="text-[11px] text-gray-500 dark:text-gray-400">Questions</p>
                </div>
                <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 text-center">
                  <p className="text-lg font-bold text-gray-900 dark:text-white">{info.durationMinutes} min</p>
                  <p className="text-[11px] text-gray-500 dark:text-gray-400">Duration</p>
                </div>
                {info.showMarks && (
                  <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 text-center">
                    <p className="text-lg font-bold text-gray-900 dark:text-white">{info.maxMarks}</p>
                    <p className="text-[11px] text-gray-500 dark:text-gray-400">Max Marks</p>
                  </div>
                )}
              </div>

              {/* Distribution */}
              {(info.showComplexity || info.showMarks) && (
                <div className="border dark:border-gray-700 rounded-lg p-4">
                  <p className="text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Distribution</p>
                  <div className="space-y-1.5 text-sm">
                    <div className="flex justify-between text-gray-700 dark:text-gray-300">
                      <span>{info.showComplexity ? "Simple" : "Section 1"}{info.showMarks ? ` (${info.simpleMarks}m)` : ""}</span>
                      <span className="font-mono text-xs">{info.simpleCount} Qs</span>
                    </div>
                    <div className="flex justify-between text-gray-700 dark:text-gray-300">
                      <span>{info.showComplexity ? "Medium" : "Section 2"}{info.showMarks ? ` (${info.mediumMarks}m)` : ""}</span>
                      <span className="font-mono text-xs">{info.mediumCount} Qs</span>
                    </div>
                    <div className="flex justify-between text-gray-700 dark:text-gray-300">
                      <span>{info.showComplexity ? "Complex" : "Section 3"}{info.showMarks ? ` (${info.complexMarks}m)` : ""}</span>
                      <span className="font-mono text-xs">{info.complexCount} Qs</span>
                    </div>
                  </div>
                </div>
              )}

              {/* Rules */}
              <div className="bg-amber-50 dark:bg-amber-900/10 border border-amber-200 dark:border-amber-800/30 rounded-lg p-3">
                <div className="flex items-start gap-2">
                  <AlertTriangle className="h-4 w-4 text-amber-600 dark:text-amber-400 mt-0.5 shrink-0" />
                  <div className="text-xs text-amber-800 dark:text-amber-300 space-y-0.5">
                    {info.negativeMarking && <p>Negative marking: -{info.negativeMarkValue} per wrong answer</p>}
                    {!info.negativeMarking && <p>No negative marking</p>}
                    {info.allowNavigation && <p>Navigate freely between questions</p>}
                    {!info.allowNavigation && <p>Linear mode — answer in sequence</p>}
                    <p>Auto-submits when time expires</p>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Action */}
          {info?.isAlreadySubmitted ? (
            <div className="border border-green-200 dark:border-green-800/30 bg-green-50 dark:bg-green-900/10 rounded-lg p-5 text-center">
              <CheckCircle className="h-8 w-8 text-green-500 mx-auto mb-2" />
              <p className="text-sm font-medium text-green-800 dark:text-green-300">Test Already Submitted</p>
              <p className="text-xs text-green-600 dark:text-green-400 mt-1">You have completed this test.</p>
              {info.submittedScore != null && info.submittedMaxScore != null && (
                <p className="text-lg font-bold text-green-700 dark:text-green-300 mt-2">
                  {info.submittedScore}/{info.submittedMaxScore} ({info.submittedPercentage}%)
                </p>
              )}
            </div>
          ) : info?.hasExistingTest ? (
            <button onClick={handleResume} className="w-full h-10 bg-amber-500 hover:bg-amber-600 text-white font-medium text-sm rounded-lg transition-colors">
              Resume Test
            </button>
          ) : (
            <button onClick={handleStart} disabled={isStarting} className="w-full h-10 bg-teal-600 hover:bg-teal-700 text-white font-medium text-sm rounded-lg transition-colors disabled:opacity-50 flex items-center justify-center gap-2">
              {isStarting ? (<><Loader2 className="h-4 w-4 animate-spin" /> Starting...</>) : "Start Test"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
