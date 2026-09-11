import { ref } from "vue";
import { getMe, AdminApiError } from "../api/api";
import type { AdminMe } from "../api/types";

export type AdminAuthState = "loading" | "anonymous" | "forbidden" | "authorized" | "error";

const state = ref<AdminAuthState>("loading");
const me = ref<AdminMe | null>(null);

async function refresh(): Promise<void> {
  state.value = "loading";
  try {
    const data = await getMe();
    if (data == null) {
      state.value = "anonymous";
      me.value = null;
      return;
    }
    me.value = data;
    state.value = data.isAdmin ? "authorized" : "forbidden";
  } catch (e) {
    // getMe() only throws for non-401 failures (401 already resolves to `null` above) —
    // a 500/network error here is not the same as "not logged in", so it gets its own
    // state rather than silently falling back to the login screen.
    state.value = "error";
    me.value = null;
  }
}

// Module-level singleton state: every component that calls this shares the same auth
// snapshot, so a logout/login triggered from one place (e.g. the layout's nav) is
// immediately visible everywhere without prop drilling or a full Pinia store.
export function useAdminAuth() {
  return { state, me, refresh };
}

// Turns an error from any admin.ts API call into a status-appropriate, user-facing
// message. A 401 here means the session cookie died mid-use (it was valid enough to
// reach this page originally) — flip the shared auth state so AdminLayout swaps back
// to the login screen instead of leaving a dead form on screen.
export function describeAdminError(e: unknown): string {
  if (e instanceof AdminApiError) {
    if (e.status === 401) {
      state.value = "anonymous";
      me.value = null;
      return "Your session has expired. Please sign in again.";
    }
    if (e.status === 403) {
      return e.detail ?? "You don't have permission to do that.";
    }
    if (e.status === 400) {
      return e.detail ?? "That request wasn't valid — check the form and try again.";
    }
    if (e.status === 502) {
      // A curated, safe message we constructed server-side about an upstream dependency
      // (Discord/Slack) failing — unlike a generic 500, this is fine to show verbatim.
      return e.detail ?? "An upstream service (Discord/Slack) failed to respond. Please try again.";
    }
    if (e.status >= 500) {
      // Deliberately ignore e.detail here even if present — a 500's detail could be an
      // internal error message, and this app never wants to surface that to the user.
      return "Something went wrong on the server. Please try again in a moment.";
    }
    return e.detail ?? `Request failed (status ${e.status}).`;
  }
  return "Network error — check your connection and try again.";
}
