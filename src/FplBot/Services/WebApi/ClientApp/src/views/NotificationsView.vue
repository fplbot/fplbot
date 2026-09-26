<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRoute } from "vue-router";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";
import { searchLeagues, getLeague, getEntry, searchEntries } from "../api/api";
import type { LeagueItem, EntryItem } from "../api/types";
import {
  enableNotifications,
  getState,
  setEvents,
  setLeague,
  setEntry,
  sendTestNotification,
  unsubscribe,
  storedSubscriberId,
  isIos,
  isStandalone,
  pushSupported,
  type SubscriberState,
} from "../api/webpush";

const route = useRoute();

const state = ref<SubscriberState | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const busy = ref(false);
const testSent = ref(false);

const leagueQuery = ref("");
const leagueResults = ref<LeagueItem[]>([]);
const searched = ref(false);
const manualLeagueInput = ref("");
const manualLeagueError = ref<string | null>(null);
const chosenLeagueId = ref<number | null>(route.query.league ? Number(route.query.league) || null : null);
const chosenLeagueName = ref<string | null>(null);
const followedLeagueName = ref<string | null>(null);
const deviceName = ref("");

const manualEntryInput = ref("");
const manualEntryError = ref<string | null>(null);
const linkedEntryName = ref<string | null>(null);
const entryQuery = ref("");
const entryResults = ref<EntryItem[]>([]);
const entrySearched = ref(false);

const needsHomeScreen = computed(() => isIos() && !isStandalone());
const supported = computed(() => pushSupported());
const shareLink = computed(() =>
  state.value?.leagueId ? `${window.location.origin}/notifications?league=${state.value.leagueId}` : null
);

const linkCopied = ref(false);
let copiedTimeout: ReturnType<typeof setTimeout> | undefined;

async function copyShareLink() {
  if (!shareLink.value) return;
  try {
    await navigator.clipboard.writeText(shareLink.value);
    linkCopied.value = true;
    clearTimeout(copiedTimeout);
    copiedTimeout = setTimeout(() => (linkCopied.value = false), 2000);
  } catch (e) {
    error.value = (e as Error).message;
  }
}

function label(event: string) {
  return event.replace(/([a-z])([A-Z])/g, "$1 $2");
}

onMounted(async () => {
  if (storedSubscriberId()) {
    try {
      state.value = await getState();
      await loadFollowedLeagueName();
      await loadLinkedEntryName();
    } catch {
      state.value = null;
    }
  }
  if (!state.value && chosenLeagueId.value) {
    try {
      chosenLeagueName.value = (await getLeague(chosenLeagueId.value))?.leagueName ?? null;
    } catch {
      chosenLeagueName.value = null;
    }
  }
  loading.value = false;
});

async function loadFollowedLeagueName() {
  if (!state.value?.leagueId) {
    followedLeagueName.value = null;
    return;
  }
  try {
    followedLeagueName.value = (await getLeague(state.value.leagueId))?.leagueName ?? null;
  } catch {
    followedLeagueName.value = null;
  }
}

async function loadLinkedEntryName() {
  if (!state.value?.entryId) {
    linkedEntryName.value = null;
    return;
  }
  try {
    const entry = await getEntry(state.value.entryId);
    linkedEntryName.value = entry?.teamName ?? entry?.realName ?? null;
  } catch {
    linkedEntryName.value = null;
  }
}

function parseEntryId(raw: string): number | null {
  const trimmed = raw.trim();
  const fromUrl = trimmed.match(/entry\/(\d+)/);
  const id = Number(fromUrl ? fromUrl[1] : trimmed);
  return Number.isInteger(id) && id > 0 ? id : null;
}

async function linkEntry() {
  manualEntryError.value = null;
  const id = parseEntryId(manualEntryInput.value);
  if (id === null) {
    manualEntryError.value = "Enter your entry id, or paste a fantasy.premierleague.com/entry/... link.";
    return;
  }
  try {
    const entry = await getEntry(id);
    if (!entry) {
      manualEntryError.value = `No FPL team found with id ${id}.`;
      return;
    }
    state.value = await setEntry(id);
    linkedEntryName.value = entry.teamName ?? entry.realName ?? null;
    manualEntryInput.value = "";
  } catch (e) {
    manualEntryError.value = (e as Error).message;
  }
}

async function unlinkEntry() {
  error.value = null;
  try {
    state.value = await setEntry(null);
    linkedEntryName.value = null;
  } catch (e) {
    error.value = (e as Error).message;
  }
}

async function runEntrySearch() {
  if (!entryQuery.value) return;
  manualEntryError.value = null;
  try {
    entryResults.value = (await searchEntries(entryQuery.value, 0)).exposedHits;
    entrySearched.value = true;
  } catch (e) {
    manualEntryError.value = (e as Error).message;
  }
}

async function pickEntry(entry: EntryItem) {
  manualEntryError.value = null;
  try {
    state.value = await setEntry(entry.id);
    linkedEntryName.value = entry.teamName ?? entry.realName ?? null;
    entryResults.value = [];
    entrySearched.value = false;
    entryQuery.value = "";
  } catch (e) {
    manualEntryError.value = (e as Error).message;
  }
}

async function runLeagueSearch() {
  if (!leagueQuery.value) return;
  error.value = null;
  try {
    leagueResults.value = (await searchLeagues(leagueQuery.value, 0)).exposedHits;
    searched.value = true;
  } catch (e) {
    error.value = (e as Error).message;
  }
}

function pickLeague(league: LeagueItem) {
  chosenLeagueId.value = league.id;
  chosenLeagueName.value = league.name ?? null;
  leagueResults.value = [];
  searched.value = false;
  leagueQuery.value = "";
}

function clearChosenLeague() {
  chosenLeagueId.value = null;
  chosenLeagueName.value = null;
}

function parseLeagueId(raw: string): number | null {
  const trimmed = raw.trim();
  const fromUrl = trimmed.match(/leagues\/(\d+)/);
  const id = Number(fromUrl ? fromUrl[1] : trimmed);
  return Number.isInteger(id) && id > 0 ? id : null;
}

async function resolveManualLeague(raw: string): Promise<LeagueItem | null> {
  const id = parseLeagueId(raw);
  if (id === null) {
    manualLeagueError.value = "Enter a league id, or paste a fantasy.premierleague.com league link.";
    return null;
  }
  try {
    const league = await getLeague(id);
    if (!league) {
      manualLeagueError.value = `No league found with id ${id}.`;
      return null;
    }
    return { id, name: league.leagueName };
  } catch (e) {
    manualLeagueError.value = (e as Error).message;
    return null;
  }
}

async function useManualLeague() {
  manualLeagueError.value = null;
  const league = await resolveManualLeague(manualLeagueInput.value);
  if (!league) return;
  if (state.value) {
    await followLeague(league);
  } else {
    pickLeague(league);
  }
  manualLeagueInput.value = "";
}

async function enable() {
  busy.value = true;
  error.value = null;
  try {
    state.value = await enableNotifications(chosenLeagueId.value, deviceName.value || null);
    await loadFollowedLeagueName();
  } catch (e) {
    error.value = (e as Error).message;
  } finally {
    busy.value = false;
  }
}

async function toggle(event: string) {
  if (!state.value) return;
  const next = state.value.events.includes(event)
    ? state.value.events.filter((e) => e !== event)
    : [...state.value.events, event];
  error.value = null;
  try {
    state.value = await setEvents(next);
  } catch (e) {
    error.value = (e as Error).message;
  }
}

const requiresLeague = (event: string) =>
  !!state.value && state.value.requiresLeague.includes(event) && state.value.leagueId === null;

async function followLeague(league: LeagueItem) {
  error.value = null;
  try {
    state.value = await setLeague(league.id);
    followedLeagueName.value = league.name ?? null;
    leagueResults.value = [];
    searched.value = false;
    leagueQuery.value = "";
  } catch (e) {
    error.value = (e as Error).message;
  }
}

async function stopFollowing() {
  error.value = null;
  try {
    state.value = await setLeague(null);
    followedLeagueName.value = null;
  } catch (e) {
    error.value = (e as Error).message;
  }
}

async function sendTest() {
  error.value = null;
  testSent.value = false;
  try {
    await sendTestNotification();
    testSent.value = true;
  } catch (e) {
    error.value = (e as Error).message;
  }
}

async function stop() {
  busy.value = true;
  error.value = null;
  try {
    await unsubscribe();
    state.value = null;
    followedLeagueName.value = null;
  } catch (e) {
    error.value = (e as Error).message;
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div class="page">
    <NavBar />

    <div class="content">
      <h1>Notifications on this device</h1>

      <section v-if="needsHomeScreen" class="card">
        <h2>Add FplBot to your Home Screen first</h2>
        <p>
          iPhones and iPads only deliver notifications to sites added to the Home Screen. Tap the
          Share button in Safari, choose <strong>Add to Home Screen</strong>, then open FplBot from
          the icon and come back to this page.
        </p>
      </section>

      <section v-else-if="!supported" class="card">
        <p>This browser doesn't support web push notifications.</p>
      </section>

      <div v-else-if="loading" class="spinner-wrap">
        <div class="spinner"></div>
      </div>

      <section v-else-if="!state" class="card">
        <h2>Which league do you want to follow?</h2>

        <div v-if="chosenLeagueId" class="chosen-league">
          <p>
            Following <strong>{{ chosenLeagueName ?? `league ${chosenLeagueId}` }}</strong>
            <button class="link-btn" @click="clearChosenLeague">Change</button>
          </p>
        </div>
        <template v-else>
          <form class="search-form" @submit.prevent="runLeagueSearch">
            <input v-model="leagueQuery" placeholder="Search for your league" class="search-input" />
            <button type="submit" class="btn">Search</button>
          </form>
          <ul v-if="leagueResults.length" class="league-list">
            <li v-for="league in leagueResults" :key="league.id">
              <button class="league-row" @click="pickLeague(league)">
                <span class="league-name">{{ league.name }}</span>
                <span class="league-admin" v-if="league.adminName">Admin: {{ league.adminName }}</span>
              </button>
            </li>
          </ul>
          <p v-else-if="searched">No leagues matched "{{ leagueQuery }}".</p>

          <form class="search-form manual-league" @submit.prevent="useManualLeague">
            <input
              v-model="manualLeagueInput"
              placeholder="Or paste a league id or fantasy.premierleague.com link"
              class="search-input"
            />
            <button type="submit" class="btn">Use this</button>
          </form>
          <p v-if="manualLeagueError" class="error">{{ manualLeagueError }}</p>

          <p class="hint">
            You can skip this and add a league later — league-specific notifications stay off until you do.
          </p>
        </template>

        <input v-model="deviceName" placeholder="Name this device (optional)" class="search-input" />
        <button class="btn enable-btn" :disabled="busy" @click="enable">Enable notifications</button>
        <p v-if="error" class="error">{{ error }}</p>
      </section>

      <section v-else class="card">
        <div v-if="state.leagueId" class="chosen-league">
          <p>
            Following <strong>{{ followedLeagueName ?? `league ${state.leagueId}` }}</strong>
            <button class="link-btn" @click="stopFollowing">Stop following</button>
          </p>
        </div>
        <template v-else>
          <p>No league followed — league notifications are unavailable until you pick one.</p>
          <form class="search-form" @submit.prevent="runLeagueSearch">
            <input v-model="leagueQuery" placeholder="Search for your league" class="search-input" />
            <button type="submit" class="btn">Search</button>
          </form>
          <ul v-if="leagueResults.length" class="league-list">
            <li v-for="league in leagueResults" :key="league.id">
              <button class="league-row" @click="followLeague(league)">
                <span class="league-name">{{ league.name }}</span>
                <span class="league-admin" v-if="league.adminName">Admin: {{ league.adminName }}</span>
              </button>
            </li>
          </ul>
          <p v-else-if="searched">No leagues matched "{{ leagueQuery }}".</p>

          <form class="search-form manual-league" @submit.prevent="useManualLeague">
            <input
              v-model="manualLeagueInput"
              placeholder="Or paste a league id or fantasy.premierleague.com link"
              class="search-input"
            />
            <button type="submit" class="btn">Use this</button>
          </form>
          <p v-if="manualLeagueError" class="error">{{ manualLeagueError }}</p>
        </template>

        <h2>Your FPL team</h2>
        <div v-if="state.entryId" class="chosen-league">
          <p>
            Linked to <strong>{{ linkedEntryName ?? `entry ${state.entryId}` }}</strong>
            <button class="link-btn" @click="unlinkEntry">Unlink</button>
          </p>
        </div>
        <template v-else>
          <p class="hint">Link your FPL team for a more personalized experience later.</p>
          <form class="search-form" @submit.prevent="runEntrySearch">
            <input v-model="entryQuery" placeholder="Search for your team by name" class="search-input" />
            <button type="submit" class="btn">Search</button>
          </form>
          <ul v-if="entryResults.length" class="league-list">
            <li v-for="entry in entryResults" :key="entry.id">
              <button class="league-row" @click="pickEntry(entry)">
                <span class="league-name">{{ entry.teamName ?? entry.realName ?? `entry ${entry.id}` }}</span>
                <span class="league-admin" v-if="entry.teamName && entry.realName">{{ entry.realName }}</span>
              </button>
            </li>
          </ul>
          <p v-else-if="entrySearched">No teams matched "{{ entryQuery }}".</p>

          <form class="search-form manual-league" @submit.prevent="linkEntry">
            <input
              v-model="manualEntryInput"
              placeholder="Or paste your entry id or fantasy.premierleague.com/entry/... link"
              class="search-input"
            />
            <button type="submit" class="btn">Use this</button>
          </form>
          <p v-if="manualEntryError" class="error">{{ manualEntryError }}</p>
        </template>

        <h2>Notifications</h2>
        <ul class="event-list">
          <li v-for="event in state.available" :key="event">
            <label :class="{ disabled: requiresLeague(event) }">
              <input
                type="checkbox"
                :checked="state.events.includes(event)"
                :disabled="requiresLeague(event)"
                @change="toggle(event)"
              />
              {{ label(event) }}
              <em v-if="requiresLeague(event)">needs a league</em>
            </label>
          </li>
        </ul>

        <div class="actions">
          <button class="btn" @click="sendTest">Send a test notification</button>
          <button class="btn danger" :disabled="busy" @click="stop">Turn off notifications</button>
        </div>
        <p v-if="testSent">Test notification sent — it should arrive in a moment.</p>
        <p v-if="error" class="error">{{ error }}</p>

        <div v-if="shareLink" class="share">
          <p>Share this with your league:</p>
          <button class="btn small share-btn" @click="copyShareLink">
            <code>{{ shareLink }}</code>
            <span class="copy-label">{{ linkCopied ? "Copied!" : "Copy" }}</span>
          </button>
        </div>
      </section>
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
  max-width: 36rem;
  width: 100%;
  margin: 0 auto;
  padding: 3rem 1rem;
}

h1 {
  font-size: 2rem;
  font-weight: bold;
  margin-bottom: 1.5rem;
  text-align: center;
}

h2 {
  font-size: 1.25rem;
  font-weight: bold;
  margin: 1rem 0 0.75rem;
}

.card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.search-form {
  display: flex;
  gap: 0.5rem;
}

.search-form .search-input {
  flex-grow: 1;
}

.manual-league {
  margin-top: 0.5rem;
}

.league-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.league-row {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  cursor: pointer;
  text-align: left;
}

.league-row:hover {
  background: #f9fafb;
}

.league-name {
  font-weight: bold;
}

.league-admin {
  font-size: 0.875rem;
  color: #6b7280;
}

.link-btn {
  background: none;
  border: none;
  font-weight: bold;
  text-decoration: underline;
  cursor: pointer;
  color: var(--fpl-purple);
  padding: 0;
  margin-left: 0.5rem;
}

.hint {
  font-size: 0.875rem;
  color: #6b7280;
}

.enable-btn {
  margin-top: 0.5rem;
}

.event-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.event-list label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.event-list label.disabled {
  color: #9ca3af;
}

.event-list em {
  font-size: 0.875rem;
  color: #9ca3af;
}

.actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-top: 0.5rem;
}

.error {
  color: #b91c1c;
}

.share {
  margin-top: 0.5rem;
}

.share-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 0.375rem;
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  text-align: left;
}

.share-btn code {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.875rem;
  background: none;
  padding: 0;
}

.copy-label {
  flex-shrink: 0;
  font-weight: bold;
  font-size: 0.8125rem;
  color: var(--fpl-purple);
}

.spinner-wrap {
  display: flex;
  justify-content: center;
  padding: 1rem 0;
}
</style>
