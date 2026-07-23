import {
  Type, AlignLeft, Hash, Mail, Phone, Calendar, Clock,
  ChevronDown, List, CircleDot, CheckSquare, Star,
  Sliders, Upload, Minus, FileText, ToggleLeft, Grid3X3,
} from 'lucide-react';
import { SurveyFieldType, FIELD_TYPE_CATEGORIES, FIELD_TYPE_LABELS } from '../../../types/survey';

interface FieldPaletteProps {
  onAddField: (type: SurveyFieldType) => void;
}

const ICONS: Record<SurveyFieldType, React.ReactNode> = {
  [SurveyFieldType.ShortText]: <Type className="w-4 h-4" />,
  [SurveyFieldType.LongText]: <AlignLeft className="w-4 h-4" />,
  [SurveyFieldType.Number]: <Hash className="w-4 h-4" />,
  [SurveyFieldType.Email]: <Mail className="w-4 h-4" />,
  [SurveyFieldType.Phone]: <Phone className="w-4 h-4" />,
  [SurveyFieldType.Date]: <Calendar className="w-4 h-4" />,
  [SurveyFieldType.DateTime]: <Calendar className="w-4 h-4" />,
  [SurveyFieldType.Time]: <Clock className="w-4 h-4" />,
  [SurveyFieldType.Dropdown]: <ChevronDown className="w-4 h-4" />,
  [SurveyFieldType.MultiSelect]: <List className="w-4 h-4" />,
  [SurveyFieldType.Radio]: <CircleDot className="w-4 h-4" />,
  [SurveyFieldType.Checkbox]: <CheckSquare className="w-4 h-4" />,
  [SurveyFieldType.Rating]: <Star className="w-4 h-4" />,
  [SurveyFieldType.Scale]: <Sliders className="w-4 h-4" />,
  [SurveyFieldType.FileUpload]: <Upload className="w-4 h-4" />,
  [SurveyFieldType.Section]: <Minus className="w-4 h-4" />,
  [SurveyFieldType.Paragraph]: <FileText className="w-4 h-4" />,
  [SurveyFieldType.YesNo]: <ToggleLeft className="w-4 h-4" />,
  [SurveyFieldType.Matrix]: <Grid3X3 className="w-4 h-4" />,
};

const CATEGORY_LABELS = {
  text: 'Text Fields',
  choice: 'Choice Fields',
  dateTime: 'Date & Time',
  advanced: 'Advanced',
  layout: 'Layout',
};

export default function FieldPalette({ onAddField }: FieldPaletteProps) {
  return (
    <div className="p-3">
      <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider mb-3 px-1">
        Add Field
      </p>

      {Object.entries(FIELD_TYPE_CATEGORIES).map(([category, types]) => (
        <div key={category} className="mb-4">
          <p className="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1.5 px-1">
            {CATEGORY_LABELS[category as keyof typeof CATEGORY_LABELS]}
          </p>
          <div className="grid gap-0.5">
            {types.map((type) => (
              <button
                key={type}
                onClick={() => onAddField(type)}
                className="flex items-center gap-2 px-2 py-1.5 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md transition-colors text-left w-full"
              >
                <span className="text-gray-400">{ICONS[type]}</span>
                {FIELD_TYPE_LABELS[type]}
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
