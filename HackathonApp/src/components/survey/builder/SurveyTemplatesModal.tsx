import { X, FileText, ClipboardList, MessageSquare, Award, UserCheck } from 'lucide-react';
import { CreateFieldDto, SurveyFieldType } from '../../../types/survey';

interface SurveyTemplatesModalProps {
  onApply: (fields: CreateFieldDto[]) => void;
  onClose: () => void;
}

interface Template {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  fields: CreateFieldDto[];
}

const TEMPLATES: Template[] = [
  {
    id: 'feedback', name: 'Feedback Form', description: 'General feedback collection with rating and comments', icon: <MessageSquare className="w-5 h-5" />,
    fields: [
      { fieldType: SurveyFieldType.Section, label: 'Feedback', sectionTitle: 'Your Feedback' },
      { fieldType: SurveyFieldType.Rating, label: 'How would you rate your overall experience?', isRequired: true },
      { fieldType: SurveyFieldType.Radio, label: 'Would you recommend this to others?', isRequired: true, options: JSON.stringify([{ value: 'definitely', label: 'Definitely' }, { value: 'probably', label: 'Probably' }, { value: 'not_sure', label: 'Not Sure' }, { value: 'no', label: 'No' }]) },
      { fieldType: SurveyFieldType.LongText, label: 'What did you like the most?', placeholder: 'Tell us what went well...' },
      { fieldType: SurveyFieldType.LongText, label: 'What could be improved?', placeholder: 'Share your suggestions...' },
    ],
  },
  {
    id: 'event-registration', name: 'Event Registration', description: 'Collect attendee details for events', icon: <ClipboardList className="w-5 h-5" />,
    fields: [
      { fieldType: SurveyFieldType.Section, label: 'Registration', sectionTitle: 'Event Registration' },
      { fieldType: SurveyFieldType.ShortText, label: 'Full Name', isRequired: true, placeholder: 'Your full name' },
      { fieldType: SurveyFieldType.Email, label: 'Email Address', isRequired: true },
      { fieldType: SurveyFieldType.Phone, label: 'Phone Number' },
      { fieldType: SurveyFieldType.Dropdown, label: 'Department', isRequired: true, options: JSON.stringify([{ value: 'engineering', label: 'Engineering' }, { value: 'sales', label: 'Sales' }, { value: 'hr', label: 'HR' }, { value: 'finance', label: 'Finance' }, { value: 'other', label: 'Other' }]) },
      { fieldType: SurveyFieldType.YesNo, label: 'Do you need transportation assistance?' },
    ],
  },
  {
    id: 'training-assessment', name: 'Training Assessment', description: 'Post-training feedback and knowledge check', icon: <Award className="w-5 h-5" />,
    fields: [
      { fieldType: SurveyFieldType.Section, label: 'Assessment', sectionTitle: 'Training Assessment' },
      { fieldType: SurveyFieldType.Scale, label: "Rate the trainer's knowledge", isRequired: true },
      { fieldType: SurveyFieldType.Scale, label: 'Rate the quality of material', isRequired: true },
      { fieldType: SurveyFieldType.Radio, label: 'Was the duration adequate?', isRequired: true, options: JSON.stringify([{ value: 'too_short', label: 'Too Short' }, { value: 'just_right', label: 'Just Right' }, { value: 'too_long', label: 'Too Long' }]) },
      { fieldType: SurveyFieldType.Rating, label: 'Overall training rating', isRequired: true },
    ],
  },
  {
    id: 'employee-satisfaction', name: 'Employee Satisfaction', description: 'Measure team satisfaction and engagement', icon: <UserCheck className="w-5 h-5" />,
    fields: [
      { fieldType: SurveyFieldType.Section, label: 'Satisfaction', sectionTitle: 'Employee Satisfaction' },
      { fieldType: SurveyFieldType.Scale, label: 'How satisfied are you with your role?', isRequired: true },
      { fieldType: SurveyFieldType.Scale, label: 'How well does your manager support you?', isRequired: true },
      { fieldType: SurveyFieldType.Radio, label: 'Would you recommend this company?', isRequired: true, options: JSON.stringify([{ value: 'definitely', label: 'Definitely' }, { value: 'probably', label: 'Probably' }, { value: 'not_sure', label: 'Not Sure' }, { value: 'no', label: 'No' }]) },
      { fieldType: SurveyFieldType.LongText, label: 'What one thing would you change?' },
    ],
  },
  { id: 'blank', name: 'Blank Form', description: 'Start from scratch', icon: <FileText className="w-5 h-5" />, fields: [] },
];

export default function SurveyTemplatesModal({ onApply, onClose }: SurveyTemplatesModalProps) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-xl w-full max-w-2xl max-h-[80vh] overflow-hidden flex flex-col shadow-2xl">
        <div className="flex items-center justify-between p-5 border-b dark:border-gray-700">
          <div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Survey Templates</h2>
            <p className="text-sm text-gray-500 mt-0.5">Choose a template to add pre-built fields</p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 dark:hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-5">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {TEMPLATES.map((tmpl) => (
              <button
                key={tmpl.id}
                onClick={() => onApply(tmpl.fields)}
                className="text-left p-4 bg-gray-50 dark:bg-gray-900 border dark:border-gray-700 rounded-xl hover:border-teal-500 hover:bg-teal-50 dark:hover:bg-teal-900/10 transition-all group"
              >
                <div className="flex items-center gap-3 mb-2">
                  <span className="text-gray-400 group-hover:text-teal-600 transition-colors">{tmpl.icon}</span>
                  <h3 className="text-sm font-medium text-gray-800 dark:text-gray-100">{tmpl.name}</h3>
                </div>
                <p className="text-xs text-gray-500">{tmpl.description}</p>
                {tmpl.fields.length > 0 && <p className="text-xs text-gray-400 mt-2">{tmpl.fields.length} fields</p>}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
