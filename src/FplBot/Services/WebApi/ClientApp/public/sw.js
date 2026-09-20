self.addEventListener("push", (event) => {
  const payload = event.data ? event.data.json() : {};
  const title = payload.title || "FplBot";
  event.waitUntil(
    self.registration.showNotification(title, {
      body: payload.body || "",
      icon: "/android-icon-192x192.png",
      badge: "/android-icon-96x96.png",
      data: { link: payload.link || null },
    })
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const link = event.notification.data && event.notification.data.link;
  if (!link) {
    return;
  }
  const target = new URL(link, self.location.origin).href;

  event.waitUntil(
    (async () => {
      const windowClients = await clients.matchAll({ type: "window", includeUncontrolled: true });
      for (const client of windowClients) {
        if ("navigate" in client) {
          try {
            await client.navigate(target);
            await client.focus();
            return;
          } catch {
            continue;
          }
        }
      }
      await clients.openWindow(target);
    })()
  );
});
