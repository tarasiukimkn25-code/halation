using UnityEngine;

public static class PlayerSkinSystem
{
    public const string DefaultSkin = "default";
    public const string FreeSkin = "engineer";
    public const string PremiumSkin = "commander";
    public const int PremiumPrice = 60;

    private const string SelectedKey = "Parsec.Skins.Selected.v1";
    private const string PremiumOwnedKey = "Parsec.Skins.CommanderOwned.v1";

    public static string SelectedSkin
    {
        get
        {
            string saved = PlayerPrefs.GetString(SelectedKey, DefaultSkin);
            return IsOwned(saved) ? saved : DefaultSkin;
        }
    }
    public static bool IsPremiumOwned => PlayerPrefs.GetInt(PremiumOwnedKey, 0) == 1;
    public static bool IsSkinMenuUnlocked => ParsecProgress.IsUnlocked(2);

    public static bool IsOwned(string skinId)
    {
        if (skinId == DefaultSkin || skinId == FreeSkin) return true;
        return skinId == PremiumSkin && IsPremiumOwned;
    }

    public static bool Select(string skinId)
    {
        if (!IsSkinMenuUnlocked || !IsOwned(skinId)) return false;
        PlayerPrefs.SetString(SelectedKey, skinId);
        PlayerPrefs.Save();
        ApplyToPlayers();
        return true;
    }

    public static bool PurchasePremium()
    {
        if (!IsSkinMenuUnlocked) return false;
        if (IsPremiumOwned) return Select(PremiumSkin);
        if (!MoonInventory.TrySpendCrystals(PremiumPrice)) return false;
        PlayerPrefs.SetInt(PremiumOwnedKey, 1);
        PlayerPrefs.Save();
        return Select(PremiumSkin);
    }

    public static string DisplayName(string skinId)
    {
        switch (skinId)
        {
            case FreeSkin: return GameLocalization.T("skin_engineer");
            case PremiumSkin: return GameLocalization.T("skin_commander");
            default: return GameLocalization.T("skin_default");
        }
    }

    public static Sprite LoadPreview(string skinId)
    {
        return PlayerSkinAnimator.LoadPreview(skinId);
    }

    public static void ApplyToPlayers()
    {
        foreach (PlayerController player in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            ApplyToPlayer(player);
    }

    public static void ApplyToPlayer(PlayerController player)
    {
        if (player == null) return;
        PlayerSkinAnimator visual = player.GetComponent<PlayerSkinAnimator>();
        if (SelectedSkin == DefaultSkin)
        {
            if (visual != null) visual.RestoreDefault();
            return;
        }

        if (visual == null) visual = player.gameObject.AddComponent<PlayerSkinAnimator>();
        visual.ApplySkin(SelectedSkin);
    }

    public static void ResetSelection()
    {
        PlayerPrefs.SetString(SelectedKey, DefaultSkin);
        PlayerPrefs.Save();
        ApplyToPlayers();
    }
}
