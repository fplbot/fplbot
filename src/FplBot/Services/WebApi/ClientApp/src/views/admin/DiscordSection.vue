<template>
  <div>
    <nav class="sub-nav">
      <router-link to="/admin/discord/subscriptions">Subscriptions</router-link>
      <router-link to="/admin/discord/broadcast">Broadcast</router-link>
      <router-link to="/admin/discord/slashcommands">Slash Commands</router-link>
    </nav>
    <router-view />
  </div>
</template>
