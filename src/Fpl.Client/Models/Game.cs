

using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Game
{
    [JsonPropertyName("bps_assists")]
    public long BpsAssists { get; set; }

    [JsonPropertyName("bps_attempted_passes_limit")]
    public long BpsAttemptedPassesLimit { get; set; }

    [JsonPropertyName("bps_big_chances_created")]
    public long BpsBigChancesCreated { get; set; }

    [JsonPropertyName("bps_big_chances_missed")]
    public double BpsBigChancesMissed { get; set; }

    [JsonPropertyName("bps_cbi_limit")]
    public long BpsCbiLimit { get; set; }

    [JsonPropertyName("bps_clearances_blocks_interceptions")]
    public long BpsClearancesBlocksInterceptions { get; set; }

    [JsonPropertyName("bps_dribbles")]
    public long BpsDribbles { get; set; }

    [JsonPropertyName("bps_errors_leading_to_goal")]
    public double BpsErrorsLeadingToGoal { get; set; }

    [JsonPropertyName("bps_errors_leading_to_goal_attempt")]
    public double BpsErrorsLeadingToGoalAttempt { get; set; }

    [JsonPropertyName("bps_fouls")]
    public double BpsFouls { get; set; }

    [JsonPropertyName("bps_key_passes")]
    public long BpsKeyPasses { get; set; }

    [JsonPropertyName("bps_long_play")]
    public long BpsLongPlay { get; set; }

    [JsonPropertyName("bps_long_play_limit")]
    public long BpsLongPlayLimit { get; set; }

    [JsonPropertyName("bps_offside")]
    public double BpsOffside { get; set; }

    [JsonPropertyName("bps_open_play_crosses")]
    public long BpsOpenPlayCrosses { get; set; }

    [JsonPropertyName("bps_own_goals")]
    public double BpsOwnGoals { get; set; }

    [JsonPropertyName("bps_pass_percentage_70")]
    public long BpsPassPercentage70 { get; set; }

    [JsonPropertyName("bps_pass_percentage_80")]
    public long BpsPassPercentage80 { get; set; }

    [JsonPropertyName("bps_pass_percentage_90")]
    public long BpsPassPercentage90 { get; set; }

    [JsonPropertyName("bps_penalties_conceded")]
    public double BpsPenaltiesConceded { get; set; }

    [JsonPropertyName("bps_penalties_missed")]
    public double BpsPenaltiesMissed { get; set; }

    [JsonPropertyName("bps_penalties_saved")]
    public long BpsPenaltiesSaved { get; set; }

    [JsonPropertyName("bps_recoveries")]
    public long BpsRecoveries { get; set; }

    [JsonPropertyName("bps_recoveries_limit")]
    public long BpsRecoveriesLimit { get; set; }

    [JsonPropertyName("bps_red_cards")]
    public double BpsRedCards { get; set; }

    [JsonPropertyName("bps_saves")]
    public long BpsSaves { get; set; }

    [JsonPropertyName("bps_short_play")]
    public long BpsShortPlay { get; set; }

    [JsonPropertyName("bps_tackled")]
    public double BpsTackled { get; set; }

    [JsonPropertyName("bps_tackles")]
    public long BpsTackles { get; set; }

    [JsonPropertyName("bps_target_missed")]
    public double BpsTargetMissed { get; set; }

    [JsonPropertyName("bps_winning_goals")]
    public long BpsWinningGoals { get; set; }

    [JsonPropertyName("bps_yellow_cards")]
    public double BpsYellowCards { get; set; }

    [JsonPropertyName("cup_start_event_id")]
    public long CupStartEventId { get; set; }

    [JsonPropertyName("currency_decimal_places")]
    public long CurrencyDecimalPlaces { get; set; }

    [JsonPropertyName("currency_multiplier")]
    public long CurrencyMultiplier { get; set; }

    [JsonPropertyName("currency_symbol")]
    public string CurrencySymbol { get; set; }

    [JsonPropertyName("default_formation")]
    public long[][] DefaultFormation { get; set; }

    [JsonPropertyName("facebook_app_id")]
    public string FacebookAppId { get; set; }

    [JsonPropertyName("fifa_league_id")]
    public long FifaLeagueId { get; set; }

    [JsonPropertyName("game_timezone")]
    public string GameTimezone { get; set; }

    [JsonPropertyName("league_h2h_tiebreak")]
    public string LeagueH2hTiebreak { get; set; }

    [JsonPropertyName("league_join_private_max")]
    public long LeagueJoinPrivateMax { get; set; }

    [JsonPropertyName("league_join_public_max")]
    public long LeagueJoinPublicMax { get; set; }

    [JsonPropertyName("league_max_ko_rounds_h2h")]
    public long LeagueMaxKoRoundsH2h { get; set; }

    [JsonPropertyName("league_points_h2h_draw")]
    public long LeaguePointsH2hDraw { get; set; }

    [JsonPropertyName("league_points_h2h_lose")]
    public long LeaguePointsH2hLose { get; set; }

    [JsonPropertyName("league_points_h2h_win")]
    public long LeaguePointsH2hWin { get; set; }

    [JsonPropertyName("league_prefix_public")]
    public string LeaguePrefixPublic { get; set; }

    [JsonPropertyName("league_size_classic_max")]
    public long LeagueSizeClassicMax { get; set; }

    [JsonPropertyName("league_size_h2h_max")]
    public long LeagueSizeH2hMax { get; set; }

    [JsonPropertyName("photo_base_url")]
    public string PhotoBaseUrl { get; set; }

    [JsonPropertyName("scoring_assists")]
    public long ScoringAssists { get; set; }

    [JsonPropertyName("scoring_bonus")]
    public long ScoringBonus { get; set; }

    [JsonPropertyName("scoring_bps")]
    public long ScoringBps { get; set; }

    [JsonPropertyName("scoring_concede_limit")]
    public long ScoringConcedeLimit { get; set; }

    [JsonPropertyName("scoring_creativity")]
    public long ScoringCreativity { get; set; }

    [JsonPropertyName("scoring_ea_index")]
    public long ScoringEaIndex { get; set; }

    [JsonPropertyName("scoring_ict_index")]
    public long ScoringIctIndex { get; set; }

    [JsonPropertyName("scoring_influence")]
    public long ScoringInfluence { get; set; }

    [JsonPropertyName("scoring_long_play")]
    public long ScoringLongPlay { get; set; }

    [JsonPropertyName("scoring_long_play_limit")]
    public long ScoringLongPlayLimit { get; set; }

    [JsonPropertyName("scoring_own_goals")]
    public double ScoringOwnGoals { get; set; }

    [JsonPropertyName("scoring_penalties_missed")]
    public double ScoringPenaltiesMissed { get; set; }

    [JsonPropertyName("scoring_penalties_saved")]
    public long ScoringPenaltiesSaved { get; set; }

    [JsonPropertyName("scoring_red_cards")]
    public double ScoringRedCards { get; set; }

    [JsonPropertyName("scoring_saves")]
    public long ScoringSaves { get; set; }

    [JsonPropertyName("scoring_saves_limit")]
    public long ScoringSavesLimit { get; set; }

    [JsonPropertyName("scoring_short_play")]
    public long ScoringShortPlay { get; set; }

    [JsonPropertyName("scoring_threat")]
    public long ScoringThreat { get; set; }

    [JsonPropertyName("scoring_yellow_cards")]
    public double ScoringYellowCards { get; set; }

    [JsonPropertyName("squad_squadplay")]
    public long SquadSquadplay { get; set; }

    [JsonPropertyName("squad_squadsize")]
    public long SquadSquadsize { get; set; }

    [JsonPropertyName("squad_team_limit")]
    public long SquadTeamLimit { get; set; }

    [JsonPropertyName("squad_total_spend")]
    public long SquadTotalSpend { get; set; }

    [JsonPropertyName("static_game_url")]
    public string StaticGameUrl { get; set; }

    [JsonPropertyName("support_email_address")]
    public string SupportEmailAddress { get; set; }

    [JsonPropertyName("sys_cdn_cache_enabled")]
    public bool SysCdnCacheEnabled { get; set; }

    [JsonPropertyName("sys_use_event_live_api")]
    public bool SysUseEventLiveApi { get; set; }

    [JsonPropertyName("sys_vice_captain_enabled")]
    public bool SysViceCaptainEnabled { get; set; }

    [JsonPropertyName("transfers_cost")]
    public long TransfersCost { get; set; }

    [JsonPropertyName("transfers_limit")]
    public long TransfersLimit { get; set; }

    [JsonPropertyName("transfers_sell_on_fee")]
    public double TransfersSellOnFee { get; set; }

    [JsonPropertyName("transfers_type")]
    public string TransfersType { get; set; }

    [JsonPropertyName("ui_el_hide_currency_qi")]
    public bool UiElHideCurrencyQi { get; set; }

    [JsonPropertyName("ui_el_hide_currency_sy")]
    public bool UiElHideCurrencySy { get; set; }

    [JsonPropertyName("ui_element_wrap")]
    public long UiElementWrap { get; set; }

    [JsonPropertyName("ui_selection_player_limit")]
    public long UiSelectionPlayerLimit { get; set; }

    [JsonPropertyName("ui_selection_price_gap")]
    public long UiSelectionPriceGap { get; set; }

    [JsonPropertyName("ui_selection_short_team_names")]
    public bool UiSelectionShortTeamNames { get; set; }

    [JsonPropertyName("ui_show_home_away")]
    public bool UiShowHomeAway { get; set; }
}