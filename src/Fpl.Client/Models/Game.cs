using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Game
    {
        [JsonProperty("bps_assists")]
        public long BpsAssists { get; set; }

        [JsonProperty("bps_attempted_passes_limit")]
        public long BpsAttemptedPassesLimit { get; set; }

        [JsonProperty("bps_big_chances_created")]
        public long BpsBigChancesCreated { get; set; }

        [JsonProperty("bps_big_chances_missed")]
        public double BpsBigChancesMissed { get; set; }

        [JsonProperty("bps_cbi_limit")]
        public long BpsCbiLimit { get; set; }

        [JsonProperty("bps_clearances_blocks_interceptions")]
        public long BpsClearancesBlocksInterceptions { get; set; }

        [JsonProperty("bps_dribbles")]
        public long BpsDribbles { get; set; }

        [JsonProperty("bps_errors_leading_to_goal")]
        public double BpsErrorsLeadingToGoal { get; set; }

        [JsonProperty("bps_errors_leading_to_goal_attempt")]
        public double BpsErrorsLeadingToGoalAttempt { get; set; }

        [JsonProperty("bps_fouls")]
        public double BpsFouls { get; set; }

        [JsonProperty("bps_key_passes")]
        public long BpsKeyPasses { get; set; }

        [JsonProperty("bps_long_play")]
        public long BpsLongPlay { get; set; }

        [JsonProperty("bps_long_play_limit")]
        public long BpsLongPlayLimit { get; set; }

        [JsonProperty("bps_offside")]
        public double BpsOffside { get; set; }

        [JsonProperty("bps_open_play_crosses")]
        public long BpsOpenPlayCrosses { get; set; }

        [JsonProperty("bps_own_goals")]
        public double BpsOwnGoals { get; set; }

        [JsonProperty("bps_pass_percentage_70")]
        public long BpsPassPercentage70 { get; set; }

        [JsonProperty("bps_pass_percentage_80")]
        public long BpsPassPercentage80 { get; set; }

        [JsonProperty("bps_pass_percentage_90")]
        public long BpsPassPercentage90 { get; set; }

        [JsonProperty("bps_penalties_conceded")]
        public double BpsPenaltiesConceded { get; set; }

        [JsonProperty("bps_penalties_missed")]
        public double BpsPenaltiesMissed { get; set; }

        [JsonProperty("bps_penalties_saved")]
        public long BpsPenaltiesSaved { get; set; }

        [JsonProperty("bps_recoveries")]
        public long BpsRecoveries { get; set; }

        [JsonProperty("bps_recoveries_limit")]
        public long BpsRecoveriesLimit { get; set; }

        [JsonProperty("bps_red_cards")]
        public double BpsRedCards { get; set; }

        [JsonProperty("bps_saves")]
        public long BpsSaves { get; set; }

        [JsonProperty("bps_short_play")]
        public long BpsShortPlay { get; set; }

        [JsonProperty("bps_tackled")]
        public double BpsTackled { get; set; }

        [JsonProperty("bps_tackles")]
        public long BpsTackles { get; set; }

        [JsonProperty("bps_target_missed")]
        public double BpsTargetMissed { get; set; }

        [JsonProperty("bps_winning_goals")]
        public long BpsWinningGoals { get; set; }

        [JsonProperty("bps_yellow_cards")]
        public double BpsYellowCards { get; set; }

        [JsonProperty("cup_start_event_id")]
        public long CupStartEventId { get; set; }

        [JsonProperty("currency_decimal_places")]
        public long CurrencyDecimalPlaces { get; set; }

        [JsonProperty("currency_multiplier")]
        public long CurrencyMultiplier { get; set; }

        [JsonProperty("currency_symbol")]
        public string CurrencySymbol { get; set; }

        [JsonProperty("default_formation")]
        public long[][] DefaultFormation { get; set; }

        [JsonProperty("facebook_app_id")]
        public string FacebookAppId { get; set; }

        [JsonProperty("fifa_league_id")]
        public long FifaLeagueId { get; set; }

        [JsonProperty("game_timezone")]
        public string GameTimezone { get; set; }

        [JsonProperty("league_h2h_tiebreak")]
        public string LeagueH2hTiebreak { get; set; }

        [JsonProperty("league_join_private_max")]
        public long LeagueJoinPrivateMax { get; set; }

        [JsonProperty("league_join_public_max")]
        public long LeagueJoinPublicMax { get; set; }

        [JsonProperty("league_max_ko_rounds_h2h")]
        public long LeagueMaxKoRoundsH2h { get; set; }

        [JsonProperty("league_points_h2h_draw")]
        public long LeaguePointsH2hDraw { get; set; }

        [JsonProperty("league_points_h2h_lose")]
        public long LeaguePointsH2hLose { get; set; }

        [JsonProperty("league_points_h2h_win")]
        public long LeaguePointsH2hWin { get; set; }

        [JsonProperty("league_prefix_public")]
        public string LeaguePrefixPublic { get; set; }

        [JsonProperty("league_size_classic_max")]
        public long LeagueSizeClassicMax { get; set; }

        [JsonProperty("league_size_h2h_max")]
        public long LeagueSizeH2hMax { get; set; }

        [JsonProperty("photo_base_url")]
        public string PhotoBaseUrl { get; set; }

        [JsonProperty("scoring_assists")]
        public long ScoringAssists { get; set; }

        [JsonProperty("scoring_bonus")]
        public long ScoringBonus { get; set; }

        [JsonProperty("scoring_bps")]
        public long ScoringBps { get; set; }

        [JsonProperty("scoring_concede_limit")]
        public long ScoringConcedeLimit { get; set; }

        [JsonProperty("scoring_creativity")]
        public long ScoringCreativity { get; set; }

        [JsonProperty("scoring_ea_index")]
        public long ScoringEaIndex { get; set; }

        [JsonProperty("scoring_ict_index")]
        public long ScoringIctIndex { get; set; }

        [JsonProperty("scoring_influence")]
        public long ScoringInfluence { get; set; }

        [JsonProperty("scoring_long_play")]
        public long ScoringLongPlay { get; set; }

        [JsonProperty("scoring_long_play_limit")]
        public long ScoringLongPlayLimit { get; set; }

        [JsonProperty("scoring_own_goals")]
        public double ScoringOwnGoals { get; set; }

        [JsonProperty("scoring_penalties_missed")]
        public double ScoringPenaltiesMissed { get; set; }

        [JsonProperty("scoring_penalties_saved")]
        public long ScoringPenaltiesSaved { get; set; }

        [JsonProperty("scoring_red_cards")]
        public double ScoringRedCards { get; set; }

        [JsonProperty("scoring_saves")]
        public long ScoringSaves { get; set; }

        [JsonProperty("scoring_saves_limit")]
        public long ScoringSavesLimit { get; set; }

        [JsonProperty("scoring_short_play")]
        public long ScoringShortPlay { get; set; }

        [JsonProperty("scoring_threat")]
        public long ScoringThreat { get; set; }

        [JsonProperty("scoring_yellow_cards")]
        public double ScoringYellowCards { get; set; }

        [JsonProperty("squad_squadplay")]
        public long SquadSquadplay { get; set; }

        [JsonProperty("squad_squadsize")]
        public long SquadSquadsize { get; set; }

        [JsonProperty("squad_team_limit")]
        public long SquadTeamLimit { get; set; }

        [JsonProperty("squad_total_spend")]
        public long SquadTotalSpend { get; set; }

        [JsonProperty("static_game_url")]
        public string StaticGameUrl { get; set; }

        [JsonProperty("support_email_address")]
        public string SupportEmailAddress { get; set; }

        [JsonProperty("sys_cdn_cache_enabled")]
        public bool SysCdnCacheEnabled { get; set; }

        [JsonProperty("sys_use_event_live_api")]
        public bool SysUseEventLiveApi { get; set; }

        [JsonProperty("sys_vice_captain_enabled")]
        public bool SysViceCaptainEnabled { get; set; }

        [JsonProperty("transfers_cost")]
        public long TransfersCost { get; set; }

        [JsonProperty("transfers_limit")]
        public long TransfersLimit { get; set; }

        [JsonProperty("transfers_sell_on_fee")]
        public double TransfersSellOnFee { get; set; }

        [JsonProperty("transfers_type")]
        public string TransfersType { get; set; }

        [JsonProperty("ui_el_hide_currency_qi")]
        public bool UiElHideCurrencyQi { get; set; }

        [JsonProperty("ui_el_hide_currency_sy")]
        public bool UiElHideCurrencySy { get; set; }

        [JsonProperty("ui_element_wrap")]
        public long UiElementWrap { get; set; }

        [JsonProperty("ui_selection_player_limit")]
        public long UiSelectionPlayerLimit { get; set; }

        [JsonProperty("ui_selection_price_gap")]
        public long UiSelectionPriceGap { get; set; }

        [JsonProperty("ui_selection_short_team_names")]
        public bool UiSelectionShortTeamNames { get; set; }

        [JsonProperty("ui_show_home_away")]
        public bool UiShowHomeAway { get; set; }
    }
}
