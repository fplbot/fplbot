<script setup lang="ts">
const props = defineProps<{ page: number; totalPages: number; totalCount: number }>();
const emit = defineEmits<{ (e: "update:page", value: number): void }>();
</script>

<template>
  <div class="pager">
    <button class="btn small" :disabled="props.page <= 1" @click="emit('update:page', props.page - 1)">&larr; Prev</button>
    <span>Page {{ props.page }} of {{ props.totalPages }} ({{ props.totalCount }} total)</span>
    <button class="btn small" :disabled="props.page >= props.totalPages" @click="emit('update:page', props.page + 1)">Next &rarr;</button>
  </div>
</template>
