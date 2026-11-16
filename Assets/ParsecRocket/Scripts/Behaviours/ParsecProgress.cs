using UnityEngine;

public static class ParsecProgress
{
    private static string Key(int level) => "ParsecCampaign.Completed." + level;
    public static bool IsCompleted(int level) => PlayerPrefs.GetInt(Key(level), 0) == 1;
    public static bool IsUnlocked(int level) => level == 1 || IsCompleted(level - 1);
    public static void CompleteLevel(int level)
    {
        if (level < 1 || level > 3) return;
        PlayerPrefs.SetInt(Key(level), 1);
        PlayerPrefs.Save();
    }
}
