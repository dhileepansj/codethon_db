import { useState, useEffect, useRef } from "react";
import { Upload, Trash2, FileSpreadsheet, FileText, AlertTriangle, CheckCircle } from "lucide-react";
import { toast } from "sonner";
import { hackathonService } from "@/services/hackathonService";

interface SubmissionFile {
  id: number;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
}

interface SubmitPanelProps {
  onSubmitted: () => void;
  onCancel: () => void;
}

export default function SubmitPanel({ onSubmitted, onCancel }: SubmitPanelProps) {
  const [files, setFiles] = useState<SubmissionFile[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const MAX_TOTAL_SIZE = 5 * 1024 * 1024; // 5 MB

  useEffect(() => {
    loadFiles();
  }, []);

  const loadFiles = async () => {
    try {
      const data = await hackathonService.getSubmissionFiles();
      setFiles(data);
    } catch {
      // silent
    } finally {
      setIsLoading(false);
    }
  };

  const totalSize = files.reduce((sum, f) => sum + f.fileSizeBytes, 0);
  const remainingSize = MAX_TOTAL_SIZE - totalSize;

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(e.target.files || []);
    if (selectedFiles.length === 0) return;

    // Validate extensions
    const allowedExts = [".docx", ".doc", ".xlsx", ".xls"];
    for (const file of selectedFiles) {
      const ext = file.name.substring(file.name.lastIndexOf(".")).toLowerCase();
      if (!allowedExts.includes(ext)) {
        toast.error(`Invalid file type: ${file.name}. Only Word (.docx, .doc) and Excel (.xlsx, .xls) are allowed.`);
        if (fileInputRef.current) fileInputRef.current.value = "";
        return;
      }
    }

    // Validate size
    const newSize = selectedFiles.reduce((sum, f) => sum + f.size, 0);
    if (totalSize + newSize > MAX_TOTAL_SIZE) {
      toast.error(`Total file size exceeds 5 MB limit. Remaining space: ${(remainingSize / 1024).toFixed(0)} KB`);
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }

    setIsUploading(true);
    try {
      await hackathonService.uploadSubmissionFiles(selectedFiles);
      toast.success(`${selectedFiles.length} file(s) uploaded`);
      await loadFiles();
    } catch (err: any) {
      toast.error(err.response?.data?.data?.message || err.response?.data?.message || "Upload failed");
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleDelete = async (fileId: number, fileName: string) => {
    if (!confirm(`Delete "${fileName}"?`)) return;
    try {
      await hackathonService.deleteSubmissionFile(fileId);
      toast.success("File deleted");
      await loadFiles();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Delete failed");
    }
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);
    try {
      await hackathonService.submit();
      toast.success("Submission successful!");
      onSubmitted();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Submission failed");
    } finally {
      setIsSubmitting(false);
      setShowConfirm(false);
    }
  };

  const getFileIcon = (contentType: string) => {
    if (contentType.includes("spreadsheet") || contentType.includes("excel"))
      return <FileSpreadsheet className="h-4 w-4 text-green-600" />;
    return <FileText className="h-4 w-4 text-blue-600" />;
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60]">
      <div className="bg-white dark:bg-gray-800 rounded-xl w-full max-w-lg max-h-[85vh] overflow-hidden flex flex-col shadow-2xl">
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-700">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Submit Your Work</h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Upload supporting documents (optional) and submit your hackathon work.
          </p>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-5">
          {/* File Upload */}
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2 block">
              Supporting Documents (Word/Excel only, 5 MB total)
            </label>

            <div className="border-2 border-dashed dark:border-gray-600 rounded-lg p-4 text-center hover:border-teal-400 transition-colors">
              <input
                ref={fileInputRef}
                type="file"
                accept=".docx,.doc,.xlsx,.xls"
                multiple
                onChange={handleFileSelect}
                className="hidden"
                id="submission-files"
              />
              <label htmlFor="submission-files" className="cursor-pointer">
                <Upload className="h-6 w-6 text-gray-400 mx-auto mb-1" />
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  {isUploading ? "Uploading..." : "Click to upload files"}
                </p>
                <p className="text-xs text-gray-400 mt-0.5">
                  .docx, .doc, .xlsx, .xls • {(remainingSize / 1024).toFixed(0)} KB remaining
                </p>
              </label>
            </div>

            {/* Uploaded Files List */}
            {files.length > 0 && (
              <div className="mt-3 space-y-2">
                {files.map((file) => (
                  <div key={file.id} className="flex items-center justify-between bg-gray-50 dark:bg-gray-700 rounded-lg px-3 py-2">
                    <div className="flex items-center gap-2 min-w-0">
                      {getFileIcon(file.contentType)}
                      <div className="min-w-0">
                        <p className="text-sm text-gray-700 dark:text-gray-200 truncate">{file.fileName}</p>
                        <p className="text-xs text-gray-400">{(file.fileSizeBytes / 1024).toFixed(1)} KB</p>
                      </div>
                    </div>
                    <button
                      onClick={() => handleDelete(file.id, file.fileName)}
                      className="p-1 text-red-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                ))}
                <div className="text-xs text-gray-400 text-right">
                  Total: {(totalSize / 1024).toFixed(1)} KB / 5120 KB
                </div>
              </div>
            )}
          </div>

          {/* Warning */}
          <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <AlertTriangle className="h-5 w-5 text-amber-500 shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-amber-800 dark:text-amber-300">Important</p>
                <p className="text-sm text-amber-700 dark:text-amber-400 mt-1">
                  Once you submit, you will <strong>not be able to edit, execute queries, or make any changes</strong>. 
                  Make sure you have saved all your scripts before submitting.
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-3 px-6 py-4 border-t dark:border-gray-700">
          <button
            onClick={onCancel}
            className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
          >
            Cancel
          </button>
          <button
            onClick={() => setShowConfirm(true)}
            className="flex items-center gap-2 px-5 py-2 text-sm font-medium bg-green-600 hover:bg-green-700 text-white rounded-lg transition-colors"
          >
            <CheckCircle className="h-4 w-4" />
            Submit
          </button>
        </div>
      </div>

      {/* Confirm Dialog */}
      {showConfirm && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-[70]">
          <div className="bg-white dark:bg-gray-800 rounded-xl p-6 max-w-sm shadow-2xl">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">Confirm Submission</h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-5">
              Are you sure you want to submit? Once submitted, <strong>you cannot edit or view your work anymore</strong>. 
              This action cannot be undone (only an admin can release your submission).
            </p>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setShowConfirm(false)}
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              >
                Cancel
              </button>
              <button
                onClick={handleSubmit}
                disabled={isSubmitting}
                className="px-4 py-2 text-sm font-medium bg-red-600 hover:bg-red-700 text-white rounded-lg disabled:opacity-50"
              >
                {isSubmitting ? "Submitting..." : "Yes, Submit Final"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
