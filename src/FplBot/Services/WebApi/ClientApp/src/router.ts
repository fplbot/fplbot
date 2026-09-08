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
  ],
});

export default router;
