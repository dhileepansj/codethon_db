import { useEffect, useState } from "react";
import { ShieldAlert } from "lucide-react";
import { toast } from "sonner";
import { startTabSwitchTracking, stopTabSwitchTracking } from "@/services/activityService";
import { startDevToolsProtection, stopDevToolsProtection, setOnDevToolsBlocked } from "@/services/devtoolsDetection";
import { validatePaste, setOnPasteBlocked, registerInternalCopy } from "@/services/clipboardGuard";

interface SecurityShieldProps {
  children: React.ReactNode;
  /** Enable tab switch / window blur monitoring */
  tabSwitch?: boolean;
  /** Enable DevTools detection with blocking overlay */
  devTools?: boolean;
  /** Block right-click context menu (handled by devTools protection) */
  rightClick?: boolean;
  /** Block keyboard shortcuts like F12, Ctrl+Shift+I (handled by devTools protection) */
  keyboardShortcuts?: boolean;
  /** Block external paste */
  clipboardGuard?: boolean;
}

/**
 * Wraps any page with security features (anti-cheat).
 * Reusable across SQL workspace, MCQ test, or any future assessment type.
 * 
 * Usage:
 *   <SecurityShield tabSwitch devTools>
 *     <YourPage />
 *   </SecurityShield>
 */
export default function SecurityShield({
  children,
  tabSwitch = true,
  devTools = true,
  clipboardGuard = false,
}: SecurityShieldProps) {
  const [devToolsBlocked, setDevToolsBlocked] = useState(false);

  useEffect(() => {
    // Start tab switch monitoring
    if (tabSwitch) {
      startTabSwitchTracking();
    }

    // Start DevTools protection (includes right-click + keyboard shortcut blocking)
    if (devTools) {
      startDevToolsProtection();
      setOnDevToolsBlocked((blocked) => {
        setDevToolsBlocked(blocked);
      });
    }

    // Clipboard guard — intercept paste on all inputs
    if (clipboardGuard) {
      const handleCopy = (e: ClipboardEvent) => {
        const selection = window.getSelection()?.toString();
        if (selection) registerInternalCopy(selection);
      };

      const handlePaste = (e: ClipboardEvent) => {
        const pastedText = e.clipboardData?.getData("text/plain");
        if (pastedText && !validatePaste(pastedText)) {
          e.preventDefault();
          e.stopPropagation();
        }
      };

      setOnPasteBlocked(() => {
        toast.error("External paste is not allowed. You can only paste content copied within this page.", { duration: 4000 });
      });

      document.addEventListener("copy", handleCopy, true);
      document.addEventListener("cut", handleCopy, true);
      document.addEventListener("paste", handlePaste, true);

      return () => {
        if (tabSwitch) stopTabSwitchTracking();
        if (devTools) stopDevToolsProtection();
        document.removeEventListener("copy", handleCopy, true);
        document.removeEventListener("cut", handleCopy, true);
        document.removeEventListener("paste", handlePaste, true);
      };
    }

    return () => {
      if (tabSwitch) stopTabSwitchTracking();
      if (devTools) stopDevToolsProtection();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <>
      {children}

      {/* DevTools Blocking Overlay */}
      {devToolsBlocked && (
        <div className="fixed inset-0 z-[9999] bg-gray-900/95 flex items-center justify-center backdrop-blur-sm">
          <div className="bg-white dark:bg-gray-800 rounded-2xl p-8 max-w-md w-full mx-4 text-center shadow-2xl">
            <div className="bg-red-100 dark:bg-red-900/30 rounded-full w-16 h-16 flex items-center justify-center mx-auto mb-4">
              <ShieldAlert className="h-8 w-8 text-red-600" />
            </div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-2">
              Developer Tools Detected
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
              Developer tools are not allowed during the assessment. Please close them to continue.
            </p>
            <p className="text-xs text-red-600 font-medium">
              This attempt has been logged.
            </p>
          </div>
        </div>
      )}
    </>
  );
}
