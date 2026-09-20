import { createRouter, createWebHistory } from "vue-router";
import { slackInstallationAdapter, discordInstallationAdapter } from "./composables/installationAdapters";

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
      path: "/notifications",
      name: "notifications",
      component: () => import("./views/NotificationsView.vue"),
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
      path: "/install-cancelled",
      name: "install-cancelled",
      component: () => import("./views/InstallCancelledView.vue"),
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
              redirect: "/admin/discord/servers",
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
              path: "servers",
              name: "admin-discord-servers",
              component: () => import("./views/admin/DiscordSubscriptionsView.vue"),
            },
          ],
        },
        {
          path: "search",
          component: () => import("./views/admin/SearchSection.vue"),
          children: [
            {
              path: "",
              redirect: "/admin/search/indexing",
            },
            {
              path: "indexing",
              name: "admin-search-indexing",
              component: () => import("./views/admin/IndexingView.vue"),
            },
            {
              path: "analytics",
              name: "admin-search-analytics",
              component: () => import("./views/admin/SearchAnalyticsView.vue"),
            },
          ],
        },
        {
          path: "errors",
          component: () => import("./views/admin/ErrorsSection.vue"),
          children: [
            {
              path: "",
              name: "admin-errors-queues",
              component: () => import("./views/admin/ErrorQueuesView.vue"),
            },
            {
              path: ":queue",
              name: "admin-errors-queue-detail",
              component: () => import("./views/admin/ErrorQueueDetailView.vue"),
              props: true,
            },
          ],
        },
        {
          path: "teams/:entityId",
          name: "admin-team-details",
          component: () => import("./views/admin/InstallationDetailsView.vue"),
          props: (route) => ({ entityId: route.params.entityId, adapter: slackInstallationAdapter }),
        },
        {
          path: "subscriptions/:subscriptionId",
          name: "admin-subscription-manage",
          component: () => import("./views/admin/ChannelManageView.vue"),
          props: (route) => ({ subscriptionId: route.params.subscriptionId }),
        },
        {
          path: "guilds/:entityId",
          name: "admin-guild-details",
          component: () => import("./views/admin/InstallationDetailsView.vue"),
          props: (route) => ({ entityId: route.params.entityId, adapter: discordInstallationAdapter }),
        },
      ],
    },
    {
      path: "/:pathMatch(.*)*",
      name: "not-found",
      component: () => import("./views/NotFoundView.vue"),
    },
  ],
});

export default router;
