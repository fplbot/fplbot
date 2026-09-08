// Matches the backend SearchType enum (Fpl.Search.Models.SearchType): All, Entries, Leagues.
export type SearchType = "All" | "Entries" | "Leagues";

export interface EntryItem {
  id: number;
  realName?: string;
  teamName?: string;
  alias?: string;
  description?: string;
  country?: string;
  numberOfPastSeasons: number;
  thumbprint?: string;
}

export interface LeagueItem {
  id: number;
  name?: string;
  adminEntry?: number;
  adminName?: string;
  adminTeamName?: string;
  adminCountry?: string;
}

// The /search/any endpoint wraps each hit in a { type, source } container
// (see SearchService.SearchAny / SearchContainer in the backend) rather than
// exposing EntryItem/LeagueItem directly in the array.
export interface SearchHit {
  type: "entry" | "league" | null;
  source: EntryItem | LeagueItem;
}

export interface SearchAnyResult {
  exposedHits: SearchHit[];
  maxHits: number;
  totalHits: number;
  page: number;
  totalPages: number;
}

export interface SearchAnyResponse {
  hits: SearchAnyResult;
}

export async function searchAny(
  query: string,
  page: number,
  type: SearchType = "All"
): Promise<SearchAnyResult> {
  const params = new URLSearchParams({ query, page: String(page), type });
  const res = await fetch(`/search/any?${params.toString()}`);
  if (!res.ok) {
    throw new Error(`Search request failed with status ${res.status}`);
  }
  const data: SearchAnyResponse = await res.json();
  return data.hits;
}
