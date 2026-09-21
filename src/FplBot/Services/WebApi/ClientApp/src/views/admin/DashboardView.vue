<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getDiscordReachStats } from "../../api/api";
import type { GuildReachStats } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { formatNumber } from "../../formatting";
import StatTile from "../../components/StatTile.vue";

const loading = ref(true);
const error = ref("");
const discordReach = ref<GuildReachStats | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    discordReach.value = await getDiscordReachStats();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <h1>Dashboard</h1>

    <p v-if="error" class="alert alert-error">{{ error }}</p>
    <div v-if="loading" class="spinner"></div>

    <div v-else-if="discordReach" class="stat-grid">
      <StatTile label="Total Discord servers" :value="formatNumber(discordReach.totalGuilds)" />
      <StatTile
        label="Total Discord reach"
        :value="formatNumber(discordReach.totalApproximateMembers)"
        sublabel="Includes bot accounts, not just people. Servers not yet counted contribute zero."
      />
    </div>
  </div>
</template>

<style scoped>
.stat-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
  gap: 1.5rem;
}
</style>
