<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{ value: number; total: number; label: string }>();

const percent = computed(() => (props.total > 0 ? Math.round((props.value / props.total) * 100) : 0));
const caption = computed(() => `${percent.value}% ${props.label}`);
</script>

<template>
  <div class="stat-meter">
    <div
      class="stat-meter-track"
      role="meter"
      :aria-valuenow="percent"
      aria-valuemin="0"
      aria-valuemax="100"
      :aria-label="caption"
      :title="caption"
    >
      <div class="stat-meter-fill" :style="{ width: `${percent}%` }" />
    </div>
    <span class="stat-meter-caption">{{ caption }}</span>
  </div>
</template>

<style scoped>
.stat-meter {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-top: 0.125rem;
}

.stat-meter-track {
  height: 6px;
  border-radius: 3px;
  /* Unfilled track is a lighter step of the fill's own hue, not a neutral gray, so the whole
     bar reads as one state rather than "colored bit + unrelated background". */
  background: color-mix(in srgb, var(--fpl-purple) 18%, transparent);
  overflow: hidden;
}

.stat-meter-fill {
  height: 100%;
  border-radius: 3px;
  background: var(--fpl-purple);
}

.stat-meter-caption {
  font-size: 0.75rem;
  color: #6b7280;
}
</style>
