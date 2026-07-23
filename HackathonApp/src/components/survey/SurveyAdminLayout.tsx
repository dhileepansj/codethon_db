import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Database, LogOut, ChevronRight, LayoutDashboard,
  FileEdit, Users, BarChart3, Settings, ArrowLeft,
} from 'lucide-react';
import { logout } from '../../redux/slices/authSlice';
import type { AppDispatch, RootState } from '../../redux/store';

interface SurveyAdminLayoutProps {
  children: React.ReactNode;
}

export default function SurveyAdminLayout({ children }: SurveyAdminLayoutProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useSelector((s: RootState) => s.auth);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login', { replace: true });
  };

  // Extract surveyId from path if present
  const pathParts = location.pathname.split('/');
  const surveyIdIndex = pathParts.indexOf('surveys') + 1;
  const surveyId = surveyIdIndex > 0 && surveyIdIndex < pathParts.length ? pathParts[surveyIdIndex] : null;
  const isSubPage = surveyId && surveyId !== 'surveys';

  return (
    <div className="h-screen flex bg-gray-50 dark:bg-gray-950">
      {/* Sidebar */}
      <aside className="w-60 bg-white dark:bg-gray-900 border-r dark:border-gray-800 flex flex-col shrink-0">
        {/* Logo */}
        <div className="h-14 flex items-center gap-2.5 px-5 border-b dark:border-gray-800">
          <div className="bg-gradient-to-br from-teal-500 to-teal-700 rounded-lg p-1.5">
            <Database className="h-4 w-4 text-white" />
          </div>
          <div>
            <span className="text-sm font-semibold text-gray-800 dark:text-gray-100">NovacCodeLab</span>
            <span className="block text-[10px] text-gray-400 -mt-0.5">Survey Manager</span>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 py-4 px-3 space-y-1 overflow-y-auto">
          {/* Back to Admin */}
          <button
            onClick={() => navigate('/admin')}
            className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm font-medium text-gray-500 dark:text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800 hover:text-gray-700 dark:hover:text-gray-300 transition-colors mb-3"
          >
            <ArrowLeft className="h-4 w-4" />
            <span>Back to Admin</span>
          </button>

          <div className="border-t dark:border-gray-800 pt-3 mb-2" />

          {/* Survey main nav */}
          <NavItem
            icon={<LayoutDashboard className="h-4 w-4" />}
            label="All Surveys"
            active={location.pathname.endsWith('/surveys')}
            onClick={() => navigate('/admin/surveys')}
          />

          {/* Sub-navigation when inside a survey */}
          {isSubPage && (
            <>
              <div className="border-t dark:border-gray-800 my-3" />
              <p className="px-3 text-[10px] font-semibold text-gray-400 dark:text-gray-600 uppercase tracking-wider mb-2">
                Current Survey
              </p>
              <NavItem
                icon={<FileEdit className="h-4 w-4" />}
                label="Form Builder"
                active={location.pathname.includes('/build')}
                onClick={() => navigate(`/admin/surveys/${surveyId}/build`)}
              />
              <NavItem
                icon={<Users className="h-4 w-4" />}
                label="Participants"
                active={location.pathname.includes('/participants')}
                onClick={() => navigate(`/admin/surveys/${surveyId}/participants`)}
              />
              <NavItem
                icon={<BarChart3 className="h-4 w-4" />}
                label="Dashboard"
                active={location.pathname.includes('/dashboard')}
                onClick={() => navigate(`/admin/surveys/${surveyId}/dashboard`)}
              />
              <NavItem
                icon={<Settings className="h-4 w-4" />}
                label="Settings"
                active={location.pathname.includes('/settings')}
                onClick={() => navigate(`/admin/surveys/${surveyId}/settings`)}
              />
            </>
          )}
        </nav>

        {/* Footer */}
        <div className="px-3 py-3 border-t dark:border-gray-800">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="w-6 h-6 rounded-full bg-teal-600 flex items-center justify-center text-white text-[10px] font-bold">
                {user?.userID?.charAt(0)?.toUpperCase() || 'A'}
              </div>
              <span className="text-xs text-gray-600 dark:text-gray-400">{user?.userID || 'Admin'}</span>
            </div>
            <button
              onClick={handleLogout}
              className="flex items-center gap-1 text-xs text-gray-500 hover:text-red-500 transition-colors"
            >
              <LogOut className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 flex flex-col overflow-hidden">
        {children}
      </main>
    </div>
  );
}

function NavItem({ icon, label, active, onClick }: { icon: React.ReactNode; label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
        active
          ? 'bg-teal-50 dark:bg-teal-900/20 text-teal-700 dark:text-teal-300'
          : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200'
      }`}
    >
      {icon}<span>{label}</span>
    </button>
  );
}
