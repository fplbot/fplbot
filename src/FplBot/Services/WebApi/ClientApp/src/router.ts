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
          redirect: "/admin/slack",
        },
        {
          path: "slack",
          component: () => import("./views/admin/SlackSection.vue"),
          children: [
            {
              path: "",
              name: "admin-slack-workspaces",
              component: () => import("./views/admin/SlackWorkspacesView.vue"),
            },
            {
              path: "broadcast",
              name: "admin-slack-broadcast",
              component: () => import("./views/admin/SlackBroadcastView.vue"),
            },
          ],
        },
        {
          path: "discord",
          component: () => import("./views/admin/DiscordSection.vue"),
          children: [
            {
              path: "",
              redirect: "/admin/discord/broadcast",
            },
            {
              path: "broadcast",
              name: "admin-discord-broadcast",
              component: () => import("./views/admin/DiscordBroadcastView.vue"),
            },
            {
              path: "slashcommands",
              name: "admin-discord-slashcommands",
              component: () => import("./views/admin/DiscordSlashCommandsView.vue"),
            },
            {
              path: "subscriptions",
              name: "admin-discord-subscriptions",
              component: () => import("./views/admin/DiscordSubscriptionsView.vue"),
            },
          ],
        },
        {
          path: "indexing",
          name: "admin-indexing",
          component: () => import("./views/admin/IndexingView.vue"),
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
