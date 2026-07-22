import { useState, useRef } from "react";
import { Upload, FileSpreadsheet, X } from "lucide-react";
import { toast } from "sonner";
import { adminService } from "@/services/adminService";

interface BulkUserImportProps {
  onComplete: () => void;
  onClose: () => void;
}

export default function BulkUserImport({ onComplete, onClose }: BulkUserImportProps) {
  const [users, setUsers] = useState<{ UserID: string; Password: string; FullName: string; Email: string; DbEnginePreference?: string }[]>([]);
  const [isImporting, setIsImporting] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (ev) => {
      const text = ev.target?.result as string;
      const lines = text.split("\n").filter((l) => l.trim());

      // Skip header row if present
      const startIdx = lines[0]?.toLowerCase().includes("userid") ? 1 : 0;
      const parsed = lines.slice(startIdx).map((line) => {
        const parts = line.split(",").map((p) => p.trim().replace(/^["']|["']$/g, ""));
        return {
          UserID: parts[0] || "",
          Password: parts[1] || "",
          FullName: parts[2] || "",
          Email: parts[3] || "",
          DbEnginePreference: parts[4]?.toLowerCase() === "oracle" ? "Oracle" : "SqlServer",
        };
      }).filter((u) => u.UserID && u.Password);

      setUsers(parsed);
      toast.success(`Parsed ${parsed.length} users from CSV`);
    };
    reader.readAsText(file);
  };

  const handleImport = async () => {
    if (users.length === 0) {
      toast.error("No users to import");
      return;
    }
    setIsImporting(true);
    try {
      const result = await adminService.bulkCreateUsers(users);
      toast.success(result.data?.message || `${users.length} users imported`);
      onComplete();
    } catch (err: any) {
      toast.error(err.response?.data?.message || "Import failed");
    } finally {
      setIsImporting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl p-6 w-full max-w-lg max-h-[80vh] flex flex-col">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-800">Bulk Import Users</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Instructions */}
        <div className="bg-gray-50 rounded-lg p-3 mb-4 text-sm text-gray-600">
          <p className="font-medium mb-1">CSV Format:</p>
          <code className="text-xs font-mono bg-white px-2 py-1 rounded border">UserID,Password,FullName,Email</code>
          <p className="mt-2 text-xs text-gray-500">First row can be a header (auto-detected). Password and UserID are required.</p>
        </div>

        {/* Upload */}
        <div className="mb-4">
          <input ref={fileRef} type="file" accept=".csv,.txt" onChange={handleFileUpload} className="hidden" />
          <button
            onClick={() => fileRef.current?.click()}
            className="flex items-center gap-2 w-full px-4 py-3 border-2 border-dashed border-gray-300 rounded-lg text-gray-600 hover:border-teal-400 hover:text-teal-600 transition-colors"
          >
            <Upload className="h-5 w-5" />
            <span>Click to upload CSV file</span>
          </button>
        </div>

        {/* Preview */}
        {users.length > 0 && (
          <div className="flex-1 overflow-auto border rounded-lg mb-4">
            <table className="w-full text-xs">
              <thead className="bg-gray-50 sticky top-0">
                <tr>
                  <th className="px-2 py-1.5 text-left font-semibold">#</th>
                  <th className="px-2 py-1.5 text-left font-semibold">UserID</th>
                  <th className="px-2 py-1.5 text-left font-semibold">Password</th>
                  <th className="px-2 py-1.5 text-left font-semibold">FullName</th>
                  <th className="px-2 py-1.5 text-left font-semibold">Email</th>
                </tr>
              </thead>
              <tbody>
                {users.slice(0, 50).map((u, i) => (
                  <tr key={i} className="border-t">
                    <td className="px-2 py-1 text-gray-400">{i + 1}</td>
                    <td className="px-2 py-1 font-mono">{u.UserID}</td>
                    <td className="px-2 py-1 text-gray-400">•••</td>
                    <td className="px-2 py-1">{u.FullName || "—"}</td>
                    <td className="px-2 py-1">{u.Email || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {users.length > 50 && (
              <div className="px-2 py-1 text-xs text-gray-400 border-t bg-gray-50">
                Showing 50 of {users.length} users
              </div>
            )}
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-between pt-2 border-t">
          <span className="text-sm text-gray-500">
            {users.length > 0 ? `${users.length} users ready to import` : "Upload a CSV to begin"}
          </span>
          <div className="flex gap-2">
            <button onClick={onClose} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
            <button
              onClick={handleImport}
              disabled={users.length === 0 || isImporting}
              className="px-4 py-2 bg-teal-600 text-white rounded-lg text-sm hover:bg-teal-700 disabled:opacity-50 flex items-center gap-1.5"
            >
              <FileSpreadsheet className="h-4 w-4" />
              {isImporting ? "Importing..." : "Import All"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
