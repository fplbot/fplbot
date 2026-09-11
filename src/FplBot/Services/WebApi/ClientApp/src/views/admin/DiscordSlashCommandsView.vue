<script setup lang="ts">
import { ref, onMounted } from "vue";
import {
  getSlashCommands,
  installSlashCommands,
  installGlobalSlashCommands,
  uninstallSlashCommands,
  type DiscordSlashCommand,
} from "../../api/admin";

const commands = ref<DiscordSlashCommand[]>([]);
const loading = ref(true);
const busy = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  try {
    commands.value = await getSlashCommands();
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
      <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ feedback.text }}
      </p>

      <div class="actions">
        <button class="btn small" :disabled="busy" @click="run(installSlashCommands)">Install (guild)</button>
        <button class="btn small" :disabled="busy" @click="run(installGlobalSlashCommands)">Install (global)</button>
        <button class="btn small danger" :disabled="busy" @click="run(uninstallSlashCommands)">Uninstall (guild)</button>
      </div>

      <div v-if="loading" class="spinner"></div>
      <table v-else class="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(c, i) in commands" :key="i">
            <td>{{ c.name }}</td>
            <td>{{ c.description }}</td>
          </tr>
          <tr v-if="commands.length === 0">
            <td colspan="2">No slash commands installed.</td>
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

.actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-bottom: 1.5rem;
}
</style>
