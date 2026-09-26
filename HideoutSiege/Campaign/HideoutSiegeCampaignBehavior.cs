using System;
using System.Collections.Generic;
using System.Linq;
using HideoutSiege.Config;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace HideoutSiege.Campaign;

public sealed class HideoutSiegeCampaignBehavior : CampaignBehaviorBase
{
    private bool _rallyActive;
    private Settlement? _targetHideout;
    private CampaignTime _rallyStartTime;
    private CampaignTime _rallyDeadline;
    private List<MobileParty> _rallyParties = new();
    private int _playerPrisonersBeforeBattle;

    public override void RegisterEvents()
    {
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(
            this,
            new Action<CampaignGameStarter>(OnNewGameCreated));

        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(
            this,
            new Action<CampaignGameStarter>(OnGameLoaded));

        CampaignEvents.SettlementEntered.AddNonSerializedListener(
            this,
            new Action<MobileParty, Settlement, Hero>(OnSettlementEntered));

        CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(
            this,
            new Action<MobileParty, Settlement>(OnSettlementLeft));

        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(
            this,
            new Action<MapEvent>(OnPlayerBattleEnd));

        CampaignEvents.MapEventEnded.AddNonSerializedListener(
            this,
            new Action<MapEvent>(OnMapEventEnded));

        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(
            this,
            new Action(OnHourlyTick));
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData<bool>("hideout_siege_rally_active", ref _rallyActive);
        dataStore.SyncData<Settlement?>("hideout_siege_target_hideout", ref _targetHideout);
        dataStore.SyncData<CampaignTime>("hideout_siege_rally_start", ref _rallyStartTime);
        dataStore.SyncData<CampaignTime>("hideout_siege_rally_deadline", ref _rallyDeadline);
        dataStore.SyncData<List<MobileParty>>("hideout_siege_rally_parties", ref _rallyParties);

        _rallyParties ??= new List<MobileParty>();
    }

    private void OnNewGameCreated(CampaignGameStarter starter)
    {
        AddGameMenus(starter);
    }

    private void OnGameLoaded(CampaignGameStarter starter)
    {
        AddGameMenus(starter);

        if (_rallyActive)
        {
            ReassertRallyOrders();
        }
    }

    private void AddGameMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption(
            "hideout_place",
            HideoutSiegeConfig.ChallengeOptionId,
            "{=!}讨敌骂阵",
            ChallengeOnCondition,
            ChallengeOnConsequence,
            false,
            1,
            false,
            null);

        starter.AddWaitGameMenu(
            HideoutSiegeConfig.RallyMenuId,
            "{=!}你命人在寨前讨敌骂阵。附近同类强盗正在赶回此处；至少一整天之后，双方将在随后的正午决战。",
            RallyMenuOnInit,
            RallyMenuOnCondition,
            RallyMenuOnConsequence,
            RallyMenuOnTick,
            GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
            GameMenu.MenuOverlayType.None,
            0f,
            GameMenu.MenuFlags.None,
            null);

        starter.AddGameMenuOption(
            HideoutSiegeConfig.RallyMenuId,
            HideoutSiegeConfig.StopOptionId,
            "{=!}结束攻城",
            StopSiegeOnCondition,
            StopSiegeOnConsequence,
            true,
            -1,
            false,
            null);
    }

    private bool ChallengeOnCondition(MenuCallbackArgs args)
    {
        var hideout = Settlement.CurrentSettlement;
        if (hideout == null || !hideout.IsHideout)
        {
            return false;
        }

        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

        if (_rallyActive)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("{=!}讨敌骂阵已经开始。");
            return true;
        }

        if (!hideout.Hideout.NextPossibleAttackTime.IsPast)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("{=!}这个藏身处目前无法再次进攻。");
            return true;
        }

        if (Hero.MainHero.IsWounded)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("{=!}你负伤在身，无法发动攻寨。");
            return true;
        }

        if (!hideout.Parties.Any(p => p.IsBandit))
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("{=!}藏身处内已经没有可以应战的强盗。");
            return true;
        }

        return true;
    }

    private void ChallengeOnConsequence(MenuCallbackArgs args)
    {
        var hideout = Settlement.CurrentSettlement;
        if (hideout == null || !hideout.IsHideout)
        {
            return;
        }

        BeginRally(hideout);
        GameMenu.SwitchToMenu(HideoutSiegeConfig.RallyMenuId);
    }

    private void BeginRally(Settlement hideout)
    {
        _rallyActive = true;
        _targetHideout = hideout;
        _rallyStartTime = CampaignTime.Now;
        _rallyDeadline = CampaignTime.HoursFromNow(CalculateHoursUntilBattle());
        _rallyParties = CollectRallyParties(hideout);
        var calledPartyCount = _rallyParties.Count(party => party.CurrentSettlement == null);
        var defendingPartyCount = _rallyParties.Count - calledPartyCount;

        foreach (var party in _rallyParties)
        {
            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

            OrderPartyBackToHideout(party, hideout);
        }

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

        InformationManager.DisplayMessage(
            new InformationMessage(
                $"讨敌骂阵：{calledPartyCount} 支附近同类强盗队伍被召回，寨内现有 {defendingPartyCount} 支守军。" +
                "只有决战前实际赶到藏身处的队伍会参战。"));
    }

    private float CalculateHoursUntilBattle()
    {
        var currentHour = CampaignTime.Now.CurrentHourInDay;
        var additionalHoursToNoon =
            (HideoutSiegeConfig.BattleStartHour - currentHour + CampaignTime.HoursInDay) %
            CampaignTime.HoursInDay;

        return HideoutSiegeConfig.MinimumRallyHours + additionalHoursToNoon;
    }

    private static List<MobileParty> CollectRallyParties(Settlement hideout)
    {
        var radiusSquared =
            HideoutSiegeConfig.RallyRadiusMapUnits *
            HideoutSiegeConfig.RallyRadiusMapUnits;

        return MobileParty.AllBanditParties
            .Where(party =>
                party.IsActive &&
                !party.Ai.IsDisabled &&
                party.MapEvent == null &&
                party.AttachedTo == null &&
                party.Party.Culture == hideout.Culture &&
                (party.CurrentSettlement == hideout ||
                 (party.CurrentSettlement == null &&
                  party.Position.DistanceSquared(hideout.Position) <= radiusSquared)))
            .ToList();
    }

    private static void OrderPartyBackToHideout(MobileParty party, Settlement hideout)
    {
        if (!party.IsActive || party.MapEvent != null)
        {
            return;
        }

        if (party.CurrentSettlement == hideout)
        {
            HoldRallyPartyInHideout(party);
            return;
        }

        if (party.CurrentSettlement != null)
        {
            return;
        }

        party.Ai.SetDoNotMakeNewDecisions(true);
        party.SetMoveGoToSettlement(hideout, party.NavigationCapability, false);
        party.RecalculateShortTermBehavior();
    }

    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        var tracked = IsTrackedRallyParty(party);

        if (tracked)
        {
            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
        }

        if (!_rallyActive ||
            settlement != _targetHideout ||
            !tracked)
        {
            return;
        }

        HoldRallyPartyInHideout(party);
    }

    private static void HoldRallyPartyInHideout(MobileParty party)
    {
        party.Ai.SetDoNotMakeNewDecisions(true);
        party.Ai.DisableAi();
    }

    private void OnSettlementLeft(MobileParty party, Settlement settlement)
    {
        if (!IsTrackedRallyParty(party))
        {
            return;
        }

        var shouldReturn = _rallyActive &&
                           settlement == _targetHideout &&
                           party.IsActive &&
                           party.MapEvent == null;

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

        if (!shouldReturn)
        {
            return;
        }

        HoldRallyPartyInHideout(party);
        EnterSettlementAction.ApplyForParty(party, settlement);

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
    }

    internal bool IsTrackedRallyParty(MobileParty party)
    {
        return _rallyParties.Contains(party);
    }

    internal bool ShouldPreventRallyPartyFromLeaving(MobileParty party)
    {
        return _rallyActive &&
               _targetHideout != null &&
               party.CurrentSettlement == _targetHideout &&
               _rallyParties.Contains(party);
    }

    private void ReassertRallyOrders()
    {
        var hideout = _targetHideout;
        if (hideout == null)
        {
            CancelRally(false);
            return;
        }

        foreach (var party in _rallyParties)
        {
            OrderPartyBackToHideout(party, hideout);
        }
    }

    private void OnHourlyTick()
    {
        if (_rallyActive)
        {
            ReassertRallyOrders();
        }
    }

    private void RallyMenuOnInit(MenuCallbackArgs args)
    {
        var hideout = _targetHideout;
        if (hideout?.Hideout != null)
        {
            args.MenuContext.SetBackgroundMeshName(hideout.Hideout.WaitMeshName);
        }

        UpdateRallyProgress(args);
    }

    private bool RallyMenuOnCondition(MenuCallbackArgs args)
    {
        return _rallyActive &&
               _targetHideout != null &&
               Settlement.CurrentSettlement == _targetHideout;
    }

    private void RallyMenuOnTick(MenuCallbackArgs args, CampaignTime elapsed)
    {
        if (!_rallyActive)
        {
            return;
        }

        UpdateRallyProgress(args);
    }

    private void UpdateRallyProgress(MenuCallbackArgs args)
    {
        var totalHours = (_rallyDeadline - _rallyStartTime).ToHours;
        var elapsedHours = (CampaignTime.Now - _rallyStartTime).ToHours;

        var progress = totalHours <= 0d
            ? 1f
            : (float)Math.Max(0d, Math.Min(1d, elapsedHours / totalHours));

        if (_rallyDeadline.IsPast || _rallyDeadline.IsNow)
        {
            progress = 1f;
        }

        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(progress);
    }

    private void RallyMenuOnConsequence(MenuCallbackArgs args)
    {
        if (!_rallyActive || _targetHideout == null)
        {
            return;
        }

        StartHideoutSiegeBattle(_targetHideout);
    }

    private bool StopSiegeOnCondition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
        return true;
    }

    private void StopSiegeOnConsequence(MenuCallbackArgs args)
    {
        CancelRally(true);

        if (PlayerEncounter.IsActive)
        {
            if (MobileParty.MainParty.CurrentSettlement != null)
            {
                PlayerEncounter.LeaveSettlement();
            }

            PlayerEncounter.Finish(true);
        }

        MobileParty.MainParty.SetMoveModeHold();

        InformationManager.DisplayMessage(
            new InformationMessage("你结束了这次攻城。强盗恢复自主行动，讨敌骂阵状态已经取消。"));
    }

    private void StartHideoutSiegeBattle(Settlement hideout)
    {
        var sceneName = HideoutSiegeSceneSelector.SelectBattanianSiegeScene();
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            InformationManager.DisplayMessage(
                new InformationMessage("未找到可复用的巴丹尼亚城镇攻城场景，本次攻城已结束。"));

            CancelRally(true);
            GameMenu.SwitchToMenu("hideout_place");
            return;
        }

        TaleWorlds.CampaignSystem.Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;

        LogRallyArrivalState(hideout);

        GameMenu.SwitchToMenu("hideout_place");
        hideout.Hideout.SetNextPossibleAttackTime(
            TaleWorlds.CampaignSystem.Campaign.Current.Models.HideoutModel.HideoutHiddenDuration);

        if (PlayerEncounter.IsActive)
        {
            PlayerEncounter.LeaveEncounter = false;
        }
        else
        {
            PlayerEncounter.Start();
            PlayerEncounter.Current.SetupFields(PartyBase.MainParty, hideout.Party);
        }

        HideoutSiegeRuntime.PendingHideoutBattle = true;

        if (PlayerEncounter.Battle == null)
        {
            PlayerEncounter.StartBattle();
            PlayerEncounter.Update();
        }

        var activeMapEvent = PlayerEncounter.Battle;
        if (activeMapEvent == null)
        {
            HideoutSiegeRuntime.PendingHideoutBattle = false;
            ReleaseRallyPartyAi();
            _rallyActive = false;
            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
            ClearRallyStateAfterBattleStart();
            return;
        }

        HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent = activeMapEvent;
        HideoutSiegeRuntime.PendingHideoutBattle = false;

        ReleaseRallyPartyAi();
        _rallyActive = false;

        _playerPrisonersBeforeBattle = PartyBase.MainParty.PrisonRoster.TotalManCount;
        LogMapEventSnapshot("BATTLE_OPEN", activeMapEvent);

        CampaignMission.OpenSiegeMissionNoDeployment(sceneName);

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

        ClearRallyStateAfterBattleStart();
    }

    private void OnPlayerBattleEnd(MapEvent mapEvent)
    {
        if (!ReferenceEquals(HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent, mapEvent))
        {
            return;
        }

        LogMapEventSnapshot("RESULTS_PENDING", mapEvent);
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (!ReferenceEquals(HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent, mapEvent))
        {
            return;
        }

        LogMapEventSnapshot("MAP_EVENT_ENDED", mapEvent);

        var playerPrisonersAfterBattle = PartyBase.MainParty.PrisonRoster.TotalManCount;
        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

        HideoutSiegeRuntime.ActiveHideoutSiegeMapEvent = null;
    }

    private static void LogMapEventSnapshot(string stage, MapEvent mapEvent)
    {
        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

        LogMapEventSide(stage, "ATTACKER", mapEvent.AttackerSide);
        LogMapEventSide(stage, "DEFENDER", mapEvent.DefenderSide);
    }

    private static void LogMapEventSide(string stage, string sideName, MapEventSide side)
    {
        foreach (var mapEventParty in side.Parties)
        {
            var party = mapEventParty.Party;
            var lootPrisoners = mapEventParty.IsNpcParty
                ? mapEventParty.RosterToReceiveLootPrisoners.TotalManCount
                : PlayerEncounter.Current?.RosterToReceiveLootPrisoners.TotalManCount ?? -1;

            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
        }
    }

    private void LogRallyArrivalState(Settlement hideout)
    {
        var arrivedCount = 0;
        var missedCount = 0;

        foreach (var party in _rallyParties)
        {
            var arrived = party.IsActive && party.CurrentSettlement == hideout;
            if (arrived)
            {
                arrivedCount++;
            }
            else
            {
                missedCount++;
            }

            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
        }

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
    }

    private void ReleaseRallyPartyAi()
    {
        foreach (var party in _rallyParties)
        {
            if (!party.IsActive)
            {
                continue;
            }

            party.Ai.EnableAi();
            party.Ai.SetDoNotMakeNewDecisions(false);
            party.RecalculateShortTermBehavior();
        }
    }

    private void CancelRally(bool releaseAi)
    {
        if (releaseAi)
        {
            ReleaseRallyPartyAi();
        }

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;

        _rallyActive = false;
        _targetHideout = null;
        _rallyParties.Clear();
        _rallyStartTime = CampaignTime.Zero;
        _rallyDeadline = CampaignTime.Zero;
    }

    private void ClearRallyStateAfterBattleStart()
    {
        _targetHideout = null;
        _rallyParties.Clear();
        _rallyStartTime = CampaignTime.Zero;
        _rallyDeadline = CampaignTime.Zero;
    }
}
