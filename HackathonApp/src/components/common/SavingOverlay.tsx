import { Shield, Scan } from "lucide-react";

interface SavingOverlayProps {
  visible: boolean;
}

export default function SavingOverlay({ visible }: SavingOverlayProps) {
  if (!visible) return null;

  return (
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 backdrop-blur-sm"
      role="status"
      aria-label="Saving and scanning file"
    >
      <div className="relative flex flex-col items-center">
        {/* Animated background glow */}
        <div className="absolute -inset-20 bg-gradient-to-r from-purple-500/20 via-teal-500/20 to-orange-500/20 rounded-full blur-3xl animate-pulse" />

        {/* Spinner */}
        <div className="relative mb-6">
          <div className="w-16 h-16 border-4 border-teal-500/30 border-t-teal-500 rounded-full animate-spin" />
          <div
            className="absolute inset-0 w-16 h-16 border-4 border-purple-500/20 border-b-purple-500 rounded-full animate-spin"
            style={{ animationDirection: "reverse", animationDuration: "1.5s" }}
          />
          <div className="absolute inset-0 flex items-center justify-center">
            <Shield className="h-6 w-6 text-teal-400 animate-pulse" />
          </div>
        </div>

        {/* Text */}
        <div className="relative text-center">
          <div className="flex items-center gap-2 mb-2">
            <Scan className="h-4 w-4 text-purple-400 animate-pulse" />
            <p className="text-white text-lg font-semibold">Scanning & Saving...</p>
          </div>
          <p className="text-gray-400 text-sm">
            AI detection in progress. This may take a few seconds.
          </p>
        </div>
      </div>
    </div>
  );
}
