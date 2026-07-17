import { useEffect, useState } from "react";
import { Code, Plus, Trash2, Save, XCircle, ChevronUp, ChevronDown, Eye, EyeOff } from "lucide-react";
import { toast } from "sonner";
import { scaffoldService } from "@/services/scaffoldService";
import type { ScaffoldScript } from "@/services/scaffoldService";

interface ScaffoldScriptsPanelProps {
  onClose: () => void;
}

export default function ScaffoldScriptsPanel({ onClose }: ScaffoldScriptsPanelProps) {
  const [scripts, setScripts] = useState<ScaffoldScript[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);

  // New script form
  const [newTitle, setNewTitle] = useState("");
  const [newFileName, setNewFileName] = useState("");
  const [newSql, setNewSql] = useState("");

  useEffect(() => { loadScripts(); }, []);

  const loadScripts = async () => {
    setLoading(true);
    try {
      const data = await scaffoldService.getAll();
      setScripts(data);
    } catch {
      toast.error("Failed to load scaffold scripts");
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = async () => {
    if (!newTitle.trim() || !newSql.trim()) {
      toast.error("Title and SQL content are required");
      return;
    }
    try {
      await scaffoldService.create({
        title: newTitle.trim(),
        fileName: newFileName.trim() || `${newTitle.trim()}.sql`,
        sqlContent: newSql,
        executionOrder: scripts.length + 1,
      });
      toast.success("Script added");
      setShowAdd(false);
      setNewTitle("");
      setNewFileName("");
      setNewSql("");
      loadScripts();
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Failed to add");
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Delete this scaffold script?")) return;
    try {
      await scaffoldService.delete(id);
      toast.success("Script deleted");
      loadScripts();
    } catch {
      toast.error("Failed to delete");
    }
  };

  const handleToggleActive = async (script: ScaffoldScript) => {
    try {
      await scaffoldService.update(script.id, { ...script, isActive: !script.isActive });
      loadScripts();
    } catch {
      toast.error("Failed to update");
    }
  };

  const handleMoveUp = async (index: number) => {
    if (index === 0) return;
    const updated = [...scripts];
    [updated[index - 1], updated[index]] = [updated[index], updated[index - 1]];
    // Update execution orders
    for (let i = 0; i < updated.length; i++) {
      await scaffoldService.update(updated[i].id, { ...updated[i], executionOrder: i + 1 });
    }
    loadScripts();
  };

  const handleMoveDown = async (index: number) => {
    if (index === scripts.length - 1) return;
    const updated = [...scripts];
    [updated[index], updated[index + 1]] = [updated[index + 1], updated[index]];
    for (let i = 0; i < updated.length; i++) {
      await scaffoldService.update(updated[i].id, { ...updated[i], executionOrder: i + 1 });
    }
    loadScripts();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={onClose}>
      <div className="w-full max-w-3xl bg-white dark:bg-gray-900 h-full overflow-hidden flex flex-col shadow-2xl" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="px-6 py-4 border-b dark:border-gray-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="bg-gradient-to-r from-violet-500 to-fuchsia-500 rounded-lg p-2">
              <Code className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Scaffold Scripts</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Scripts executed when participants create their DB. Also provided in their File Manager.
              </p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">
            <XCircle className="h-5 w-5 text-gray-500" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4">
          {loading ? (
            <p className="text-center text-gray-400 py-12">Loading...</p>
          ) : (
            <>
              {/* Script List */}
              <div className="space-y-2 mb-4">
                {scripts.map((script, idx) => (
                  <div
                    key={script.id}
                    className={`border dark:border-gray-700 rounded-lg overflow-hidden ${!script.isActive ? "opacity-50" : ""}`}
                  >
                    <div className="px-4 py-3 flex items-center gap-3 bg-gray-50 dark:bg-gray-800/50">
                      {/* Order badge */}
                      <span className="w-7 h-7 rounded-full bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-400 text-xs font-bold flex items-center justify-center">
                        {idx + 1}
                      </span>

                      {/* Info */}
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-800 dark:text-gray-100 truncate">{script.title}</p>
                        <p className="text-xs text-gray-400 font-mono">{script.fileName}</p>
                      </div>

                      {/* Actions */}
                      <div className="flex items-center gap-1">
                        <button onClick={() => handleMoveUp(idx)} disabled={idx === 0} className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded disabled:opacity-30" title="Move up">
                          <ChevronUp className="h-4 w-4 text-gray-500" />
                        </button>
                        <button onClick={() => handleMoveDown(idx)} disabled={idx === scripts.length - 1} className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded disabled:opacity-30" title="Move down">
                          <ChevronDown className="h-4 w-4 text-gray-500" />
                        </button>
                        <button onClick={() => handleToggleActive(script)} className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded" title={script.isActive ? "Disable" : "Enable"}>
                          {script.isActive ? <Eye className="h-4 w-4 text-green-500" /> : <EyeOff className="h-4 w-4 text-gray-400" />}
                        </button>
                        <button onClick={() => setEditingId(editingId === script.id ? null : script.id)} className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded" title="View SQL">
                          <Code className="h-4 w-4 text-blue-500" />
                        </button>
                        <button onClick={() => handleDelete(script.id)} className="p-1 hover:bg-red-100 dark:hover:bg-red-900/20 rounded" title="Delete">
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </button>
                      </div>
                    </div>

                    {/* Expanded SQL */}
                    {editingId === script.id && (
                      <div className="px-4 py-3 border-t dark:border-gray-700 bg-gray-900">
                        <pre className="text-xs text-gray-300 font-mono whitespace-pre-wrap max-h-60 overflow-auto">{script.sqlContent}</pre>
                      </div>
                    )}
                  </div>
                ))}

                {scripts.length === 0 && !showAdd && (
                  <div className="text-center py-12">
                    <Code className="h-12 w-12 text-gray-300 mx-auto mb-3" />
                    <p className="text-gray-500">No scaffold scripts configured</p>
                    <p className="text-xs text-gray-400 mt-1">Add scripts that will be executed when participants create their database.</p>
                  </div>
                )}
              </div>

              {/* Add New Script Form */}
              {showAdd ? (
                <div className="border dark:border-gray-700 rounded-lg p-4 bg-blue-50 dark:bg-blue-900/10 space-y-3">
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-200">Add Scaffold Script</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">Title *</label>
                      <input
                        value={newTitle}
                        onChange={(e) => setNewTitle(e.target.value)}
                        placeholder="e.g., Create Tables"
                        className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                      />
                    </div>
                    <div>
                      <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">File Name</label>
                      <input
                        value={newFileName}
                        onChange={(e) => setNewFileName(e.target.value)}
                        placeholder="e.g., 01_CreateTables.sql"
                        className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white"
                      />
                    </div>
                  </div>
                  <div>
                    <label className="text-xs font-medium text-gray-600 dark:text-gray-300 mb-1 block">SQL Content *</label>
                    <textarea
                      value={newSql}
                      onChange={(e) => setNewSql(e.target.value)}
                      placeholder="-- Paste your SQL script here..."
                      rows={10}
                      className="w-full px-3 py-2 border dark:border-gray-600 rounded-lg text-sm font-mono dark:bg-gray-700 dark:text-white resize-y"
                    />
                  </div>
                  <div className="flex gap-2">
                    <button onClick={handleAdd} className="flex items-center gap-1.5 px-4 py-2 bg-violet-600 hover:bg-violet-700 text-white rounded-lg text-sm font-medium">
                      <Save className="h-3.5 w-3.5" /> Add Script
                    </button>
                    <button onClick={() => setShowAdd(false)} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg">
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  onClick={() => setShowAdd(true)}
                  className="w-full flex items-center justify-center gap-2 px-4 py-3 border-2 border-dashed dark:border-gray-700 rounded-lg text-sm font-medium text-gray-500 hover:text-violet-600 hover:border-violet-400 transition-colors"
                >
                  <Plus className="h-4 w-4" /> Add Scaffold Script
                </button>
              )}
            </>
          )}
        </div>

        {/* Footer info */}
        <div className="px-6 py-3 border-t dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Scripts are executed in order when a participant creates their database. They're also saved into a "Starter Scripts" folder in the participant's File Manager.
          </p>
        </div>
      </div>
    </div>
  );
}
