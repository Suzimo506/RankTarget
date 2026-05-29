using MelonLoader;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Suzimo.MuseDashMods.RankTarget
{
    public static class RankTargetConfig
    {
        public static int FontSize = 40;
        public static string FontColor = "#FFFFFF";
        public static string FontName = "Normal";
        public static string FontStyle = "Normal";
        public static bool OutlineEnabled = true;
        public static string OutlineColor = "#000000";
        public static float PositionOffsetX = -20f;
        public static float PositionOffsetY = 20f;
        public static string MyScoreColor = "#FFFFFF";
        public static string DiffColor = "#ffff00";
        public static string FormatTop1 = "当前<color=#ff0000>1</color>名，分数：{myscore}";
        public static string FormatNotTop100 = " 距离前百差{diff}";
        public static string FormatInTop100 = "当前第<color=#00ff00>{myrank}</color>名 {diff}";

        public static readonly string FilePath = "UserData/RankTarget.cfg";

        public static void Load(bool saveBack = true)
        {
            if (File.Exists(FilePath))
            {
                string content = File.ReadAllText(FilePath);
                if (!content.Contains("RankTarget UI Configuration"))
                {
                    SaveConfig();
                    return;
                }

                string[] lines = File.ReadAllLines(FilePath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || trimmed.StartsWith("//") || !trimmed.Contains("=")) continue;

                    var parts = trimmed.Split(new[] { '=' }, 2);
                    string key = parts[0].Trim();
                    // 去除首尾的引号与空白
                    string value = parts[1].Trim().Trim('"', '\'').Trim();

                    switch (key)
                    {
                        case "FontSize": int.TryParse(value, out FontSize); break;
                        case "FontColor": FontColor = value; break;
                        case "FontName": FontName = value; break;
                        case "FontStyle": FontStyle = value; break;
                        case "OutlineEnabled": bool.TryParse(value, out OutlineEnabled); break;
                        case "OutlineColor": OutlineColor = value; break;
                        case "PositionOffsetX": float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out PositionOffsetX); break;
                        case "PositionOffsetY": float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out PositionOffsetY); break;
                        case "MyScoreColor": MyScoreColor = value; break;
                        case "DiffColor": DiffColor = value; break;
                        case "FormatTop1": FormatTop1 = value; break;
                        case "FormatNotTop100": FormatNotTop100 = value; break;
                        case "FormatInTop100": FormatInTop100 = value; break;
                    }
                }
            }
            else
            {
                SaveConfig();
                return;
            }
            
            if (saveBack)
            {
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            string cfg = $@"# RankTarget UI Configuration

# 字体大小 (Font Size)
FontSize = {FontSize}

# 字体颜色/HEX (Font Color)
FontColor = ""{FontColor}""

# 字体名 (Font Name from game assets: Snaps Taste, Lato-Regular, LuckiestGuy-Regular, Chinese-Regular, Normal)
FontName = ""{FontName}""

# 字体样式 (Font Style: Normal, Bold, Italic, BoldAndItalic)
FontStyle = ""{FontStyle}""

# 启用描边 (Enable outline)
OutlineEnabled = {OutlineEnabled.ToString().ToLower()}

# 描边颜色/HEX (Outline color)
OutlineColor = ""{OutlineColor}""

# X轴位置偏移 - 基于右下角 (X Position offset from bottom right)
PositionOffsetX = {PositionOffsetX.ToString(System.Globalization.CultureInfo.InvariantCulture)}

# Y轴位置偏移 - 基于右下角 (Y Position offset from bottom right)
PositionOffsetY = {PositionOffsetY.ToString(System.Globalization.CultureInfo.InvariantCulture)}

# 你的分数的颜色/HEX (Color for the {{myscore}} placeholder)
MyScoreColor = ""{MyScoreColor}""

# 分差的颜色/HEX (Color for the {{diff}} placeholder)
DiffColor = ""{DiffColor}""

# 第一名的文本格式 (Text format for Rank 1)
# 支持的变量/Placeholders: {{myscore}}
FormatTop1 = ""{FormatTop1}""

# 未进榜(100名外)的文本格式 (Text format when not in top 100)
# 支持的变量/Placeholders: {{myscore}}, {{nextscore}}, {{myrank}}, {{nextrank}}, {{diff}}
FormatNotTop100 = ""{FormatNotTop100}""

# 100名以内的文本格式 (Text format when in top 100)
# 支持的变量/Placeholders: {{myscore}}, {{nextscore}}, {{myrank}}, {{nextrank}}, {{diff}}
FormatInTop100 = ""{FormatInTop100}""
";
            try
            {
                RankTargetMod.DisableWatcher();
                File.WriteAllText(FilePath, cfg);
            }
            finally
            {
                RankTargetMod.EnableWatcher();
            }
        }
    }

    public class RankTargetMod : MelonMod
    {
        public static List<int> TopScores { get; private set; } = new List<int>();

        public static UnityEngine.UI.Text? TargetText;

        private static FileSystemWatcher? _configWatcher;
        private static System.Threading.Timer? _debounceTimer;
        private static readonly object _lock = new object();
        public static bool NeedsUIUpdate = false;

        public override void OnInitializeMelon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<UIAnimator>();
            RankTargetConfig.Load();
            SetupConfigWatcher();
            MelonLogger.Msg("RankTargetMod initialized! Config loaded from UserData/RankTarget.cfg.");
        }

        private static void SetupConfigWatcher()
        {
            try
            {
                string dir = Path.GetDirectoryName(RankTargetConfig.FilePath) ?? "UserData";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _configWatcher = new FileSystemWatcher(dir, Path.GetFileName(RankTargetConfig.FilePath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                _configWatcher.Changed += OnConfigChanged;
                _configWatcher.Created += OnConfigChanged;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("Failed to set up config watcher: " + ex.ToString());
            }
        }

        public static void DisableWatcher()
        {
            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = false;
            }
        }

        public static void EnableWatcher()
        {
            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = true;
            }
        }

        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                if (_debounceTimer != null)
                {
                    _debounceTimer.Change(400, System.Threading.Timeout.Infinite);
                }
                else
                {
                    _debounceTimer = new System.Threading.Timer(OnDebouncedConfigChanged, null, 400, System.Threading.Timeout.Infinite);
                }
            }
        }

        private static void OnDebouncedConfigChanged(object? state)
        {
            lock (_lock)
            {
                if (_debounceTimer != null)
                {
                    _debounceTimer.Dispose();
                    _debounceTimer = null;
                }
            }

            int attempt = 0;
            int delay = 100;
            double backoff = 1.8;
            int maxAttempts = 6;

            while (true)
            {
                try
                {
                    RankTargetConfig.Load(false);
                    MelonLogger.Msg("Reloaded config file: " + Path.GetFileName(RankTargetConfig.FilePath));
                    NeedsUIUpdate = true;
                    return;
                }
                catch (IOException)
                {
                }
                catch (System.UnauthorizedAccessException)
                {
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error("Error reloading config: " + ex.ToString());
                    return;
                }

                attempt++;
                if (attempt > maxAttempts)
                {
                    MelonLogger.Error("Failed to reload config after max retry attempts.");
                    return;
                }

                System.Threading.Thread.Sleep(delay);
                delay = System.Math.Max(delay + 1, (int)(delay * backoff));
            }
        }

        public static void ApplyConfigToText(UnityEngine.UI.Text? uiText)
        {
            if (uiText == null) return;

            uiText.fontSize = RankTargetConfig.FontSize;
            uiText.alignment = TextAnchor.LowerRight;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.supportRichText = true;

            UnityEngine.Font? customFont = null;
            string fontConfig = RankTargetConfig.FontName ?? "";
            string fontNameToLoad = fontConfig;

            string normalized = fontConfig.Trim().ToLower().Replace(" ", "").Replace("-", "");
            bool isGameAsset = false;
            if (normalized == "snapstaste") { fontNameToLoad = "Snaps Taste"; isGameAsset = true; }
            else if (normalized == "lato" || normalized == "latoregular") { fontNameToLoad = "Lato-Regular"; isGameAsset = true; }
            else if (normalized == "luckiestguy" || normalized == "luckiestguyregular") { fontNameToLoad = "LuckiestGuy-Regular"; isGameAsset = true; }
            else if (normalized == "normal") { fontNameToLoad = "Normal"; isGameAsset = true; }

            if (isGameAsset)
            {
                try
                {
                    var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Font>(fontNameToLoad);
                    customFont = handle.WaitForCompletion();
                    if (customFont == null)
                    {
                        MelonLogger.Warning($"[RankTarget] Addressables returned null for game asset font: '{fontNameToLoad}'");
                    }
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error($"[RankTarget] Failed to load game asset font '{fontNameToLoad}' via Addressables: {ex.ToString()}");
                }
            }
            else if (!string.IsNullOrEmpty(fontNameToLoad))
            {
                try
                {
                    // 尝试从操作系统中加载系统字体（如 楷体 "KaiTi"、黑体 "SimHei" 等）
                    customFont = UnityEngine.Font.CreateDynamicFontFromOSFont(fontNameToLoad, RankTargetConfig.FontSize);
                    if (customFont == null)
                    {
                        MelonLogger.Warning($"[RankTarget] CreateDynamicFontFromOSFont returned null for OS font name: '{fontNameToLoad}'");
                    }
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error($"[RankTarget] Failed to load OS font '{fontNameToLoad}': {ex.ToString()}");
                }
            }

            if (customFont != null)
            {
                uiText.font = customFont;
                var animator = uiText.GetComponent<UIAnimator>();
                if (animator != null) animator.CustomFont = customFont;
                MelonLogger.Msg($"[RankTarget] Successfully applied font: '{fontNameToLoad}'");
            }
            else
            {
                MelonLogger.Warning($"[RankTarget] customFont is null! Font was not applied. uiText.font remains '{uiText.font?.name}'");
            }

            UnityEngine.Color fontColor;
            if (UnityEngine.ColorUtility.TryParseHtmlString(RankTargetConfig.FontColor, out fontColor))
            {
                uiText.color = fontColor;
            }

            // 尝试解析字体样式
            if (System.Enum.TryParse<FontStyle>(RankTargetConfig.FontStyle, true, out var style))
            {
                uiText.fontStyle = style;
            }
            else
            {
                uiText.fontStyle = FontStyle.Normal;
            }

            if (RankTargetConfig.OutlineEnabled)
            {
                var outline = uiText.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = uiText.gameObject.AddComponent<Outline>();
                }
                
                if (UnityEngine.ColorUtility.TryParseHtmlString(RankTargetConfig.OutlineColor, out UnityEngine.Color outlineColor))
                {
                    outline.effectColor = outlineColor;
                }
                outline.effectDistance = new Vector2(2f, -2f);
            }
            else 
            {
                var outline = uiText.GetComponent<Outline>();
                if (outline != null) UnityEngine.Object.Destroy(outline);
            }

            var rect = uiText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                
                rect.anchoredPosition = new Vector2(RankTargetConfig.PositionOffsetX, RankTargetConfig.PositionOffsetY); 
                rect.anchoredPosition3D = new Vector3(RankTargetConfig.PositionOffsetX, RankTargetConfig.PositionOffsetY, 0f);
                rect.sizeDelta = new Vector2(800f, 200f);
                rect.localScale = Vector3.one; 
            }
        }

        public static void UpdateTopScores(List<int> newScores)
        {
            TopScores = newScores;
        }

        public override void OnUpdate()
        {
            if (NeedsUIUpdate)
            {
                NeedsUIUpdate = false;
                if (TargetText != null)
                {
                    ApplyConfigToText(TargetText);
                }
            }

            if (TargetText == null || TopScores == null || TopScores.Count == 0)
            {
                if (TargetText != null)
                {
                    TargetText.text = "未获取到排行榜数据";
                }
                return;
            }

            var targetInstance = Il2CppAssets.Scripts.GameCore.HostComponent.TaskStageTarget.instance;
            var currentScore = targetInstance != null ? targetInstance.m_Score : 0;

            string coloredMyScore = $"<color={RankTargetConfig.MyScoreColor}>{currentScore}</color>";
            string finalDisplayText = "";

            if (currentScore > TopScores[0])
            {
                finalDisplayText = RankTargetConfig.FormatTop1
                    .Replace("{myscore}", coloredMyScore);
            }
            else
            {
                int currentRank = 101;
                for (int i = 0; i < TopScores.Count; i++)
                {
                    if (currentScore > TopScores[i])
                    {
                        currentRank = i + 1;
                        break;
                    }
                }

                if (currentRank > 100)
                {
                    int nextRank = TopScores.Count; 
                    int rank100Score = TopScores[TopScores.Count - 1]; 

                    int difference = rank100Score - currentScore;
                    if (difference < 0) difference = 0;
                    
                    string coloredDiff = $"<color={RankTargetConfig.DiffColor}>{difference}</color>";

                    string format = RankTargetConfig.FormatNotTop100;
                    // 排行榜人数不足百人时动态修正文本描述
                    if (TopScores.Count < 100)
                    {
                        if (format.Contains("距离前百"))
                        {
                            format = format.Replace("距离前百", $"距离第{nextRank}名");
                        }
                        else if (format.Contains("前百"))
                        {
                            format = format.Replace("前百", $"第{nextRank}名");
                        }
                        else if (format.Contains("Top 100"))
                        {
                            format = format.Replace("Top 100", $"Top {nextRank}");
                        }
                    }

                    finalDisplayText = format
                        .Replace("{myscore}", coloredMyScore)
                        .Replace("{nextscore}", rank100Score.ToString())
                        .Replace("{myrank}", "100+")
                        .Replace("{nextrank}", nextRank.ToString())
                        .Replace("{diff}", coloredDiff);
                }
                else
                {
                    int nextRankIndex = currentRank - 2;
                    if (nextRankIndex < 0) nextRankIndex = 0;
                    
                    int nextTargetScore = TopScores[nextRankIndex];
                    int nextRank = nextRankIndex + 1;
                    int difference = nextTargetScore - currentScore;
                    
                    string coloredDiff = $"<color={RankTargetConfig.DiffColor}>{difference}</color>";

                    finalDisplayText = RankTargetConfig.FormatInTop100
                        .Replace("{myscore}", coloredMyScore)
                        .Replace("{nextscore}", nextTargetScore.ToString())
                        .Replace("{myrank}", currentRank.ToString())
                        .Replace("{nextrank}", nextRank.ToString())
                        .Replace("{diff}", coloredDiff);
                }
            }

            if (TargetText != null) TargetText.text = finalDisplayText;
        }
    }
}
