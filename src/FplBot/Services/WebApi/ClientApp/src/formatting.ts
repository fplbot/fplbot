const LOCALE = "nb-NO";

export function formatDateTime(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleString(LOCALE) : "";
}

export function formatChannelName(name: string): string {
  return name.startsWith("#") ? name : `#${name}`;
}

export function formatNumber(value: number): string {
  return value.toLocaleString(LOCALE);
}
