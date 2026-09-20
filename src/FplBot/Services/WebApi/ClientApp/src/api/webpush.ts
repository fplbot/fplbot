export interface SubscriberState {
  subscriberId: string;
  leagueId: number | null;
  events: string[];
  available: string[];
  requiresLeague: string[];
}

const STORAGE_KEY = "fplbot.subscriberId";

export const storedSubscriberId = () => localStorage.getItem(STORAGE_KEY);

export const isIos = () =>
  /iPad|iPhone|iPod/.test(navigator.userAgent) ||
  (navigator.userAgent.includes("Mac") && "ontouchend" in document);

export const isStandalone = () =>
  window.matchMedia("(display-mode: standalone)").matches ||
  (window.navigator as { standalone?: boolean }).standalone === true;

export const pushSupported = () => "serviceWorker" in navigator && "PushManager" in window;

function urlBase64ToUint8Array(base64: string): Uint8Array {
  const padded = (base64 + "=".repeat((4 - (base64.length % 4)) % 4)).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(padded);
  return Uint8Array.from([...raw].map((c) => c.charCodeAt(0)));
}

async function json<T>(input: string, init?: RequestInit): Promise<T> {
  const response = await fetch(input, init);
  if (!response.ok) throw new Error(`${init?.method ?? "GET"} ${input} failed: ${response.status}`);
  return (await response.json()) as T;
}

function authed(method: string, body?: unknown): RequestInit {
  return {
    method,
    headers: {
      "X-Subscriber-Id": storedSubscriberId() ?? "",
      ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
  };
}

export async function enableNotifications(leagueId: number | null, name: string | null): Promise<SubscriberState> {
  const permission = await Notification.requestPermission();
  if (permission !== "granted") throw new Error("Notifications were not allowed.");

  const registration = await navigator.serviceWorker.register("/sw.js");
  await navigator.serviceWorker.ready;

  const existing = await registration.pushManager.getSubscription();
  if (existing) await existing.unsubscribe();

  const { publicKey } = await json<{ publicKey: string }>("/api/web/push/key");
  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(publicKey).buffer as ArrayBuffer,
  });

  const raw = subscription.toJSON() as { endpoint: string; keys: { p256dh: string; auth: string } };
  const created = await json<{ subscriberId: string }>("/api/web/push/subscribe", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ endpoint: raw.endpoint, p256dh: raw.keys.p256dh, auth: raw.keys.auth, leagueId, name }),
  });

  localStorage.setItem(STORAGE_KEY, created.subscriberId);
  return await getState();
}

export const getState = () => json<SubscriberState>("/api/web/me", authed("GET"));

export const setEvents = (events: string[]) =>
  json<SubscriberState>("/api/web/me/events", authed("PUT", { events }));

export const setLeague = (leagueId: number | null) =>
  json<SubscriberState>("/api/web/me/league", authed("PUT", { leagueId }));

export async function sendTestNotification(): Promise<void> {
  const response = await fetch("/api/web/me/test", authed("POST"));
  if (!response.ok) throw new Error(`POST /api/web/me/test failed: ${response.status}`);
}

export async function unsubscribe(): Promise<void> {
  const response = await fetch("/api/web/me", authed("DELETE"));
  if (!response.ok && response.status !== 404) {
    throw new Error(`DELETE /api/web/me failed: ${response.status}`);
  }
  const registration = await navigator.serviceWorker.getRegistration("/sw.js");
  const subscription = await registration?.pushManager.getSubscription();
  await subscription?.unsubscribe();
  localStorage.removeItem(STORAGE_KEY);
}
