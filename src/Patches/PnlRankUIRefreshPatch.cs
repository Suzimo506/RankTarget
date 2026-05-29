using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

using Il2CppAssets.Scripts.UI.Panels;
using Suzimo.MuseDashMods.RankTarget.Data.Api;

namespace Suzimo.MuseDashMods.RankTarget.Patches
{
    [HarmonyPatch(typeof(PnlRank), nameof(PnlRank.UIRefresh))]
    public class PnlRankUIRefreshPatch
    {
        private static void Postfix(string uid, PnlRank __instance)
        {
            if (__instance.m_Ranks.ContainsKey(uid))
            {
                var raw = __instance.m_Ranks[uid].ToString();
                
                try
                {
                    var scores = new List<int>();
                    var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"""score""\s*:\s*(\d+)");
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Success && match.Groups.Count > 1)
                        {
                            if (int.TryParse(match.Groups[1].Value, out int score))
                            {
                                scores.Add(score);
                            }
                        }
                    }
                    RankTargetMod.UpdateTopScores(scores);
                }
                catch (System.Exception ex)
                {
                    MelonLoader.MelonLogger.Warning($"Failed to parse leaderboard data: {ex.Message}");
                }
            }
        }
    }
}
