import { useState } from "react";
import {
  CheckCircle, XCircle, X, ChevronRight, ChevronLeft,
  Database, Code, Upload, Clock, Shield, FileText, AlertTriangle
} from "lucide-react";

interface HackathonGuidelinesProps {
  onClose: () => void;
  onAccept?: () => void;
  showAcceptButton?: boolean;
}

const steps = [
  {
    id: "welcome",
    title: "Welcome to the Hackathon!",
    subtitle: "Please read these guidelines carefully before you begin.",
    icon: <Code className="h-8 w-8 text-white" />,
    gradient: "from-teal-500 to-cyan-500",
  },
  {
    id: "database",
    title: "Database Setup",
    subtitle: "Your personal SQL Server database",
    icon: <Database className="h-8 w-8 text-white" />,
    gradient: "from-blue-500 to-indigo-500",
  },
  {
    id: "workflow",
    title: "Working in the Editor",
    subtitle: "Writing and executing SQL",
    icon: <FileText className="h-8 w-8 text-white" />,
    gradient: "from-purple-500 to-pink-500",
  },
  {
    id: "submission",
    title: "Submission & Upload",
    subtitle: "How to submit your work",
    icon: <Upload className="h-8 w-8 text-white" />,
    gradient: "from-orange-500 to-red-500",
  },
  {
    id: "rules",
    title: "DOs & DON'Ts",
    subtitle: "Important rules to follow",
    icon: <Shield className="h-8 w-8 text-white" />,
    gradient: "from-emerald-500 to-teal-500",
  },
];

export default function HackathonGuidelines({ onClose, onAccept, showAcceptButton = false }: HackathonGuidelinesProps) {
  const [currentStep, setCurrentStep] = useState(0);

  const isLastStep = currentStep === steps.length - 1;

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[80]" onClick={onClose}>
      <div
        className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`px-6 py-5 bg-gradient-to-r ${steps[currentStep].gradient} flex items-center justify-between shrink-0`}>
          <div className="flex items-center gap-4">
            <div className="bg-white/20 rounded-xl p-2.5">
              {steps[currentStep].icon}
            </div>
            <div>
              <h2 className="text-xl font-bold text-white">{steps[currentStep].title}</h2>
              <p className="text-white/80 text-sm">{steps[currentStep].subtitle}</p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-white/20 rounded-lg transition-colors">
            <X className="h-5 w-5 text-white" />
          </button>
        </div>

        {/* Step Indicators */}
        <div className="flex items-center gap-1 px-6 py-3 border-b dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
          {steps.map((step, idx) => (
            <button
              key={step.id}
              onClick={() => setCurrentStep(idx)}
              className={`flex-1 h-1.5 rounded-full transition-all ${
                idx === currentStep
                  ? "bg-teal-500"
                  : idx < currentStep
                  ? "bg-teal-300 dark:bg-teal-700"
                  : "bg-gray-200 dark:bg-gray-700"
              }`}
            />
          ))}
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-6 py-5">
          {currentStep === 0 && <WelcomeStep />}
          {currentStep === 1 && <DatabaseStep />}
          {currentStep === 2 && <WorkflowStep />}
          {currentStep === 3 && <SubmissionStep />}
          {currentStep === 4 && <RulesStep />}
        </div>

        {/* Footer Navigation */}
        <div className="px-6 py-4 border-t dark:border-gray-800 flex items-center justify-between shrink-0 bg-gray-50 dark:bg-gray-800/50">
          <button
            onClick={() => setCurrentStep(Math.max(0, currentStep - 1))}
            disabled={currentStep === 0}
            className="flex items-center gap-1.5 px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors disabled:opacity-40 disabled:pointer-events-none"
          >
            <ChevronLeft className="h-4 w-4" /> Previous
          </button>

          <span className="text-xs text-gray-400">{currentStep + 1} / {steps.length}</span>

          {isLastStep ? (
            showAcceptButton ? (
              <button
                onClick={onAccept || onClose}
                className="flex items-center gap-1.5 px-5 py-2 text-sm font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg transition-colors"
              >
                <CheckCircle className="h-4 w-4" /> I Understand, Continue
              </button>
            ) : (
              <button
                onClick={onClose}
                className="flex items-center gap-1.5 px-5 py-2 text-sm font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg transition-colors"
              >
                Got it!
              </button>
            )
          ) : (
            <button
              onClick={() => setCurrentStep(Math.min(steps.length - 1, currentStep + 1))}
              className="flex items-center gap-1.5 px-4 py-2 text-sm font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg transition-colors"
            >
              Next <ChevronRight className="h-4 w-4" />
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Step Content Components ──────────────────────────────────────

function WelcomeStep() {
  return (
    <div className="space-y-4">
      <p className="text-gray-700 dark:text-gray-300 leading-relaxed">
        Welcome to <strong>NovacCodeLab</strong> — your online SQL hackathon platform.
        This guide will walk you through everything you need to know before you start.
      </p>
      <div className="grid grid-cols-2 gap-3">
        {[
          { icon: <Database className="h-5 w-5 text-blue-500" />, label: "Personal Database", desc: "Your own SQL Server instance" },
          { icon: <Code className="h-5 w-5 text-purple-500" />, label: "SQL Editor", desc: "Write & execute queries" },
          { icon: <FileText className="h-5 w-5 text-teal-500" />, label: "File Manager", desc: "Save & organize scripts" },
          { icon: <Upload className="h-5 w-5 text-orange-500" />, label: "Submit Work", desc: "Upload your final output" },
        ].map((item) => (
          <div key={item.label} className="flex items-start gap-3 p-3 rounded-xl bg-gray-50 dark:bg-gray-800">
            <div className="shrink-0 mt-0.5">{item.icon}</div>
            <div>
              <p className="text-sm font-medium text-gray-800 dark:text-gray-100">{item.label}</p>
              <p className="text-xs text-gray-500 dark:text-gray-400">{item.desc}</p>
            </div>
          </div>
        ))}
      </div>
      <div className="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl">
        <div className="flex items-center gap-2 mb-1">
          <Clock className="h-4 w-4 text-amber-600" />
          <span className="text-sm font-medium text-amber-800 dark:text-amber-300">Time Limit</span>
        </div>
        <p className="text-xs text-amber-700 dark:text-amber-400">
          Your session has a time limit set by the administrator. Keep an eye on the timer in the top bar. Save your work frequently!
        </p>
      </div>
    </div>
  );
}

function DatabaseStep() {
  return (
    <div className="space-y-4">
      <p className="text-gray-700 dark:text-gray-300 leading-relaxed">
        After this guide, you'll create your personal SQL Server database. Here's what you should know:
      </p>
      <div className="space-y-3">
        <DoItem text="Your database is exclusively yours — no one else can access it" />
        <DoItem text="You have full DDL & DML access (CREATE, ALTER, DROP, INSERT, UPDATE, DELETE)" />
        <DoItem text="Create tables, procedures, functions, triggers, and views freely" />
      </div>
      <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl">
        <div className="flex items-center gap-2 mb-1">
          <Code className="h-4 w-4 text-blue-600" />
          <span className="text-sm font-medium text-blue-800 dark:text-blue-300">Starter Scripts</span>
        </div>
        <p className="text-xs text-blue-700 dark:text-blue-400">
          When you create your database, starter scripts may be automatically executed to set up initial tables and sample data.
          These scripts will also appear in a <strong>"Starter Scripts"</strong> folder in your File Manager for reference.
          You can open, study, and use them as templates for your own work.
        </p>
      </div>
      <div className="mt-4 space-y-3">
        <DontItem text="Don't try to access system databases or other participants' databases" />
        <DontItem text="Don't run infinite loops or resource-heavy operations that may crash the server" />
      </div>
    </div>
  );
}

function WorkflowStep() {
  return (
    <div className="space-y-4">
      <p className="text-gray-700 dark:text-gray-300 leading-relaxed">
        The SQL Editor is your main workspace. Here are the key features:
      </p>
      <div className="space-y-3">
        <DoItem text="Use Ctrl+S to save your scripts regularly" />
        <DoItem text="Use F5 or Ctrl+E to execute queries" />
        <DoItem text="Select specific text and press F5 to execute only that selection" />
        <DoItem text="Use GO between batches to execute multiple statements (like SSMS)" />
        <DoItem text="Use Ctrl+R to toggle the results panel" />
        <DoItem text="Use multiple tabs to work on different scripts simultaneously" />
        <DoItem text="Use the Schema Explorer to view your database structure" />
        <DoItem text="Use the File Manager to organize scripts in folders" />
        <DoItem text="Refer to the Question Paper panel for your task requirements" />
      </div>
      <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl">
        <div className="flex items-center gap-2 mb-1">
          <AlertTriangle className="h-4 w-4 text-blue-600" />
          <span className="text-sm font-medium text-blue-800 dark:text-blue-300">Multiple Statements</span>
        </div>
        <p className="text-xs text-blue-700 dark:text-blue-400">
          Use <strong>GO</strong> on a separate line to separate batches — just like SSMS.
          All statements within a batch execute together. Example:<br />
          <code className="text-[11px]">CREATE TABLE X (...)<br />GO<br />INSERT INTO X VALUES (...)</code>
        </p>
      </div>
      <div className="mt-3 space-y-3">
        <DontItem text="Don't refresh the page without saving — unsaved work may be lost" />
        <DontItem text="Don't close tabs with unsaved changes without saving first" />
      </div>
    </div>
  );
}

function SubmissionStep() {
  return (
    <div className="space-y-4">
      <p className="text-gray-700 dark:text-gray-300 leading-relaxed">
        When you're ready to submit your work:
      </p>
      <div className="space-y-3">
        <DoItem text="Click the Submit button in the header to open the submission panel" />
        <DoItem text="You can upload documents (PDF, images, etc.) as supporting evidence" />
        <DoItem text="Save all your SQL scripts before submitting" />
        <DoItem text="You can submit only once — make sure everything is ready" />
        <DoItem text="After submission, the editor becomes read-only" />
      </div>
      <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl">
        <div className="flex items-center gap-2 mb-1">
          <AlertTriangle className="h-4 w-4 text-blue-600" />
          <span className="text-sm font-medium text-blue-800 dark:text-blue-300">Important</span>
        </div>
        <p className="text-xs text-blue-700 dark:text-blue-400">
          Submission is final. Once submitted, you cannot make further changes. Double-check your work before submitting.
        </p>
      </div>
    </div>
  );
}

function RulesStep() {
  return (
    <div className="space-y-5">
      {/* DOs */}
      <div>
        <h3 className="flex items-center gap-2 text-sm font-bold text-green-700 dark:text-green-400 uppercase tracking-wider mb-3">
          <CheckCircle className="h-4 w-4" /> Do's
        </h3>
        <div className="space-y-2">
          <DoItem text="Write your own SQL code — this is a test of YOUR skills" />
          <DoItem text="Save your work frequently using Ctrl+S" />
          <DoItem text="Read the question paper carefully before starting" />
          <DoItem text="Test your queries by executing them before final submission" />
          <DoItem text="Organize your scripts in meaningful files and folders" />
          <DoItem text="Keep the browser tab focused on the hackathon platform" />
        </div>
      </div>

      {/* DON'Ts */}
      <div>
        <h3 className="flex items-center gap-2 text-sm font-bold text-red-700 dark:text-red-400 uppercase tracking-wider mb-3">
          <XCircle className="h-4 w-4" /> Don'ts
        </h3>
        <div className="space-y-2">
          <DontItem text="Don't use AI tools (ChatGPT, Copilot, etc.) — AI detection is enabled" />
          <DontItem text="Don't copy-paste code from external sources" />
          <DontItem text="Don't open Developer Tools (F12) — it will be detected and may force logout" />
          <DontItem text="Don't switch tabs/windows frequently — tab switching is monitored" />
          <DontItem text="Don't share your credentials or collaborate with others" />
          <DontItem text="Don't try to tamper with the platform or access unauthorized areas" />
        </div>
      </div>

      {/* Warning */}
      <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl">
        <div className="flex items-center gap-2 mb-1">
          <Shield className="h-4 w-4 text-red-600" />
          <span className="text-sm font-medium text-red-800 dark:text-red-300">Anti-Cheat Active</span>
        </div>
        <p className="text-xs text-red-700 dark:text-red-400">
          The platform monitors for AI-generated content, tab switching, DevTools usage, and external paste.
          Violations are logged and visible to administrators.
        </p>
      </div>
    </div>
  );
}

// ─── Reusable Items ───────────────────────────────────────────────

function DoItem({ text }: { text: string }) {
  return (
    <div className="flex items-start gap-2.5">
      <CheckCircle className="h-4 w-4 text-green-500 shrink-0 mt-0.5" />
      <span className="text-sm text-gray-700 dark:text-gray-300">{text}</span>
    </div>
  );
}

function DontItem({ text }: { text: string }) {
  return (
    <div className="flex items-start gap-2.5">
      <XCircle className="h-4 w-4 text-red-500 shrink-0 mt-0.5" />
      <span className="text-sm text-gray-700 dark:text-gray-300">{text}</span>
    </div>
  );
}
