import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Copy, Trash2, BarChart3, FileEdit, Users } from 'lucide-react';
import { toast } from 'sonner';
import { surveyApi } from '../../services/surveyApi';
import { SurveyDto, SurveyStatus } from '../../types/survey';
import SurveyAdminLayout from '../../components/survey/SurveyAdminLayout';
import { ConfirmDialog } from '../../components/common/CustomDialog';

const STATUS_LABELS: Record<SurveyStatus, string> = {
  [SurveyStatus.Draft]: 'Draft',
  [SurveyStatus.Active]: 'Active',
  [SurveyStatus.Closed]: 'Closed',
  [SurveyStatus.Archived]: 'Archived',
};

const STATUS_COLORS: Record<SurveyStatus, string> = {
  [SurveyStatus.Draft]: 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300',
  [SurveyStatus.Active]: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  [SurveyStatus.Closed]: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  [SurveyStatus.Archived]: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
};

export default function SurveyListPage() {
  const [surveys, setSurveys] = useState<SurveyDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    loadSurveys();
  }, []);

  async function loadSurveys() {
    try {
      const data = await surveyApi.getAll();
      setSurveys(Array.isArray(data) ? data : []);
    } catch {
      toast.error('Failed to load surveys');
    } finally {
      setLoading(false);
    }
  }

  async function handleCreate() {
    if (!newTitle.trim()) return;
    try {
      const survey = await surveyApi.create({ title: newTitle.trim() });
      toast.success('Survey created');
      setShowCreate(false);
      setNewTitle('');
      navigate(`/admin/surveys/${survey.id}/build`);
    } catch {
      toast.error('Failed to create survey');
    }
  }

  async function handleClone(id: string) {
    try {
      const cloned = await surveyApi.clone(id);
      toast.success('Survey cloned');
      setSurveys((prev) => [cloned, ...prev]);
    } catch {
      toast.error('Failed to clone survey');
    }
  }

  async function handleDelete(id: string) {
    try {
      await surveyApi.delete(id);
      toast.success('Survey deleted');
      setSurveys((prev) => prev.filter((s) => s.id !== id));
    } catch {
      toast.error('Failed to delete survey');
    } finally {
      setDeleteId(null);
    }
  }

  if (loading) {
    return (
      <SurveyAdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="text-gray-500">Loading surveys...</div>
        </div>
      </SurveyAdminLayout>
    );
  }

  return (
    <SurveyAdminLayout>
      <div className="flex-1 overflow-y-auto bg-gray-50 dark:bg-gray-950">
        {/* Top Header Bar */}
        <header className="h-14 bg-white dark:bg-gray-900 border-b dark:border-gray-800 flex items-center justify-between px-6 sticky top-0 z-10">
          <div>
            <h1 className="text-base font-semibold text-gray-800 dark:text-gray-100">Surveys</h1>
            <p className="text-xs text-gray-500">Create and manage surveys for your team</p>
          </div>
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-2 px-3 py-1.5 bg-teal-600 hover:bg-teal-700 text-white text-sm font-medium rounded-md transition-colors"
          >
            <Plus className="w-4 h-4" />
            New Survey
          </button>
        </header>

        {/* Content */}
        <div className="p-6 max-w-7xl mx-auto">
          {/* Create Modal */}
          {showCreate && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
              <div className="bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-xl p-6 w-full max-w-md shadow-2xl">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Create New Survey</h2>
                <label className="text-sm text-gray-600 dark:text-gray-400 mb-1 block">Survey Title</label>
                <input
                  type="text"
                  value={newTitle}
                  onChange={(e) => setNewTitle(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCreate()}
                  placeholder="e.g., Q3 Training Feedback"
                  className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-gray-100 bg-gray-50 dark:bg-gray-900 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500"
                  autoFocus
                />
                <div className="flex justify-end gap-3 mt-5">
                  <button
                    onClick={() => { setShowCreate(false); setNewTitle(''); }}
                    className="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleCreate}
                    disabled={!newTitle.trim()}
                    className="px-4 py-2 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
                  >
                    Create Survey
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Delete Confirm */}
          {deleteId && (
            <ConfirmDialog
              title="Delete Survey"
              message="Are you sure you want to delete this survey? This cannot be undone."
              confirmLabel="Delete"
              confirmVariant="danger"
              onConfirm={() => handleDelete(deleteId)}
              onCancel={() => setDeleteId(null)}
            />
          )}

          {/* Empty State */}
          {surveys.length === 0 ? (
            <div className="text-center py-20">
              <div className="w-16 h-16 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center mx-auto mb-4">
                <FileEdit className="w-7 h-7 text-gray-400" />
              </div>
              <p className="text-lg text-gray-600 dark:text-gray-400">No surveys yet</p>
              <p className="text-sm text-gray-400 mt-1">Click "New Survey" to get started</p>
            </div>
          ) : (
            <div className="grid gap-4">
              {surveys.map((survey) => (
                <div
                  key={survey.id}
                  className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-5 hover:shadow-md transition-shadow"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3">
                        <h3 className="text-sm font-semibold text-gray-800 dark:text-gray-100">{survey.title}</h3>
                        <span className={`px-2 py-0.5 text-[10px] font-semibold rounded-full ${STATUS_COLORS[survey.status]}`}>
                          {STATUS_LABELS[survey.status]}
                        </span>
                      </div>
                      {survey.description && (
                        <p className="text-sm text-gray-500 mt-1">{survey.description}</p>
                      )}
                      <div className="flex items-center gap-4 mt-3 text-xs text-gray-400">
                        <span>{survey.fieldCount} field{survey.fieldCount !== 1 ? 's' : ''}</span>
                        <span>{survey.totalParticipants} participant{survey.totalParticipants !== 1 ? 's' : ''}</span>
                        <span>{survey.totalResponses} response{survey.totalResponses !== 1 ? 's' : ''}</span>
                        <span>Created {new Date(survey.createdAt).toLocaleDateString()}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      <button
                        onClick={() => navigate(`/admin/surveys/${survey.id}/build`)}
                        className="p-2 text-gray-400 hover:text-teal-600 hover:bg-teal-50 dark:hover:bg-teal-900/20 rounded-lg transition-colors"
                        title="Edit Form"
                      >
                        <FileEdit className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => navigate(`/admin/surveys/${survey.id}/participants`)}
                        className="p-2 text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-colors"
                        title="Participants"
                      >
                        <Users className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => navigate(`/admin/surveys/${survey.id}/dashboard`)}
                        className="p-2 text-gray-400 hover:text-purple-600 hover:bg-purple-50 dark:hover:bg-purple-900/20 rounded-lg transition-colors"
                        title="Dashboard"
                      >
                        <BarChart3 className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleClone(survey.id)}
                        className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
                        title="Clone"
                      >
                        <Copy className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => setDeleteId(survey.id)}
                        className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                        title="Delete"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </SurveyAdminLayout>
  );
}
