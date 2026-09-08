<script setup lang="ts">
</script>

<template>
  <nav class="navbar">
    <div class="container navbar-inner">
      <div class="navbar-top">
        <router-link to="/" class="brand">fplbot.app</router-link>
      </div>

    </div>
  </nav>
</template>

<style scoped>
.navbar {
  padding: 0.75rem 0;
}

.navbar-inner {
  display: flex;
  flex-direction: column;
}

.navbar-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.brand {
  font-size: 1.5rem;
  font-weight: bold;
  text-decoration: none;
  color: var(--fpl-purple);
}


.nav-links a {
  text-decoration: none;
  font-weight: bold;
  color: var(--fpl-purple);
}



</style>
