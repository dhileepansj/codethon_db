/**
 * Clipboard Guard — Blocks external paste into the SQL editor.
 *
 * How it works:
 * 1. Intercepts copy/cut events within the app and stores the copied text internally.
 * 2. On paste, compares clipboard content against the internal store.
 * 3. If it doesn't match → the paste came from outside → BLOCK it.
 * 4. Logs blocked paste attempts to the backend for admin review.
 */

let internalClipboard: string | null = null;
let onBlockCallback: ((pastedText: string) => void) | null = null;
let knownFileContents: Set<string> = new Set();

const STORAGE_KEY = "clipboard_guard_internal";

// Restore from sessionStorage on load (survives page refresh)
try {
  const stored = sessionStorage.getItem(STORAGE_KEY);
  if (stored) internalClipboard = stored;
} catch {}

// ─── Public API ───────────────────────────────────────────────

/**
 * Call this when Monaco editor (or any internal source) performs a copy/cut.
 * Stores the content so we can validate future pastes.
 */
export function registerInternalCopy(text: string) {
  internalClipboard = text;
  try { sessionStorage.setItem(STORAGE_KEY, text); } catch {}
}

/**
 * Register content loaded from user's own files so paste from those is allowed.
 */
export function registerFileContent(content: string) {
  if (content && content.trim().length > 0) {
    knownFileContents.add(content.replace(/\r\n/g, "\n").trim());
  }
}

/**
 * Validates whether a paste operation is internal (allowed) or external (blocked).
 * Returns true if the paste should be ALLOWED, false if BLOCKED.
 */
export function validatePaste(pastedText: string): boolean {
  if (!pastedText || pastedText.trim().length === 0) return true;

  // If internal clipboard matches — it's an internal copy-paste
  // Normalize line endings for comparison (clipboard may convert \n to \r\n)
  if (internalClipboard !== null) {
    const normalizedInternal = internalClipboard.replace(/\r\n/g, "\n").trim();
    const normalizedPaste = pastedText.replace(/\r\n/g, "\n").trim();
    if (normalizedInternal === normalizedPaste) {
      return true;
    }
  }

  // Small pastes (< 10 chars) — likely just typing corrections, allow
  if (pastedText.trim().length < 10) {
    return true;
  }

  // Check if pasted content is a substring of what was internally copied
  // (user might copy a block but paste only part via multiple pastes)
  if (internalClipboard !== null) {
    const normalizedInternal = internalClipboard.replace(/\r\n/g, "\n");
    const normalizedPaste = pastedText.replace(/\r\n/g, "\n");
    if (normalizedInternal.includes(normalizedPaste)) {
      return true;
    }
  }

  // Check if pasted content comes from a known file opened by the user
  const normalizedForFileCheck = pastedText.replace(/\r\n/g, "\n").trim();
  for (const fileContent of knownFileContents) {
    if (fileContent.includes(normalizedForFileCheck)) {
      return true;
    }
  }

  // External paste detected — block
  logBlockedPaste(pastedText);
  onBlockCallback?.(pastedText);
  return false;
}

/**
 * Sets a callback for when a paste is blocked (e.g., show toast).
 */
export function setOnPasteBlocked(callback: (pastedText: string) => void) {
  onBlockCallback = callback;
}

/**
 * Clears the internal clipboard (e.g., on logout).
 */
export function clearInternalClipboard() {
  internalClipboard = null;
  knownFileContents.clear();
  try { sessionStorage.removeItem(STORAGE_KEY); } catch {}
}

// ─── Backend Logging ─────────────────────────────────────────

function logBlockedPaste(pastedText: string) {
  try {
    const token = sessionStorage.getItem("token");
    const baseUrl = (import.meta.env.VITE_API_BASE_URL || "") + "/hackathonapi";
    const url = `${baseUrl}/api/activity/tab-switch`;

    const preview = pastedText.length > 200 ? pastedText.substring(0, 200) + "..." : pastedText;

    const payload = JSON.stringify({
      events: [
        {
          eventType: "paste_blocked",
          timestamp: new Date().toISOString(),
          awayDurationSeconds: preview.length, // Reuse this field to store content length
        },
      ],
    });

    const fetchHeaders: Record<string, string> = { "Content-Type": "application/json" };
    if (token) fetchHeaders["Authorization"] = `Bearer ${token}`;

    fetch(url, {
      method: "POST",
      headers: fetchHeaders,
      body: payload,
      keepalive: true,
    });
  } catch {
    // Silent
  }
}
