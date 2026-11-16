using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Short visual arrival that plays when the player enters the moon base.</summary>
public sealed class RocketArrivalCutscene : MonoBehaviour
{
    private static bool queued;

    private RectTransform safeRoot;
    private RectTransform rocketRect;
    private RectTransform doorLeft;
    private RectTransform doorRight;
    private RectTransform astronautRect;
    private Image astronautImage;
    private Sprite[] astronautWalkFrames;
    private CanvasGroup cutsceneGroup;
    private PlayerController player;
    private bool playerWasEnabled;
    private float previousTimeScale = 1f;
    private bool pausedWorld;

    public static void QueueForNextEntry() => queued = true;

    public static void PlayIfQueued()
    {
        if (!queued) return;
        queued = false;
        new GameObject("Rocket arrival cutscene").AddComponent<RocketArrivalCutscene>();
    }

    private void Start()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        // Let the scene player apply the saved skin before taking its preview sprite.
        yield return null;
        yield return new WaitForEndOfFrame();

        player = FindFirstObjectByType<PlayerController>();
        RocketBehaviour rocket = FindFirstObjectByType<RocketBehaviour>();
        SpriteRenderer rocketRenderer = rocket != null ? rocket.GetComponentInChildren<SpriteRenderer>(true) : null;
        SpriteRenderer playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        if (rocketRenderer == null || rocketRenderer.sprite == null || player == null)
        {
            Destroy(gameObject);
            yield break;
        }

        playerWasEnabled = player.enabled;
        player.enabled = false;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedWorld = true;

        BuildScene(rocketRenderer.sprite, playerRenderer != null ? playerRenderer.sprite : null);
        Canvas.ForceUpdateCanvases();
        float rootHeight = Mathf.Max(1f, safeRoot.rect.height);
        float rocketHeight = rocketRect.rect.height;
        float groundHeight = safeRoot.rect.height * .36f;
        float touchdownY = (groundHeight * .23f + rocketHeight * .5f) / rootHeight;
        Vector2 start = new Vector2(.5f, 1.22f);
        Vector2 touchdown = new Vector2(.5f, touchdownY);
        SetNormalizedPosition(rocketRect, start);
        yield return Fade(0f, 1f, .35f);

        float elapsed = 0f;
        const float landingDuration = 3.2f;
        while (elapsed < landingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / landingDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float wobble = Mathf.Sin(progress * Mathf.PI * 2f) * .003f;
            SetNormalizedPosition(rocketRect, Vector2.Lerp(start, touchdown, eased) + new Vector2(0, wobble));
            rocketRect.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(progress * Mathf.PI * 2f) * .7f * (1f - progress));
            yield return null;
        }

        SetNormalizedPosition(rocketRect, touchdown);
        rocketRect.localRotation = Quaternion.identity;
        yield return BounceRocket();

        yield return AnimateDoor(0f, 1f, .62f);
        if (astronautRect != null && astronautRect.gameObject.activeSelf)
            yield return WalkDownRamp(touchdown, rocketHeight);
        yield return new WaitForSecondsRealtime(.25f);
        yield return Fade(1f, 0f, .8f);

        if (player != null) player.enabled = playerWasEnabled;
        Time.timeScale = previousTimeScale;
        pausedWorld = false;
        Destroy(gameObject);
    }

    private void BuildScene(Sprite rocketSprite, Sprite fallbackPlayerSprite)
    {
        GameObject canvasObject = new GameObject("Arrival overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 640);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        safeRoot = new GameObject("Full screen cinematic content", typeof(RectTransform)).GetComponent<RectTransform>();
        safeRoot.SetParent(canvas.transform, false);
        ParsecResponsiveUI.Stretch(safeRoot);
        Canvas.ForceUpdateCanvases();

        GameObject backgroundObject = new GameObject("Arrival sky", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        backgroundObject.transform.SetParent(safeRoot, false);
        RectTransform backgroundRect = (RectTransform)backgroundObject.transform;
        ParsecResponsiveUI.Stretch(backgroundRect);
        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(.018f, .032f, .075f, 1f);
        background.raycastTarget = true;
        cutsceneGroup = backgroundObject.GetComponent<CanvasGroup>();
        cutsceneGroup.alpha = 0f;
        cutsceneGroup.blocksRaycasts = true;
        cutsceneGroup.interactable = true;

        CreateStars(backgroundObject.transform);
        CreateGround(backgroundObject.transform);

        GameObject rocketObject = new GameObject("Incoming rocket", typeof(RectTransform), typeof(Image));
        rocketObject.transform.SetParent(backgroundObject.transform, false);
        rocketRect = (RectTransform)rocketObject.transform;
        rocketRect.anchorMin = rocketRect.anchorMax = new Vector2(.5f, .5f);
        rocketRect.pivot = new Vector2(.5f, .5f);
        float aspect = rocketSprite.bounds.size.x / Mathf.Max(.01f, rocketSprite.bounds.size.y);
        float height = Mathf.Min(safeRoot.rect.height * .68f, safeRoot.rect.width * .8f / Mathf.Max(.01f, aspect));
        height = Mathf.Min(height, 600f);
        float width = height * aspect;
        rocketRect.sizeDelta = new Vector2(width, height);
        Image rocketImage = rocketObject.GetComponent<Image>();
        rocketImage.sprite = rocketSprite;
        rocketImage.preserveAspect = true;
        rocketImage.raycastTarget = false;
        rocketImage.color = Color.white;

        CreateHatch();

        string selectedSkin = PlayerSkinSystem.SelectedSkin;
        Sprite astronautSprite = PlayerSkinSystem.LoadPreview(selectedSkin);
        if (astronautSprite == null) astronautSprite = fallbackPlayerSprite;
        astronautWalkFrames = PlayerSkinAnimator.LoadCutsceneFrames(selectedSkin, true);
        if (astronautSprite != null)
        {
            GameObject astronautObject = new GameObject("Traveler leaving the rocket", typeof(RectTransform), typeof(Image));
            astronautObject.transform.SetParent(backgroundObject.transform, false);
            astronautRect = (RectTransform)astronautObject.transform;
            astronautRect.anchorMin = astronautRect.anchorMax = new Vector2(.5f, .5f);
            astronautRect.pivot = new Vector2(.5f, 0f);
            astronautRect.sizeDelta = new Vector2(104f, 156f);
            astronautImage = astronautObject.GetComponent<Image>();
            astronautImage.sprite = astronautSprite;
            astronautImage.preserveAspect = true;
            astronautImage.raycastTarget = false;
            astronautObject.SetActive(false);
        }

    }

    private void CreateStars(Transform parent)
    {
        System.Random random = new System.Random(73);
        for (int i = 0; i < 40; i++)
        {
            GameObject star = new GameObject("Arrival star " + (i + 1), typeof(RectTransform), typeof(Image));
            star.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)star.transform;
            Vector2 point = new Vector2(.03f + (float)random.NextDouble() * .94f, .38f + (float)random.NextDouble() * .61f);
            PlaceNormalized(rect, point, new Vector2(i % 6 == 0 ? 4f : 2f, i % 6 == 0 ? 4f : 2f));
            Image image = star.GetComponent<Image>();
            image.color = new Color(.72f, .86f, 1f, i % 3 == 0 ? .9f : .5f);
            image.raycastTarget = false;
        }
    }

    private void CreateGround(Transform parent)
    {
        GameObject ground = new GameObject("Moon landing ground", typeof(RectTransform), typeof(Image));
        ground.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)ground.transform;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.sizeDelta = new Vector2(0, safeRoot.rect.height * .36f);
        Image image = ground.GetComponent<Image>();
        image.sprite = RocketLaunchMinigame.GetLunarGroundSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        GameObject pad = new GameObject("Landing pad", typeof(RectTransform), typeof(Image));
        pad.transform.SetParent(parent, false);
        RectTransform padRect = (RectTransform)pad.transform;
        padRect.anchorMin = padRect.anchorMax = new Vector2(.5f, 0);
        padRect.pivot = new Vector2(.5f, .5f);
        padRect.sizeDelta = new Vector2(300f, 18f);
        padRect.anchoredPosition = new Vector2(0, safeRoot.rect.height * .083f);
        Image padImage = pad.GetComponent<Image>();
        padImage.color = new Color(.55f, .62f, .71f, 1f);
        padImage.raycastTarget = false;
    }

    private void CreateHatch()
    {
        GameObject frame = new GameObject("Rocket hatch frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(rocketRect, false);
        RectTransform frameRect = (RectTransform)frame.transform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(.58f, .42f);
        frameRect.pivot = new Vector2(.5f, .5f);
        frameRect.sizeDelta = new Vector2(rocketRect.sizeDelta.x * .22f, rocketRect.sizeDelta.y * .19f);
        frame.GetComponent<Image>().color = new Color(.67f, .75f, .84f, 1f);
        frame.GetComponent<Image>().raycastTarget = false;

        GameObject opening = new GameObject("Hatch interior", typeof(RectTransform), typeof(Image));
        opening.transform.SetParent(frame.transform, false);
        RectTransform openingRect = (RectTransform)opening.transform;
        ParsecResponsiveUI.Stretch(openingRect);
        openingRect.offsetMin = new Vector2(5, 5);
        openingRect.offsetMax = new Vector2(-5, -5);
        opening.GetComponent<Image>().color = new Color(.07f, .11f, .18f, 1f);
        opening.GetComponent<Image>().raycastTarget = false;

        doorLeft = CreateDoorPanel(frame.transform, new Vector2(.25f, .5f));
        doorRight = CreateDoorPanel(frame.transform, new Vector2(.75f, .5f));
        doorLeft.sizeDelta = new Vector2(frameRect.sizeDelta.x * .48f, frameRect.sizeDelta.y * .78f);
        doorRight.sizeDelta = doorLeft.sizeDelta;
        doorLeft.anchoredPosition = new Vector2(doorLeft.sizeDelta.x * .25f, 0);
        doorRight.anchoredPosition = new Vector2(-doorRight.sizeDelta.x * .25f, 0);
    }

    private static RectTransform CreateDoorPanel(Transform parent, Vector2 anchor)
    {
        GameObject panel = new GameObject("Sliding hatch panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(.5f, .5f);
        panel.GetComponent<Image>().color = new Color(.46f, .56f, .68f, 1f);
        panel.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    private IEnumerator WalkDownRamp(Vector2 touchdown, float rocketHeight)
    {
        astronautRect.gameObject.SetActive(true);
        float rootHeight = Mathf.Max(1f, safeRoot.rect.height);
        Vector2 startPosition = touchdown + new Vector2(rocketRect.sizeDelta.x * .075f / safeRoot.rect.width, -rocketHeight * .13f / rootHeight);
        float groundY = (safeRoot.rect.height * .36f * .23f + astronautRect.sizeDelta.y * .5f) / rootHeight;
        Vector2 endPosition = new Vector2(.67f, groundY);
        SetNormalizedPosition(astronautRect, startPosition);

        float elapsed = 0f;
        float frameElapsed = 0f;
        int frameIndex = 0;
        const float duration = 1.7f;
        if (astronautWalkFrames != null && astronautWalkFrames.Length > 0)
            astronautImage.sprite = astronautWalkFrames[0];
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            frameElapsed += Time.unscaledDeltaTime;
            if (astronautWalkFrames != null && astronautWalkFrames.Length > 0 && frameElapsed >= .1f)
            {
                int framesToAdvance = Mathf.FloorToInt(frameElapsed / .1f);
                frameElapsed -= framesToAdvance * .1f;
                frameIndex = (frameIndex + framesToAdvance) % astronautWalkFrames.Length;
                astronautImage.sprite = astronautWalkFrames[frameIndex];
            }
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector2 position = Vector2.Lerp(startPosition, endPosition, eased);
            position.y += Mathf.Abs(Mathf.Sin(progress * 5f * Mathf.PI)) * .006f;
            SetNormalizedPosition(astronautRect, position);
            astronautRect.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(progress * 9f * Mathf.PI) * 2f);
            yield return null;
        }

        SetNormalizedPosition(astronautRect, endPosition);
        astronautRect.localRotation = Quaternion.identity;
    }

    private IEnumerator BounceRocket()
    {
        Vector3 original = rocketRect.localScale;
        float elapsed = 0f;
        const float duration = .4f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            rocketRect.localScale = new Vector3(original.x * (1f + pulse * .01f), original.y * (1f - pulse * .01f), 1f);
            yield return null;
        }
        rocketRect.localScale = original;
    }

    private IEnumerator AnimateDoor(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetDoor(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress)));
            yield return null;
        }
        SetDoor(to);
    }

    private void SetDoor(float amount)
    {
        if (doorLeft == null || doorRight == null) return;
        float openDistance = doorLeft.sizeDelta.x * .68f * Mathf.Clamp01(amount);
        doorLeft.anchoredPosition = new Vector2(doorLeft.sizeDelta.x * .25f - openDistance, 0);
        doorRight.anchoredPosition = new Vector2(-doorRight.sizeDelta.x * .25f + openDistance, 0);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cutsceneGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cutsceneGroup.alpha = to;
    }

    private void SetNormalizedPosition(RectTransform rect, Vector2 point)
    {
        rect.anchoredPosition = new Vector2((point.x - .5f) * safeRoot.rect.width, (point.y - .5f) * safeRoot.rect.height);
    }

    private void PlaceNormalized(RectTransform rect, Vector2 point, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        SetNormalizedPosition(rect, point);
    }

    private void OnDestroy()
    {
        if (!pausedWorld) return;
        if (player != null) player.enabled = playerWasEnabled;
        Time.timeScale = previousTimeScale;
    }
}
