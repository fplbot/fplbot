<script setup lang="ts">
import { computed, ref } from "vue";
import type { GuildSizeBucket } from "../api/types";
import { formatNumber } from "../formatting";

const props = defineProps<{ buckets: GuildSizeBucket[] }>();

const showTable = ref(false);
const hoveredIndex = ref<number | null>(null);

// Layout constants (SVG viewBox units).
const WIDTH = 720;
const HEIGHT = 280;
const MARGIN = { top: 24, right: 12, bottom: 56, left: 40 };
const PLOT_WIDTH = WIDTH - MARGIN.left - MARGIN.right;
const PLOT_HEIGHT = HEIGHT - MARGIN.top - MARGIN.bottom;
const BAR_MAX_WIDTH = 24;
const BAR_RADIUS = 4;

const maxCount = computed(() => Math.max(0, ...props.buckets.map((b) => b.count)));

// "Nice" tick step so the y-axis reads 0 / round-number / round-number instead of raw maxima.
function niceStep(max: number): number {
  if (max <= 4) return 1;
  const roughStep = max / 4;
  const exp = Math.floor(Math.log10(roughStep));
  const base = 10 ** exp;
  const frac = roughStep / base;
  const niceFrac = frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 5 ? 5 : 10;
  return niceFrac * base;
}

const yTicks = computed(() => {
  if (maxCount.value === 0) return [0];
  const step = niceStep(maxCount.value);
  const niceMax = Math.ceil(maxCount.value / step) * step;
  const ticks: number[] = [];
  for (let v = 0; v <= niceMax; v += step) ticks.push(v);
  return ticks;
});

const yMax = computed(() => yTicks.value[yTicks.value.length - 1] || 1);

function yFor(count: number): number {
  return MARGIN.top + PLOT_HEIGHT - (count / yMax.value) * PLOT_HEIGHT;
}

const bandWidth = computed(() => PLOT_WIDTH / Math.max(1, props.buckets.length));
const barWidth = computed(() => Math.min(BAR_MAX_WIDTH, bandWidth.value - 12));

// The single bar the direct label rides on - the mode of the distribution, per "label the
// extreme, let the axis/tooltip/table carry the rest" (never a number on every bar).
const tallestIndex = computed(() => {
  if (maxCount.value === 0) return -1;
  return props.buckets.findIndex((b) => b.count === maxCount.value);
});

function barX(index: number): number {
  return MARGIN.left + index * bandWidth.value + (bandWidth.value - barWidth.value) / 2;
}

// Square baseline, rounded top corners only - a plain rx/ry rect would round the bottom too.
function barPath(index: number, count: number): string {
  const x = barX(index);
  const yTop = yFor(count);
  const yBase = MARGIN.top + PLOT_HEIGHT;
  const w = barWidth.value;
  const r = Math.min(BAR_RADIUS, w / 2, Math.max(0, yBase - yTop));

  if (yBase - yTop <= 0) return "";

  return [
    `M ${x},${yBase}`,
    `L ${x},${yTop + r}`,
    `A ${r},${r} 0 0 1 ${x + r},${yTop}`,
    `L ${x + w - r},${yTop}`,
    `A ${r},${r} 0 0 1 ${x + w},${yTop + r}`,
    `L ${x + w},${yBase}`,
    "Z",
  ].join(" ");
}

function onEnter(index: number) {
  hoveredIndex.value = index;
}

function onLeave() {
  hoveredIndex.value = null;
}

const tooltip = computed(() => {
  if (hoveredIndex.value === null) return null;
  const bucket = props.buckets[hoveredIndex.value];
  if (!bucket) return null;
  const x = barX(hoveredIndex.value) + barWidth.value / 2;
  const y = yFor(bucket.count);
  return { bucket, x, y };
});
</script>

<template>
  <div class="chart-card">
    <div class="chart-header">
      <h2>Discord servers by size</h2>
      <button class="btn-link" @click="showTable = !showTable">
        {{ showTable ? "View as chart" : "View as table" }}
      </button>
    </div>

    <table v-if="showTable" class="admin-table">
      <thead>
        <tr>
          <th>Size</th>
          <th>Servers</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="bucket in buckets" :key="bucket.label">
          <td>{{ bucket.label }}</td>
          <td>{{ formatNumber(bucket.count) }}</td>
        </tr>
      </tbody>
    </table>

    <div v-else class="chart-wrap">
      <svg :viewBox="`0 0 ${WIDTH} ${HEIGHT}`" class="chart-svg" role="img" aria-label="Distribution of Discord servers by approximate member count">
        <!-- Gridlines + y-axis ticks -->
        <g class="gridlines">
          <line
            v-for="tick in yTicks"
            :key="tick"
            :x1="MARGIN.left"
            :x2="WIDTH - MARGIN.right"
            :y1="yFor(tick)"
            :y2="yFor(tick)"
          />
          <text v-for="tick in yTicks" :key="`label-${tick}`" :x="MARGIN.left - 8" :y="yFor(tick)" class="y-tick">
            {{ formatNumber(tick) }}
          </text>
        </g>

        <!-- Bars -->
        <g
          v-for="(bucket, index) in buckets"
          :key="bucket.label"
          tabindex="0"
          role="img"
          :aria-label="`${bucket.label}: ${bucket.count} servers`"
          class="bar-group"
          @pointerenter="onEnter(index)"
          @pointerleave="onLeave"
          @focus="onEnter(index)"
          @blur="onLeave"
        >
          <!-- Hit target: full band height, wider than the painted bar -->
          <rect
            :x="MARGIN.left + index * bandWidth"
            :y="MARGIN.top"
            :width="bandWidth"
            :height="PLOT_HEIGHT"
            fill="transparent"
          />
          <path
            :d="barPath(index, bucket.count)"
            class="bar"
            :class="{ hovered: hoveredIndex === index }"
          />
          <text
            v-if="index === tallestIndex"
            :x="barX(index) + barWidth / 2"
            :y="yFor(bucket.count) - 8"
            class="bar-value-label"
          >
            {{ formatNumber(bucket.count) }}
          </text>
          <text
            :x="MARGIN.left + index * bandWidth + bandWidth / 2"
            :y="HEIGHT - MARGIN.bottom + 14"
            class="x-tick"
            :transform="`rotate(-35 ${MARGIN.left + index * bandWidth + bandWidth / 2} ${HEIGHT - MARGIN.bottom + 14})`"
          >
            {{ bucket.label }}
          </text>
        </g>
      </svg>

      <div
        v-if="tooltip"
        class="chart-tooltip"
        :style="{ left: `${(tooltip.x / WIDTH) * 100}%`, top: `${(tooltip.y / HEIGHT) * 100}%` }"
      >
        <strong>{{ formatNumber(tooltip.bucket.count) }}</strong> servers
        <span class="tooltip-label">{{ tooltip.bucket.label }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.chart-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.06);
  padding: 1.5rem;
  margin-top: 2rem;
}

.chart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.chart-header h2 {
  margin: 0;
  font-size: 1.1rem;
}

.btn-link {
  background: none;
  border: none;
  padding: 0;
  font-size: 0.8rem;
  color: var(--fpl-purple);
  cursor: pointer;
  text-decoration: underline;
}

.chart-wrap {
  position: relative;
}

.chart-svg {
  width: 100%;
  height: auto;
  overflow: visible;
}

.gridlines line {
  stroke: #e5e7eb;
  stroke-width: 1;
}

.y-tick {
  font-size: 10px;
  fill: #6b7280;
  text-anchor: end;
  dominant-baseline: middle;
}

.x-tick {
  font-size: 10px;
  fill: #6b7280;
  text-anchor: end;
}

.bar-group {
  cursor: pointer;
  outline: none;
}

.bar {
  fill: var(--fpl-purple);
  transition: fill 0.1s ease;
}

.bar.hovered {
  fill: var(--fpl-pink);
}

.bar-group:focus-visible .bar {
  fill: var(--fpl-pink);
}

.bar-value-label {
  font-size: 11px;
  font-weight: 600;
  fill: var(--fpl-purple);
  text-anchor: middle;
}

.chart-tooltip {
  position: absolute;
  transform: translate(-50%, -100%) translateY(-8px);
  background: var(--fpl-purple);
  color: white;
  padding: 0.4rem 0.6rem;
  border-radius: 0.375rem;
  font-size: 0.8rem;
  white-space: nowrap;
  pointer-events: none;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
}

.tooltip-label {
  display: block;
  font-size: 0.7rem;
  opacity: 0.85;
}
</style>
