import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { respondApi } from '../../services/surveyApi';
import {
  SurveyFormDto, ParticipantInfoDto, SurveyFieldDto,
  SurveyFieldType, FieldDependencyDto, DependencyCondition, DependencyAction,
} from '../../types/survey';
import { CheckCircle, Loader2 } from 'lucide-react';

type Step = 'email' | 'otp' | 'form' | 'submitted';

export default function SurveyRespondPage() {
  const { token } = useParams<{ token: string }>();
  const [step, setStep] = useState<Step>('email');
  const [email, setEmail] = useState('');
  const [maskedEmail, setMaskedEmail] = useState('');
  const [otp, setOtp] = useState('');
  const [sessionToken, setSessionToken] = useState('');
  const [participant, setParticipant] = useState<ParticipantInfoDto | null>(null);
  const [form, setForm] = useState<SurveyFormDto | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [thankYouMessage, setThankYouMessage] = useState('');
  const [startTime] = useState(Date.now());

  // ─── Step 1: Email Verification ──────────────────────────────────────────
  async function handleVerifyEmail() {
    if (!email.trim() || !token) return;
    setLoading(true);
    try {
      const result = await respondApi.verifyEmail(token, email.trim());
      if (result.isValid) {
        setMaskedEmail(result.maskedEmail || email);
        await handleSendOtp();
      } else {
        toast.error(result.message || 'Email verification failed');
      }
    } catch (err: any) {
      const msg = err?.response?.data?.data?.message || err?.response?.data?.message || 'Verification failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }

  async function handleSendOtp() {
    if (!token) return;
    setLoading(true);
    try {
      const result = await respondApi.sendOtp(token, email.trim());
      if (result.success) {
        setMaskedEmail(result.maskedEmail || email);
        setStep('otp');
        toast.success('OTP sent to your email');
      } else {
        toast.error(result.message || 'Failed to send OTP');
      }
    } catch (err: any) {
      const msg = err?.response?.data?.data?.message || err?.response?.data?.message || 'Failed to send OTP';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }

  // ─── Step 2: OTP Verification ────────────────────────────────────────────
  async function handleVerifyOtp() {
    if (!otp.trim() || !token) return;
    setLoading(true);
    try {
      const result = await respondApi.verifyOtp(token, email.trim(), otp.trim());
      if (result.success && result.sessionToken) {
        setSessionToken(result.sessionToken);
        setParticipant(result.participantInfo || null);
        // Load the form
        const formData = await respondApi.getForm(token, result.sessionToken);
        setForm(formData);
        setStep('form');
      } else {
        toast.error(result.message || 'OTP verification failed');
      }
    } catch (err: any) {
      const msg = err?.response?.data?.data?.message || err?.response?.data?.message || 'OTP verification failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }

  // ─── Step 3: Submit Form ─────────────────────────────────────────────────
  async function handleSubmit() {
    if (!token || !form) return;

    // Validate required fields
    const visibleFields = getVisibleFields(form.fields, answers);
    const missingRequired = visibleFields.filter(
      (f) => f.isRequired && !answers[f.id]?.trim() &&
        f.fieldType !== SurveyFieldType.Section && f.fieldType !== SurveyFieldType.Paragraph
    );

    if (missingRequired.length > 0) {
      toast.error(`Please fill all required fields (${missingRequired.length} missing)`);
      return;
    }

    setLoading(true);
    try {
      const timeTaken = Math.round((Date.now() - startTime) / 1000);
      const result = await respondApi.submit(token, sessionToken, {
        answers: Object.entries(answers).map(([fieldId, value]) => ({ fieldId, value })),
        timeTakenSeconds: timeTaken,
      });
      setThankYouMessage((result as any)?.message || form.thankYouMessage || 'Thank you for your response!');
      setStep('submitted');
    } catch (err: any) {
      const msg = err?.response?.data?.data?.message || err?.response?.data?.message || 'Submission failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }

  // ─── Render ──────────────────────────────────────────────────────────────
  return (
    <div className="min-h-screen bg-gray-100 dark:bg-gray-950 flex items-center justify-center p-4">
      <div className="w-full max-w-xl">
        {step === 'email' && (
          <EmailStep
            email={email}
            setEmail={setEmail}
            onSubmit={handleVerifyEmail}
            loading={loading}
            surveyTitle="Survey"
          />
        )}

        {step === 'otp' && (
          <OtpStep
            maskedEmail={maskedEmail}
            otp={otp}
            setOtp={setOtp}
            onVerify={handleVerifyOtp}
            onResend={handleSendOtp}
            loading={loading}
          />
        )}

        {step === 'form' && form && participant && (
          <FormStep
            form={form}
            participant={participant}
            answers={answers}
            setAnswers={setAnswers}
            onSubmit={handleSubmit}
            loading={loading}
          />
        )}

        {step === 'submitted' && (
          <SubmittedStep message={thankYouMessage} />
        )}
      </div>
    </div>
  );
}

// ─── Step Components ──────────────────────────────────────────────────────────

function EmailStep({ email, setEmail, onSubmit, loading, surveyTitle }: {
  email: string; setEmail: (v: string) => void; onSubmit: () => void; loading: boolean; surveyTitle: string;
}) {
  return (
    <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-8 text-center">
      <h1 className="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-2">{surveyTitle}</h1>
      <p className="text-sm text-gray-500 mb-6">Enter your email to begin</p>
      <input
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        onKeyDown={(e) => e.key === 'Enter' && onSubmit()}
        placeholder="your.email@company.com"
        className="w-full px-4 py-3 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 mb-4"
        autoFocus
      />
      <button
        onClick={onSubmit}
        disabled={!email.trim() || loading}
        className="w-full px-4 py-3 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-gray-800 dark:text-gray-100 rounded-lg font-medium transition-colors flex items-center justify-center gap-2"
      >
        {loading && <Loader2 className="w-4 h-4 animate-spin" />}
        Continue
      </button>
    </div>
  );
}

function OtpStep({ maskedEmail, otp, setOtp, onVerify, onResend, loading }: {
  maskedEmail: string; otp: string; setOtp: (v: string) => void;
  onVerify: () => void; onResend: () => void; loading: boolean;
}) {
  return (
    <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-8 text-center">
      <h2 className="text-lg font-medium text-gray-800 dark:text-gray-100 mb-2">Verify Your Identity</h2>
      <p className="text-sm text-gray-500 mb-6">
        We've sent a 6-digit OTP to <span className="text-gray-300">{maskedEmail}</span>
      </p>
      <input
        type="text"
        value={otp}
        onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
        onKeyDown={(e) => e.key === 'Enter' && otp.length === 6 && onVerify()}
        placeholder="Enter 6-digit OTP"
        className="w-full px-4 py-3 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-gray-800 dark:text-gray-100 text-center text-lg tracking-[0.5em] placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 mb-4"
        maxLength={6}
        autoFocus
      />
      <button
        onClick={onVerify}
        disabled={otp.length !== 6 || loading}
        className="w-full px-4 py-3 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-gray-800 dark:text-gray-100 rounded-lg font-medium transition-colors flex items-center justify-center gap-2 mb-3"
      >
        {loading && <Loader2 className="w-4 h-4 animate-spin" />}
        Verify
      </button>
      <button
        onClick={onResend}
        className="text-sm text-teal-600 hover:text-blue-300 transition-colors"
      >
        Didn't receive? Resend OTP
      </button>
    </div>
  );
}

function FormStep({ form, participant, answers, setAnswers, onSubmit, loading }: {
  form: SurveyFormDto; participant: ParticipantInfoDto;
  answers: Record<string, string>; setAnswers: (a: Record<string, string>) => void;
  onSubmit: () => void; loading: boolean;
}) {
  const visibleFields = getVisibleFields(form.fields, answers);

  function updateAnswer(fieldId: string, value: string) {
    setAnswers({ ...answers, [fieldId]: value });
  }

  return (
    <div className="space-y-4">
      {/* Survey Header */}
      <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-6">
        <h1 className="text-xl font-semibold text-gray-800 dark:text-gray-100">{form.title}</h1>
        {form.description && <p className="text-sm text-gray-500 mt-2">{form.description}</p>}
      </div>

      {/* Participant Info (read-only) */}
      <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-4">
        <p className="text-xs text-gray-500 mb-2">Your Information (auto-filled)</p>
        <div className="grid grid-cols-3 gap-3 text-sm">
          <div>
            <span className="text-gray-500">ID:</span>
            <span className="ml-2 text-gray-300">{participant.employeeId}</span>
          </div>
          <div>
            <span className="text-gray-500">Name:</span>
            <span className="ml-2 text-gray-300">{participant.employeeName}</span>
          </div>
          <div>
            <span className="text-gray-500">Email:</span>
            <span className="ml-2 text-gray-300">{participant.employeeEmail}</span>
          </div>
        </div>
      </div>

      {/* Fields */}
      {visibleFields.map((field) => (
        <div key={field.id} className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-5">
          <SurveyFieldRenderer
            field={field}
            value={answers[field.id] || ''}
            answers={answers}
            onChange={(val) => updateAnswer(field.id, val)}
          />
        </div>
      ))}

      {/* Submit */}
      <button
        onClick={onSubmit}
        disabled={loading}
        className="w-full px-4 py-3 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-gray-800 dark:text-gray-100 rounded-lg font-medium transition-colors flex items-center justify-center gap-2"
      >
        {loading && <Loader2 className="w-4 h-4 animate-spin" />}
        Submit Response
      </button>
    </div>
  );
}

function SubmittedStep({ message }: { message: string }) {
  return (
    <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-8 text-center">
      <CheckCircle className="w-16 h-16 text-green-400 mx-auto mb-4" />
      <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-2">Response Submitted</h2>
      <p className="text-sm text-gray-400">{message}</p>
    </div>
  );
}

// ─── Field Renderer ───────────────────────────────────────────────────────────

function SurveyFieldRenderer({ field, value, answers, onChange }: {
  field: SurveyFieldDto; value: string; answers: Record<string, string>; onChange: (v: string) => void;
}) {
  if (field.fieldType === SurveyFieldType.Section) {
    return (
      <div>
        <h3 className="text-lg font-medium text-gray-800 dark:text-gray-100">{field.sectionTitle || field.label}</h3>
        {field.description && <p className="text-sm text-gray-400 mt-1">{field.description}</p>}
      </div>
    );
  }

  if (field.fieldType === SurveyFieldType.Paragraph) {
    return <p className="text-sm text-gray-400">{field.label}</p>;
  }

  // Resolve options with cascading support
  const options = resolveCascadingOptions(field, answers);

  return (
    <div>
      <label className="text-sm font-medium text-gray-800 dark:text-gray-100">
        {field.label}
        {field.isRequired && <span className="text-red-400 ml-1">*</span>}
      </label>
      {field.description && <p className="text-xs text-gray-500 mt-0.5">{field.description}</p>}

      <div className="mt-2">
        {(field.fieldType === SurveyFieldType.ShortText || field.fieldType === SurveyFieldType.Email ||
          field.fieldType === SurveyFieldType.Phone || field.fieldType === SurveyFieldType.Number) && (
          <input
            type={field.fieldType === SurveyFieldType.Number ? 'number' : field.fieldType === SurveyFieldType.Email ? 'email' : 'text'}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder || ''}
            className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500"
          />
        )}

        {field.fieldType === SurveyFieldType.LongText && (
          <textarea
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder || ''}
            rows={4}
            className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 resize-y"
          />
        )}

        {(field.fieldType === SurveyFieldType.Dropdown || field.fieldType === SurveyFieldType.MultiSelect) && (
          <select
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500"
          >
            <option value="">Select...</option>
            {options.map((opt) => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        )}

        {field.fieldType === SurveyFieldType.Radio && (
          <div className="space-y-2">
            {options.map((opt) => (
              <label key={opt.value} className="flex items-center gap-3 cursor-pointer">
                <input
                  type="radio"
                  name={field.id}
                  value={opt.value}
                  checked={value === opt.value}
                  onChange={() => onChange(opt.value)}
                  className="w-4 h-4 accent-teal-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">{opt.label}</span>
              </label>
            ))}
          </div>
        )}

        {field.fieldType === SurveyFieldType.Checkbox && (
          <div className="space-y-2">
            {options.map((opt) => {
              const selected = value ? JSON.parse(value || '[]') : [];
              const isChecked = selected.includes(opt.value);
              return (
                <label key={opt.value} className="flex items-center gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={isChecked}
                    onChange={() => {
                      const updated = isChecked
                        ? selected.filter((v: string) => v !== opt.value)
                        : [...selected, opt.value];
                      onChange(JSON.stringify(updated));
                    }}
                    className="w-4 h-4 accent-teal-500"
                  />
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
                <input
                  type="radio"
                  name={field.id}
                  value={opt}
                  checked={value === opt}
                  onChange={() => onChange(opt)}
                  className="w-4 h-4 accent-teal-500"
                />
                <span className="text-sm text-gray-300">{opt}</span>
              </label>
            ))}
          </div>
        )}

        {field.fieldType === SurveyFieldType.Rating && (
          <div className="flex gap-2">
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                onClick={() => onChange(star.toString())}
                className={`text-2xl ${Number(value) >= star ? 'text-yellow-400' : 'text-gray-600'} hover:text-yellow-400 transition-colors`}
              >
                ★
              </button>
            ))}
          </div>
        )}

        {field.fieldType === SurveyFieldType.Scale && (
          <div className="flex gap-1">
            {Array.from({ length: 10 }, (_, i) => i + 1).map((n) => (
              <button
                key={n}
                type="button"
                onClick={() => onChange(n.toString())}
                className={`w-8 h-8 rounded text-sm font-medium transition-colors ${
                  value === n.toString() ? 'bg-teal-600 text-gray-800 dark:text-gray-100' : 'bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 text-gray-400 hover:border-teal-500'
                }`}
              >
                {n}
              </button>
            ))}
          </div>
        )}

        {(field.fieldType === SurveyFieldType.Date || field.fieldType === SurveyFieldType.DateTime) && (
          <input
            type={field.fieldType === SurveyFieldType.DateTime ? 'datetime-local' : 'date'}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500"
          />
        )}

        {field.fieldType === SurveyFieldType.Time && (
          <input
            type="time"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500"
          />
        )}
      </div>
    </div>
  );
}

// ─── Cascading Dropdown Support ───────────────────────────────────────────────

function resolveCascadingOptions(field: SurveyFieldDto, answers: Record<string, string>): { value: string; label: string }[] {
  // Check if this field has a SetOptions dependency
  const setOptionsDep = field.dependencies.find((d) => d.action === DependencyAction.SetOptions);

  if (setOptionsDep && setOptionsDep.optionMap) {
    const parentValue = answers[setOptionsDep.dependsOnFieldId] || '';
    if (parentValue) {
      try {
        const optionMap: Record<string, string[]> = JSON.parse(setOptionsDep.optionMap);
        const dynamicOptions = optionMap[parentValue];
        if (dynamicOptions) {
          return dynamicOptions.map((opt) => ({ value: opt.toLowerCase().replace(/\s+/g, '_'), label: opt }));
        }
      } catch {
        // Fall through to static options
      }
    }
    // If parent not selected, return empty
    return [];
  }

  // Use static options
  return parseFieldOptions(field.options);
}

// ─── Dependency Engine ────────────────────────────────────────────────────────

function getVisibleFields(fields: SurveyFieldDto[], answers: Record<string, string>): SurveyFieldDto[] {
  return fields.filter((field) => {
    if (field.dependencies.length === 0) return true;

    // Evaluate all dependency rules
    return field.dependencies.every((dep) => {
      if (dep.action !== DependencyAction.Show) return true;
      return evaluateCondition(dep, answers);
    });
  });
}

function evaluateCondition(dep: FieldDependencyDto, answers: Record<string, string>): boolean {
  const parentValue = answers[dep.dependsOnFieldId] || '';

  switch (dep.condition) {
    case DependencyCondition.Equals:
      return parentValue === dep.value;
    case DependencyCondition.NotEquals:
      return parentValue !== dep.value;
    case DependencyCondition.Contains:
      return parentValue.toLowerCase().includes((dep.value || '').toLowerCase());
    case DependencyCondition.GreaterThan:
      return Number(parentValue) > Number(dep.value || 0);
    case DependencyCondition.LessThan:
      return Number(parentValue) < Number(dep.value || 0);
    case DependencyCondition.IsEmpty:
      return !parentValue;
    case DependencyCondition.IsNotEmpty:
      return !!parentValue;
    default:
      return true;
  }
}

function parseFieldOptions(json?: string): { value: string; label: string }[] {
  if (!json) return [];
  try { return JSON.parse(json); } catch { return []; }
}
