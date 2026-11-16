using UnityEngine;

public class RocketBehaviour : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public bool IsUnlocked => MoonInventory.Delivered;

    private void Start()
    {
        if (GetComponent<NonPlayerCharacter>() == null)
            return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        MoonInventory.Changed += RefreshAppearance;
        RefreshAppearance();
    }

    public static string GetDialogue()
    {
        return MoonInventory.Delivered
            ? GameLocalization.T("rocket_unlocked")
            : GameLocalization.T("rocket_locked");
    }

    public bool TryOpenMinigame()
    {
        if (!IsUnlocked)
            return false;

        RocketLaunchMinigame minigame = GetComponent<RocketLaunchMinigame>();
        if (minigame == null)
            return false;

        minigame.OpenFromRocket();
        return true;
    }

    private void RefreshAppearance()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = IsUnlocked ? Color.white : new Color(.62f, .62f, .7f, 1f);
    }

    private void OnDestroy()
    {
        MoonInventory.Changed -= RefreshAppearance;
    }
}
