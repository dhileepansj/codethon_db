import { GripVertical, Trash2, Copy, Link, Calendar } from 'lucide-react';
import { SurveyFieldDto, SurveyFieldType, FIELD_TYPE_LABELS } from '../../../types/survey';

interface BuilderCanvasProps {
  fields: SurveyFieldDto[];
  selectedFieldId: string | null;
  onSelectField: (id: string) => void;
  onDeleteField: (id: string) => void;
  onDuplicateField: (id: string) => void;
  onReorder: (fields: SurveyFieldDto[]) => void;
}

export default function BuilderCanvas({
  fields, selectedFieldId, onSelectField, onDeleteField, onDuplicateField, onReorder,
}: BuilderCanvasProps) {
  function handleDragStart(e: React.DragEvent, index: number) {
    e.dataTransfer.setData('text/plain', index.toString());
    e.dataTransfer.effectAllowed = 'move';
  }

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  }

  function handleDrop(e: React.DragEvent, dropIndex: number) {
    e.preventDefault();
    const dragIndex = parseInt(e.dataTransfer.getData('text/plain'));
    if (dragIndex === dropIndex) return;
    const reordered = [...fields];
    const [moved] = reordered.splice(dragIndex, 1);
    reordered.splice(dropIndex, 0, moved);
    onReorder(reordered);
  }

  if (fields.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-gray-400">
        <p className="text-lg mb-2">Your form is empty</p>
        <p className="text-sm">Click a field type from the left panel to add it here</p>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto space-y-3">
      {fields.map((field, index) => (
        <div
          key={field.id}
          draggable
          onDragStart={(e) => handleDragStart(e, index)}
          onDragOver={handleDragOver}
          onDrop={(e) => handleDrop(e, index)}
          onClick={() => onSelectField(field.id)}
          className={`group relative bg-white dark:bg-gray-900 border rounded-xl p-4 cursor-pointer transition-all shadow-sm ${
            selectedFieldId === field.id
              ? 'border-teal-500 ring-2 ring-teal-500/20'
              : 'border-gray-200 dark:border-gray-800 hover:border-gray-300 dark:hover:border-gray-700 hover:shadow-md'
          }`}
        >
          {/* Drag Handle */}
          <div className="absolute left-2 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity cursor-grab active:cursor-grabbing">
            <GripVertical className="w-4 h-4 text-gray-300" />
          </div>

          {/* Field Content */}
          <div className="ml-4">
            {field.fieldType === SurveyFieldType.Section ? (
              <div>
                <h3 className="text-base font-semibold text-gray-800 dark:text-gray-100">{field.sectionTitle || field.label}</h3>
                {field.description && <p className="text-sm text-gray-500 mt-1">{field.description}</p>}
              </div>
            ) : field.fieldType === SurveyFieldType.Paragraph ? (
              <p className="text-sm text-gray-500 italic">{field.label}</p>
            ) : (
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-gray-800 dark:text-gray-100">{field.label}</span>
                  {field.isRequired && <span className="text-red-500 text-xs">*</span>}
                  {field.dependencies.length > 0 && (
                    <span className="inline-flex items-center gap-1 px-1.5 py-0.5 bg-purple-50 dark:bg-purple-900/20 text-purple-600 dark:text-purple-400 text-[10px] font-semibold rounded">
                      <Link className="w-3 h-3" /> Logic
                    </span>
                  )}
                </div>
                <div className="mt-2"><FieldPreview field={field} /></div>
                <p className="text-[11px] text-gray-400 mt-2">{FIELD_TYPE_LABELS[field.fieldType]}</p>
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="absolute right-3 top-3 flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
            <button onClick={(e) => { e.stopPropagation(); onDuplicateField(field.id); }} className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded transition-colors" title="Duplicate"><Copy className="w-3.5 h-3.5" /></button>
            <button onClick={(e) => { e.stopPropagation(); onDeleteField(field.id); }} className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded transition-colors" title="Delete"><Trash2 className="w-3.5 h-3.5" /></button>
          </div>
        </div>
      ))}
    </div>
  );
}

function FieldPreview({ field }: { field: SurveyFieldDto }) {
  const type = field.fieldType;

  switch (type) {
    case SurveyFieldType.ShortText:
    case SurveyFieldType.Email:
    case SurveyFieldType.Phone:
    case SurveyFieldType.Number:
      return <div className="h-9 border dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 px-3 flex items-center text-sm text-gray-400">{field.placeholder || 'Your answer'}</div>;

    case SurveyFieldType.LongText:
      return <div className="h-16 border dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 px-3 pt-2 text-sm text-gray-400">{field.placeholder || 'Long answer text...'}</div>;

    case SurveyFieldType.Dropdown:
    case SurveyFieldType.MultiSelect:
      return <div className="h-9 border dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 px-3 flex items-center justify-between text-sm text-gray-400"><span>Select...</span><span>▾</span></div>;

    case SurveyFieldType.Radio:
    case SurveyFieldType.Checkbox: {
      const options = parseOptions(field.options);
      return (
        <div className="space-y-1.5">
          {(options.length > 0 ? options.slice(0, 4) : [{ value: '1', label: 'Option 1' }]).map((opt) => (
            <div key={opt.value} className="flex items-center gap-2 text-sm text-gray-500">
              <div className={`w-4 h-4 border border-gray-300 dark:border-gray-600 ${type === SurveyFieldType.Radio ? 'rounded-full' : 'rounded'}`} />
              {opt.label}
            </div>
          ))}
          {options.length > 4 && <p className="text-xs text-gray-400">+{options.length - 4} more</p>}
        </div>
      );
    }

    case SurveyFieldType.Rating:
      return <div className="flex gap-1">{[1, 2, 3, 4, 5].map((i) => <span key={i} className="text-lg text-gray-300">☆</span>)}</div>;

    case SurveyFieldType.Scale:
      return (
        <div className="flex items-center gap-1">
          <span className="text-xs text-gray-400">1</span>
          <div className="flex gap-0.5">{Array.from({ length: 10 }, (_, i) => <div key={i} className="w-6 h-6 border border-gray-300 dark:border-gray-600 rounded text-center text-xs text-gray-400 leading-6">{i + 1}</div>)}</div>
          <span className="text-xs text-gray-400">10</span>
        </div>
      );

    case SurveyFieldType.YesNo:
      return (
        <div className="flex gap-3">
          <div className="flex items-center gap-2 text-sm text-gray-500"><div className="w-4 h-4 border border-gray-300 dark:border-gray-600 rounded-full" /> Yes</div>
          <div className="flex items-center gap-2 text-sm text-gray-500"><div className="w-4 h-4 border border-gray-300 dark:border-gray-600 rounded-full" /> No</div>
        </div>
      );

    case SurveyFieldType.Date:
    case SurveyFieldType.DateTime:
    case SurveyFieldType.Time:
      return <div className="h-9 border dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 px-3 flex items-center text-sm text-gray-400"><Calendar className="w-4 h-4 mr-2" />{type === SurveyFieldType.Time ? 'HH:MM' : 'DD/MM/YYYY'}</div>;

    case SurveyFieldType.FileUpload:
      return <div className="h-14 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg flex items-center justify-center text-sm text-gray-400">Click or drag file to upload</div>;

    default: return null;
  }
}

function parseOptions(optionsJson?: string): { value: string; label: string }[] {
  if (!optionsJson) return [];
  try { return JSON.parse(optionsJson); } catch { return []; }
}
