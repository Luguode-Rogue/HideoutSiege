using TaleWorlds.CampaignSystem.MapEvents;

namespace HideoutSiege.Campaign;

internal static class HideoutSiegeRuntime
{
    internal static bool PendingHideoutBattle { get; set; }

    internal static MapEvent? ActiveHideoutSiegeMapEvent { get; set; }
}
