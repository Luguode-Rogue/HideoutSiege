using HarmonyLib;
using Helpers;
using HideoutSiege.Campaign;
using TaleWorlds.CampaignSystem.MapEvents;

namespace HideoutSiege.Patches;

[HarmonyPatch(typeof(MapEventComponentHelper), nameof(MapEventComponentHelper.AddNearbyPartiesToPlayerMapEvent))]
internal static class HideoutNearbyJoinPatch
{
    private static bool Prefix(MapEvent mapEvent)
    {
        if (HideoutSiegeRuntime.PendingHideoutBattle && mapEvent.IsHideoutBattle)
        {
            return false;
        }

        return !ReferenceEquals(HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent, mapEvent);
    }
}
