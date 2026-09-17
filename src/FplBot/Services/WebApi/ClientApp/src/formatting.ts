const LOCALE = "nb-NO";

export function formatDateTime(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleString(LOCALE) : "";
}
