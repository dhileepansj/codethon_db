import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Monitor, Smartphone } from 'lucide-react';
import { toast } from 'sonner';
import { surveyApi } from '../../services/surveyApi';
import {
  SurveyDetailDto, SurveyFieldDto, SurveyFieldType,
  FieldDependencyDto, DependencyCondition, DependencyAction,
} from '../../types/survey';

export default function SurveyPreviewPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const navigate = useNavigate();
  const [survey, setSurvey] = useState<SurveyDetailDto | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [viewMode, setViewMode] = useState<'desktop' | 'mobile'>('desktop');

  useEffect(() => { if (surveyId) loadSurvey(); }, [surveyId]);

  async function loadSurvey() {
    try { const data = await surveyApi.getById(surveyId!); setSurvey(data); }
    catch { toast.error('Failed to load survey'); }
    finally { setLoading(false); }
  }

  function updateAnswer(fieldId: string, value: string) { setAnswers({ ...answers, [fieldId]: value }); }

  if (loading) return <div className="flex items-center justify-center h-screen bg-gray-50 dark:bg-gray-950 text-gray-500">Loading...</div>;
  if (!survey) return <div className="flex items-center justify-center h-screen bg-gray-50 dark:bg-gray-950 text-red-500">Survey not found</div>;

  const visibleFields = getVisibleFields(survey.fields, answers);

  return (
    <div className="h-screen flex flex-col bg-gray-50 dark:bg-gray-950">
      {/* Top Bar */}
      <header className="h-12 border-b dark:border-gray-800 flex items-center justify-between px-4 bg-white dark:bg-gray-900 shrink-0">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate(`/admin/surveys/${surveyId}/build`)} className="p-1.5 text-gray-500 hover:text-gray-800 dark:hover:text-white rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800">
            <ArrowLeft className="w-4 h-4" />
          </button>
          <span className="text-sm text-gray-800 dark:text-gray-100 font-medium">Preview: {survey.title}</span>
          <span className="text-[10px] font-semibold text-amber-700 bg-amber-100 dark:bg-amber-900/30 dark:text-amber-400 px-2 py-0.5 rounded-full">Preview Mode</span>
        </div>
        <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-0.5">
          <button onClick={() => setViewMode('desktop')} className={`p-1.5 rounded ${viewMode === 'desktop' ? 'bg-white dark:bg-gray-700 text-gray-800 dark:text-white shadow-sm' : 'text-gray-400'}`} title="Desktop"><Monitor className="w-4 h-4" /></button>
          <button onClick={() => setViewMode('mobile')} className={`p-1.5 rounded ${viewMode === 'mobile' ? 'bg-white dark:bg-gray-700 text-gray-800 dark:text-white shadow-sm' : 'text-gray-400'}`} title="Mobile"><Smartphone className="w-4 h-4" /></button>
        </div>
      </header>

      {/* Preview Content */}
      <div className="flex-1 overflow-y-auto flex justify-center p-6 bg-gray-100 dark:bg-gray-950">
        <div className={`${viewMode === 'mobile' ? 'max-w-sm' : 'max-w-xl'} w-full space-y-4 transition-all`}>
          {/* Survey Header */}
          <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-6">
            <h1 className="text-xl font-semibold text-gray-800 dark:text-gray-100">{survey.title}</h1>
            {survey.description && <p className="text-sm text-gray-500 mt-2">{survey.description}</p>}
          </div>

          {/* Simulated Participant Info */}
          <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-4">
            <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-2">Participant Information (auto-filled)</p>
            <div className="grid grid-cols-3 gap-3 text-sm">
              <div><span className="text-gray-400 text-xs">ID:</span> <span className="text-gray-700 dark:text-gray-300">EMP001</span></div>
              <div><span className="text-gray-400 text-xs">Name:</span> <span className="text-gray-700 dark:text-gray-300">John Doe</span></div>
              <div><span className="text-gray-400 text-xs">Email:</span> <span className="text-gray-700 dark:text-gray-300">john@co.com</span></div>
            </div>
          </div>

          {/* Fields */}
          {visibleFields.map((field) => (
            <div key={field.id} className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-5">
              <PreviewFieldRenderer field={field} allFields={survey.fields} value={answers[field.id] || ''} answers={answers} onChange={(val) => updateAnswer(field.id, val)} />
            </div>
          ))}

          {/* Submit (disabled) */}
          <button disabled className="w-full px-4 py-3 bg-teal-600/50 text-white rounded-lg font-medium cursor-not-allowed text-sm">
            Submit Response (disabled in preview)
          </button>

          {survey.thankYouMessage && (
            <div className="bg-green-50 dark:bg-green-900/10 border border-green-200 dark:border-green-800 rounded-xl p-4 text-center">
              <p className="text-sm text-green-700 dark:text-green-400">{survey.thankYouMessage}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Preview Field Renderer ───────────────────────────────────────────────────

function PreviewFieldRenderer({ field, allFields, value, answers, onChange }: {
  field: SurveyFieldDto; allFields: SurveyFieldDto[]; value: string; answers: Record<string, string>; onChange: (v: string) => void;
}) {
  if (field.fieldType === SurveyFieldType.Section) {
    return (<div><h3 className="text-base font-semibold text-gray-800 dark:text-gray-100">{field.sectionTitle || field.label}</h3>{field.description && <p className="text-sm text-gray-500 mt-1">{field.description}</p>}</div>);
  }
  if (field.fieldType === SurveyFieldType.Paragraph) {
    return <p className="text-sm text-gray-500 italic">{field.label}</p>;
  }

  const options = resolveCascadingOptions(field, answers);
  const INPUT_CLS = "w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500";

  return (
    <div>
      <label className="text-sm font-medium text-gray-800 dark:text-gray-100">
        {field.label}{field.isRequired && <span className="text-red-500 ml-1">*</span>}
      </label>
      {field.description && <p className="text-xs text-gray-400 mt-0.5">{field.description}</p>}

      <div className="mt-2">
        {(field.fieldType === SurveyFieldType.ShortText || field.fieldType === SurveyFieldType.Email || field.fieldType === SurveyFieldType.Phone || field.fieldType === SurveyFieldType.Number) && (
          <input type={field.fieldType === SurveyFieldType.Number ? 'number' : field.fieldType === SurveyFieldType.Email ? 'email' : 'text'} value={value} onChange={(e) => onChange(e.target.value)} placeholder={field.placeholder || ''} className={INPUT_CLS} />
        )}

        {field.fieldType === SurveyFieldType.LongText && (
          <textarea value={value} onChange={(e) => onChange(e.target.value)} placeholder={field.placeholder || ''} rows={4} className={INPUT_CLS + ' resize-y'} />
        )}

        {(field.fieldType === SurveyFieldType.Dropdown || field.fieldType === SurveyFieldType.MultiSelect) && (
          <select value={value} onChange={(e) => onChange(e.target.value)} className={INPUT_CLS}>
            <option value="">Select...</option>
            {options.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
        )}

        {field.fieldType === SurveyFieldType.Radio && (
          <div className="space-y-2">
            {options.map((opt) => (
              <label key={opt.value} className="flex items-center gap-3 cursor-pointer">
                <input type="radio" name={`preview_${field.id}`} value={opt.value} checked={value === opt.value} onChange={() => onChange(opt.value)} className="w-4 h-4 accent-teal-500" />
                <span className="text-sm text-gray-700 dark:text-gray-300">{opt.label}</span>
              </label>
            ))}
            {options.some((o) => o.value === '__other__') && value === '__other__' && (
              <input type="text" placeholder="Please specify..." className={"ml-7 " + INPUT_CLS} />
            )}
          </div>
        )}

        {field.fieldType === SurveyFieldType.Checkbox && (
          <div className="space-y-2">
            {options.map((opt) => {
              const selected: string[] = value ? (() => { try { return JSON.parse(value); } catch { return []; } })() : [];
              const isChecked = selected.includes(opt.value);
              return (
                <label key={opt.value} className="flex items-center gap-3 cursor-pointer">
                  <input type="checkbox" checked={isChecked} onChange={() => { const updated = isChecked ? selected.filter((v) => v !== opt.value) : [...selected, opt.value]; onChange(JSON.stringify(updated)); }} className="w-4 h-4 accent-teal-500" />
                  <span className="text-sm text-gray-700 dark:text-gray-300">{opt.label}</span>
                </label>
              );
            })}
          </div>
        )}

        {field.fieldType === SurveyFieldType.YesNo && (
          <div className="flex gap-4">
            {['Yes', 'No'].map((opt) => (
              <label key={opt} className="flex items-center gap-2 cursor-pointer">
                <input type="radio" name={`preview_${field.id}`} value={opt} checked={value === opt} onChange={() => onChange(opt)} className="w-4 h-4 accent-teal-500" />
                <span className="text-sm text-gray-700 dark:text-gray-300">{opt}</span>
              </label>
            ))}
          </div>
        )}

        {field.fieldType === SurveyFieldType.Rating && (
          <div className="flex gap-2">
            {[1, 2, 3, 4, 5].map((star) => (
              <button key={star} type="button" onClick={() => onChange(star.toString())} className={`text-2xl transition-colors ${Number(value) >= star ? 'text-amber-400' : 'text-gray-300 dark:text-gray-600'} hover:text-amber-400`}>★</button>
            ))}
          </div>
        )}

        {field.fieldType === SurveyFieldType.Scale && (
          <div className="flex gap-1">
            {Array.from({ length: 10 }, (_, i) => i + 1).map((n) => (
              <button key={n} type="button" onClick={() => onChange(n.toString())} className={`w-8 h-8 rounded text-sm font-medium transition-colors ${value === n.toString() ? 'bg-teal-600 text-white' : 'bg-gray-100 dark:bg-gray-800 border dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-teal-500'}`}>{n}</button>
            ))}
          </div>
        )}

        {(field.fieldType === SurveyFieldType.Date || field.fieldType === SurveyFieldType.DateTime) && (
          <input type={field.fieldType === SurveyFieldType.DateTime ? 'datetime-local' : 'date'} value={value} onChange={(e) => onChange(e.target.value)} className={INPUT_CLS} />
        )}

        {field.fieldType === SurveyFieldType.Time && (
          <input type="time" value={value} onChange={(e) => onChange(e.target.value)} className={INPUT_CLS} />
        )}

        {field.fieldType === SurveyFieldType.FileUpload && (
          <div className="h-16 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg flex items-center justify-center text-sm text-gray-400">Click or drag file to upload</div>
        )}
      </div>
    </div>
  );
}

// ─── Cascading + Dependency Engine ────────────────────────────────────────────

function resolveCascadingOptions(field: SurveyFieldDto, answers: Record<string, string>): { value: string; label: string }[] {
  const setOptionsDep = field.dependencies.find((d) => d.action === DependencyAction.SetOptions);
  if (setOptionsDep && setOptionsDep.optionMap) {
    const parentValue = answers[setOptionsDep.dependsOnFieldId] || '';
    if (parentValue) {
      try { const map: Record<string, string[]> = JSON.parse(setOptionsDep.optionMap); const opts = map[parentValue]; if (opts) return opts.map((o) => ({ value: o.toLowerCase().replace(/\s+/g, '_'), label: o })); } catch {}
    }
    return [];
  }
  if (!field.options) return [];
  try { return JSON.parse(field.options); } catch { return []; }
}

function getVisibleFields(fields: SurveyFieldDto[], answers: Record<string, string>): SurveyFieldDto[] {
  return fields.filter((field) => {
    if (field.dependencies.length === 0) return true;
    const showDeps = field.dependencies.filter((d) => d.action === DependencyAction.Show);
    if (showDeps.length === 0) return true;
    return showDeps.every((dep) => evaluateCondition(dep, answers));
  });
}

function evaluateCondition(dep: FieldDependencyDto, answers: Record<string, string>): boolean {
  const v = answers[dep.dependsOnFieldId] || '';
  switch (dep.condition) {
    case DependencyCondition.Equals: return v === dep.value;
    case DependencyCondition.NotEquals: return v !== dep.value;
    case DependencyCondition.Contains: return v.toLowerCase().includes((dep.value || '').toLowerCase());
    case DependencyCondition.GreaterThan: return Number(v) > Number(dep.value || 0);
    case DependencyCondition.LessThan: return Number(v) < Number(dep.value || 0);
    case DependencyCondition.IsEmpty: return !v;
    case DependencyCondition.IsNotEmpty: return !!v;
    default: return true;
  }
}
