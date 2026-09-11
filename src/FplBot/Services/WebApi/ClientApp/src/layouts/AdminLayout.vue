<script setup lang="ts">
import { onMounted, computed } from "vue";
import { useRoute } from "vue-router";
import { useAdminAuth } from "../composables/useAdminAuth";
import { loginUrl, logout } from "../api/admin";

const route = useRoute();
const { state, me, refresh } = useAdminAuth();

const signedOut = computed(() => route.query.signedout === "1");

onMounted(refresh);

function signInHref() {
  return loginUrl(typeof route.query.returnUrl === "string" ? route.query.returnUrl : "/admin");
}

async function handleLogout() {
  await logout();
  window.location.href = "/admin?signedout=1";
}

const navLinks = [
  { to: "/admin/slack", label: "Slack" },
  { to: "/admin/discord", label: "Discord" },
  { to: "/admin/indexing", label: "Search" },
];
</script>

<template>
  <div class="admin-shell">
    <div v-if="state === 'loading'" class="gate">
      <div class="spinner"></div>
    </div>

    <div v-else-if="state === 'anonymous'" class="gate">
      <div class="card gate-card">
        <h1>fplbot admin</h1>
        <p v-if="signedOut" class="alert alert-success">You've been signed out.</p>
        <p class="lead">Sign in with the fplbot admin Slack workspace to continue.</p>
        <a class="btn slack-btn long" :href="signInHref()">
          <span class="slack-mark">#</span> Sign in with Slack
        </a>
      </div>
    </div>

    <div v-else-if="state === 'forbidden'" class="gate">
      <div class="card gate-card">
        <h1>&#9888;&#65039; Forbidden</h1>
        <p>
          <b>{{ me?.name }}</b> from <b>{{ me?.teamName }}</b> does not have access to this admin panel.
        </p>
        <a class="btn long" :href="signInHref()">Try another workspace</a>
      </div>
    </div>

    <template v-else>
      <header class="admin-nav">
        <div class="container admin-nav-inner">
          <router-link to="/admin" class="brand">fplbot admin</router-link>
          <nav class="links">
            <router-link v-for="link in navLinks" :key="link.to" :to="link.to">{{ link.label }}</router-link>
          </nav>
          <div class="identity">
            <span>{{ me?.teamName }}</span>
            <button class="btn small" @click="handleLogout">Sign out</button>
          </div>
        </div>
      </header>
      <main class="container admin-content">
        <router-view />
      </main>
    </template>
  </div>
</template>

<style scoped>
.admin-shell {
  min-height: 100vh;
}

.gate {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem 1rem;
}

.gate-card {
  max-width: 24rem;
  width: 100%;
  text-align: center;
}

.gate-card h1 {
  font-size: 1.5rem;
  margin-bottom: 1rem;
}

.gate-card .lead {
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.slack-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  background: #4a154b;
  color: white;
}

.slack-btn:hover {
  background: #611f69;
  color: white;
}

.slack-mark {
  font-weight: 900;
}

.admin-nav {
  background: var(--fpl-purple);
  color: white;
}

.admin-nav-inner {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.75rem 1.5rem;
  flex-wrap: wrap;
}

.brand {
  font-weight: bold;
  font-size: 1.125rem;
  text-decoration: none;
  color: white;
}

.links {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  flex-grow: 1;
}

.links a {
  color: #e5e7eb;
  text-decoration: none;
  font-size: 0.9rem;
}

.links a.router-link-active {
  color: var(--fpl-green);
  font-weight: bold;
}

.identity {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.875rem;
  color: #e5e7eb;
}

.admin-content {
  padding: 2rem 1.5rem;
}
</style>
