import { useState, useEffect, useRef } from "react";
import { X, Upload, Save, FileText, Timer } from "lucide-react";
import { toast } from "sonner";
import { adminService } from "@/services/adminService";

interface HackathonSetupPanelProps {
  onClose: () => void;
}

export default function HackathonSetupPanel({ onClose }: HackathonSetupPanelProps) {
  const [title, setTitle] = useState("");
  const [htmlContent, setHtmlContent] = useState("");
  const [durationMinutes, setDurationMinutes] = useState<number | "">("");
  const [isSaving, setIsSaving] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    loadExisting();
  }, []);

  const loadExisting = async () => {
    try {
      const data = await adminService.getQuestionPaper();
      if (data?.configured) {
        setTitle(data.title || "");
        setHtmlContent(data.htmlContent || "");
        setDurationMinutes(data.durationMinutes ?? "");
      }
    } catch {
      // No existing config
    } finally {
      setIsLoading(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.name.endsWith(".html") && !file.name.endsWith(".htm")) {
      toast.error("Only .html or .htm files are allowed");
      return;
    }

    try {
      const result = await adminService.uploadQuestionHtml(file);
      setHtmlContent(result.htmlContent);
      setUploadedFileName(result.fileName);
      toast.success(`Uploaded: ${result.fileName}`);
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Upload failed");
    }

    // Reset file input
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleSave = async () => {
    if (!title.trim()) {
      toast.error("Title is required");
      return;
    }
    if (!htmlContent.trim()) {
      toast.error("Please upload a question HTML file");
      return;
    }

    setIsSaving(true);
    try {
      await adminService.saveQuestionPaper({
        title: title.trim(),
        htmlContent,
        durationMinutes: durationMinutes ? Number(durationMinutes) : undefined,
      });
      toast.success("Hackathon setup saved successfully");
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Save failed");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-white dark:bg-gray-800 rounded-xl p-8">
          <p className="text-gray-500">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b dark:border-gray-700">
          <div className="flex items-center gap-2">
            <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5">
              <FileText className="h-4 w-4 text-white" />
            </div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Hackathon Setup</h2>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-6">
          {/* Title */}
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">
              Hackathon Title *
            </label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g., SQL Hackathon 2026 — Smart Ticket System"
              className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none"
            />
          </div>

          {/* Duration */}
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 flex items-center gap-1">
              <Timer className="h-3.5 w-3.5" /> Session Duration (minutes)
            </label>
            <input
              type="number"
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(e.target.value ? parseInt(e.target.value) : "")}
              placeholder="e.g., 180 (used when activating sessions)"
              className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none"
            />
            <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
              This duration will be used as default when bulk-activating sessions.
            </p>
          </div>

          {/* Question Paper Upload */}
          <div>
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">
              Question Paper (HTML) *
            </label>
            <div className="border-2 border-dashed dark:border-gray-600 rounded-lg p-6 text-center hover:border-teal-400 dark:hover:border-teal-600 transition-colors">
              <input
                ref={fileInputRef}
                type="file"
                accept=".html,.htm"
                onChange={handleFileUpload}
                className="hidden"
                id="question-upload"
              />
              <label htmlFor="question-upload" className="cursor-pointer">
                <Upload className="h-8 w-8 text-gray-400 mx-auto mb-2" />
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Click to upload an HTML file
                </p>
                <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                  .html or .htm files only
                </p>
              </label>
            </div>

            {/* Upload status */}
            {(htmlContent || uploadedFileName) && (
              <div className="mt-3 flex items-center gap-2 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg px-3 py-2">
                <FileText className="h-4 w-4 text-green-600" />
                <span className="text-sm text-green-700 dark:text-green-400">
                  {uploadedFileName || "Question paper loaded"}
                  <span className="text-xs text-green-600 dark:text-green-500 ml-2">
                    ({(htmlContent.length / 1024).toFixed(1)} KB)
                  </span>
                </span>
              </div>
            )}

            {/* Preview */}
            {htmlContent && (
              <div className="mt-3">
                <p className="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Preview:</p>
                <div className="border dark:border-gray-600 rounded-lg overflow-hidden h-48">
                  <iframe
                    srcDoc={htmlContent}
                    title="Preview"
                    className="w-full h-full border-0"
                    sandbox="allow-same-origin"
                  />
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-3 px-6 py-4 border-t dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving}
            className="flex items-center gap-2 px-5 py-2 text-sm font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg disabled:opacity-50 transition-colors"
          >
            <Save className="h-4 w-4" />
            {isSaving ? "Saving..." : "Save Setup"}
          </button>
        </div>
      </div>
    </div>
  );
}
