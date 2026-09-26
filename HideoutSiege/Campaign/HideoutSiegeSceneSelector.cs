using System;
using System.Collections.Generic;
using HideoutSiege.Config;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace HideoutSiege.Campaign;

internal static class HideoutSiegeSceneSelector
{
    internal static string? SelectBattanianSiegeScene()
    {
        var scenes = new List<string>();

        foreach (Settlement settlement in Settlement.All)
        {
            if (!settlement.IsTown ||
                !string.Equals(settlement.Culture.StringId, HideoutSiegeConfig.BattanianCultureId, StringComparison.Ordinal))
            {
                continue;
            }

            var center = settlement.LocationComplex?.GetLocationWithId("center");
            if (center == null)
            {
                continue;
            }

            // Location 支持 0..3 四个升级级别；空级别会回退到 scene_name。
            for (var level = 0; level < 4; level++)
            {
                var scene = center.GetSceneName(level);
                if (!string.IsNullOrWhiteSpace(scene) && !scenes.Contains(scene))
                {
                    scenes.Add(scene);
                }
            }
        }

        if (scenes.Count == 0)
        {
            return null;
        }

        return scenes[MBRandom.RandomInt(scenes.Count)];
    }
}
