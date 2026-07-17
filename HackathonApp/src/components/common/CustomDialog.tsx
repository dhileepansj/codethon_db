import { useState } from "react";
import { X, AlertTriangle } from "lucide-react";

interface ConfirmDialogProps {
  title: string;
  message: string;
  confirmLabel?: string;
  confirmVariant?: "danger" | "primary";
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({ title, message, confirmLabel = "Confirm", confirmVariant = "primary", onConfirm, onCancel }: ConfirmDialogProps) {
  const btnClass = confirmVariant === "danger"
    ? "bg-red-600 hover:bg-red-700 text-white"
    : "bg-teal-600 hover:bg-teal-700 text-white";

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60]" onClick={onCancel}>
      <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-sm shadow-2xl animate-in fade-in zoom-in-95" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-start gap-3 mb-4">
          {confirmVariant === "danger" && (
            <div className="p-2 bg-red-100 dark:bg-red-900/30 rounded-lg shrink-0">
              <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400" />
            </div>
          )}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{title}</h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">{message}</p>
          </div>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onCancel} className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors">
            Cancel
          </button>
          <button onClick={onConfirm} className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${btnClass}`}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}

interface InputDialogProps {
  title: string;
  message: string;
  inputLabel?: string;
  inputPlaceholder?: string;
  inputType?: string;
  confirmLabel?: string;
  allowEmpty?: boolean;
  onConfirm: (value: string) => void;
  onCancel: () => void;
}

export function InputDialog({ title, message, inputLabel, inputPlaceholder, inputType = "text", confirmLabel = "Confirm", allowEmpty = true, onConfirm, onCancel }: InputDialogProps) {
  const [value, setValue] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onConfirm(value);
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60]" onClick={onCancel}>
      <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-sm shadow-2xl animate-in fade-in zoom-in-95" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-2">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{title}</h3>
          <button onClick={onCancel} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
            <X className="h-4 w-4" />
          </button>
        </div>
        <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">{message}</p>
        <form onSubmit={handleSubmit}>
          {inputLabel && (
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1 block">{inputLabel}</label>
          )}
          <input
            type={inputType}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder={inputPlaceholder}
            autoFocus
            className="w-full px-3 py-2.5 border dark:border-gray-600 rounded-lg text-sm dark:bg-gray-700 dark:text-white focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 outline-none transition-all mb-4"
          />
          <div className="flex justify-end gap-2">
            <button type="button" onClick={onCancel} className="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors">
              Cancel
            </button>
            <button
              type="submit"
              disabled={!allowEmpty && !value.trim()}
              className="px-4 py-2 text-sm font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg transition-colors disabled:opacity-50"
            >
              {confirmLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
