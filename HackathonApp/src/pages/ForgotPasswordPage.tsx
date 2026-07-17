import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { User, ArrowLeft, Send } from "lucide-react";
import { toast } from "sonner";
import httpClient from "@/services/httpClient";

export default function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [userId, setUserId] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userId.trim()) {
      toast.error("Please enter your User ID");
      return;
    }

    setIsSubmitting(true);
    try {
      await httpClient.post("/api/auth/forgot-password", { userID: userId.trim() });
      setSubmitted(true);
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || "Request failed");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (submitted) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-teal-50">
        <div className="max-w-md w-full mx-4 bg-white rounded-2xl shadow-xl p-8 text-center">
          <div className="bg-green-100 rounded-full p-4 w-16 h-16 mx-auto mb-4 flex items-center justify-center">
            <Send className="h-8 w-8 text-green-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 mb-3">Request Submitted</h1>
          <p className="text-gray-500 mb-6">
            Your password reset request has been sent to the administrator. They will reset your password and inform you.
          </p>
          <button
            onClick={() => navigate("/login", { replace: true })}
            className="w-full h-12 bg-gradient-to-r from-teal-600 to-orange-500 text-white font-semibold rounded-xl"
          >
            Back to Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-teal-50">
      <div className="max-w-md w-full mx-4 bg-white rounded-2xl shadow-xl p-8">
        <div className="text-center mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Forgot Password</h1>
          <p className="text-gray-500 mt-2">Enter your User ID and we'll notify the administrator to reset your password.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <label className="text-sm font-medium text-gray-700">User ID</label>
            <div className="relative">
              <User className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-teal-500" />
              <input
                type="text"
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                placeholder="Enter your User ID"
                className="w-full pl-12 pr-4 h-12 border-2 border-teal-200 focus:border-orange-400 rounded-xl text-base outline-none transition-all"
                required
                autoFocus
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full h-12 bg-gradient-to-r from-teal-600 to-orange-500 hover:from-teal-700 hover:to-orange-600 text-white font-semibold text-lg rounded-xl transition-all disabled:opacity-50"
          >
            {isSubmitting ? "Submitting..." : "Submit Request"}
          </button>
        </form>

        <button
          onClick={() => navigate("/login")}
          className="flex items-center gap-2 mx-auto mt-4 text-teal-600 hover:text-orange-600 text-sm font-medium transition-colors"
        >
          <ArrowLeft className="h-4 w-4" /> Back to Login
        </button>
      </div>
    </div>
  );
}
