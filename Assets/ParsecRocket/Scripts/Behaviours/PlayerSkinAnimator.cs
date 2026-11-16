using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(32000)]
public sealed class PlayerSkinAnimator : MonoBehaviour
{
    private const int FrameCount = 8;
    private const float WalkFramesPerSecond = 10f;
    private const float IdleFramesPerSecond = 6f;
    private const float ShootAnimationDuration = .28f;

    private static readonly Dictionary<string, Sprite> PreviewCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite[]> CutsceneFrameCache = new Dictionary<string, Sprite[]>();

    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private Sprite originalSprite;
    private Sprite[] idleFrames;
    private Sprite[] walkFrames;
    private Sprite[] createdSprites;
    private string activeSkin;
    private float frameTimer;
    private int frameIndex;
    private bool lastMoving;
    private float shootTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    public bool ApplySkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || skinId == PlayerSkinSystem.DefaultSkin)
        {
            RestoreDefault();
            return true;
        }
        if (activeSkin == skinId && idleFrames != null && walkFrames != null) return true;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (originalSprite == null) originalSprite = spriteRenderer.sprite;

        if (!TryLoadFrames(skinId, out Sprite[] idle, out Sprite[] walk, out List<Sprite> generated))
            return false;

        ReleaseGeneratedSprites();
        idleFrames = idle;
        walkFrames = walk;
        createdSprites = generated.ToArray();
        activeSkin = skinId;
        frameIndex = 0;
        frameTimer = 0f;
        lastMoving = playerController != null && playerController.IsMoving;
        spriteRenderer.sprite = idleFrames[0];
        return true;
    }

    public void RestoreDefault()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (originalSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = originalSprite;
        activeSkin = PlayerSkinSystem.DefaultSkin;
        idleFrames = null;
        walkFrames = null;
        ReleaseGeneratedSprites();
        shootTimer = 0f;
    }

    public void PlayShootAnimation()
    {
        if (idleFrames == null || walkFrames == null)
            return;

        shootTimer = ShootAnimationDuration;
        frameTimer = 0f;
    }

    private void LateUpdate()
    {
        if (idleFrames == null || walkFrames == null || spriteRenderer == null) return;

        if (shootTimer > 0f)
        {
            shootTimer = Mathf.Max(0f, shootTimer - Time.deltaTime);
            float shootProgress = 1f - shootTimer / ShootAnimationDuration;
            int shotPose = Mathf.Clamp(Mathf.FloorToInt(shootProgress * 4f), 0, 3);
            // These imported skins have idle/walk frames only. Use a quick step pose
            // and return to idle to give the existing launch event a visible recoil.
            Sprite[] shotFrames = { idleFrames[0], walkFrames[0], walkFrames[1], idleFrames[0] };
            spriteRenderer.sprite = shotFrames[shotPose];
            if (shootTimer > 0f) return;
        }

        bool moving = playerController != null && playerController.IsMoving;
        Sprite[] frames = moving ? walkFrames : idleFrames;
        if (moving != lastMoving)
        {
            lastMoving = moving;
            frameIndex = 0;
            frameTimer = 0f;
            spriteRenderer.sprite = frames[0];
        }

        frameTimer += Time.deltaTime;
        float interval = 1f / (moving ? WalkFramesPerSecond : IdleFramesPerSecond);
        while (frameTimer >= interval)
        {
            frameTimer -= interval;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        // The built-in Animator also writes SpriteRenderer.sprite. Apply the selected
        // skin last on every frame so the default clip cannot intermittently replace it.
        spriteRenderer.sprite = frames[frameIndex];
    }

    private bool TryLoadFrames(string skinId, out Sprite[] idle, out Sprite[] walk, out List<Sprite> generated)
    {
        idle = new Sprite[FrameCount];
        walk = new Sprite[FrameCount];
        generated = new List<Sprite>(FrameCount * 2);
        var idleTextures = new Texture2D[FrameCount];
        var walkTextures = new Texture2D[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            idleTextures[i] = Resources.Load<Texture2D>($"PlayerSkins/{skinId}/idle_{i:00}");
            walkTextures[i] = Resources.Load<Texture2D>($"PlayerSkins/{skinId}/walk_{i:00}");
            if (idleTextures[i] == null || walkTextures[i] == null)
            {
                Debug.LogError($"Player skin '{skinId}' is missing animation frame {i:00}.", this);
                return false;
            }
        }

        RectInt bounds = FindAlphaBounds(idleTextures, walkTextures);
        if (bounds.width <= 0 || bounds.height <= 0)
        {
            Debug.LogError($"Player skin '{skinId}' has no visible pixels.", this);
            return false;
        }

        float targetLocalHeight = originalSprite != null ? originalSprite.bounds.size.y : 2f;
        if (targetLocalHeight <= 0f) targetLocalHeight = 2f;
        float pixelsPerUnit = bounds.height / targetLocalHeight;
        Vector2 pivot = new Vector2(.5f, 0f);
        if (originalSprite != null && originalSprite.rect.width > 0f && originalSprite.rect.height > 0f)
        {
            pivot = new Vector2(
                originalSprite.pivot.x / originalSprite.rect.width,
                originalSprite.pivot.y / originalSprite.rect.height);
        }

        for (int i = 0; i < FrameCount; i++)
        {
            idle[i] = Sprite.Create(idleTextures[i], new Rect(bounds.x, bounds.y, bounds.width, bounds.height), pivot, pixelsPerUnit);
            walk[i] = Sprite.Create(walkTextures[i], new Rect(bounds.x, bounds.y, bounds.width, bounds.height), pivot, pixelsPerUnit);
            idle[i].name = $"{skinId}_idle_{i:00}";
            walk[i].name = $"{skinId}_walk_{i:00}";
            generated.Add(idle[i]);
            generated.Add(walk[i]);
        }
        return true;
    }

    private static RectInt FindAlphaBounds(Texture2D[] idleTextures, Texture2D[] walkTextures)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        ScanAlpha(idleTextures, ref minX, ref minY, ref maxX, ref maxY);
        ScanAlpha(walkTextures, ref minX, ref minY, ref maxX, ref maxY);
        if (maxX < minX || maxY < minY) return new RectInt(0, 0, 0, 0);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static void ScanAlpha(Texture2D[] textures, ref int minX, ref int minY, ref int maxX, ref int maxY)
    {
        foreach (Texture2D texture in textures)
        {
            Color32[] pixels;
            try { pixels = texture.GetPixels32(); }
            catch (UnityException)
            {
                Debug.LogError($"Skin frame '{texture.name}' must have Read/Write enabled in its texture import settings.");
                return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= 8) continue;
                int x = i % texture.width;
                int y = i / texture.width;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }
    }

    private void ReleaseGeneratedSprites()
    {
        if (createdSprites == null) return;
        foreach (Sprite sprite in createdSprites)
            if (sprite != null) Destroy(sprite);
        createdSprites = null;
    }

    public static Sprite LoadPreview(string skinId)
    {
        if (PreviewCache.TryGetValue(skinId, out Sprite cached) && cached != null) return cached;
        string path = skinId == PlayerSkinSystem.DefaultSkin
            ? "PlayerSkins/default_preview"
            : $"PlayerSkins/{skinId}/idle_00";
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;

        RectInt bounds = FindAlphaBounds(new[] { texture }, new Texture2D[0]);
        if (bounds.width <= 0 || bounds.height <= 0) return null;
        Sprite preview = Sprite.Create(texture, new Rect(bounds.x, bounds.y, bounds.width, bounds.height), new Vector2(.5f, 0f), 100f);
        preview.name = skinId + "_preview";
        PreviewCache[skinId] = preview;
        return preview;
    }

    public static Sprite[] LoadCutsceneFrames(string skinId, bool walking)
    {
        if (string.IsNullOrEmpty(skinId) || skinId == PlayerSkinSystem.DefaultSkin)
            return null;

        string motion = walking ? "walk" : "idle";
        string cacheKey = skinId + ":" + motion;
        if (CutsceneFrameCache.TryGetValue(cacheKey, out Sprite[] cached) && cached != null && cached.Length == FrameCount)
            return cached;

        Texture2D[] textures = new Texture2D[FrameCount];
        Texture2D[] pairedTextures = new Texture2D[FrameCount];
        string pairedMotion = walking ? "idle" : "walk";
        for (int i = 0; i < FrameCount; i++)
        {
            textures[i] = Resources.Load<Texture2D>($"PlayerSkins/{skinId}/{motion}_{i:00}");
            pairedTextures[i] = Resources.Load<Texture2D>($"PlayerSkins/{skinId}/{pairedMotion}_{i:00}");
            if (textures[i] == null)
            {
                Debug.LogError($"Player skin '{skinId}' is missing {motion} frame {i:00}.");
                return null;
            }
        }

        RectInt bounds = FindAlphaBounds(textures, pairedTextures);
        if (bounds.width <= 0 || bounds.height <= 0)
            return null;

        Sprite[] frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            frames[i] = Sprite.Create(textures[i], new Rect(bounds.x, bounds.y, bounds.width, bounds.height), new Vector2(.5f, 0f), 100f);
            frames[i].name = $"{skinId}_{motion}_cutscene_{i:00}";
            frames[i].hideFlags = HideFlags.HideAndDontSave;
        }

        CutsceneFrameCache[cacheKey] = frames;
        return frames;
    }

    private void OnDestroy()
    {
        ReleaseGeneratedSprites();
    }
}
