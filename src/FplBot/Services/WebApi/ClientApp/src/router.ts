import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      name: "home",
      component: () => import("./views/HomeView.vue"),
    },
    {
      path: "/search",
      name: "search",
      component: () => import("./views/SearchView.vue"),
    },
    {
      path: "/leagues/:id",
      name: "league-details",
      component: () => import("./views/LeagueDetailsView.vue"),
      props: true,
    },
    {
      path: "/success",
      name: "success",
      component: () => import("./views/SuccessView.vue"),
    },
    {
      path: "/error",
      name: "error",
      component: () => import("./views/ErrorView.vue"),
    },
    {
      path: "/admin",
      component: () => import("./layouts/AdminLayout.vue"),
      children: [
        {
          path: "",
          name: "admin-home",
          component: () => import("./views/admin/AdminHomeView.vue"),
        },
        {
          path: "slack/broadcast",
          name: "admin-slack-broadcast",
          component: () => import("./views/admin/SlackBroadcastView.vue"),
        },
        {
          path: "discord/broadcast",
          name: "admin-discord-broadcast",
          component: () => import("./views/admin/DiscordBroadcastView.vue"),
        },
        {
          path: "indexing",
          name: "admin-indexing",
          component: () => import("./views/admin/IndexingView.vue"),
        },
        {
          path: "discord/slashcommands",
          name: "admin-discord-slashcommands",
          component: () => import("./views/admin/DiscordSlashCommandsView.vue"),
        },
        {
          path: "discord/subscriptions",
          name: "admin-discord-subscriptions",
          component: () => import("./views/admin/DiscordSubscriptionsView.vue"),
        },
        {
          path: "teams/:teamId",
          name: "admin-team-details",
          component: () => import("./views/admin/TeamDetailsView.vue"),
          props: true,
        },
      ],
    },
  ],
});

export default router;
