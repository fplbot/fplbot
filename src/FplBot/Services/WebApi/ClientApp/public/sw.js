self.addEventListener("push", (event) => {
  const payload = event.data ? event.data.json() : {};
  const title = payload.title || "FplBot";
  event.waitUntil(
    self.registration.showNotification(title, {
      body: payload.body || "",
      icon: "/android-icon-192x192.png",
      badge: "/android-icon-96x96.png",
      data: { link: payload.link || "/" },
    })
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const link = (event.notification.data && event.notification.data.link) || "/";
  event.waitUntil(clients.openWindow(link));
});
