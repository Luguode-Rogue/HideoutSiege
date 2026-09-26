namespace HideoutSiege.Config;

internal static class HideoutSiegeConfig
{
    internal const string ChallengeOptionId = "hideout_siege_challenge";
    internal const string RallyMenuId = "hideout_siege_rally_wait";
    internal const string StopOptionId = "hideout_siege_stop";

    // "n 个地图单位"：首版先集中在常量，后续可接 MCM。
    internal const float RallyRadiusMapUnits = 600f;

    // 规则：至少完整流逝 24 小时，然后等待到随后第一个正午 12:00。
    internal const float MinimumRallyHours = 24f;
    internal const float BattleStartHour = 12f;

    // 破城寨场景优先复用巴丹尼亚城镇攻城场景。
    internal const string BattanianCultureId = "battania";
}
