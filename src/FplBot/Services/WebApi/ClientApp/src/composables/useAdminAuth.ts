import { ref } from "vue";
import { getMe, type AdminMe } from "../api/admin";

export type AdminAuthState = "loading" | "anonymous" | "forbidden" | "authorized";

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
    state.value = "anonymous";
    me.value = null;
  }
}

// Module-level singleton state: every component that calls this shares the same auth
// snapshot, so a logout/login triggered from one place (e.g. the layout's nav) is
// immediately visible everywhere without prop drilling or a full Pinia store.
export function useAdminAuth() {
  return { state, me, refresh };
}
