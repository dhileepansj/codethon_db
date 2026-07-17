import { useEffect, useState, useCallback } from "react";
import { Clock, CheckCircle, XCircle, AlertTriangle, ChevronLeft, ChevronRight, Loader2, Copy, ExternalLink } from "lucide-react";
import { toast } from "sonner";
import { registerInternalCopy } from "@/services/clipboardGuard";
import httpClient from "@/services/httpClient";

interface HistoryItem {
  id: number;
  databaseName?: string;
  queryText: string;
  queryPreview: string;
  queryType?: string;
  status: string;
  errorMessage?: string;
  rowsAffected?: number;
  durationMs?: number;
  executedAt: string;
}

interface ExecutionHistoryProps {
  onOpenInEditor?: (content: string) => void;
}

export default function ExecutionHistory({ onOpenInEditor }: ExecutionHistoryProps) {
  const [items, setItems] = useState<HistoryItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const pageSize = 20;

  const loadHistory = useCallback(async () => {
    setLoading(true);
    try {
      const res = await httpClient.get("/api/history", { params: { page, pageSize } });
      const data = res.data.data;
      setItems(data.items);
      setTotalCount(data.totalCount);
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => { loadHistory(); }, [loadHistory]);

  const totalPages = Math.ceil(totalCount / pageSize);

  const statusIcon = (status: string) => {
    switch (status) {
      case "Success": return <CheckCircle className="h-3.5 w-3.5 text-green-500" />;
      case "Failed": return <XCircle className="h-3.5 w-3.5 text-red-500" />;
      case "Timeout": return <AlertTriangle className="h-3.5 w-3.5 text-orange-500" />;
      default: return <Clock className="h-3.5 w-3.5 text-gray-400" />;
    }
  };

  const statusColor = (status: string) => {
    switch (status) {
      case "Success": return "bg-green-100 text-green-700";
      case "Failed": return "bg-red-100 text-red-700";
      case "Timeout": return "bg-orange-100 text-orange-700";
      default: return "bg-gray-100 text-gray-700";
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="h-6 w-6 animate-spin text-teal-500" />
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col overflow-hidden">
      {/* Header */}
      <div className="px-4 py-2 border-b bg-gray-50 flex items-center justify-between shrink-0">
        <div className="flex items-center gap-2">
          <Clock className="h-4 w-4 text-teal-600" />
          <span className="font-semibold text-gray-700 text-sm">Execution History</span>
          <span className="text-xs text-gray-400">({totalCount} total)</span>
        </div>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto">
        <table className="w-full">
          <thead className="bg-gray-50 sticky top-0">
            <tr className="text-left">
              <th className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase w-8"></th>
              <th className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase">Query</th>
              <th className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase w-20">Type</th>
              <th className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase w-16">Time</th>
              <th className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase w-16">Rows</th>
              <th className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase w-36">Executed</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr
                key={item.id}
                className="border-b hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
                onClick={() => setExpandedId(expandedId === item.id ? null : item.id)}
                title={item.errorMessage || "Click to expand full query"}
              >
                <td className="px-3 py-2">{statusIcon(item.status)}</td>
                <td className="px-3 py-2">
                  {expandedId === item.id ? (
                    <div>
                      <pre className="text-xs text-gray-700 dark:text-gray-300 font-mono whitespace-pre-wrap max-h-60 overflow-auto bg-gray-50 dark:bg-gray-800 p-2 rounded border dark:border-gray-700">{item.queryText}</pre>
                      <div className="flex items-center gap-2 mt-1.5">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigator.clipboard.writeText(item.queryText);
                            registerInternalCopy(item.queryText);
                            toast.success("Query copied to clipboard");
                          }}
                          className="flex items-center gap-1 px-2 py-1 text-[10px] font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded transition-colors"
                        >
                          <Copy className="h-3 w-3" /> Copy
                        </button>
                        {onOpenInEditor && (
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              onOpenInEditor(item.queryText);
                            }}
                            className="flex items-center gap-1 px-2 py-1 text-[10px] font-medium text-teal-600 dark:text-teal-400 hover:bg-teal-50 dark:hover:bg-teal-900/20 rounded transition-colors"
                          >
                            <ExternalLink className="h-3 w-3" /> Open in Editor
                          </button>
                        )}
                      </div>
                    </div>
                  ) : (
                    <code className="text-xs text-gray-700 dark:text-gray-300 font-mono line-clamp-1">{item.queryPreview}</code>
                  )}
                  {item.errorMessage && (
                    <p className="text-xs text-red-500 mt-0.5 line-clamp-2">{item.errorMessage}</p>
                  )}
                </td>
                <td className="px-3 py-2">
                  <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium ${statusColor(item.status)}`}>
                    {item.queryType || "—"}
                  </span>
                </td>
                <td className="px-3 py-2 text-xs text-gray-500 dark:text-gray-400 font-mono">{item.durationMs}ms</td>
                <td className="px-3 py-2 text-xs text-gray-500 dark:text-gray-400">{item.rowsAffected ?? "—"}</td>
                <td className="px-3 py-2 text-xs text-gray-500 dark:text-gray-400">
                  {new Date(item.executedAt).toLocaleString("en-IN", { hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit", day: "2-digit", month: "short" })}
                </td>
              </tr>
            ))}
            {items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-400 text-sm">
                  No execution history yet. Start running queries!
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="px-4 py-2 border-t bg-gray-50 flex items-center justify-between shrink-0">
          <span className="text-xs text-gray-500">Page {page} of {totalPages}</span>
          <div className="flex items-center gap-1">
            <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1} className="p-1 rounded hover:bg-gray-200 disabled:opacity-30">
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="p-1 rounded hover:bg-gray-200 disabled:opacity-30">
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
