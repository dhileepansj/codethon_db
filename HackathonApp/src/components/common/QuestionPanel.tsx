import { useState, useEffect } from "react";
import { X, FileText, Maximize2, Minimize2 } from "lucide-react";
import { hackathonService } from "@/services/hackathonService";
import { registerFileContent } from "@/services/clipboardGuard";

interface QuestionPanelProps {
  onClose: () => void;
}

export default function QuestionPanel({ onClose }: QuestionPanelProps) {
  const [paper, setPaper] = useState<{
    title: string;
    htmlContent: string;
    scheduledDate?: string;
    startTime?: string;
    endTime?: string;
    durationMinutes?: number;
  } | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isFullScreen, setIsFullScreen] = useState(false);

  useEffect(() => {
    hackathonService.getQuestionPaper().then((data) => {
      setPaper(data);
      setIsLoading(false);
      // Register question paper text content so copy-paste from it is allowed
      if (data?.htmlContent) {
        const tempDiv = document.createElement("div");
        tempDiv.innerHTML = data.htmlContent;
        const textContent = tempDiv.textContent || tempDiv.innerText || "";
        registerFileContent(textContent);
      }
    });
  }, []);

  if (isLoading) {
    return (
      <div className="h-full flex items-center justify-center bg-white dark:bg-gray-900">
        <div className="text-sm text-gray-500 dark:text-gray-400">Loading question paper...</div>
      </div>
    );
  }

  if (!paper) {
    return (
      <div className="h-full flex flex-col bg-white dark:bg-gray-900">
        <div className="h-10 border-b dark:border-gray-700 flex items-center justify-between px-3 shrink-0 bg-gray-50 dark:bg-gray-800">
          <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-200">
            <FileText className="h-4 w-4 text-teal-600" />
            Question Paper
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="flex-1 flex items-center justify-center">
          <p className="text-sm text-gray-400">No question paper has been published yet.</p>
        </div>
      </div>
    );
  }

  const panelClass = isFullScreen
    ? "fixed inset-0 z-[80] flex flex-col bg-white dark:bg-gray-900"
    : "h-full flex flex-col bg-white dark:bg-gray-900";

  return (
    <div className={panelClass}>
      {/* Header */}
      <div className="h-10 border-b dark:border-gray-700 flex items-center justify-between px-3 shrink-0 bg-gray-50 dark:bg-gray-800">
        <div className="flex items-center gap-2">
          <FileText className="h-4 w-4 text-teal-600" />
          <span className="text-sm font-medium text-gray-700 dark:text-gray-200 truncate max-w-[200px]">
            {paper.title}
          </span>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={() => setIsFullScreen(!isFullScreen)}
            className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded"
            title={isFullScreen ? "Exit full screen" : "Full screen"}
          >
            {isFullScreen ? <Minimize2 className="h-3.5 w-3.5" /> : <Maximize2 className="h-3.5 w-3.5" />}
          </button>
          <button onClick={onClose} className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded">
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* HTML Content */}
      <div className="flex-1 overflow-auto">
        <iframe
          srcDoc={paper.htmlContent}
          title="Question Paper"
          className="w-full h-full border-0"
          sandbox="allow-same-origin"
          style={{ minHeight: "100%" }}
        />
      </div>
    </div>
  );
}
