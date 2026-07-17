import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { Lock, Eye, EyeOff, KeyRound } from "lucide-react";
import { toast } from "sonner";
import { authService } from "@/services/authService";
import { logout } from "@/redux/slices/authSlice";
import type { AppDispatch } from "@/redux/store";

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (newPassword.length < 6) {
      toast.error("Password must be at least 6 characters");
      return;
    }
    if (newPassword !== confirmPassword) {
      toast.error("Passwords do not match");
      return;
    }

    setIsSubmitting(true);
    try {
      await authService.changePassword(newPassword);
      toast.success("Password changed successfully! Please login with your new password.");
      // Logout and redirect to login
      dispatch(logout());
      navigate("/login", { replace: true });
    } catch (err: any) {
      const data = err.response?.data;
      const msg = data?.errors?.[0] || data?.Errors?.[0] || data?.message || "Failed to change password";
      toast.error(msg === "Bad Request" ? (data?.errors?.[0] || data?.Errors?.[0] || "Password change failed") : msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-teal-50">
      <div className="max-w-md w-full mx-4 bg-white rounded-2xl shadow-xl p-8">
        <div className="text-center mb-6">
          <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-2xl p-4 inline-block mb-4">
            <KeyRound className="h-10 w-10 text-white" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900">Change Your Password</h1>
          <p className="text-gray-500 mt-2">You must set a new password before continuing.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <label className="text-sm font-medium text-gray-700">New Password</label>
            <div className="relative">
              <Lock className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-teal-500" />
              <input
                type={showNew ? "text" : "password"}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Enter new password (min 6 chars)"
                className="w-full pl-12 pr-12 h-12 border-2 border-teal-200 focus:border-orange-400 rounded-xl text-base outline-none transition-all"
                required
                minLength={6}
              />
              <button type="button" onClick={() => setShowNew(!showNew)} className="absolute right-4 top-1/2 -translate-y-1/2">
                {showNew ? <EyeOff className="h-5 w-5 text-gray-400" /> : <Eye className="h-5 w-5 text-gray-400" />}
              </button>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium text-gray-700">Confirm Password</label>
            <div className="relative">
              <Lock className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-teal-500" />
              <input
                type={showConfirm ? "text" : "password"}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Confirm your new password"
                className="w-full pl-12 pr-12 h-12 border-2 border-teal-200 focus:border-orange-400 rounded-xl text-base outline-none transition-all"
                required
              />
              <button type="button" onClick={() => setShowConfirm(!showConfirm)} className="absolute right-4 top-1/2 -translate-y-1/2">
                {showConfirm ? <EyeOff className="h-5 w-5 text-gray-400" /> : <Eye className="h-5 w-5 text-gray-400" />}
              </button>
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full h-12 bg-gradient-to-r from-teal-600 to-orange-500 hover:from-teal-700 hover:to-orange-600 text-white font-semibold text-lg rounded-xl transition-all disabled:opacity-50"
          >
            {isSubmitting ? "Changing..." : "Set New Password"}
          </button>
        </form>
      </div>
    </div>
  );
}
