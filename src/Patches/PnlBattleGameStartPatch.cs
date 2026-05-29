using HarmonyLib;
using Il2CppAssets.Scripts.UI.Panels;
using UnityEngine;
using UnityEngine.UI;
using Suzimo.MuseDashMods.RankTarget;
using Suzimo.MuseDashMods.RankTarget.Data.Api;

namespace Suzimo.MuseDashMods.RankTarget.Patches
{
    [HarmonyPatch(typeof(PnlBattle), nameof(PnlBattle.GameStart))]
    public class PnlBattleGameStartPatch
    {
        private static void Postfix(PnlBattle __instance)
        {
            // Navigate down like Info+ finds active panels
            Transform pnlBattleUI = __instance.transform.Find("PnlBattleUI");
            if (pnlBattleUI == null) return;

            GameObject? activePanel = null;
            for (int i = 0; i < pnlBattleUI.childCount; i++)
            {
                if (pnlBattleUI.GetChild(i).gameObject.activeSelf)
                {
                    activePanel = pnlBattleUI.GetChild(i).gameObject;
                    break;
                }
            }

            if (activePanel == null) return;

            // 获取 PnlBattleOthers 的 Djmax/TxtScore_djmax 文本模板，该模板带有原生 Text 组件
            Transform pnlBattleOthers = pnlBattleUI.Find("PnlBattleOthers");
            if (pnlBattleOthers == null) return;

            Transform scoreChild = pnlBattleOthers.Find("Score/Djmax/TxtScore_djmax");
            if (scoreChild == null) return;

            // 诊断：打印模板对象的所有组件，确认其包含 Text 还是 TextMeshPro
            MelonLoader.MelonLogger.Msg("[RankTarget] scoreChild template components:");
            foreach (var comp in scoreChild.GetComponents<Component>())
            {
                if (comp != null) MelonLoader.MelonLogger.Msg(" - " + comp.GetIl2CppType().FullName);
            }

            try
            {
                // Anchor to the active panel itself, not the nested Score container
                GameObject rankTargetObj = UnityEngine.Object.Instantiate(scoreChild.gameObject, activePanel.transform);
                rankTargetObj.name = "RankTargetText";

                // Safe destruction of known unwanted parts
                var icon1 = rankTargetObj.transform.Find("ImgIconApDjmax");
                if (icon1 != null) UnityEngine.Object.Destroy(icon1.gameObject);
                
                var icon2 = rankTargetObj.transform.Find("ImgIconAp");
                if (icon2 != null) UnityEngine.Object.Destroy(icon2.gameObject);

                var fitter = rankTargetObj.GetComponent<ContentSizeFitter>();
                if (fitter != null) UnityEngine.Object.Destroy(fitter);

                // 调整原生 UI Text 组件并应用配置
                var uiText = rankTargetObj.GetComponent<Text>();
                if (uiText == null)
                {
                    MelonLoader.MelonLogger.Warning("[RankTarget] uiText is null! The template GameObject does not have a UnityEngine.UI.Text component.");
                }
                else
                {
                    RankTargetMod.ApplyConfigToText(uiText);
                }

                rankTargetObj.SetActive(true);
                rankTargetObj.AddComponent<UIAnimator>();

                // 将 Text 组件导出至主 OnUpdate 循环
                RankTargetMod.TargetText = uiText;
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Msg("[RankTarget] Error during UI cloning and initialization: " + ex.ToString());
            }
        }
    }
}
