import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { Database, Loader2, LogOut } from "lucide-react";
import { toast } from "sonner";
import { hackathonService } from "@/services/hackathonService";
import { logout } from "@/redux/slices/authSlice";
import HackathonGuidelines from "@/components/common/HackathonGuidelines";
import type { AppDispatch, RootState } from "@/redux/store";

export default function CreateDatabasePage() {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useSelector((s: RootState) => s.auth);
  const [isCreating, setIsCreating] = useState(false);
  const [showGuidelines, setShowGuidelines] = useState(true);

  const isOracle = user?.dbEnginePreference === "Oracle";

  const handleCreate = async () => {
    setIsCreating(true);
    try {
      const result = await hackathonService.createDatabase();
      toast.success(result.message || (isOracle ? "Schema created!" : "Database created!"));
      navigate("/", { replace: true });
    } catch (err: any) {
      const msg = err.response?.data?.errors?.[0] || err.response?.data?.message || (isOracle ? "Failed to create schema" : "Failed to create database");
      toast.error(msg);
    } finally {
      setIsCreating(false);
    }
  };

  const handleLogout = () => {
    dispatch(logout());
    navigate("/login", { replace: true });
  };

  return (
    <div className="min-h-screen flex flex-col bg-gradient-to-br from-slate-50 to-teal-50">
      {/* Header */}
      <header className="h-12 bg-white border-b flex items-center justify-between px-6 shrink-0 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-lg p-1.5">
            <Database className="h-4 w-4 text-white" />
          </div>
          <span className="font-semibold text-teal-800">NovacCodeLab</span>
        </div>
        <button
          onClick={handleLogout}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-500 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors"
        >
          <LogOut className="h-4 w-4" />
          Logout
        </button>
      </header>

      {/* Guidelines Modal */}
      {showGuidelines && (
        <HackathonGuidelines
          onClose={() => setShowGuidelines(false)}
          onAccept={() => setShowGuidelines(false)}
          showAcceptButton
        />
      )}

      {/* Content */}
      <div className="flex-1 flex items-center justify-center">
        <div className="max-w-md w-full mx-4 bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-2xl p-4 inline-block mb-6">
            <Database className="h-12 w-12 text-white" />
          </div>

          <h1 className="text-2xl font-bold text-gray-900 mb-3">
            {isOracle ? "Create Your Schema" : "Create Your Database"}
          </h1>
          <p className="text-gray-500 mb-8">
            {isOracle
              ? "Before you start the hackathon, you need to create your personal schema on the Oracle server. This will be your playground — full DDL/DML access within your schema."
              : "Before you start the hackathon, you need to create your personal database on the server. This will be your playground — full DDL/DML access."}
          </p>

          <button
            onClick={handleCreate}
            disabled={isCreating}
            className="w-full h-12 bg-gradient-to-r from-teal-600 to-orange-500 hover:from-teal-700 hover:to-orange-600 text-white font-semibold text-lg rounded-xl transition-all disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {isCreating ? (
              <>
                <Loader2 className="h-5 w-5 animate-spin" />
                {isOracle ? "Creating Schema..." : "Creating Database..."}
              </>
            ) : (
              isOracle ? "Create My Schema" : "Create My Database"
            )}
          </button>

          <p className="text-xs text-gray-400 mt-4">
            {isOracle
              ? "This action creates an Oracle schema (user) assigned exclusively to you."
              : "This action creates a SQL Server database assigned exclusively to you."}
          </p>
        </div>
      </div>
    </div>
  );
}
