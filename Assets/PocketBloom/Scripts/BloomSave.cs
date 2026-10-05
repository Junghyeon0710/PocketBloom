using System;
using System.IO;
using UnityEngine;

namespace PocketBloom
{
    [Serializable]
    public sealed class BloomProfile
    {
        public int version = 1, unlockedStage = 1, seeds, best, completedRuns, garden;
        public int[] stars = new int[36];
        public bool sound = true, music = true, haptics = true, reducedMotion, tutorialSeen;
        public string language = "ko", lastDailyReward = "", lastGift = "";
        public int dailyBest;
        public string dailyBestDay = "";
        public BloomRun active;
    }

    public static class BloomSave
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "pocket-bloom-v1.json");
        public static BloomProfile Load()
        {
            foreach (var path in new[] { SavePath, SavePath + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var p = JsonUtility.FromJson<BloomProfile>(File.ReadAllText(path));
                    if (p == null || p.version != 1 || p.stars?.Length != 36) continue;
                    p.unlockedStage = Mathf.Clamp(p.unlockedStage, 1, 36); p.seeds = Mathf.Max(0, p.seeds);
                    p.garden = Mathf.Clamp(p.garden, 0, 5);
                    if (p.active != null && !p.active.IsValid()) p.active = null;
                    return p;
                }
                catch (Exception e) { Debug.LogWarning("저장 복구: " + e.Message); }
            }
            return new BloomProfile { language = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en" };
        }
        public static bool Write(BloomProfile p)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SavePath + ".tmp", JsonUtility.ToJson(p));
                if (File.Exists(SavePath)) File.Replace(SavePath + ".tmp", SavePath, SavePath + ".bak");
                else File.Move(SavePath + ".tmp", SavePath);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("저장 실패: " + e.Message); return false; }
        }
    }
}
