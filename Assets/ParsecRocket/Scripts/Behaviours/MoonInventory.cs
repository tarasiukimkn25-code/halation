using System;
using System.Collections.Generic;
using UnityEngine;

public static class MoonInventory
{
    private const string ArtifactKey = "Parsec.Moon.Artifacts.v1";
    private const string CrystalKey = "Parsec.Moon.Crystals.v1";
    public const int Capacity = 5;
    public const int WandererReward = 50;
    private const string DeliveredKey = "Parsec.Moon.ArtifactsDelivered.v1";
    public static bool Delivered => PlayerPrefs.GetInt(DeliveredKey, 0) == 1;
    public static bool CanDeliver => !Delivered && Artifacts.Count == Capacity;
    public static bool HasClaimed(int id) => Delivered || Artifacts.Contains(id);
    public static bool DeliverToWanderer()
    {
        if (!CanDeliver) return false;
        PlayerPrefs.SetInt(DeliveredKey, 1);
        PlayerPrefs.SetString(ArtifactKey, JsonUtility.ToJson(new SaveData()));
        PlayerPrefs.SetInt(CrystalKey, Crystals + WandererReward);
        ParsecProgress.CompleteLevel(2);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }
    public static event Action Changed;
    [Serializable] private class SaveData { public List<int> items = new List<int>(); }
    public static int Crystals => Mathf.Max(0, PlayerPrefs.GetInt(CrystalKey, 0));
    public static List<int> Artifacts
    {
        get
        {
            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(ArtifactKey, "{}")); }
            catch (ArgumentException) { data = new SaveData(); }
            var valid = new List<int>();
            if (data?.items != null)
                foreach (int id in data.items)
                    if (id >= 0 && id < Capacity && !valid.Contains(id)) valid.Add(id);
            return valid;
        }
    }
    public static void AddCrystals(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(CrystalKey, Crystals + amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
    public static bool TrySpendCrystals(int amount)
    {
        if (amount < 0 || Crystals < amount) return false;
        if (amount == 0) return true;
        PlayerPrefs.SetInt(CrystalKey, Crystals - amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }
    public static bool Collect(int id)
    {
        var items = Artifacts;
        if (Delivered || id < 0 || id >= Capacity || items.Contains(id) || items.Count >= Capacity) return false;
        items.Add(id);
        PlayerPrefs.SetString(ArtifactKey, JsonUtility.ToJson(new SaveData { items = items }));
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(ArtifactKey);
        PlayerPrefs.DeleteKey(CrystalKey);
        PlayerPrefs.DeleteKey(DeliveredKey);
        for (int level = 1; level <= 3; level++) PlayerPrefs.DeleteKey("ParsecCampaign.Completed." + level);
        PlayerPrefs.Save();
        PlayerSkinSystem.ResetSelection();
        Changed?.Invoke();
    }
    public static Color ColorFor(int id)
    {
        Color[] colors = { new Color(.5f,1f,1f), new Color(1f,.65f,1f), new Color(1f,.85f,.4f), new Color(.55f,1f,.65f), new Color(1f,.55f,.45f) };
        return colors[Mathf.Clamp(id,0,Capacity-1)];
    }
}
