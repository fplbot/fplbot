interface InstallUrlResponse {
  redirectUri: string;
}

export async function redirectToSlackInstall(): Promise<void> {
  const res = await fetch("/api/oauth/install-url");
  const data: InstallUrlResponse = await res.json();
  window.location.href = data.redirectUri;
}

export async function redirectToDiscordInstall(): Promise<void> {
  const res = await fetch("/api/oauth/install-url-discord");
  const data: InstallUrlResponse = await res.json();
  window.location.href = data.redirectUri;
}
