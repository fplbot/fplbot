<script setup lang="ts">
import { computed, ref, watch } from "vue";

export interface ChannelPickerOption {
  value: string;
  label: string;
  group?: string;
  disabled?: boolean;
}

const props = withDefaults(
  defineProps<{
    id: string;
    options: ChannelPickerOption[];
    label?: string;
    placeholder?: string;
  }>(),
  { label: "Channel", placeholder: "Select a channel…" }
);

const selected = defineModel<string>({ default: "" });

const filter = ref("");
const open = ref(false);
const highlighted = ref("");
const selectedLabel = ref("");

const matching = computed(() => {
  const needle = filter.value.trim().toLowerCase().replace(/^#/, "");
  if (!needle) return props.options;
  return props.options.filter((o) => o.label.toLowerCase().includes(needle));
});

const selectable = computed(() => matching.value.filter((o) => !o.disabled));

const groups = computed(() => {
  const byGroup = new Map<string, ChannelPickerOption[]>();
  for (const option of matching.value) {
    const key = option.group ?? "";
    byGroup.set(key, [...(byGroup.get(key) ?? []), option]);
  }
  return [...byGroup.entries()].map(([name, options]) => ({ name, options }));
});

function openPicker() {
  if (selected.value) filter.value = "";
  open.value = true;
  highlighted.value = selectable.value[0]?.value ?? "";
}

function closePicker() {
  open.value = false;
  if (selected.value) filter.value = selectedLabel.value;
}

function choose(option: ChannelPickerOption) {
  selected.value = option.value;
  selectedLabel.value = option.label;
  filter.value = option.label;
  open.value = false;
}

function moveHighlight(delta: number) {
  open.value = true;
  const options = selectable.value;
  if (options.length === 0) return;
  const current = options.findIndex((o) => o.value === highlighted.value);
  const next = Math.min(Math.max(current + delta, 0), options.length - 1);
  highlighted.value = options[current === -1 ? 0 : next].value;
}

function chooseHighlighted() {
  const option = selectable.value.find((o) => o.value === highlighted.value);
  if (option) choose(option);
}

watch(filter, (value) => {
  if (selected.value && value !== selectedLabel.value && open.value) {
    selected.value = "";
  }
});
</script>

<template>
  <div class="field combobox">
    <label :for="id">{{ label }}</label>
    <input
      :id="id"
      v-model="filter"
      type="text"
      autocomplete="off"
      role="combobox"
      :aria-controls="`${id}-list`"
      :aria-expanded="open"
      :placeholder="placeholder"
      @focus="openPicker"
      @input="open = true"
      @keydown.down.prevent="moveHighlight(1)"
      @keydown.up.prevent="moveHighlight(-1)"
      @keydown.enter.prevent="chooseHighlighted"
      @keydown.esc="closePicker"
      @blur="closePicker"
    />
    <div class="picker-anchor">
      <ul v-if="open" :id="`${id}-list`" class="picker" role="listbox">
        <template v-for="group in groups" :key="group.name">
          <li v-if="group.name" class="picker-group">{{ group.name }}</li>
          <li
            v-for="o in group.options"
            :key="o.value"
            :class="['picker-option', { highlighted: o.value === highlighted, taken: o.disabled }]"
            role="option"
            :aria-selected="o.value === selected"
            @mousedown.prevent="o.disabled || choose(o)"
            @mouseenter="o.disabled || (highlighted = o.value)"
          >
            {{ o.label }}
          </li>
        </template>
        <li v-if="matching.length === 0" class="picker-empty">No channel matches &ldquo;{{ filter }}&rdquo;</li>
      </ul>
    </div>
    <p class="picker-count">
      <template v-if="selected">Selected {{ selectedLabel }}</template>
      <template v-else-if="filter">{{ matching.length }} of {{ options.length }} channels</template>
      <template v-else>{{ options.length }} channels &mdash; start typing to filter</template>
    </p>
  </div>
</template>

<style scoped>
.combobox {
  position: relative;
}

.picker-anchor {
  position: relative;
}

.picker {
  position: absolute;
  z-index: 10;
  top: 0;
  left: 0;
  right: 0;
  max-height: 16rem;
  overflow-y: auto;
  margin: 0.25rem 0 0;
  padding: 0;
  list-style: none;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  box-shadow: 0 8px 24px rgb(0 0 0 / 12%);
}

.picker-group {
  padding: 0.35rem 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #6b7280;
  background: #f9fafb;
  border-top: 1px solid #e5e7eb;
}

.picker-group:first-child {
  border-top: none;
}

.picker-option {
  padding: 0.4rem 0.75rem;
  cursor: pointer;
}

.picker-option.highlighted {
  background: #eef2ff;
}

.picker-option.taken {
  color: #9ca3af;
  cursor: not-allowed;
}

.picker-empty {
  padding: 0.5rem 0.75rem;
  color: #6b7280;
  font-style: italic;
}

.picker-count {
  color: #6b7280;
  font-style: italic;
  font-size: 0.9rem;
}
</style>
