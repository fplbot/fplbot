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

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 365 * 24 * 60 * 60],
  ["month", 30 * 24 * 60 * 60],
  ["day", 24 * 60 * 60],
  ["hour", 60 * 60],
  ["minute", 60],
];

const relativeTimeFormatter = new Intl.RelativeTimeFormat(LOCALE, { numeric: "auto" });

// "Never" for null/epoch (the sentinel a metric that predates timestamp-tracking reads back as —
// see GuildMemberCountRepository's UnixEpoch fallback), otherwise a coarse "X hours ago".
export function formatRelativeTime(value: string | null | undefined): string {
  if (!value) return "Never";
  const date = new Date(value);
  if (date.getUTCFullYear() <= 1970) return "Never";

  const diffSeconds = (date.getTime() - Date.now()) / 1000;
  const absSeconds = Math.abs(diffSeconds);
  if (absSeconds < 60) return "Just now";

  for (const [unit, secondsInUnit] of RELATIVE_UNITS) {
    if (absSeconds >= secondsInUnit) {
      return relativeTimeFormatter.format(Math.round(diffSeconds / secondsInUnit), unit);
    }
  }

  return relativeTimeFormatter.format(Math.round(diffSeconds / 60), "minute");
}
