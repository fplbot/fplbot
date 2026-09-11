export interface StandingEntry {
  entry: number;
  playerName?: string;
  teamName?: string;
  rank: number;
  lastRank: number;
  total: number;
  eventTotal: number;
}

export interface Transfer {
  playerIn?: string;
  playerOut?: string;
  playerInCost: number;
  playerOutCost: number;
  time: string;
}

export interface EntrySummary {
  entry: number;
  playerName?: string;
  captain?: string;
  viceCaptain?: string;
  chip?: string;
  transfers: Transfer[];
}

export interface LeagueDetails {
  leagueName?: string;
  leagueAdmin?: string;
  gameweek?: number;
  standings: StandingEntry[];
  summaries: EntrySummary[];
}

export async function getLeagueDetails(leagueId: number): Promise<LeagueDetails | null> {
  const res = await fetch(`/api/fpl/leagues/${leagueId}/details`);
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`League details request failed with status ${res.status}`);
  }
  return res.json();
}
