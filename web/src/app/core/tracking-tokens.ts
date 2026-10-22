// Anonymous reporters get a one-time token per report. Keeping it in this browser lets them reopen their report
// even while it waits for moderator approval; the link with ?t=... works from any device.
const storageKey = 'gjubrarko.tracking';

export function saveTrackingToken(reportId: string, token: string) {
  const all = readAll();
  all[reportId] = token;
  localStorage.setItem(storageKey, JSON.stringify(all));
}

export function trackingToken(reportId: string): string | null {
  return readAll()[reportId] ?? null;
}

function readAll(): Record<string, string> {
  try {
    return JSON.parse(localStorage.getItem(storageKey) ?? '{}');
  } catch {
    return {};
  }
}
