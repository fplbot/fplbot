namespace FplBot.WebApi.Infrastructure;

public static class ViteDevServerPage
{
    public const string Url = "http://localhost:5173";

    public static Task Write(HttpContext context)
    {
        var target = $"{Url}{context.Request.Path}{context.Request.QueryString}";
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "text/html; charset=utf-8";
        return context.Response.WriteAsync(Html(target));
    }

    private static string Html(string target) =>
        $$"""
          <!doctype html>
          <html lang="en">
          <head>
            <meta charset="utf-8">
            <title>Vite dev server</title>
            <style>
              body {
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                color: #37003c;
                background: linear-gradient(to top right, #ffffff, #e5e7eb);
                min-height: 100vh;
                margin: 0;
                display: flex;
                align-items: center;
                justify-content: center;
              }
              main { max-width: 34rem; padding: 2rem; }
              h1 { font-size: 1.4rem; }
              pre {
                background: #ffffff;
                border: 1px solid #d1d5db;
                border-radius: 0.5rem;
                padding: 1rem;
                overflow-x: auto;
              }
              p { line-height: 1.5; }
              .muted { color: #6b7280; font-size: 0.9rem; }
            </style>
          </head>
          <body>
            <main>
              <h1 id="checking">Looking for the Vite dev server…</h1>
              <div id="down" hidden>
                <h1>The Vue app isn't running</h1>
                <p>
                  In local dev this port serves the API only — the SPA comes from the Vite dev
                  server, so you always see the code you just edited instead of whatever
                  <code>client-build</code> last wrote to wwwroot.
                </p>
                <pre>cd src/FplBot/Services/WebApi/ClientApp
          npm install
          npm run dev</pre>
                <p>In Rider: the <b>WebApi + Vite</b> or <b>All Services</b> run configuration starts it for you.</p>
                <p class="muted">This page keeps probing, and jumps to <a href="{{target}}">{{target}}</a> the moment it answers.</p>
                <p class="muted" id="status"></p>
              </div>
            </main>
            <script>
              let attempt = 0;

              function probe() {
                fetch("{{Url}}/@vite/client", { mode: "no-cors", cache: "no-store" })
                  .then(() => location.replace("{{target}}"))
                  .catch(() => {
                    document.getElementById("checking").hidden = true;
                    document.getElementById("down").hidden = false;
                    document.getElementById("status").textContent =
                      `No answer on port 5173. Last checked ${new Date().toLocaleTimeString()} (attempt ${++attempt}), retrying every second.`;
                    setTimeout(probe, 1000);
                  });
              }

              probe();
            </script>
          </body>
          </html>
          """;
}
