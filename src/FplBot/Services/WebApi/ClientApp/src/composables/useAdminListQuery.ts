import { ref, watch, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";

// Shared by every paginated/searchable admin list (Slack workspaces, Discord servers, ...):
// keeps `query`/`page` in the URL so a refresh or shared link restores the same view, and
// debounces search-driven reloads so typing doesn't hit the backend on every keystroke.
// Pager clicks reload immediately — only text input goes through the debounce.
export function useAdminListQuery(load: () => void | Promise<void>, debounceMs = 350) {
  const route = useRoute();
  const router = useRouter();

  const initialPage = Number(route.query.page);
  const query = ref(typeof route.query.q === "string" ? route.query.q : "");
  const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1);

  let searchTimer: ReturnType<typeof setTimeout> | undefined;

  function syncUrl() {
    const q: Record<string, string> = {};
    if (query.value) q.q = query.value;
    if (page.value > 1) q.page = String(page.value);
    router.replace({ query: q });
  }

  watch(query, () => {
    if (searchTimer) clearTimeout(searchTimer);
    searchTimer = setTimeout(() => {
      page.value = 1;
      syncUrl();
      load();
    }, debounceMs);
  });

  function goToPage(newPage: number) {
    page.value = newPage;
    syncUrl();
    load();
  }

  onMounted(load);

  return { query, page, goToPage };
}
