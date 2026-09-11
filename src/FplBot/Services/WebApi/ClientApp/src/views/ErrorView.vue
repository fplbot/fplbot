<script setup lang="ts">
import { useRoute } from "vue-router";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";

const route = useRoute();
const message = typeof route.query.msg === "string" ? route.query.msg : null;
</script>

<template>
  <div class="page">
    <NavBar />

    <div class="content">
      <h1>&#128680; Error!</h1>
      <p v-if="message">{{ message }}</p>

      <p>
        Try another <a href="/api/admin/login">login</a>
      </p>
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
</style>
