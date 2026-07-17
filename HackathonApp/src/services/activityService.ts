import httpClient from "./httpClient";

interface TabSwitchEvent {
  eventType: string;
  timestamp: string;
  awayDurationSeconds?: number;
}

let eventQueue: TabSwitchEvent[] = [];
let lastHiddenTime: number | null = null;
let flushTimer: ReturnType<typeof setInterval> | null = null;
let isTracking = false;

/**
 * Starts tracking tab visibility and window focus.
 * Batches events and sends them to the server every 15 seconds.
 * Safe to call multiple times — only initializes once.
 */
export function startTabSwitchTracking() {
  if (isTracking) return; // Prevent duplicate listeners (React strict mode)
  isTracking = true;

  // Visibility change (tab switch / minimize) — primary signal
  document.addEventListener("visibilitychange", handleVisibilityChange);

  // Window blur/focus — only for Alt+Tab (when tab stays visible)
  window.addEventListener("blur", handleWindowBlur);
  window.addEventListener("focus", handleWindowFocus);

  // Flush queue every 15 seconds
  flushTimer = setInterval(flushEvents, 15000);
}

export function stopTabSwitchTracking() {
  if (!isTracking) return;
  isTracking = false;

  document.removeEventListener("visibilitychange", handleVisibilityChange);
  window.removeEventListener("blur", handleWindowBlur);
  window.removeEventListener("focus", handleWindowFocus);

  if (flushTimer) {
    clearInterval(flushTimer);
    flushTimer = null;
  }
  // Final flush
  flushEvents();
}

function handleVisibilityChange() {
  if (document.hidden) {
    lastHiddenTime = Date.now();
    eventQueue.push({
      eventType: "tab_hidden",
      timestamp: new Date().toISOString(),
    });
  } else {
    const awaySeconds = lastHiddenTime ? Math.floor((Date.now() - lastHiddenTime) / 1000) : undefined;
    lastHiddenTime = null;
    eventQueue.push({
      eventType: "tab_visible",
      timestamp: new Date().toISOString(),
      awayDurationSeconds: awaySeconds,
    });
  }
}

function handleWindowBlur() {
  // Only log blur if the tab is still VISIBLE (i.e., Alt+Tab, not a tab switch)
  // If tab is hidden, visibilitychange already handled it
  if (!document.hidden) {
    lastHiddenTime = Date.now();
    eventQueue.push({
      eventType: "window_blur",
      timestamp: new Date().toISOString(),
    });
  }
}

function handleWindowFocus() {
  // Only log focus if the tab is visible (coming back from Alt+Tab)
  if (!document.hidden && lastHiddenTime) {
    const awaySeconds = Math.floor((Date.now() - lastHiddenTime) / 1000);
    lastHiddenTime = null;
    eventQueue.push({
      eventType: "window_focus",
      timestamp: new Date().toISOString(),
      awayDurationSeconds: awaySeconds,
    });
  }
}

async function flushEvents() {
  if (eventQueue.length === 0) return;

  const events = [...eventQueue];
  eventQueue = [];

  try {
    await httpClient.post("/api/activity/tab-switch", { events });
  } catch {
    // If failed, put them back (retry next cycle)
    eventQueue = [...events, ...eventQueue];
  }
}

// Admin service
export const activityAdminService = {
  getOverview: async () => {
    const res = await httpClient.get("/api/activity/overview");
    return res.data.data;
  },

  getUserLogs: async (userId: string) => {
    const res = await httpClient.get(`/api/activity/${userId}/logs`);
    return res.data.data;
  },
};
