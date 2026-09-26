using HarmonyLib;
using HideoutSiege.Campaign;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace HideoutSiege.Patches;

[HarmonyPatch(typeof(LeaveSettlementAction), nameof(LeaveSettlementAction.ApplyForParty))]
internal static class RallyPartySettlementPatch
{
    private static bool Prefix(MobileParty mobileParty)
    {
        var campaign = TaleWorlds.CampaignSystem.Campaign.Current;
        var behavior = campaign?.GetCampaignBehavior<HideoutSiegeCampaignBehavior>();
        var isTracked = behavior?.IsTrackedRallyParty(mobileParty) == true;
        var shouldPrevent = behavior?.ShouldPreventRallyPartyFromLeaving(mobileParty) == true;

        if (isTracked ||
            (mobileParty.IsBandit && mobileParty.CurrentSettlement?.IsHideout == true))
        {
            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
        }

        return !shouldPrevent;
    }
}

[HarmonyPatch(typeof(AiLandBanditPatrollingBehavior), "AiHourlyTick")]
internal static class RallyPartyPatrollingPatch
{
    private static bool Prefix(MobileParty __0)
    {
        var campaign = TaleWorlds.CampaignSystem.Campaign.Current;
        var behavior = campaign?.GetCampaignBehavior<HideoutSiegeCampaignBehavior>();
        var shouldPrevent = behavior?.ShouldPreventRallyPartyFromLeaving(__0) == true;

        if (shouldPrevent)
        {
            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
        }

        return !shouldPrevent;
    }
}
