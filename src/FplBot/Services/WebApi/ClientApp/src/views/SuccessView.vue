<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";

const route = useRoute();
const type = computed(() => {
  const t = route.query.type;
  if (t === "slack" || t === "discord") return t;
  return "slack";
});
</script>

<template>
  <div class="page">
    <NavBar />

    <section class="hero">
      <div class="container">
        <h1>Success! &#127881;</h1>

        <template v-if="type === 'slack'">
          <p class="lead">fplbot is now installed in your Slack workspace.</p>
          <p class="tip"><b>Tip:</b> invite <code>@fplbot</code> into a channel and configure it to start getting updates.</p>
        </template>
        <template v-else>
          <p class="lead">@fplbot is now installed in your Discord server.</p>
          <p class="tip"><b>Tip:</b> set up @fplbot in a channel using its slash commands.</p>
        </template>

        <p class="help">Type <code>@fplbot help</code> at any time to see available commands.</p>

        <router-link to="/" class="btn">Back to fplbot.app</router-link>
      </div>
    </section>

    <AppFooter />
  </div>
</template>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.hero {
  flex: 1;
  text-align: center;
  padding: 4rem 0;
}

h1 {
  font-size: 2.5rem;
  font-weight: bold;
  margin-bottom: 1rem;
}

.lead {
  font-size: 1.125rem;
}

.tip, .help {
  font-size: 1.05rem;
  margin-top: 1.5rem;
  max-width: 32rem;
  margin-left: auto;
  margin-right: auto;
}

code {
  background: rgba(55, 0, 60, 0.08);
  padding: 0.1rem 0.4rem;
  border-radius: 0.25rem;
}

.btn {
  display: inline-block;
  margin-top: 2.5rem;
  text-decoration: none;
}
</style>
