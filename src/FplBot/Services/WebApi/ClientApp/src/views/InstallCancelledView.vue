<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";
import { isSiteRelativePath } from "../oauthState";

const route = useRoute();

const backTo = computed(() => (isSiteRelativePath(route.query.state) ? route.query.state : null));

// Our own middleware reports `reason`; the Discord install middleware reports `details`.
const reason = computed(() => route.query.reason ?? route.query.details);

const wasDeclined = computed(() => reason.value === "access_denied");
</script>

<template>
  <div class="page">
    <NavBar />

    <div class="content">
      <h1>{{ wasDeclined ? "Install cancelled" : "Install didn't complete" }}</h1>

      <p v-if="wasDeclined">fplbot wasn't installed, because the permissions it asked for weren't granted.</p>
      <p v-else>fplbot wasn't installed. Nothing has changed.</p>

      <p class="hint">
        You can start the install again whenever you like.<template v-if="!wasDeclined && reason">
          If it keeps failing, this is what we were told: <code>{{ reason }}</code>.</template>
      </p>

      <router-link v-if="backTo" :to="backTo" class="btn">Back to where you were</router-link>
      <router-link v-else to="/" class="btn">Back to fplbot.app</router-link>
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
  max-width: 32rem;
  margin: 0 auto;
  width: 100%;
  padding: 3rem 1rem;
  text-align: center;
}

h1 {
  font-size: 1.5rem;
  margin-bottom: 1rem;
}

.hint {
  color: #6b7280;
  margin-bottom: 1.5rem;
}
</style>
