<script setup lang="ts">
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";
</script>

<template>
  <div class="page">
    <NavBar />

    <div class="content">
      <p class="card-emoji">&#128308;</p>
      <h1>Red Card</h1>
      <p class="lead">
        VAR have reviewed the incident. This page was offside long before you got here —
        the assistant referee's flag has been up since kickoff.
      </p>
      <p class="score">Final score: You 0 &ndash; 1 The Internet</p>
      <router-link to="/" class="btn">Back to safety</router-link>
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
  padding: 4rem 1rem;
  text-align: center;
}

.card-emoji {
  font-size: 3rem;
  margin: 0 0 0.5rem;
}

h1 {
  font-size: 2rem;
  font-weight: bold;
  margin-bottom: 1rem;
}

.lead {
  font-size: 1.05rem;
  margin-bottom: 1.5rem;
}

.score {
  font-weight: bold;
  color: #6b7280;
  margin-bottom: 2rem;
}
</style>
