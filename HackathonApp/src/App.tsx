import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { Toaster } from "sonner";
import { ThemeProvider } from "./contexts/ThemeContext";
import type { RootState } from "./redux/store";
import LoginPage from "./pages/LoginPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage";
import ChangePasswordPage from "./pages/ChangePasswordPage";
import WorkspacePage from "./pages/WorkspacePage";
import AdminPage from "./pages/AdminPage";
import CreateDatabasePage from "./pages/CreateDatabasePage";
import McqStartPage from "./pages/McqStartPage";
import McqTestPage from "./pages/McqTestPage";
import SecurityShield from "./components/common/SecurityShield";

const BASEPATH = import.meta.env.VITE_APP_BASEPATH || "/novaccodelab";

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user } = useSelector((s: RootState) => s.auth);
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  // Force password change
  if (user?.mustChangePassword && window.location.pathname.replace(BASEPATH, "") !== "/change-password") {
    return <Navigate to="/change-password" replace />;
  }
  return <>{children}</>;
}

function AdminRoute({ children }: { children: React.ReactNode }) {
  const { user } = useSelector((s: RootState) => s.auth);
  if (!user || (user.role !== "SuperAdmin" && user.role !== "Admin")) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <ThemeProvider>
      <BrowserRouter basename={BASEPATH}>
        <Toaster position="top-right" richColors />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route
            path="/change-password"
            element={
              <ProtectedRoute>
                <ChangePasswordPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/admin/*"
            element={
              <ProtectedRoute>
                <AdminRoute>
                  <AdminPage />
                </AdminRoute>
              </ProtectedRoute>
            }
          />
          <Route
            path="/create-database"
            element={
              <ProtectedRoute>
                <CreateDatabasePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/mcq-start"
            element={
              <ProtectedRoute>
                <SecurityShield tabSwitch devTools>
                  <McqStartPage />
                </SecurityShield>
              </ProtectedRoute>
            }
          />
          <Route
            path="/mcq-test"
            element={
              <ProtectedRoute>
                <SecurityShield tabSwitch devTools>
                  <McqTestPage />
                </SecurityShield>
              </ProtectedRoute>
            }
          />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <WorkspacePage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
}
