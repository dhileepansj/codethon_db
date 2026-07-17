import { useState, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Navigate, useNavigate, Link, useSearchParams } from "react-router-dom";
import { Eye, EyeOff, Lock, User, Zap, Database, Shield } from "lucide-react";
import { toast } from "sonner";
import { login, clearError } from "@/redux/slices/authSlice";
import type { RootState, AppDispatch } from "@/redux/store";

export default function LoginPage() {
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAuthenticated, isLoading, error } = useSelector((s: RootState) => s.auth);

  const [formData, setFormData] = useState({ UserID: "", Password: "" });
  const [showPassword, setShowPassword] = useState(false);

  // Show devtools detection message
  useEffect(() => {
    if (searchParams.get("reason") === "devtools") {
      toast.error("Session terminated — unauthorized developer tools detected.", { duration: 10000 });
    }
  }, [searchParams]);

  if (isAuthenticated) {
    const user = JSON.parse(sessionStorage.getItem("user") || "{}");
    return <Navigate to={user.role === "SuperAdmin" ? "/admin" : "/"} replace />;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.UserID || !formData.Password) {
      toast.error("Please fill in all fields");
      return;
    }
    dispatch(clearError());
    const result = await dispatch(login(formData));
    if (login.fulfilled.match(result)) {
      const data = result.payload;
      toast.success("Login successful!");
      if (data.mustChangePassword) {
        navigate("/change-password", { replace: true });
      } else if (data.role === "SuperAdmin") {
        navigate("/admin", { replace: true });
      } else if (data.session?.databaseCreated) {
        navigate("/", { replace: true });
      } else {
        navigate("/create-database", { replace: true });
      }
    } else {
      toast.error(result.payload as string || "Login failed");
    }
  };

  return (
    <div className="min-h-screen flex bg-gradient-to-br from-slate-900 via-teal-900 to-orange-900 relative overflow-hidden">
      {/* Material pattern overlay — same as ebslosbreui */}
      <div className="login-material-pattern" aria-hidden="true" />

      {/* Animated background blobs — same as ebslosbreui */}
      <div className="absolute inset-0 pointer-events-none" aria-hidden="true">
        <div className="absolute top-20 left-20 w-64 h-64 bg-teal-400/15 rounded-full blur-2xl animate-pulse"></div>
        <div className="absolute bottom-20 right-20 w-80 h-80 bg-orange-400/15 rounded-full blur-2xl animate-pulse" style={{ animationDelay: "1s" }}></div>
        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-72 h-72 bg-purple-400/10 rounded-full blur-2xl animate-pulse" style={{ animationDelay: "2s" }}></div>
      </div>

      {/* Left Panel — Features */}
      <div className="hidden lg:flex lg:flex-1 flex-col justify-center px-12 relative z-10">
        <div className="max-w-md">
          <div className="flex items-center space-x-3 mb-8">
            <div className="bg-gradient-to-r from-teal-400 to-orange-400 rounded-xl p-3 shadow-xl">
              <img
                src={`${import.meta.env.VITE_APP_BASEPATH || "/novaccodelab"}/uploads/novac-logo.png`}
                alt="Novac Technology"
                className="h-8 w-auto"
              />
            </div>
            <div>
              <h1 className="text-3xl font-bold text-white">NovacCodeLab</h1>
              <p className="text-teal-200">SQL Challenge Platform</p>
            </div>
          </div>

          <h2 className="text-4xl font-bold text-white mb-6 leading-tight">
            Build, Create, Execute
            <span className="text-teal-300"> SQL Challenges</span>
          </h2>

          <p className="text-xl text-gray-300 mb-12 leading-relaxed">
            Your own SQL Server instance. Full DDL &amp; DML access. Create databases, tables, procedures — everything.
          </p>

          <div className="space-y-6">
            <div className="flex items-center space-x-4 text-white">
              <div className="bg-teal-500/20 p-3 rounded-lg">
                <Zap className="h-6 w-6 text-teal-400" />
              </div>
              <div>
                <h3 className="font-semibold text-lg">Full SQL Server Access</h3>
                <p className="text-gray-300">CREATE, ALTER, DROP — complete database freedom</p>
              </div>
            </div>

            <div className="flex items-center space-x-4 text-white">
              <div className="bg-orange-500/20 p-3 rounded-lg">
                <Shield className="h-6 w-6 text-orange-400" />
              </div>
              <div>
                <h3 className="font-semibold text-lg">Monaco SQL Editor</h3>
                <p className="text-gray-300">Syntax highlighting, multi-batch execution with GO</p>
              </div>
            </div>

            <div className="flex items-center space-x-4 text-white">
              <div className="bg-purple-500/20 p-3 rounded-lg">
                <Database className="h-6 w-6 text-purple-400" />
              </div>
              <div>
                <h3 className="font-semibold text-lg">Real-time Schema Explorer</h3>
                <p className="text-gray-300">SSMS-like tree view of your database objects</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Right Panel — Login Form */}
      <div className="flex-1 flex items-center justify-center px-6 py-12 relative z-10">
        <div className="w-full max-w-md shadow-2xl border-0 bg-white/95 backdrop-blur-xl rounded-2xl p-8">
          <div className="text-center mb-8">
            <div className="flex justify-center mb-6 lg:hidden">
              <div className="bg-gradient-to-r from-teal-500 to-orange-500 rounded-xl p-4 shadow-xl">
                <img
                  src={`${import.meta.env.VITE_APP_BASEPATH || "/novaccodelab"}/uploads/novac-logo.png`}
                  alt="Novac Technology"
                  className="h-8 w-auto"
                />
              </div>
            </div>
            <h2 className="text-3xl font-bold bg-gradient-to-r from-teal-700 to-orange-500 bg-clip-text text-transparent">
              Sign In
            </h2>
            <p className="text-teal-600 text-lg mt-2">Welcome to NovacCodeLab</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <label className="text-teal-800 font-medium text-sm">User ID</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-teal-500" />
                </div>
                <input
                  type="text"
                  value={formData.UserID}
                  onChange={(e) => setFormData((p) => ({ ...p, UserID: e.target.value }))}
                  placeholder="Enter your user ID"
                  className="w-full pl-12 pr-4 h-12 border-2 border-teal-200 focus:border-orange-400 focus:ring-orange-400 rounded-xl text-lg outline-none transition-all"
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-teal-800 font-medium text-sm">Password</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                  <Lock className="h-5 w-5 text-teal-500" />
                </div>
                <input
                  type={showPassword ? "text" : "password"}
                  value={formData.Password}
                  onChange={(e) => setFormData((p) => ({ ...p, Password: e.target.value }))}
                  placeholder="Enter your password"
                  className="w-full pl-12 pr-12 h-12 border-2 border-teal-200 focus:border-orange-400 focus:ring-orange-400 rounded-xl text-lg outline-none transition-all"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-4 flex items-center hover:bg-teal-50 rounded-r-xl transition-colors"
                >
                  {showPassword ? (
                    <EyeOff className="h-5 w-5 text-teal-500" />
                  ) : (
                    <Eye className="h-5 w-5 text-teal-500" />
                  )}
                </button>
              </div>
            </div>

            {error && <p className="text-red-500 text-sm text-center">{error}</p>}

            <button
              type="submit"
              disabled={isLoading}
              className="w-full h-12 bg-gradient-to-r from-teal-600 to-orange-500 hover:from-teal-700 hover:to-orange-600 text-white font-semibold text-lg shadow-xl rounded-xl transition-all duration-200 hover:shadow-2xl transform hover:-translate-y-0.5 disabled:opacity-50"
            >
              {isLoading ? (
                <div className="flex items-center justify-center space-x-2">
                  <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  <span>Signing in...</span>
                </div>
              ) : (
                "Sign In"
              )}
            </button>

            <div className="text-center pt-2">
              <Link to="/forgot-password" className="text-teal-600 hover:text-orange-600 hover:underline transition-colors text-sm font-medium">
                Forgot Password?
              </Link>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}


