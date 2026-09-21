<script setup lang="ts">
defineProps<{ label: string; value: string; sublabel?: string }>();
</script>

<template>
  <div class="card stat-tile">
    <span class="stat-label">{{ label }}</span>
    <span class="stat-value">{{ value }}</span>
    <span v-if="sublabel" class="stat-sublabel">{{ sublabel }}</span>
  </div>
</template>

<style scoped>
.stat-tile {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  border-top: 3px solid var(--fpl-green);
}

.stat-label {
  font-weight: bold;
  font-size: 0.875rem;
  color: #6b7280;
}

.stat-value {
  font-size: 2.5rem;
  font-weight: 600;
  color: var(--fpl-purple);
}

.stat-sublabel {
  font-size: 0.8rem;
  color: #6b7280;
}
</style>
