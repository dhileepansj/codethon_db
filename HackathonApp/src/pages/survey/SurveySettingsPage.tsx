import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Save } from 'lucide-react';
import { toast } from 'sonner';
import { surveyApi, distributionApi } from '../../services/surveyApi';
import { SurveyDetailDto, SurveyStatus, UpdateEmailSettingsDto } from '../../types/survey';
import SurveyAdminLayout from '../../components/survey/SurveyAdminLayout';

export default function SurveySettingsPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState<'general' | 'email' | 'status'>('general');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [thankYouMessage, setThankYouMessage] = useState('');
  const [allowMultiple, setAllowMultiple] = useState(false);
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [startsAt, setStartsAt] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [status, setStatus] = useState<SurveyStatus>(SurveyStatus.Draft);
  const [includeRm, setIncludeRm] = useState(false);
  const [includeVh, setIncludeVh] = useState(false);
  const [emailMode, setEmailMode] = useState(0);
  const [additionalCc, setAdditionalCc] = useState('');
  const [emailSubject, setEmailSubject] = useState('');
  const [emailBody, setEmailBody] = useState('');
  const [reminderEnabled, setReminderEnabled] = useState(false);
  const [reminderDays, setReminderDays] = useState(3);
  const [maxReminders, setMaxReminders] = useState(2);

  useEffect(() => { if (surveyId) loadData(); }, [surveyId]);

  async function loadData() {
    try {
      const [survey, emailSettings] = await Promise.all([surveyApi.getById(surveyId!), distributionApi.getEmailSettings(surveyId!).catch(() => null)]);
      setTitle(survey.title); setDescription(survey.description || ''); setThankYouMessage(survey.thankYouMessage || '');
      setAllowMultiple(survey.allowMultiple); setIsAnonymous(survey.isAnonymous);
      setStartsAt(survey.startsAt ? survey.startsAt.slice(0, 16) : ''); setExpiresAt(survey.expiresAt ? survey.expiresAt.slice(0, 16) : '');
      setStatus(survey.status);
      if (emailSettings && 'includeRmByDefault' in emailSettings) {
        setIncludeRm(emailSettings.includeRmByDefault); setIncludeVh(emailSettings.includeVhByDefault);
        setEmailMode(emailSettings.emailMode ?? 0);
        setAdditionalCc(emailSettings.additionalCcEmails || ''); setEmailSubject(emailSettings.emailSubject || '');
        setEmailBody(emailSettings.emailBody || ''); setReminderEnabled(emailSettings.reminderEnabled);
        setReminderDays(emailSettings.reminderDays); setMaxReminders(emailSettings.maxReminders);
      }
    } catch { toast.error('Failed to load settings'); } finally { setLoading(false); }
  }

  async function handleSaveGeneral() {
    setSaving(true);
    try { await surveyApi.update(surveyId!, { title: title || undefined, description: description || undefined, thankYouMessage: thankYouMessage || undefined, allowMultiple, isAnonymous, startsAt: startsAt || undefined, expiresAt: expiresAt || undefined }); toast.success('Settings saved'); }
    catch { toast.error('Failed to save settings'); } finally { setSaving(false); }
  }

  async function handleSaveEmail() {
    setSaving(true);
    try { await distributionApi.updateEmailSettings(surveyId!, { includeRmByDefault: includeRm, includeVhByDefault: includeVh, emailMode, additionalCcEmails: additionalCc || undefined, emailSubject: emailSubject || undefined, emailBody: emailBody || undefined, reminderEnabled, reminderDays, maxReminders }); toast.success('Email settings saved'); }
    catch { toast.error('Failed to save email settings'); } finally { setSaving(false); }
  }

  async function handleStatusChange(newStatus: SurveyStatus) {
    try { await surveyApi.updateStatus(surveyId!, newStatus); setStatus(newStatus); toast.success(`Survey status changed to ${STATUS_LABELS[newStatus]}`); }
    catch { toast.error('Failed to update status'); }
  }

  if (loading) return <SurveyAdminLayout><div className="flex items-center justify-center h-64 text-gray-500">Loading settings...</div></SurveyAdminLayout>;

  return (
    <SurveyAdminLayout>
      <div className="flex-1 overflow-y-auto bg-gray-50 dark:bg-gray-950">
        <header className="h-14 bg-white dark:bg-gray-900 border-b dark:border-gray-800 flex items-center px-6 sticky top-0 z-10">
          <h1 className="text-base font-semibold text-gray-800 dark:text-gray-100">Survey Settings</h1>
        </header>
        <div className="p-6 max-w-4xl mx-auto">
          <div className="flex gap-1 mb-6 border-b dark:border-gray-800 pb-px">
            {(['general', 'email', 'status'] as const).map((tab) => (
              <button key={tab} onClick={() => setActiveTab(tab)} className={`px-4 py-2 text-sm font-medium rounded-t-lg transition-colors capitalize ${activeTab === tab ? 'text-gray-800 dark:text-gray-100 bg-white dark:bg-gray-900 border dark:border-gray-800 border-b-transparent' : 'text-gray-500 hover:text-gray-800 dark:hover:text-white'}`}>
                {tab === 'email' ? 'Email & Distribution' : tab}
              </button>
            ))}
          </div>

          {activeTab === 'general' && (
            <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-6 space-y-5">
              <FormField label="Survey Title"><input type="text" value={title} onChange={(e) => setTitle(e.target.value)} className={INPUT_CLS} /></FormField>
              <FormField label="Description"><textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} placeholder="Brief description shown to respondents..." className={INPUT_CLS + ' resize-y'} /></FormField>
              <FormField label="Thank You Message"><textarea value={thankYouMessage} onChange={(e) => setThankYouMessage(e.target.value)} rows={2} placeholder="Message shown after submission..." className={INPUT_CLS + ' resize-y'} /></FormField>
              <div className="grid grid-cols-2 gap-4">
                <FormField label="Starts At (optional)"><input type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} className={INPUT_CLS} /></FormField>
                <FormField label="Expires At (optional)"><input type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} className={INPUT_CLS} /></FormField>
              </div>
              <div className="flex items-center gap-6 pt-2">
                <ToggleSetting label="Allow multiple responses" description="Participants can submit more than once" checked={allowMultiple} onChange={setAllowMultiple} />
                <ToggleSetting label="Anonymous responses" description="Don't link responses to participant identity" checked={isAnonymous} onChange={setIsAnonymous} />
              </div>
              <div className="pt-4 border-t dark:border-gray-800"><button onClick={handleSaveGeneral} disabled={saving} className="flex items-center gap-2 px-4 py-2 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg"><Save className="w-4 h-4" />{saving ? 'Saving...' : 'Save Settings'}</button></div>
            </div>
          )}

          {activeTab === 'email' && (
            <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-6 space-y-5">
              <h3 className="text-sm font-semibold text-gray-800 dark:text-gray-100">Email Sending Mode</h3>
          <div className="space-y-2">
            {[
              { value: 0, label: 'Single Bulk Email', desc: 'One email with all participants in TO, RMs/VHs in CC. Everyone sees everyone.' },
              { value: 1, label: 'Individual + Manager Summary', desc: 'Each participant gets their own email. RMs/VHs get one summary email listing their reportees.' },
              { value: 2, label: 'Individual with CC', desc: 'Each participant gets their own email with their RM/VH in CC. (RMs may receive multiple emails)' },
            ].map((mode) => (
              <label key={mode.value} className={`flex items-start gap-3 p-3 border rounded-lg cursor-pointer transition-all ${emailMode === mode.value ? 'border-teal-500 bg-teal-50 dark:bg-teal-900/10' : 'border-gray-200 dark:border-gray-800 hover:border-gray-300'}`}>
                <input type="radio" name="emailMode" value={mode.value} checked={emailMode === mode.value} onChange={() => setEmailMode(mode.value)} className="mt-0.5 accent-teal-500" />
                <div><p className="text-sm font-medium text-gray-800 dark:text-gray-100">{mode.label}</p><p className="text-xs text-gray-500 mt-0.5">{mode.desc}</p></div>
              </label>
            ))}
          </div>

          <hr className="border-gray-200 dark:border-gray-800" />
          <h3 className="text-sm font-semibold text-gray-800 dark:text-gray-100">Email Recipients</h3>
              <div className="flex items-center gap-6"><ToggleSetting label="Include Reporting Manager" description="CC the RM on invitation emails" checked={includeRm} onChange={setIncludeRm} /><ToggleSetting label="Include Vertical Head" description="CC the VH on invitation emails" checked={includeVh} onChange={setIncludeVh} /></div>
              <FormField label="Additional CC Emails"><input type="text" value={additionalCc} onChange={(e) => setAdditionalCc(e.target.value)} placeholder="hr@company.com, admin@company.com" className={INPUT_CLS} /><p className="text-xs text-gray-500 mt-1">Comma-separated email addresses</p></FormField>
              <hr className="border-gray-200 dark:border-gray-800" />
              <h3 className="text-sm font-semibold text-gray-800 dark:text-gray-100">Email Template</h3>
              <FormField label="Subject"><input type="text" value={emailSubject} onChange={(e) => setEmailSubject(e.target.value)} placeholder="You're invited to fill: {{SurveyTitle}}" className={INPUT_CLS} /><p className="text-xs text-gray-500 mt-1">Variables: {'{{EmployeeName}}'}, {'{{SurveyTitle}}'}, {'{{Deadline}}'}</p></FormField>
              <FormField label="Email Body (HTML)"><textarea value={emailBody} onChange={(e) => setEmailBody(e.target.value)} rows={6} placeholder="Leave blank for default template. Use {{SurveyLink}} for URL..." className={INPUT_CLS + ' resize-y font-mono text-xs'} /></FormField>
              <hr className="border-gray-200 dark:border-gray-800" />
              <h3 className="text-sm font-semibold text-gray-800 dark:text-gray-100">Auto-Reminders</h3>
              <ToggleSetting label="Enable auto-reminders" description="Automatically remind non-respondents" checked={reminderEnabled} onChange={setReminderEnabled} />
              {reminderEnabled && (<div className="grid grid-cols-2 gap-4 pl-4"><FormField label="Remind after (days)"><input type="number" value={reminderDays} onChange={(e) => setReminderDays(Number(e.target.value))} min={1} max={30} className={INPUT_CLS} /></FormField><FormField label="Max reminders"><input type="number" value={maxReminders} onChange={(e) => setMaxReminders(Number(e.target.value))} min={1} max={10} className={INPUT_CLS} /></FormField></div>)}
              <div className="pt-4 border-t dark:border-gray-800"><button onClick={handleSaveEmail} disabled={saving} className="flex items-center gap-2 px-4 py-2 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg"><Save className="w-4 h-4" />{saving ? 'Saving...' : 'Save Email Settings'}</button></div>
            </div>
          )}

          {activeTab === 'status' && (
            <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl p-6 space-y-5">
              <h3 className="text-sm font-semibold text-gray-800 dark:text-gray-100">Survey Lifecycle</h3>
              <p className="text-xs text-gray-500">Change the survey status. Only "Active" surveys accept responses.</p>
              <div className="flex items-center gap-3 mb-4"><span className="text-sm text-gray-500">Current:</span><span className={`px-2.5 py-1 text-xs font-semibold rounded-full ${STATUS_COLORS[status]}`}>{STATUS_LABELS[status]}</span></div>
              <div className="grid grid-cols-2 gap-3">
                {([{ value: SurveyStatus.Draft, label: 'Draft', desc: 'Not visible. Still editing.' }, { value: SurveyStatus.Active, label: 'Active', desc: 'Accepting responses.' }, { value: SurveyStatus.Closed, label: 'Closed', desc: 'No more responses.' }, { value: SurveyStatus.Archived, label: 'Archived', desc: 'Hidden from list.' }] as const).map((s) => (
                  <button key={s.value} onClick={() => handleStatusChange(s.value)} disabled={status === s.value} className={`text-left p-4 border rounded-xl transition-all ${status === s.value ? 'border-teal-500 bg-teal-50 dark:bg-teal-900/10' : 'border-gray-200 dark:border-gray-800 hover:border-gray-300 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800/30'}`}>
                    <span className={`text-sm font-medium ${status === s.value ? 'text-teal-600' : 'text-gray-800 dark:text-gray-100'}`}>{s.label}</span>
                    <p className="text-xs text-gray-500 mt-1">{s.desc}</p>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </SurveyAdminLayout>
  );
}

const INPUT_CLS = "w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500";

function FormField({ label, children }: { label: string; children: React.ReactNode }) {
  return <div><label className="text-sm text-gray-600 dark:text-gray-400 block mb-1">{label}</label>{children}</div>;
}

function ToggleSetting({ label, description, checked, onChange }: { label: string; description: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-start gap-3">
      <button onClick={() => onChange(!checked)} className={`mt-0.5 w-9 h-5 rounded-full transition-colors flex-shrink-0 ${checked ? 'bg-teal-600' : 'bg-gray-300 dark:bg-gray-600'}`}><div className={`w-3.5 h-3.5 rounded-full bg-white transition-transform mx-0.5 mt-[3px] ${checked ? 'translate-x-4' : 'translate-x-0'}`} /></button>
      <div><p className="text-sm text-gray-800 dark:text-gray-100">{label}</p><p className="text-xs text-gray-500">{description}</p></div>
    </div>
  );
}

const STATUS_LABELS: Record<SurveyStatus, string> = { [SurveyStatus.Draft]: 'Draft', [SurveyStatus.Active]: 'Active', [SurveyStatus.Closed]: 'Closed', [SurveyStatus.Archived]: 'Archived' };
const STATUS_COLORS: Record<SurveyStatus, string> = { [SurveyStatus.Draft]: 'bg-gray-100 text-gray-700', [SurveyStatus.Active]: 'bg-green-100 text-green-700', [SurveyStatus.Closed]: 'bg-orange-100 text-orange-700', [SurveyStatus.Archived]: 'bg-red-100 text-red-700' };
