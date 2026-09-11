<script setup lang="ts">
import { ref, onMounted } from "vue";
import {
  getSlashCommands,
  getSlashCommandDefinitions,
  installSlashCommands,
  installGlobalSlashCommands,
  uninstallSlashCommands,
  type DiscordSlashCommand,
  type SlashCommandDefinition,
} from "../../api/admin";

const definitions = ref<SlashCommandDefinition[]>([]);
const commands = ref<DiscordSlashCommand[]>([]);
const loading = ref(true);
const busy = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  try {
    const [defs, installed] = await Promise.all([getSlashCommandDefinitions(), getSlashCommands()]);
    definitions.value = defs;
    commands.value = installed;
  } finally {
    loading.value = false;
  }
}

onMounted(load);

async function run(action: () => Promise<{ message: string }>) {
  busy.value = true;
  feedback.value = null;
  try {
    const res = await action();
    feedback.value = { type: "success", text: res.message };
    await load();
  } catch (e) {
    feedback.value = { type: "error", text: "Action failed." };
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div>
    <h1>Discord slash commands</h1>
    <p class="lead">Manages slash commands for the test guild.</p>

    <div class="card">
      <h2>Commands installing will push</h2>
      <p class="hint">
        These are hardcoded in <code>DiscordSlashCommandsEnsurer</code> — install pushes exactly this set,
        one at a time (Discord rate-limits command registration).
      </p>
      <table class="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Description</th>
            <th>Options</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in definitions" :key="d.name">
            <td><code>/{{ d.name }}</code></td>
            <td>{{ d.description }}</td>
            <td>{{ d.optionsSummary || "—" }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card">
      <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ feedback.text }}
      </p>

      <div class="actions">
        <button class="btn small" :disabled="busy" @click="run(installSlashCommands)">Install to test guild</button>
        <button class="btn small" :disabled="busy" @click="run(installGlobalSlashCommands)">Install globally</button>
        <button class="btn small danger" :disabled="busy" @click="run(uninstallSlashCommands)">Uninstall from test guild</button>
      </div>
      <p class="hint">
        Guild installs show up in the table below within a few seconds. Global installs don't — Discord
        can take up to an hour to propagate those, and this page only lists guild-scoped commands.
      </p>

      <h2>Currently installed on the test guild</h2>
      <div v-if="loading" class="spinner"></div>
      <table v-else class="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in commands" :key="c.id">
            <td><code>/{{ c.name }}</code></td>
            <td>{{ c.description }}</td>
          </tr>
          <tr v-if="commands.length === 0">
            <td colspan="2">No slash commands installed on the test guild.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.card h2 {
  font-size: 1.1rem;
  margin-bottom: 0.5rem;
}

.hint {
  color: #6b7280;
  font-size: 0.85rem;
  margin-bottom: 1rem;
}

.actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-bottom: 0.75rem;
}

.card table + h2,
.card .hint + h2 {
  margin-top: 1.5rem;
}
</style>
