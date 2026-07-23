import { useEffect, useState, useCallback, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Eye, Settings, Undo2, Redo2, FileText } from 'lucide-react';
import { toast } from 'sonner';
import { surveyApi, fieldApi } from '../../services/surveyApi';
import { SurveyDetailDto, SurveyFieldDto, CreateFieldDto, UpdateFieldDto, SurveyFieldType } from '../../types/survey';
import FieldPalette from '../../components/survey/builder/FieldPalette';
import BuilderCanvas from '../../components/survey/builder/BuilderCanvas';
import FieldEditor from '../../components/survey/builder/FieldEditor';
import SurveyTemplatesModal from '../../components/survey/builder/SurveyTemplatesModal';

// ─── Undo/Redo History ────────────────────────────────────────────────────────
interface HistoryState {
  fields: SurveyFieldDto[];
  selectedFieldId: string | null;
}

function useHistory(initial: HistoryState) {
  const [past, setPast] = useState<HistoryState[]>([]);
  const [present, setPresent] = useState<HistoryState>(initial);
  const [future, setFuture] = useState<HistoryState[]>([]);

  const push = useCallback((state: HistoryState) => {
    setPast((p) => [...p.slice(-30), present]);
    setPresent(state);
    setFuture([]);
  }, [present]);

  const undo = useCallback(() => {
    if (past.length === 0) return null;
    const prev = past[past.length - 1];
    setPast((p) => p.slice(0, -1));
    setFuture((f) => [present, ...f]);
    setPresent(prev);
    return prev;
  }, [past, present]);

  const redo = useCallback(() => {
    if (future.length === 0) return null;
    const next = future[0];
    setFuture((f) => f.slice(1));
    setPast((p) => [...p, present]);
    setPresent(next);
    return next;
  }, [future, present]);

  return { past, present, future, push, undo, redo, setPresent };
}

export default function SurveyBuilderPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const navigate = useNavigate();
  const [survey, setSurvey] = useState<SurveyDetailDto | null>(null);
  const [fields, setFields] = useState<SurveyFieldDto[]>([]);
  const [selectedFieldId, setSelectedFieldId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [showTemplates, setShowTemplates] = useState(false);

  const history = useHistory({ fields: [], selectedFieldId: null });
  const skipHistoryRef = useRef(false);

  useEffect(() => {
    if (surveyId) loadSurvey();
  }, [surveyId]);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.key === 'z' && !e.shiftKey) { e.preventDefault(); handleUndo(); }
      if ((e.ctrlKey || e.metaKey) && (e.key === 'y' || (e.key === 'z' && e.shiftKey))) { e.preventDefault(); handleRedo(); }
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [history.past.length, history.future.length]);

  async function loadSurvey() {
    try {
      const data = await surveyApi.getById(surveyId!);
      setSurvey(data);
      setFields(data.fields);
      history.setPresent({ fields: data.fields, selectedFieldId: null });
    } catch { toast.error('Failed to load survey'); }
    finally { setLoading(false); }
  }

  function pushHistory(newFields: SurveyFieldDto[], selId: string | null) {
    if (!skipHistoryRef.current) history.push({ fields: newFields, selectedFieldId: selId });
  }

  function handleUndo() {
    const prev = history.undo();
    if (prev) { skipHistoryRef.current = true; setFields(prev.fields); setSelectedFieldId(prev.selectedFieldId); skipHistoryRef.current = false; }
  }

  function handleRedo() {
    const next = history.redo();
    if (next) { skipHistoryRef.current = true; setFields(next.fields); setSelectedFieldId(next.selectedFieldId); skipHistoryRef.current = false; }
  }

  const handleAddField = useCallback(async (fieldType: SurveyFieldType) => {
    if (!surveyId) return;
    try {
      const dto: CreateFieldDto = { fieldType, label: getDefaultLabel(fieldType), isRequired: false };
      const created = await fieldApi.create(surveyId, dto);
      const newFields = [...fields, created];
      setFields(newFields); setSelectedFieldId(created.id); pushHistory(newFields, created.id); setLastSaved(new Date());
    } catch { toast.error('Failed to add field'); }
  }, [surveyId, fields]);

  const handleUpdateField = useCallback(async (fieldId: string, dto: UpdateFieldDto) => {
    if (!surveyId) return;
    try {
      const updated = await fieldApi.update(surveyId, fieldId, dto);
      if (updated) { const newFields = fields.map((f) => (f.id === fieldId ? updated : f)); setFields(newFields); pushHistory(newFields, selectedFieldId); setLastSaved(new Date()); }
    } catch { toast.error('Failed to update field'); }
  }, [surveyId, fields, selectedFieldId]);

  const handleDeleteField = useCallback(async (fieldId: string) => {
    if (!surveyId) return;
    try {
      await fieldApi.delete(surveyId, fieldId);
      const newFields = fields.filter((f) => f.id !== fieldId);
      const newSelected = selectedFieldId === fieldId ? null : selectedFieldId;
      setFields(newFields); setSelectedFieldId(newSelected); pushHistory(newFields, newSelected); setLastSaved(new Date()); toast.success('Field deleted');
    } catch { toast.error('Failed to delete field'); }
  }, [surveyId, selectedFieldId, fields]);

  const handleDuplicateField = useCallback(async (fieldId: string) => {
    if (!surveyId) return;
    const source = fields.find((f) => f.id === fieldId);
    if (!source) return;
    try {
      const dto: CreateFieldDto = { fieldType: source.fieldType, label: `${source.label} (copy)`, description: source.description || undefined, placeholder: source.placeholder || undefined, isRequired: source.isRequired, options: source.options || undefined, validation: source.validation || undefined, sectionTitle: source.sectionTitle || undefined, defaultValue: source.defaultValue || undefined, matrixRows: source.matrixRows || undefined, matrixColumns: source.matrixColumns || undefined };
      const created = await fieldApi.create(surveyId, dto);
      const newFields = [...fields, created];
      setFields(newFields); setSelectedFieldId(created.id); pushHistory(newFields, created.id); setLastSaved(new Date()); toast.success('Field duplicated');
    } catch { toast.error('Failed to duplicate field'); }
  }, [surveyId, fields]);

  const handleReorder = useCallback(async (reorderedFields: SurveyFieldDto[]) => {
    if (!surveyId) return;
    setFields(reorderedFields); pushHistory(reorderedFields, selectedFieldId);
    try { await fieldApi.reorder(surveyId, { fields: reorderedFields.map((f, idx) => ({ fieldId: f.id, sortOrder: idx })) }); setLastSaved(new Date()); }
    catch { toast.error('Failed to reorder fields'); }
  }, [surveyId, selectedFieldId]);

  const handleApplyTemplate = useCallback(async (templateFields: CreateFieldDto[]) => {
    if (!surveyId) return;
    setShowTemplates(false);
    try {
      const created: SurveyFieldDto[] = [];
      for (const dto of templateFields) { const field = await fieldApi.create(surveyId, dto); created.push(field); }
      const newFields = [...fields, ...created];
      setFields(newFields); pushHistory(newFields, selectedFieldId); setLastSaved(new Date()); toast.success(`${created.length} fields added from template`);
    } catch { toast.error('Failed to apply template'); }
  }, [surveyId, fields, selectedFieldId]);

  const selectedField = fields.find((f) => f.id === selectedFieldId) ?? null;

  if (loading) return <div className="flex items-center justify-center h-screen bg-gray-50 dark:bg-gray-950"><div className="text-gray-500">Loading form builder...</div></div>;
  if (!survey) return <div className="flex items-center justify-center h-screen bg-gray-50 dark:bg-gray-950"><div className="text-red-500">Survey not found</div></div>;

  return (
    <div className="h-screen flex flex-col bg-gray-50 dark:bg-gray-950">
      {/* Top Bar */}
      <header className="h-14 border-b dark:border-gray-800 flex items-center justify-between px-4 bg-white dark:bg-gray-900 shrink-0">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate('/admin/surveys')} className="p-1.5 text-gray-500 hover:text-gray-800 dark:text-gray-400 dark:hover:text-white rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <h1 className="text-sm font-semibold text-gray-800 dark:text-gray-100">{survey.title}</h1>
            <p className="text-xs text-gray-400">
              {fields.length} field{fields.length !== 1 ? 's' : ''}
              {lastSaved && ` · Saved ${lastSaved.toLocaleTimeString()}`}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={handleUndo} disabled={history.past.length === 0} className="p-1.5 text-gray-500 hover:text-gray-800 dark:hover:text-white disabled:opacity-30 disabled:cursor-not-allowed rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800" title="Undo (Ctrl+Z)"><Undo2 className="w-4 h-4" /></button>
          <button onClick={handleRedo} disabled={history.future.length === 0} className="p-1.5 text-gray-500 hover:text-gray-800 dark:hover:text-white disabled:opacity-30 disabled:cursor-not-allowed rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800" title="Redo (Ctrl+Y)"><Redo2 className="w-4 h-4" /></button>
          <div className="w-px h-6 bg-gray-200 dark:bg-gray-700 mx-1" />
          <HeaderBtn icon={<FileText className="w-4 h-4" />} label="Templates" onClick={() => setShowTemplates(true)} />
          <HeaderBtn icon={<Eye className="w-4 h-4" />} label="Preview" onClick={() => navigate(`/admin/surveys/${surveyId}/preview`)} />
          <HeaderBtn icon={<Settings className="w-4 h-4" />} label="Settings" onClick={() => navigate(`/admin/surveys/${surveyId}/settings`)} />
        </div>
      </header>

      {/* Builder Layout */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left: Field Palette */}
        <aside className="w-52 border-r dark:border-gray-800 overflow-y-auto bg-white dark:bg-gray-900">
          <FieldPalette onAddField={handleAddField} />
        </aside>

        {/* Center: Canvas */}
        <main className="flex-1 overflow-y-auto p-6 bg-gray-100 dark:bg-gray-950">
          <BuilderCanvas fields={fields} selectedFieldId={selectedFieldId} onSelectField={setSelectedFieldId} onDeleteField={handleDeleteField} onDuplicateField={handleDuplicateField} onReorder={handleReorder} />
        </main>

        {/* Right: Properties */}
        <aside className="w-80 border-l dark:border-gray-800 overflow-y-auto bg-white dark:bg-gray-900">
          {selectedField ? (
            <FieldEditor field={selectedField} allFields={fields} surveyId={surveyId!} onUpdate={handleUpdateField} />
          ) : (
            <div className="flex items-center justify-center h-full text-gray-400 text-sm p-4 text-center">Select a field to edit its properties</div>
          )}
        </aside>
      </div>

      {showTemplates && <SurveyTemplatesModal onApply={handleApplyTemplate} onClose={() => setShowTemplates(false)} />}
    </div>
  );
}

function HeaderBtn({ icon, label, onClick }: { icon: React.ReactNode; label: string; onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-white border dark:border-gray-700 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
      {icon}{label}
    </button>
  );
}

function getDefaultLabel(type: SurveyFieldType): string {
  const labels: Partial<Record<SurveyFieldType, string>> = {
    [SurveyFieldType.ShortText]: 'Untitled Question', [SurveyFieldType.LongText]: 'Untitled Question', [SurveyFieldType.Number]: 'Untitled Question', [SurveyFieldType.Email]: 'Email Address', [SurveyFieldType.Phone]: 'Phone Number', [SurveyFieldType.Date]: 'Select a Date', [SurveyFieldType.DateTime]: 'Select Date & Time', [SurveyFieldType.Time]: 'Select Time', [SurveyFieldType.Dropdown]: 'Untitled Question', [SurveyFieldType.MultiSelect]: 'Untitled Question', [SurveyFieldType.Radio]: 'Untitled Question', [SurveyFieldType.Checkbox]: 'Untitled Question', [SurveyFieldType.Rating]: 'Rate your experience', [SurveyFieldType.Scale]: 'On a scale of 1-10', [SurveyFieldType.FileUpload]: 'Upload File', [SurveyFieldType.Section]: 'Section Title', [SurveyFieldType.Paragraph]: 'Description text...', [SurveyFieldType.YesNo]: 'Yes or No?', [SurveyFieldType.Matrix]: 'Matrix Question',
  };
  return labels[type] ?? 'Untitled Question';
}
