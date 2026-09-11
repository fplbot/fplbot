<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";
import { redirectToSlackInstall, redirectToDiscordInstall } from "../api/api";

const router = useRouter();
const searchValue = ref("");

function submitSearch() {
  router.push({ path: "/search", query: { q: searchValue.value } });
}

onMounted(() => {
  // Deep-link support for "#add-to-slack" anchors used elsewhere.
  if (window.location.hash === "#add-to-slack") {
    document.getElementById("add-to-slack")?.scrollIntoView({ behavior: "smooth" });
  }
});
</script>

<template>
  <div class="page">
    <NavBar />

    <section class="hero">
      <div class="container hero-inner">
        <h1>Try our search &#128373;&#65039;</h1>
        <p class="lead">Search Fantasy&nbsp;Premier&nbsp;League for managers or leagues.</p>
        <form class="search-form" @submit.prevent="submitSearch">
          <input
            v-model="searchValue"
            name="q"
            placeholder="Magnus Carlsen"
            aria-label="Search for fpl player"
            class="search-input"
          />
          <button type="submit" class="btn long">Search</button>
        </form>

        <h2 class="tagline">&#128070; This search is part of @fplbot</h2>
        <p class="subtitle">An unofficial chatbot for Fantasy&nbsp;Premier&nbsp;League</p>

        <div id="add-to-slack" class="install-section">
          <p class="install-label">Install fplbot</p>
          <div class="install-buttons">
            <button class="btn install-btn" @click="redirectToSlackInstall">
              Add to Slack
            </button>
            <button class="btn install-btn discord" @click="redirectToDiscordInstall">
              Add to Discord
            </button>
          </div>
        </div>
      </div>
    </section>

    <section id="features" class="features">
      <div class="container">
        <h2>Features</h2>
        <ul>
          <li><b>Live updates</b> on events during gameweeks</li>
          <li>Notifies about transfer deadlines</li>
          <li>Posts a summary, including <b>transfers, chips used and captain picks</b>, for your league when a gameweek starts</li>
          <li>Posts <b>league standings</b> at the end of each gameweek</li>
          <li>Price changes, injuries and more</li>
        </ul>
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
  text-align: center;
  padding: 4rem 0;
}

.hero-inner {
  max-width: 48rem;
}

h1 {
  font-size: 2.5rem;
  font-weight: bold;
  margin-bottom: 1rem;
}

.lead {
  font-size: 1.125rem;
  margin-bottom: 2rem;
}

.search-form {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.search-form .btn {
  max-width: 12rem;
}

.tagline {
  margin-top: 3rem;
  font-size: 1.75rem;
  font-weight: bold;
}

.subtitle {
  font-size: 1.125rem;
  margin-top: 1rem;
}

.install-section {
  margin-top: 3rem;
  padding-bottom: 2rem;
}

.install-label {
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: bold;
  margin-bottom: 1rem;
}

.install-buttons {
  display: flex;
  justify-content: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.install-btn {
  padding: 0.75rem 1.5rem;
}

.install-btn.discord {
  background: white;
  color: var(--fpl-purple);
  border: 1px solid #d1d5db;
}

.features {
  background: var(--fpl-purple);
  color: white;
  padding: 3rem 0;
}

.features h2 {
  font-size: 2rem;
  font-weight: bold;
  margin-bottom: 1rem;
}

.features ul {
  font-size: 1.125rem;
  line-height: 1.9;
  max-width: 40rem;
}
</style>
