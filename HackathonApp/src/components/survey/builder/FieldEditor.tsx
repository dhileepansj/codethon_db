import { useState, useEffect, useCallback } from 'react';
import { Plus, Trash2, GripVertical } from 'lucide-react';
import { fieldApi } from '../../../services/surveyApi';
import {
  SurveyFieldDto, UpdateFieldDto, SurveyFieldType,
  FIELD_TYPE_LABELS, CreateDependencyDto, DependencyCondition, DependencyAction,
} from '../../../types/survey';
import { toast } from 'sonner';

interface FieldEditorProps {
  field: SurveyFieldDto;
  allFields: SurveyFieldDto[];
  surveyId: string;
  onUpdate: (fieldId: string, dto: UpdateFieldDto) => void;
}

export default function FieldEditor({ field, allFields, surveyId, onUpdate }: FieldEditorProps) {
  const [label, setLabel] = useState(field.label);
  const [description, setDescription] = useState(field.description || '');
  const [placeholder, setPlaceholder] = useState(field.placeholder || '');
  const [isRequired, setIsRequired] = useState(field.isRequired);
  const [options, setOptions] = useState<{ value: string; label: string }[]>(parseOptions(field.options));
  const [showDependencyEditor, setShowDependencyEditor] = useState(false);

  useEffect(() => {
    setLabel(field.label); setDescription(field.description || ''); setPlaceholder(field.placeholder || '');
    setIsRequired(field.isRequired); setOptions(parseOptions(field.options));
  }, [field.id]);

  const save = useCallback((overrides: Partial<UpdateFieldDto> = {}) => {
    onUpdate(field.id, { label, description: description || undefined, placeholder: placeholder || undefined, isRequired, ...overrides });
  }, [field.id, label, description, placeholder, isRequired, onUpdate]);

  function handleBlur() { save(); }

  function handleRequiredChange(checked: boolean) { setIsRequired(checked); save({ isRequired: checked }); }

  const isChoiceField = [SurveyFieldType.Dropdown, SurveyFieldType.MultiSelect, SurveyFieldType.Radio, SurveyFieldType.Checkbox].includes(field.fieldType);

  function addOption() {
    const newOpt = { value: `option_${options.length + 1}`, label: `Option ${options.length + 1}` };
    const updated = [...options, newOpt]; setOptions(updated); onUpdate(field.id, { options: JSON.stringify(updated) });
  }

  function updateOption(index: number, label: string) {
    const updated = [...options]; updated[index] = { ...updated[index], label, value: label.toLowerCase().replace(/\s+/g, '_') }; setOptions(updated);
  }

  function saveOptions() { onUpdate(field.id, { options: JSON.stringify(options) }); }

  function removeOption(index: number) {
    const updated = options.filter((_, i) => i !== index); setOptions(updated); onUpdate(field.id, { options: JSON.stringify(updated) });
  }

  const otherFields = allFields.filter((f) => f.id !== field.id && f.fieldType !== SurveyFieldType.Section && f.fieldType !== SurveyFieldType.Paragraph);

  async function addDependency(dto: CreateDependencyDto) {
    try { await fieldApi.createDependency(surveyId, field.id, dto); toast.success('Logic rule added'); setShowDependencyEditor(false); }
    catch { toast.error('Failed to add logic rule'); }
  }

  async function removeDependency(depId: string) {
    try { await fieldApi.deleteDependency(surveyId, depId); toast.success('Logic rule removed'); }
    catch { toast.error('Failed to remove logic rule'); }
  }

  return (
    <div className="p-4 space-y-5">
      <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Field Properties</h3>

      <div>
        <label className="text-xs text-gray-500 dark:text-gray-400 block mb-1">Type</label>
        <p className="text-sm text-gray-700 dark:text-gray-300 font-medium">{FIELD_TYPE_LABELS[field.fieldType]}</p>
      </div>

      <div>
        <label className="text-xs text-gray-500 dark:text-gray-400 block mb-1">Question</label>
        <input type="text" value={label} onChange={(e) => setLabel(e.target.value)} onBlur={handleBlur}
          className="w-full px-3 py-2 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 bg-gray-50 dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500" />
      </div>

      <div>
        <label className="text-xs text-gray-500 dark:text-gray-400 block mb-1">Helper Text</label>
        <input type="text" value={description} onChange={(e) => setDescription(e.target.value)} onBlur={handleBlur} placeholder="Add a description..."
          className="w-full px-3 py-2 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 bg-gray-50 dark:bg-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500" />
      </div>

      {![SurveyFieldType.Section, SurveyFieldType.Paragraph, SurveyFieldType.Rating, SurveyFieldType.Scale, SurveyFieldType.YesNo, SurveyFieldType.Checkbox, SurveyFieldType.Radio, SurveyFieldType.FileUpload, SurveyFieldType.Matrix].includes(field.fieldType) && (
        <div>
          <label className="text-xs text-gray-500 dark:text-gray-400 block mb-1">Placeholder</label>
          <input type="text" value={placeholder} onChange={(e) => setPlaceholder(e.target.value)} onBlur={handleBlur} placeholder="Placeholder text..."
            className="w-full px-3 py-2 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 bg-gray-50 dark:bg-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500" />
        </div>
      )}

      {field.fieldType !== SurveyFieldType.Section && field.fieldType !== SurveyFieldType.Paragraph && (
        <div className="flex items-center justify-between">
          <label className="text-xs text-gray-500 dark:text-gray-400">Required</label>
          <button onClick={() => handleRequiredChange(!isRequired)} className={`w-9 h-5 rounded-full transition-colors ${isRequired ? 'bg-teal-600' : 'bg-gray-300 dark:bg-gray-600'}`}>
            <div className={`w-3.5 h-3.5 rounded-full bg-white transition-transform mx-0.5 mt-[3px] ${isRequired ? 'translate-x-4' : 'translate-x-0'}`} />
          </button>
        </div>
      )}

      {isChoiceField && (
        <div>
          <label className="text-xs text-gray-500 dark:text-gray-400 mb-2 block">Options</label>
          <div className="space-y-1.5">
            {options.map((opt, idx) => (
              <div key={idx} className="flex items-center gap-2">
                <GripVertical className="w-3 h-3 text-gray-300 flex-shrink-0" />
                <input type="text" value={opt.label} onChange={(e) => updateOption(idx, e.target.value)} onBlur={saveOptions}
                  className="flex-1 px-2 py-1.5 border dark:border-gray-700 rounded text-sm text-gray-800 dark:text-gray-100 bg-gray-50 dark:bg-gray-800 focus:outline-none focus:border-teal-500" />
                <button onClick={() => removeOption(idx)} className="p-1 text-gray-400 hover:text-red-500"><Trash2 className="w-3 h-3" /></button>
              </div>
            ))}
          </div>
          <button onClick={addOption} className="flex items-center gap-1 mt-2 text-xs text-teal-600 hover:text-teal-700 font-medium"><Plus className="w-3 h-3" /> Add option</button>
          {!options.some((o) => o.value === '__other__') && (
            <button onClick={() => { const updated = [...options, { value: '__other__', label: 'Other...' }]; setOptions(updated); onUpdate(field.id, { options: JSON.stringify(updated) }); }}
              className="flex items-center gap-1 mt-1 text-xs text-gray-400 hover:text-gray-600"><Plus className="w-3 h-3" /> Add "Other" option</button>
          )}
        </div>
      )}

      {field.fieldType !== SurveyFieldType.Section && field.fieldType !== SurveyFieldType.Paragraph && (
        <div className="border-t dark:border-gray-700 pt-4">
          <div className="flex items-center justify-between mb-2">
            <label className="text-xs text-gray-500 dark:text-gray-400">Show Logic</label>
            <button onClick={() => setShowDependencyEditor(true)} className="text-xs text-teal-600 hover:text-teal-700 font-medium">+ Add Rule</button>
          </div>

          {field.dependencies.length > 0 && (
            <div className="space-y-2">
              {field.dependencies.map((dep) => {
                const parentField = allFields.find((f) => f.id === dep.dependsOnFieldId);
                return (
                  <div key={dep.id} className="flex items-center justify-between p-2 bg-purple-50 dark:bg-purple-900/10 border border-purple-200 dark:border-purple-800 rounded-lg text-xs">
                    <span className="text-purple-700 dark:text-purple-300">Show when "{parentField?.label || '?'}" {getConditionLabel(dep.condition)} "{dep.value}"</span>
                    <button onClick={() => removeDependency(dep.id)} className="text-gray-400 hover:text-red-500"><Trash2 className="w-3 h-3" /></button>
                  </div>
                );
              })}
            </div>
          )}

          {showDependencyEditor && <DependencyForm otherFields={otherFields} onSave={addDependency} onCancel={() => setShowDependencyEditor(false)} />}
        </div>
      )}
    </div>
  );
}

function DependencyForm({ otherFields, onSave, onCancel }: { otherFields: SurveyFieldDto[]; onSave: (dto: CreateDependencyDto) => void; onCancel: () => void; }) {
  const [parentFieldId, setParentFieldId] = useState('');
  const [condition, setCondition] = useState<DependencyCondition>(DependencyCondition.Equals);
  const [value, setValue] = useState('');

  function handleSave() {
    if (!parentFieldId) return;
    onSave({ dependsOnFieldId: parentFieldId, condition, value: value || undefined, action: DependencyAction.Show });
  }

  const selectedParent = otherFields.find((f) => f.id === parentFieldId);
  const parentOptions = parseOptions(selectedParent?.options);

  return (
    <div className="mt-3 p-3 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg space-y-3">
      <p className="text-xs text-gray-700 dark:text-gray-300 font-medium">Show this field when:</p>
      <select value={parentFieldId} onChange={(e) => setParentFieldId(e.target.value)} className="w-full px-2 py-1.5 border dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 focus:outline-none focus:border-teal-500">
        <option value="">Select a field...</option>
        {otherFields.map((f) => <option key={f.id} value={f.id}>{f.label}</option>)}
      </select>
      <select value={condition} onChange={(e) => setCondition(Number(e.target.value))} className="w-full px-2 py-1.5 border dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 focus:outline-none focus:border-teal-500">
        <option value={DependencyCondition.Equals}>is equal to</option>
        <option value={DependencyCondition.NotEquals}>is not equal to</option>
        <option value={DependencyCondition.Contains}>contains</option>
        <option value={DependencyCondition.GreaterThan}>is greater than</option>
        <option value={DependencyCondition.LessThan}>is less than</option>
        <option value={DependencyCondition.IsEmpty}>is empty</option>
        <option value={DependencyCondition.IsNotEmpty}>is not empty</option>
      </select>
      {condition !== DependencyCondition.IsEmpty && condition !== DependencyCondition.IsNotEmpty && (
        parentOptions.length > 0 ? (
          <select value={value} onChange={(e) => setValue(e.target.value)} className="w-full px-2 py-1.5 border dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 focus:outline-none focus:border-teal-500">
            <option value="">Select a value...</option>
            {parentOptions.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
        ) : (
          <input type="text" value={value} onChange={(e) => setValue(e.target.value)} placeholder="Enter value..."
            className="w-full px-2 py-1.5 border dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 placeholder-gray-400 focus:outline-none focus:border-teal-500" />
        )
      )}
      <div className="flex justify-end gap-2">
        <button onClick={onCancel} className="px-3 py-1 text-xs text-gray-500 hover:text-gray-700">Cancel</button>
        <button onClick={handleSave} disabled={!parentFieldId} className="px-3 py-1 text-xs bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-white rounded font-medium">Save Rule</button>
      </div>
    </div>
  );
}

function parseOptions(json?: string): { value: string; label: string }[] { if (!json) return []; try { return JSON.parse(json); } catch { return []; } }

function getConditionLabel(condition: DependencyCondition): string {
  const labels: Record<DependencyCondition, string> = {
    [DependencyCondition.Equals]: 'equals', [DependencyCondition.NotEquals]: 'does not equal', [DependencyCondition.Contains]: 'contains',
    [DependencyCondition.GreaterThan]: '>', [DependencyCondition.LessThan]: '<', [DependencyCondition.IsEmpty]: 'is empty', [DependencyCondition.IsNotEmpty]: 'is not empty',
  };
  return labels[condition] || '?';
}
