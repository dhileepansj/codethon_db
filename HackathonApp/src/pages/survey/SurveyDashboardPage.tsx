import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Download, BarChart3, Users, CheckCircle, Clock, XCircle, Bell } from 'lucide-react';
import { toast } from 'sonner';
import { dashboardApi } from '../../services/surveyApi';
import { SurveyDashboardDto, SurveyResponseDto, FieldAnalyticsDto } from '../../types/survey';
import SurveyAdminLayout from '../../components/survey/SurveyAdminLayout';

export default function SurveyDashboardPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const navigate = useNavigate();
  const [dashboard, setDashboard] = useState<SurveyDashboardDto | null>(null);
  const [responses, setResponses] = useState<SurveyResponseDto[]>([]);
  const [analytics, setAnalytics] = useState<FieldAnalyticsDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'overview' | 'responses' | 'analytics'>('overview');
  const [selectedResponse, setSelectedResponse] = useState<SurveyResponseDto | null>(null);

  useEffect(() => { if (surveyId) loadDashboard(); }, [surveyId]);

  async function loadDashboard() { try { const [d, r, a] = await Promise.all([dashboardApi.getSummary(surveyId!), dashboardApi.getResponses(surveyId!), dashboardApi.getAnalytics(surveyId!)]); setDashboard(d); setResponses(r); setAnalytics(a); } catch { toast.error('Failed to load dashboard'); } finally { setLoading(false); } }
  async function handleExport(format: "csv" | "excel" = "csv") {
    try {
      if (format === "excel") {
        const res = await fetch(`${import.meta.env.VITE_API_BASE_URL || ""}/hackathonapi/api/surveys/${surveyId}/dashboard/export-excel`, { headers: { Authorization: `Bearer ${sessionStorage.getItem("token")}` } });
        if (!res.ok) { toast.error('Export failed'); return; }
        const blob = await res.blob();
        const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = `survey_responses.xlsx`; a.click(); URL.revokeObjectURL(url);
      } else {
        const blob = await dashboardApi.export(surveyId!); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = `survey_responses.csv`; a.click(); URL.revokeObjectURL(url);
      }
      toast.success('Export downloaded');
    } catch { toast.error('Export failed'); }
  }
  async function handleViewResponse(responseId: string) { try { const detail = await dashboardApi.getResponseDetail(surveyId!, responseId); setSelectedResponse(detail); } catch { toast.error('Failed to load response'); } }

  if (loading) return <SurveyAdminLayout><div className="flex items-center justify-center h-64 text-gray-500">Loading dashboard...</div></SurveyAdminLayout>;
  if (!dashboard) return <SurveyAdminLayout><div className="flex items-center justify-center h-64 text-red-500">Survey not found</div></SurveyAdminLayout>;

  return (
    <SurveyAdminLayout>
      <div className="flex-1 overflow-y-auto bg-gray-50 dark:bg-gray-950">
        <header className="h-14 bg-white dark:bg-gray-900 border-b dark:border-gray-800 flex items-center justify-between px-6 sticky top-0 z-10">
          <div><h1 className="text-base font-semibold text-gray-800 dark:text-gray-100">{dashboard.title}</h1><p className="text-xs text-gray-500">Survey Dashboard</p></div>
          <div className="flex items-center gap-2">
            <button onClick={() => handleExport("excel")} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs bg-emerald-600 hover:bg-emerald-700 text-white rounded-md font-medium"><Download className="w-3.5 h-3.5" /> Export Excel</button>
            <button onClick={() => handleExport("csv")} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs bg-gray-500 hover:bg-gray-600 text-white rounded-md font-medium"><Download className="w-3.5 h-3.5" /> Export CSV</button>
          </div>
        </header>

        <div className="p-6 max-w-7xl mx-auto">
          {/* Summary Cards */}
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-6">
            <StatCard icon={<Users className="w-5 h-5" />} label="Total" value={dashboard.totalParticipants} color="text-gray-600" />
            <StatCard icon={<CheckCircle className="w-5 h-5" />} label="Responded" value={dashboard.responded} color="text-green-600" />
            <StatCard icon={<Clock className="w-5 h-5" />} label="Pending" value={dashboard.pending} color="text-blue-600" />
            <StatCard icon={<Bell className="w-5 h-5" />} label="Reminded" value={dashboard.reminded} color="text-amber-600" />
            <StatCard icon={<XCircle className="w-5 h-5" />} label="Declined" value={dashboard.declined} color="text-red-600" />
            <StatCard icon={<BarChart3 className="w-5 h-5" />} label="Rate" value={`${dashboard.responseRate}%`} color="text-purple-600" />
          </div>

          {/* Progress */}
          <div className="mb-6 bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-4">
            <div className="flex items-center justify-between mb-2"><span className="text-sm text-gray-500">Response Progress</span><span className="text-sm text-gray-800 dark:text-gray-100 font-semibold">{dashboard.responseRate}%</span></div>
            <div className="w-full h-2.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden"><div className="h-full bg-teal-500 rounded-full transition-all" style={{ width: `${dashboard.responseRate}%` }} /></div>
          </div>

          {/* Tabs */}
          <div className="flex gap-1 mb-4 border-b dark:border-gray-800 pb-px">
            {(['overview', 'responses', 'analytics'] as const).map((tab) => (
              <button key={tab} onClick={() => setActiveTab(tab)} className={`px-4 py-2 text-sm font-medium rounded-t-lg transition-colors ${activeTab === tab ? 'text-gray-800 dark:text-gray-100 bg-white dark:bg-gray-900 border dark:border-gray-800 border-b-transparent' : 'text-gray-500 hover:text-gray-800'}`}>{tab.charAt(0).toUpperCase() + tab.slice(1)}</button>
            ))}
          </div>

          {activeTab === 'responses' && (
            <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl overflow-hidden">
              <table className="w-full text-sm">
                <thead><tr className="border-b dark:border-gray-800 text-[11px] font-semibold text-gray-500 uppercase tracking-wider"><th className="px-4 py-3 text-left">Employee</th><th className="px-4 py-3 text-left">Email</th><th className="px-4 py-3 text-left">Submitted</th><th className="px-4 py-3 text-left">Time Taken</th><th className="px-4 py-3 text-left">Actions</th></tr></thead>
                <tbody className="divide-y dark:divide-gray-800">
                  {responses.map((r) => (
                    <tr key={r.id} className="hover:bg-teal-50/40 dark:hover:bg-teal-900/5">
                      <td className="px-4 py-3 text-gray-800 dark:text-gray-100 font-medium">{r.employeeName || '—'}</td>
                      <td className="px-4 py-3 text-gray-500">{r.employeeEmail || '—'}</td>
                      <td className="px-4 py-3 text-gray-500">{new Date(r.submittedAt).toLocaleString()}</td>
                      <td className="px-4 py-3 text-gray-500 font-mono">{r.timeTakenSeconds ? `${Math.floor(r.timeTakenSeconds / 60)}m ${r.timeTakenSeconds % 60}s` : '—'}</td>
                      <td className="px-4 py-3"><button onClick={() => handleViewResponse(r.id)} className="text-xs text-teal-600 hover:text-teal-700 font-medium">View</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {responses.length === 0 && <div className="text-center py-12 text-gray-400">No responses yet</div>}
            </div>
          )}

          {activeTab === 'analytics' && (
            <div className="space-y-4">
              {analytics.map((field) => (
                <div key={field.fieldId} className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-5">
                  <h3 className="text-sm font-medium text-gray-800 dark:text-gray-100 mb-1">{field.label}</h3>
                  <p className="text-xs text-gray-400 mb-3">{field.totalAnswers} responses · {field.fieldType}</p>
                  {field.optionBreakdown && <div className="space-y-2">{field.optionBreakdown.map((opt) => (<div key={opt.option} className="flex items-center gap-3"><span className="text-xs text-gray-600 w-32 truncate">{opt.option}</span><div className="flex-1 h-4 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden"><div className="h-full bg-teal-500 rounded-full" style={{ width: `${opt.percentage}%` }} /></div><span className="text-xs text-gray-500 w-16 text-right">{opt.count} ({opt.percentage}%)</span></div>))}</div>}
                  {field.averageValue !== undefined && field.averageValue !== null && <div className="text-2xl font-bold text-gray-800 dark:text-gray-100">{field.averageValue}</div>}
                  {field.textResponses && <div className="space-y-1 max-h-40 overflow-y-auto">{field.textResponses.map((text, i) => <p key={i} className="text-xs text-gray-600 py-1 border-b border-gray-100 dark:border-gray-800">{text}</p>)}</div>}
                </div>
              ))}
              {analytics.length === 0 && <div className="text-center py-12 text-gray-400">No analytics data available yet</div>}
            </div>
          )}

          {activeTab === 'overview' && (
            <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-6 text-center">
              <BarChart3 className="w-12 h-12 mx-auto mb-3 text-gray-300" />
              <p className="text-gray-500">Survey overview will appear here as responses come in.</p>
              <p className="text-sm text-gray-400 mt-2">{dashboard.responded} of {dashboard.totalParticipants} participants have responded.</p>
            </div>
          )}

          {selectedResponse && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
              <div className="bg-white dark:bg-gray-800 shadow-2xl border dark:border-gray-700 rounded-xl p-6 w-full max-w-lg max-h-[80vh] overflow-y-auto">
                <div className="flex items-center justify-between mb-4"><div><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{selectedResponse.employeeName}</h2><p className="text-sm text-gray-500">{selectedResponse.employeeEmail}</p></div><button onClick={() => setSelectedResponse(null)} className="text-gray-400 hover:text-gray-600 text-xl">✕</button></div>
                <div className="space-y-3">{selectedResponse.answers.map((ans, i) => (<div key={i} className="p-3 bg-gray-50 dark:bg-gray-900 rounded-lg"><p className="text-xs text-gray-400">{ans.fieldLabel} ({ans.fieldType})</p><p className="text-sm text-gray-800 dark:text-gray-200 mt-1">{ans.value || '—'}</p></div>))}</div>
              </div>
            </div>
          )}
        </div>
      </div>
    </SurveyAdminLayout>
  );
}

function StatCard({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: number | string; color: string }) {
  return (
    <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-4 text-center">
      <div className={`mx-auto mb-2 ${color}`}>{icon}</div>
      <p className="text-xl font-bold text-gray-800 dark:text-gray-100">{value}</p>
      <p className="text-xs text-gray-500">{label}</p>
    </div>
  );
}
