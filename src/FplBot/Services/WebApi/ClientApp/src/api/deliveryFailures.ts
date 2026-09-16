import { formatDateTime } from "../formatting";

export interface ChannelFailureState {
  failureCount: number;
  failingSince: string | null;
  lastFailureReason: string | null;
}

const DISCORD_REASONS: Record<string, string> = {
  "10003": "Unknown channel",
  "50001": "Missing access",
  "50013": "Missing permissions",
};

const SLACK_REASONS: Record<string, string> = {
  channel_not_found: "Channel not found",
  is_archived: "Channel is archived",
  not_in_channel: "Bot is not in the channel",
};

export function describeFailureReason(reason: string | null): string {
  if (!reason) {
    return "";
  }
  const label = /^\d+$/.test(reason) ? DISCORD_REASONS[reason] : SLACK_REASONS[reason];
  return label ? `${label} (${reason})` : reason;
}

export function failureSummary(state: ChannelFailureState): string {
  const since = formatDateTime(state.failingSince) || "unknown";
  const deliveries = state.failureCount === 1 ? "delivery" : "deliveries";
  const summary = `${state.failureCount} failed ${deliveries} since ${since}`;
  const reason = describeFailureReason(state.lastFailureReason);
  return reason ? `${summary} — ${reason}` : summary;
}
