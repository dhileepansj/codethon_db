import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Upload, Download, Send, Bell, X, UserX } from 'lucide-react';
import { toast } from 'sonner';
import { participantApi, distributionApi } from '../../services/surveyApi';
import { SurveyParticipantDto, ParticipantStatus, DeclinedByType, BulkUploadResultDto } from '../../types/survey';
import SurveyAdminLayout from '../../components/survey/SurveyAdminLayout';
import { ConfirmDialog } from '../../components/common/CustomDialog';

const STATUS_LABELS: Record<ParticipantStatus, string> = { [ParticipantStatus.Pending]: 'Not Sent', [ParticipantStatus.Sent]: 'Sent', [ParticipantStatus.Reminded]: 'Reminded', [ParticipantStatus.Responded]: 'Responded', [ParticipantStatus.Declined]: 'Declined' };
const STATUS_COLORS: Record<ParticipantStatus, string> = { [ParticipantStatus.Pending]: 'bg-gray-100 text-gray-600', [ParticipantStatus.Sent]: 'bg-blue-100 text-blue-700', [ParticipantStatus.Reminded]: 'bg-yellow-100 text-yellow-700', [ParticipantStatus.Responded]: 'bg-green-100 text-green-700', [ParticipantStatus.Declined]: 'bg-red-100 text-red-700' };

export default function SurveyParticipantsPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const navigate = useNavigate();
  const [participants, setParticipants] = useState<SurveyParticipantDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showUpload, setShowUpload] = useState(false);
  const [showDecline, setShowDecline] = useState<string | null>(null);
  const [selectedForReminder, setSelectedForReminder] = useState<string[]>([]);
  const [uploadResult, setUploadResult] = useState<BulkUploadResultDto | null>(null);
  const [showConfirmSend, setShowConfirmSend] = useState(false);

  useEffect(() => { if (surveyId) loadParticipants(); }, [surveyId]);

  async function loadParticipants() { try { const data = await participantApi.getAll(surveyId!); setParticipants(data); } catch { toast.error('Failed to load participants'); } finally { setLoading(false); } }
  async function handleUpload(file: File) { try { const result = await participantApi.upload(surveyId!, file); setUploadResult(result); if (result.successCount > 0) { toast.success(`${result.successCount} participants added`); loadParticipants(); } if (result.errorCount > 0) toast.error(`${result.errorCount} rows had errors`); } catch { toast.error('Upload failed'); } }
  async function handleDistribute() { try { const result = await distributionApi.send(surveyId!); toast.success((result as any).message || `Emails sent`); loadParticipants(); } catch (err: any) { const msg = err?.response?.data?.data?.message || err?.response?.data?.message || 'Failed to send invitations'; toast.error(msg); } finally { setShowConfirmSend(false); } }
  async function handleSendReminder() { if (selectedForReminder.length === 0) { toast.error('Select participants to remind'); return; } try { const result = await distributionApi.remind(surveyId!, selectedForReminder); toast.success((result as any).message || 'Reminders sent'); setSelectedForReminder([]); loadParticipants(); } catch (err: any) { const msg = err?.response?.data?.data?.message || err?.response?.data?.message || 'Failed to send reminders'; toast.error(msg); } }
  async function handleDecline(participantId: string, declinedBy: DeclinedByType, reason: string, attachment?: File) { try { await participantApi.decline(surveyId!, participantId, { declinedBy, reason }, attachment); toast.success('Participant marked as declined'); setShowDecline(null); loadParticipants(); } catch { toast.error('Failed to decline participant'); } }
  async function handleReset(participantId: string) { try { await participantApi.resetStatus(surveyId!, participantId); toast.success('Status reset'); loadParticipants(); } catch { toast.error('Failed to reset status'); } }
  async function handleDownloadTemplate() { try { const blob = await participantApi.downloadTemplate(surveyId!); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = 'participant_template.csv'; a.click(); URL.revokeObjectURL(url); } catch { toast.error('Failed to download template'); } }

  const pendingParticipants = participants.filter((p) => p.status === ParticipantStatus.Sent || p.status === ParticipantStatus.Reminded);

  if (loading) return <SurveyAdminLayout><div className="flex items-center justify-center h-64 text-gray-500">Loading...</div></SurveyAdminLayout>;

  return (
    <SurveyAdminLayout>
      <div className="flex-1 overflow-y-auto bg-gray-50 dark:bg-gray-950">
        <header className="h-14 bg-white dark:bg-gray-900 border-b dark:border-gray-800 flex items-center justify-between px-6 sticky top-0 z-10">
          <div><h1 className="text-base font-semibold text-gray-800 dark:text-gray-100">Participants</h1><p className="text-xs text-gray-500">{participants.length} total</p></div>
          <div className="flex items-center gap-2">
            <button onClick={handleDownloadTemplate} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs text-gray-600 hover:text-gray-800 border rounded-md hover:bg-gray-50"><Download className="w-3.5 h-3.5" /> Template</button>
            <button onClick={() => setShowUpload(true)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs text-gray-600 hover:text-gray-800 border rounded-md hover:bg-gray-50"><Upload className="w-3.5 h-3.5" /> Upload CSV</button>
            <button onClick={() => setShowConfirmSend(true)} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs bg-teal-600 hover:bg-teal-700 text-white rounded-md font-medium"><Send className="w-3.5 h-3.5" /> Send Invitations</button>
            {selectedForReminder.length > 0 && <button onClick={handleSendReminder} className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs bg-amber-500 hover:bg-amber-600 text-white rounded-md font-medium"><Bell className="w-3.5 h-3.5" /> Remind ({selectedForReminder.length})</button>}
          </div>
        </header>

        <div className="p-6">
          {showConfirmSend && <ConfirmDialog title="Send Invitations" message="Send survey invitations to all pending participants?" confirmLabel="Send Now" onConfirm={handleDistribute} onCancel={() => setShowConfirmSend(false)} />}
          {showUpload && <UploadModal onUpload={handleUpload} onClose={() => { setShowUpload(false); setUploadResult(null); }} result={uploadResult} />}
          {showDecline && <DeclineModal onDecline={(db, r, a) => handleDecline(showDecline, db, r, a)} onClose={() => setShowDecline(null)} />}

          <div className="bg-white dark:bg-gray-900 border dark:border-gray-800 rounded-xl overflow-hidden">
            <table className="w-full text-sm">
              <thead><tr className="border-b dark:border-gray-800 text-[11px] font-semibold text-gray-500 uppercase tracking-wider">
                <th className="px-4 py-3 text-left w-8"><input type="checkbox" onChange={(e) => { if (e.target.checked) setSelectedForReminder(pendingParticipants.map((p) => p.id)); else setSelectedForReminder([]); }} className="accent-teal-500" /></th>
                <th className="px-4 py-3 text-left">Employee</th><th className="px-4 py-3 text-left">Email</th><th className="px-4 py-3 text-left">RM</th><th className="px-4 py-3 text-left">VH</th><th className="px-4 py-3 text-left">Status</th><th className="px-4 py-3 text-left w-24">Actions</th>
              </tr></thead>
              <tbody className="divide-y dark:divide-gray-800">
                {participants.map((p) => (
                  <tr key={p.id} className="hover:bg-teal-50/40 dark:hover:bg-teal-900/5">
                    <td className="px-4 py-3">{(p.status === ParticipantStatus.Sent || p.status === ParticipantStatus.Reminded) && <input type="checkbox" checked={selectedForReminder.includes(p.id)} onChange={(e) => { if (e.target.checked) setSelectedForReminder((prev) => [...prev, p.id]); else setSelectedForReminder((prev) => prev.filter((id) => id !== p.id)); }} className="accent-teal-500" />}</td>
                    <td className="px-4 py-3"><div className="text-sm font-medium text-gray-800 dark:text-gray-100">{p.employeeName}</div><div className="text-xs text-gray-400">{p.employeeId}</div></td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300 text-xs">{p.employeeEmail}</td>
                    <td className="px-4 py-3 text-gray-500 text-xs">{p.rmName || '—'}</td>
                    <td className="px-4 py-3 text-gray-500 text-xs">{p.vhName || '—'}</td>
                    <td className="px-4 py-3"><span className={`px-2 py-0.5 text-[10px] font-semibold rounded-full ${STATUS_COLORS[p.status]}`}>{STATUS_LABELS[p.status]}</span>{p.reminderCount > 0 && <span className="ml-1 text-xs text-gray-400">({p.reminderCount}x)</span>}</td>
                    <td className="px-4 py-3">
                      {p.status !== ParticipantStatus.Declined && p.status !== ParticipantStatus.Responded && <button onClick={() => setShowDecline(p.id)} className="p-1 text-gray-400 hover:text-red-500 rounded" title="Mark Declined"><UserX className="w-3.5 h-3.5" /></button>}
                      {p.status === ParticipantStatus.Declined && <button onClick={() => handleReset(p.id)} className="px-2 py-0.5 text-[10px] text-teal-600 hover:text-teal-700 border border-teal-300 rounded font-medium">Reset</button>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {participants.length === 0 && <div className="text-center py-12 text-gray-400">No participants yet. Upload a CSV to add participants.</div>}
          </div>
        </div>
      </div>
    </SurveyAdminLayout>
  );
}

function UploadModal({ onUpload, onClose, result }: { onUpload: (file: File) => void; onClose: () => void; result: BulkUploadResultDto | null }) {
  const [dragOver, setDragOver] = useState(false);
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 shadow-2xl border dark:border-gray-700 rounded-xl p-6 w-full max-w-lg">
        <div className="flex items-center justify-between mb-4"><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Upload Participants</h2><button onClick={onClose} className="text-gray-400 hover:text-gray-600"><X className="w-5 h-5" /></button></div>
        <div onDragOver={(e) => { e.preventDefault(); setDragOver(true); }} onDragLeave={() => setDragOver(false)} onDrop={(e) => { e.preventDefault(); setDragOver(false); const f = e.dataTransfer.files[0]; if (f) onUpload(f); }} className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${dragOver ? 'border-teal-500 bg-teal-50' : 'border-gray-300'}`}>
          <Upload className="w-8 h-8 text-gray-400 mx-auto mb-3" /><p className="text-sm text-gray-600 mb-2">Drag & drop a CSV file here</p>
          <label className="inline-block px-4 py-2 text-sm bg-teal-600 hover:bg-teal-700 text-white rounded-lg cursor-pointer font-medium">Browse Files<input type="file" accept=".csv,.txt" onChange={(e) => { const f = e.target.files?.[0]; if (f) onUpload(f); }} className="hidden" /></label>
          <p className="text-xs text-gray-400 mt-3">Columns: EmployeeId, EmployeeName, EmployeeEmail, RmName, RmEmail, VhName, VhEmail</p>
        </div>
        {result && <div className="mt-4 p-3 bg-gray-50 dark:bg-gray-900 rounded-lg"><div className="flex gap-4 text-sm"><span className="text-green-600">{result.successCount} added</span><span className="text-red-600">{result.errorCount} errors</span><span className="text-gray-500">{result.totalRows} rows</span></div>{result.errors.length > 0 && <div className="mt-2 max-h-32 overflow-y-auto text-xs space-y-1">{result.errors.map((err, i) => <p key={i} className="text-red-600">Row {err.row}: {err.field} — {err.message}</p>)}</div>}</div>}
      </div>
    </div>
  );
}

function DeclineModal({ onDecline, onClose }: { onDecline: (d: DeclinedByType, r: string, a?: File) => void; onClose: () => void }) {
  const [declinedBy, setDeclinedBy] = useState<DeclinedByType>(DeclinedByType.ReportingManager);
  const [reason, setReason] = useState('');
  const [attachment, setAttachment] = useState<File | undefined>();
  const INPUT_CLS = "w-full px-3 py-2 bg-gray-50 dark:bg-gray-800 border dark:border-gray-700 rounded-lg text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500";
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 shadow-2xl border dark:border-gray-700 rounded-xl p-6 w-full max-w-md">
        <div className="flex items-center justify-between mb-4"><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Mark as Declined</h2><button onClick={onClose} className="text-gray-400 hover:text-gray-600"><X className="w-5 h-5" /></button></div>
        <div className="space-y-4">
          <div><label className="text-xs text-gray-500 block mb-1">Declined by</label><select value={declinedBy} onChange={(e) => setDeclinedBy(Number(e.target.value))} className={INPUT_CLS}><option value={DeclinedByType.ReportingManager}>Reporting Manager</option><option value={DeclinedByType.VerticalHead}>Vertical Head</option><option value={DeclinedByType.Self}>Self</option></select></div>
          <div><label className="text-xs text-gray-500 block mb-1">Reason</label><textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} placeholder="Reason for declining..." className={INPUT_CLS + ' resize-none'} /></div>
          <div><label className="text-xs text-gray-500 block mb-1">Attachment (optional)</label><input type="file" accept=".pdf,.png,.jpg,.jpeg,.eml" onChange={(e) => setAttachment(e.target.files?.[0])} className="w-full text-sm text-gray-500 file:mr-4 file:py-1 file:px-3 file:rounded file:border-0 file:text-xs file:bg-gray-100 file:text-gray-700" /></div>
        </div>
        <div className="flex justify-end gap-3 mt-6"><button onClick={onClose} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-800">Cancel</button><button onClick={() => onDecline(declinedBy, reason, attachment)} disabled={!reason.trim()} className="px-4 py-2 text-sm bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white rounded-lg font-medium">Mark Declined</button></div>
      </div>
    </div>
  );
}
