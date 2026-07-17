/**
 * DevTools Detection & Prevention
 * 
 * Uses devtools-detector library for reliable detection (same approach as enterprise HRMS apps).
 * Also blocks keyboard shortcuts and right-click.
 * 
 * On detection: shows a blocking overlay (no logout), logs the attempt.
 * When DevTools is closed: overlay is automatically removed.
 */

import { addListener, launch } from "devtools-detector";

let isActive = false;
let attemptCount = 0;
let isBlocked = false;
let onBlockedCallback: ((blocked: boolean) => void) | null = null;

// ─── Public API ───────────────────────────────────────────────

export function startDevToolsProtection() {
  if (isActive) return;
  isActive = true;
  attemptCount = 0;
  isBlocked = false;

  // Block keyboard shortcuts
  document.addEventListener("keydown", handleKeyDown, true);

  // Block right-click
  document.addEventListener("contextmenu", handleContextMenu, true);

  // Use devtools-detector library for reliable detection
  addListener((isOpen) => {
    if (!isActive) return;

    if (isOpen && !isBlocked) {
      isBlocked = true;
      attemptCount++;
      logAttemptToServer("devtools_open");
      onBlockedCallback?.(true);
    } else if (!isOpen && isBlocked) {
      isBlocked = false;
      onBlockedCallback?.(false);
    }
  });

  launch();
}

export function stopDevToolsProtection() {
  if (!isActive) return;
  isActive = false;

  document.removeEventListener("keydown", handleKeyDown, true);
  document.removeEventListener("contextmenu", handleContextMenu, true);
}

/**
 * Register a callback for when blocked state changes.
 */
export function setOnDevToolsBlocked(callback: (blocked: boolean) => void) {
  onBlockedCallback = callback;
}

/**
 * Returns current blocked state.
 */
export function isDevToolsBlocked(): boolean {
  return isBlocked;
}

// ─── Keyboard Shortcut Blocking ──────────────────────────────

function handleKeyDown(e: KeyboardEvent) {
  const key = e.key.toUpperCase();
  const code = e.code;

  // F12
  if (key === "F12" || code === "F12") {
    e.preventDefault();
    e.stopPropagation();
    e.stopImmediatePropagation();
    onDevToolsAttempt("keyboard_f12");
    return false;
  }

  // Ctrl+Shift+I
  if (e.ctrlKey && e.shiftKey && (key === "I" || code === "KeyI")) {
    e.preventDefault();
    e.stopPropagation();
    e.stopImmediatePropagation();
    onDevToolsAttempt("keyboard_ctrl_shift_i");
    return false;
  }

  // Ctrl+Shift+J
  if (e.ctrlKey && e.shiftKey && (key === "J" || code === "KeyJ")) {
    e.preventDefault();
    e.stopPropagation();
    e.stopImmediatePropagation();
    onDevToolsAttempt("keyboard_ctrl_shift_j");
    return false;
  }

  // Ctrl+Shift+C
  if (e.ctrlKey && e.shiftKey && (key === "C" || code === "KeyC")) {
    e.preventDefault();
    e.stopPropagation();
    e.stopImmediatePropagation();
    onDevToolsAttempt("keyboard_ctrl_shift_c");
    return false;
  }

  // Ctrl+U
  if (e.ctrlKey && !e.shiftKey && (key === "U" || code === "KeyU")) {
    e.preventDefault();
    e.stopPropagation();
    e.stopImmediatePropagation();
    onDevToolsAttempt("keyboard_ctrl_u");
    return false;
  }
}

// ─── Right-Click Blocking ────────────────────────────────────

function handleContextMenu(e: Event) {
  e.preventDefault();
  e.stopPropagation();
  onDevToolsAttempt("right_click");
}

// ─── Attempt Handler ─────────────────────────────────────────

function onDevToolsAttempt(method: string) {
  attemptCount++;
  logAttemptToServer(method);
}

// ─── Backend Logging ─────────────────────────────────────────

function logAttemptToServer(method: string) {
  try {
    const token = sessionStorage.getItem("token");
    const baseUrl = (import.meta.env.VITE_API_BASE_URL || "") + "/hackathonapi";
    const url = `${baseUrl}/api/activity/tab-switch`;
    const payload = JSON.stringify({
      events: [
        {
          eventType: `devtools_${method}`,
          timestamp: new Date().toISOString(),
          awayDurationSeconds: null,
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
