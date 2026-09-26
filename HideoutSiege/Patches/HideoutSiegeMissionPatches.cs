using System;
using System.Collections.Generic;
using HarmonyLib;
using HideoutSiege.Campaign;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace HideoutSiege.Patches;

internal static class HideoutSiegeMissionPatchGuard
{
    internal static bool IsActiveHideoutSiege()
    {
        var active = HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent;
        return active != null && ReferenceEquals(MobileParty.MainParty.MapEvent, active);
    }
}

[HarmonyPatch(typeof(BattleEndLogic), nameof(BattleEndLogic.EnableEnemyDefenderPullBack))]
internal static class DisableDefenderPullBackPatch
{
    private static bool Prefix()
    {
        return !HideoutSiegeMissionPatchGuard.IsActiveHideoutSiege();
    }
}

[HarmonyPatch(
    typeof(MissionCombatantsLogic),
    MethodType.Constructor,
    new Type[]
    {
        typeof(IEnumerable<IBattleCombatant>),
        typeof(IBattleCombatant),
        typeof(IBattleCombatant),
        typeof(IBattleCombatant),
        typeof(Mission.MissionTeamAITypeEnum),
        typeof(bool)
    })]
internal static class ForceSiegeTeamAiPatch
{
    private static void Prefix(ref Mission.MissionTeamAITypeEnum __4)
    {
        if (HideoutSiegeMissionPatchGuard.IsActiveHideoutSiege())
        {
            __4 = Mission.MissionTeamAITypeEnum.Siege;
        }
    }
}

[HarmonyPatch(
    typeof(Mission),
    nameof(Mission.SpawnAgent),
    new Type[]
    {
        typeof(AgentBuildData),
        typeof(bool),
        typeof(Equipment),
        typeof(ItemObject)
    })]
internal static class ForceDismountedAgentsPatch
{
    private static void Prefix(AgentBuildData __0, ref Equipment __2)
    {
        if (!HideoutSiegeMissionPatchGuard.IsActiveHideoutSiege())
        {
            return;
        }

        __0.NoHorses(true);

        if (__2 == null)
        {
            return;
        }

        __2 = __2.Clone(false);
        __2[EquipmentIndex.ArmorItemEndSlot] = EquipmentElement.Invalid;
        __2[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
    }
}
