<script setup lang="ts">
import { ref, watch, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";
import { searchAny } from "../api/api";
import type { SearchAnyResult, SearchType } from "../api/types";

const route = useRoute();
const router = useRouter();

type Status = "init" | "empty" | "loading" | "success" | "error";

const searchValue = ref((route.query.q as string) ?? "");
const submittedValue = ref(searchValue.value);
const page = ref(route.query.page ? parseInt(route.query.page as string, 10) : 0);
const searchForEntries = ref(true);
const searchForLeagues = ref(true);

const status = ref<Status>(submittedValue.value ? "loading" : "init");
const result = ref<SearchAnyResult | null>(null);

function currentSearchType(): SearchType {
  if (searchForEntries.value && searchForLeagues.value) return "All";
  if (searchForEntries.value) return "Entries";
  if (searchForLeagues.value) return "Leagues";
  return "All";
}

function updateQueryParam() {
  router.replace({
    path: "/search",
    query: { q: submittedValue.value, page: String(page.value), type: currentSearchType() },
  });
}

async function runSearch() {
  if (submittedValue.value === "") {
    status.value = "empty";
    result.value = null;
    return;
  }
  status.value = "loading";
  try {
    result.value = await searchAny(submittedValue.value, page.value, currentSearchType());
    status.value = "success";
  } catch (e) {
    status.value = "error";
  }
}

function submitSearch() {
  submittedValue.value = searchValue.value;
  page.value = 0;
  updateQueryParam();
  runSearch();
}

function goToPage(newPage: number) {
  page.value = newPage;
  updateQueryParam();
  runSearch();
}

watch([searchForEntries, searchForLeagues], () => {
  page.value = 0;
  updateQueryParam();
  runSearch();
});

onMounted(() => {
  if (submittedValue.value) {
    runSearch();
  }
});

function fplEntryUrl(id: number) {
  return `https://fantasy.premierleague.com/entry/${id}/history/`;
}

function fplLeagueUrl(id: number) {
  return `https://fantasy.premierleague.com/leagues/${id}/standings/c`;
}
</script>

<template>
  <div class="page">
    <NavBar />

    <div class="content">
      <div class="search-header">
        <h1>Search FPL content</h1>
        <p>Search for managers or leagues.</p>

        <form class="search-form" @submit.prevent="submitSearch">
          <input
            v-model="searchValue"
            placeholder="Magnus Carlsen"
            aria-label="Search for FPL player"
            class="search-input"
          />
          <button type="submit" class="btn long">Search</button>

          <div class="checkboxes">
            <label>
              <input type="checkbox" v-model="searchForEntries" />
              Managers
            </label>
            <label>
              <input type="checkbox" v-model="searchForLeagues" />
              Leagues
            </label>
          </div>
        </form>
      </div>

      <div class="results">
        <p v-if="status === 'init'">Search results will appear here. You can search by name or team name</p>
        <p v-else-if="status === 'empty'">Please enter a search value. You can search by name or team name</p>
        <p v-else-if="status === 'error'">Ooops, looks like something went wrong &#129301;</p>
        <div v-else-if="status === 'loading'" class="spinner-wrap">
          <div class="spinner"></div>
        </div>
        <template v-else-if="status === 'success' && result">
          <p v-if="result.exposedHits.length < 1">
            Search for "{{ submittedValue }}" did not return any matches
          </p>
          <template v-else>
            <p class="results-title">Search results for "{{ submittedValue }}"</p>

            <div class="pagination" v-if="result.totalPages > 1">
              <button
                v-if="page > 0"
                class="link-btn"
                @click="goToPage(page - 1)"
              >
                Prev
              </button>
              <span>Page {{ page + 1 }} of {{ result.totalPages }}</span>
              <button
                v-if="page + 1 < result.totalPages"
                class="link-btn"
                @click="goToPage(page + 1)"
              >
                Next
              </button>
            </div>

            <div class="hit-list">
              <div v-for="(hit, i) in result.exposedHits" :key="i" class="hit-row">
                <template v-if="hit.type === 'entry'">
                  <span class="icon">&#128100;</span>
                  <div class="hit-main">
                    <span class="hit-title">{{ (hit.source as any).realName }}</span>
                    <span class="hit-sub">{{ (hit.source as any).teamName }}</span>
                  </div>
                  <a
                    :href="fplEntryUrl((hit.source as any).id)"
                    target="_blank"
                    rel="noreferrer"
                    class="view-link"
                  >View</a>
                </template>
                <template v-else-if="hit.type === 'league'">
                  <span class="icon">&#127942;</span>
                  <div class="hit-main">
                    <span class="hit-title">{{ (hit.source as any).name }}</span>
                    <span class="hit-sub">Admin: {{ (hit.source as any).adminName }}</span>
                  </div>
                  <a
                    :href="fplLeagueUrl((hit.source as any).id)"
                    target="_blank"
                    rel="noreferrer"
                    class="view-link"
                  >View</a>
                </template>
              </div>
            </div>
          </template>
        </template>
      </div>
    </div>

    <AppFooter />
  </div>
</template>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.content {
  flex-grow: 1;
}

.search-header {
  max-width: 48rem;
  margin: 0 auto;
  padding: 3rem 1rem;
  text-align: center;
}

.search-header h1 {
  font-size: 2rem;
  font-weight: bold;
  margin-bottom: 0.5rem;
}

.search-form {
  margin-top: 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.search-form .btn {
  max-width: 12rem;
}

.checkboxes {
  display: flex;
  gap: 1.5rem;
  margin-top: 0.5rem;
}

.checkboxes label {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.results {
  max-width: 48rem;
  margin: 0 auto;
  padding: 0 1rem 3rem;
  text-align: center;
}

.spinner-wrap {
  display: flex;
  justify-content: center;
  padding: 1rem 0;
}

.results-title {
  font-size: 1.25rem;
  margin-bottom: 1rem;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.link-btn {
  background: none;
  border: none;
  font-weight: bold;
  text-decoration: underline;
  cursor: pointer;
  color: var(--fpl-purple);
  padding: 0;
}

.hit-list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.hit-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 1rem;
  text-align: left;
}

.hit-row:hover {
  background: #f9fafb;
}

.icon {
  font-size: 1.5rem;
}

.hit-main {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
}

.hit-title {
  font-weight: bold;
}

.hit-sub {
  font-size: 0.875rem;
  color: #6b7280;
}

.view-link {
  font-weight: bold;
  text-decoration: underline;
  white-space: nowrap;
}
</style>
