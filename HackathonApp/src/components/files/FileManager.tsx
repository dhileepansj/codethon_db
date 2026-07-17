import { useEffect, useState, useCallback } from "react";
import { Folder, File, Plus, FolderPlus, Trash2, Edit, ArrowLeft, Save, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { fileService } from "@/services/fileService";
import type { FolderDto, FileListItem, FileDetail } from "@/types";

interface FileManagerProps {
  onOpenFile?: (content: string, fileName: string, fileId?: number) => void;
  currentEditorContent?: string;
}

export default function FileManager({ onOpenFile, currentEditorContent }: FileManagerProps) {
  const [folders, setFolders] = useState<FolderDto[]>([]);
  const [files, setFiles] = useState<FileListItem[]>([]);
  const [currentFolderId, setCurrentFolderId] = useState<number | null>(null);
  const [folderStack, setFolderStack] = useState<{ id: number | null; name: string }[]>([{ id: null, name: "Root" }]);
  const [loading, setLoading] = useState(true);
  const [showNewFileDialog, setShowNewFileDialog] = useState(false);
  const [showNewFolderDialog, setShowNewFolderDialog] = useState(false);
  const [newName, setNewName] = useState("");
  const [newFileType, setNewFileType] = useState("Script");

  const loadContents = useCallback(async () => {
    setLoading(true);
    try {
      const data = currentFolderId
        ? await fileService.getFolderContents(currentFolderId)
        : await fileService.getRoot();
      setFolders(data.folders);
      setFiles(data.files);
    } catch (err: any) {
      toast.error("Failed to load files");
    } finally {
      setLoading(false);
    }
  }, [currentFolderId]);

  useEffect(() => { loadContents(); }, [loadContents]);

  const navigateToFolder = (folderId: number, folderName: string) => {
    setCurrentFolderId(folderId);
    setFolderStack((prev) => [...prev, { id: folderId, name: folderName }]);
  };

  const navigateBack = () => {
    if (folderStack.length <= 1) return;
    const newStack = folderStack.slice(0, -1);
    setFolderStack(newStack);
    setCurrentFolderId(newStack[newStack.length - 1].id);
  };

  const handleCreateFolder = async () => {
    if (!newName.trim()) return;
    try {
      await fileService.createFolder(newName.trim(), currentFolderId ?? undefined);
      toast.success("Folder created");
      setShowNewFolderDialog(false);
      setNewName("");
      loadContents();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Failed");
    }
  };

  const handleCreateFile = async () => {
    if (!newName.trim()) return;
    try {
      await fileService.createFile({
        fileName: newName.trim(),
        fileType: newFileType,
        content: currentEditorContent || "",
        folderId: currentFolderId ?? undefined,
      });
      toast.success("File saved");
      setShowNewFileDialog(false);
      setNewName("");
      loadContents();
    } catch (err: any) {
      const data = err.response?.data;
      toast.error(data?.errors?.[0] || data?.Errors?.[0] || data?.message || "Failed");
    }
  };

  const handleOpenFile = async (fileId: number) => {
    try {
      const file: FileDetail = await fileService.getFile(fileId);
      if (onOpenFile) {
        onOpenFile(file.content || "", file.fileName, file.fileId);
      }
      toast.success(`Opened: ${file.fileName}`);
    } catch {
      toast.error("Failed to open file");
    }
  };

  const handleDeleteFile = async (fileId: number, fileName: string) => {
    if (!confirm(`Delete "${fileName}"?`)) return;
    try {
      await fileService.deleteFile(fileId);
      toast.success("File deleted");
      loadContents();
    } catch {
      toast.error("Failed to delete");
    }
  };

  const handleDeleteFolder = async (folderId: number, folderName: string) => {
    if (!confirm(`Delete folder "${folderName}" and all its contents?`)) return;
    try {
      await fileService.deleteFolder(folderId);
      toast.success("Folder deleted");
      loadContents();
    } catch {
      toast.error("Failed to delete folder");
    }
  };

  const fileTypeColors: Record<string, string> = {
    Script: "bg-blue-100 text-blue-700",
    StoredProcedure: "bg-purple-100 text-purple-700",
    Function: "bg-cyan-100 text-cyan-700",
    Trigger: "bg-orange-100 text-orange-700",
    View: "bg-green-100 text-green-700",
    Other: "bg-gray-100 text-gray-700",
  };

  return (
    <div className="h-full flex flex-col overflow-hidden">
      {/* Header */}
      <div className="px-3 py-2 border-b flex items-center justify-between shrink-0 bg-gray-50">
        <div className="flex items-center gap-2">
          {folderStack.length > 1 && (
            <button onClick={navigateBack} className="text-gray-400 hover:text-gray-700">
              <ArrowLeft className="h-4 w-4" />
            </button>
          )}
          <span className="font-semibold text-gray-700 text-xs uppercase tracking-wide">
            {folderStack[folderStack.length - 1].name}
          </span>
        </div>
        <div className="flex items-center gap-1">
          <button onClick={() => { setShowNewFolderDialog(true); setNewName(""); }} className="p-1 hover:bg-teal-100 rounded text-teal-600" title="New Folder">
            <FolderPlus className="h-4 w-4" />
          </button>
          <button onClick={() => { setShowNewFileDialog(true); setNewName(""); }} className="p-1 hover:bg-teal-100 rounded text-teal-600" title="Save File">
            <Plus className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Breadcrumb */}
      {folderStack.length > 1 && (
        <div className="px-3 py-1 text-xs text-gray-400 border-b shrink-0">
          {folderStack.map((f, i) => (
            <span key={i}>
              {i > 0 && " / "}
              <span className={i === folderStack.length - 1 ? "text-gray-600 font-medium" : ""}>{f.name}</span>
            </span>
          ))}
        </div>
      )}

      {/* Content */}
      <div className="flex-1 overflow-y-auto p-2">
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-5 w-5 animate-spin text-teal-500" />
          </div>
        ) : (
          <>
            {/* Folders */}
            {folders.map((folder) => (
              <div key={folder.folderId} className="flex items-center gap-2 px-2 py-1.5 rounded-md hover:bg-gray-100 group">
                <button onClick={() => navigateToFolder(folder.folderId, folder.folderName)} className="flex items-center gap-2 flex-1 text-left">
                  <Folder className="h-4 w-4 text-amber-500" />
                  <span className="text-sm text-gray-700 font-medium">{folder.folderName}</span>
                  <span className="text-xs text-gray-400">{folder.fileCount} files</span>
                </button>
                <button onClick={() => handleDeleteFolder(folder.folderId, folder.folderName)} className="p-1 opacity-0 group-hover:opacity-100 hover:bg-red-100 rounded text-red-500">
                  <Trash2 className="h-3 w-3" />
                </button>
              </div>
            ))}

            {/* Files */}
            {files.map((file) => (
              <div key={file.fileId} className="flex items-center gap-2 px-2 py-1.5 rounded-md hover:bg-gray-100 group">
                <button onClick={() => handleOpenFile(file.fileId)} className="flex items-center gap-2 flex-1 text-left">
                  <File className="h-4 w-4 text-gray-400" />
                  <span className="text-sm text-gray-700">{file.fileName}</span>
                  <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${fileTypeColors[file.fileType] || fileTypeColors.Other}`}>
                    {file.fileType}
                  </span>
                </button>
                <button onClick={() => handleDeleteFile(file.fileId, file.fileName)} className="p-1 opacity-0 group-hover:opacity-100 hover:bg-red-100 rounded text-red-500">
                  <Trash2 className="h-3 w-3" />
                </button>
              </div>
            ))}

            {folders.length === 0 && files.length === 0 && (
              <div className="text-center py-8 text-gray-400 text-sm">
                <File className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p>No files yet</p>
                <p className="text-xs mt-1">Save your SQL scripts here</p>
              </div>
            )}
          </>
        )}
      </div>

      {/* New Folder Dialog */}
      {showNewFolderDialog && (
        <Dialog title="Create Folder" onClose={() => setShowNewFolderDialog(false)} onConfirm={handleCreateFolder}>
          <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="Folder name" autoFocus className="w-full px-3 py-2 border rounded-lg text-sm" />
        </Dialog>
      )}

      {/* New File Dialog */}
      {showNewFileDialog && (
        <Dialog title="Save File" onClose={() => setShowNewFileDialog(false)} onConfirm={handleCreateFile}>
          <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="File name (e.g., CreateTables.sql)" autoFocus className="w-full px-3 py-2 border rounded-lg text-sm mb-2" />
          <select value={newFileType} onChange={(e) => setNewFileType(e.target.value)} className="w-full px-3 py-2 border rounded-lg text-sm">
            <option value="Script">Script</option>
            <option value="StoredProcedure">Stored Procedure</option>
            <option value="Function">Function</option>
            <option value="Trigger">Trigger</option>
            <option value="View">View</option>
            <option value="Other">Other</option>
          </select>
        </Dialog>
      )}
    </div>
  );
}

function Dialog({ title, onClose, onConfirm, children }: {
  title: string; onClose: () => void; onConfirm: () => void; children: React.ReactNode;
}) {
  return (
    <div className="absolute inset-0 bg-black/30 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl p-4 w-64 shadow-xl">
        <h4 className="font-semibold text-gray-800 mb-3">{title}</h4>
        {children}
        <div className="flex justify-end gap-2 mt-3">
          <button onClick={onClose} className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 rounded">Cancel</button>
          <button onClick={onConfirm} className="px-3 py-1.5 text-sm bg-teal-600 text-white rounded hover:bg-teal-700">Save</button>
        </div>
      </div>
    </div>
  );
}
