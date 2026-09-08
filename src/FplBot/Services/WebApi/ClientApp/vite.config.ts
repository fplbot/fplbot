import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  build: {
    // Build straight into wwwroot, which the backend already serves as static
    // files (see WebAppExtensions.UseWebApp). emptyOutDir stays false so this
    // doesn't wipe the pre-existing favicons/css/js/lib used by Razor pages.
    outDir: "../wwwroot",
    emptyOutDir: false,
  },
  server: {
    proxy: {
      // Catch-all: forward everything to the backend EXCEPT (a) Vite's own dev
      // assets/HMR client and (b) full-page navigations, which Vite must keep
      // serving itself (its dev index.html / history fallback for /, /search,
      // /leagues/:id). This means new backend routes/controllers just work
      // without ever touching this file again.
      //
      // FplBotApplication.RunAsWebApplication defaults to PORT=1337 when unset, and
      // serves https in Development using the trusted ASP.NET Core dev cert (see
      // Hosting/FplBotApplication.cs) since Slack requires OAuth redirect_uris to be https.
      "^/.*": {
        target: "https://localhost:1337",
        // The dev cert is trusted by the OS/browser but Node's http client doesn't know
        // about it, so skip verification for this local-only proxy hop.
        secure: false,
        // Without this, the backend sees Host: localhost:5173 (Vite's own
        // origin) instead of localhost:1337, which breaks anything that
        // reconstructs an absolute URL from the incoming request (e.g.
        // OAuthController.InstallUrl's redirect_uri).
        changeOrigin: true,
        bypass(req) {
          const url = req.url ?? "";
          const isViteInternal =
            url.startsWith("/@") ||
            url.startsWith("/src/") ||
            url.startsWith("/node_modules/");
          const isPageNavigation = req.headers["sec-fetch-mode"] === "navigate";
          if (isViteInternal || isPageNavigation) {
            return req.url;
          }
        },
      },
    },
  },
});
