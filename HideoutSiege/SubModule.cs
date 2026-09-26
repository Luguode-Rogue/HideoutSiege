using HarmonyLib;
using HideoutSiege.Campaign;
using HideoutSiege.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace HideoutSiege;

public sealed class SubModule : MBSubModuleBase
{
    private Harmony? _harmony;

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();

        _harmony = new Harmony("LuguodeRogue.HideoutSiege");
        _harmony.PatchAll(typeof(SubModule).Assembly);
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);

        if (game.GameType is TaleWorlds.CampaignSystem.Campaign &&
            gameStarterObject is CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddBehavior(new HideoutSiegeCampaignBehavior());
        }
    }

    public override void OnBeforeMissionBehaviorInitialize(Mission mission)
    {
        base.OnBeforeMissionBehaviorInitialize(mission);

        var activeMapEvent = HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent;
        if (activeMapEvent == null ||
            !ReferenceEquals(MobileParty.MainParty.MapEvent, activeMapEvent) ||
            mission.GetMissionBehavior<HideoutSiegePreparationLogic>() != null)
        {
            return;
        }

        mission.AddMissionBehavior(new HideoutSiegePreparationLogic());
    }

    protected override void OnSubModuleUnloaded()
    {
        if (_harmony != null)
        {
            _harmony.UnpatchAll(_harmony.Id);
        }

        _harmony = null;

        base.OnSubModuleUnloaded();
    }
}
