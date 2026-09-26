using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HideoutSiege.Missions;

internal sealed class HideoutSiegePreparationLogic : MissionLogic
{
    private bool _isPrepared;

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (_isPrepared)
        {
            return;
        }

        _isPrepared = true;

        EnableReinforcements();
        BreakTwoWallSegments();
        DisableSiegeEquipment();
    }

    private void EnableReinforcements()
    {
        var spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
        if (spawnLogic == null)
        {
            /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
            return;
        }

        spawnLogic.SetReinforcementsSpawnEnabled(true, true);

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
    }

    private void BreakTwoWallSegments()
    {
        var breakableWalls = Mission.ActiveMissionObjects
            .FindAllWithType<WallSegment>()
            .Where(wall =>
                wall.GameEntity.GetChildren().Any(child => child.HasTag("broken_child")))
            .ToList();

        var brokenCount = 0;

        foreach (var wall in breakableWalls)
        {
            var shouldBreak = brokenCount < 2;
            wall.OnChooseUsedWallSegment(shouldBreak);

            if (shouldBreak)
            {
                brokenCount++;
            }
        }

        /* 此代码看不到log：Debug.Print 不会写入可查看的日志文件，已禁用。 */;
    }

    private void DisableSiegeEquipment()
    {
        var siegeWeaponEntities = Mission
            .GetActiveEntitiesWithScriptComponentOfType<SiegeWeapon>()
            .ToList();

        foreach (var entity in siegeWeaponEntities)
        {
            var siegeWeapon = entity.GetFirstScriptOfType<SiegeWeapon>();
            siegeWeapon?.SetDisabledSynched();
        }

        var siegeLadders = Mission.ActiveMissionObjects
            .FindAllWithType<SiegeLadder>()
            .ToList();

        foreach (var ladder in siegeLadders)
        {
            ladder.SetDisabledSynched();
        }
    }
}
