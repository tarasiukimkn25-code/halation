using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RocketLaunchMinigame : MonoBehaviour
{
    private static bool openFromLevelMenu;

    private static readonly Color[] CircuitColors =
    {
        new Color(1f, .78f, .08f),
        new Color(.08f, .36f, 1f),
        new Color(.12f, .91f, .91f)
    };
    private static readonly float[] BarHeights = { .755f, .525f, .29f };
    private static readonly float[] ButtonHeights = { .68f, .45f, .215f };

    private GameObject canvasObject;
    private RectTransform safeRoot;
    private RectTransform boardRect;
    private RectTransform[] barRects;
    private RectTransform[] fillRects;
    private RectTransform[] buttonRects;
    private Button[] circuitButtons;
    private TextMeshProUGUI statusText;
    private GameObject successPanel;
    private RectTransform successCard;
    private TextMeshProUGUI successText;
    private Button successButton;
    private Button closeButton;
    private GameObject cutsceneRoot;
    private RectTransform cutsceneRocketRect;
    private Image cutsceneRocketImage;
    private RectTransform cutsceneAstronautRect;
    private Image cutsceneAstronautImage;
    private Sprite[] cutsceneAstronautWalkFrames;
    private RectTransform doorFrameRect;
    private RectTransform doorLeftRect;
    private RectTransform doorRightRect;
    private RectTransform lunarGroundRect;
    private RectTransform launchPadBaseRect;
    private RectTransform launchPadTopRect;
    private RectTransform[] launchPadStripes;
    private RectTransform[] launchFlames;
    private Image[] launchFlameImages;
    private Coroutine cutsceneRoutine;
    private PlayerController player;
    private bool playerWasEnabled;
    private bool openedFromMenu;
    private bool completed;
    private int nextCircuit;
    private Vector2 lastSafeSize;
    private static Sprite lunarGroundSprite;

    private void Start()
    {
        GameLocalization.LanguageChanged += RefreshLocalizedText;
        PrepareUI();
        bool launchFromMenu = openFromLevelMenu;
        openFromLevelMenu = false;
        if (launchFromMenu && MoonInventory.Delivered)
            Open(true);
    }

    private void OnDestroy()
    {
        GameLocalization.LanguageChanged -= RefreshLocalizedText;
    }

    private void RefreshLocalizedText()
    {
        if (statusText != null)
        {
            if (completed) statusText.text = GameLocalization.T("minigame_all_charged");
            else if (nextCircuit == 0) statusText.text = GameLocalization.T("minigame_instructions");
            else statusText.text = string.Format(GameLocalization.T("minigame_circuit_charged"), nextCircuit);
        }
        if (successText != null)
            successText.text = GameLocalization.T(completed ? "flight_success" : "flight_success_short");
        if (successButton != null)
        {
            TextMeshProUGUI label = successButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = GameLocalization.T(openedFromMenu ? "level_menu" : "return_lobby");
        }
    }

    private void Update()
    {
        if (canvasObject == null || !canvasObject.activeInHierarchy)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void LateUpdate()
    {
        if (safeRoot == null)
            return;

        Vector2 currentSize = safeRoot.rect.size;
        if ((currentSize - lastSafeSize).sqrMagnitude < .25f)
            return;

        lastSafeSize = currentSize;
        ResizeBoard();
    }

    public static void OpenFromLevelMenu()
    {
        if (!MoonInventory.Delivered)
            return;

        openFromLevelMenu = true;
        SceneManager.LoadScene("MainScene");
    }

    public void OpenFromRocket()
    {
        if (MoonInventory.Delivered)
            Open(false);
    }

    private void PrepareUI()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject("Rocket lock minigame", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 960);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        GameObject dim = new GameObject("Dim background", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvasObject.transform, false);
        ParsecResponsiveUI.Stretch((RectTransform)dim.transform);
        Image dimImage = dim.GetComponent<Image>();
        dimImage.color = new Color(.015f, .025f, .055f, .86f);
        dimImage.raycastTarget = true;

        safeRoot = ParsecResponsiveUI.CreateSafeRoot(canvas);
        CreateBoard();
        CreateCloseButton();
        CreateSuccessPanel();
        CreateLaunchCutscene();
        canvasObject.SetActive(false);
    }

    private void CreateBoard()
    {
        GameObject board = new GameObject("Rocket lock control panel", typeof(RectTransform), typeof(Image));
        board.transform.SetParent(safeRoot, false);
        boardRect = (RectTransform)board.transform;
        boardRect.anchorMin = boardRect.anchorMax = new Vector2(.5f, .5f);
        boardRect.pivot = new Vector2(.5f, .5f);
        boardRect.anchoredPosition = Vector2.zero;
        boardRect.sizeDelta = new Vector2(900, 900);

        Image boardImage = board.GetComponent<Image>();
        boardImage.sprite = Resources.Load<Sprite>("MoonItems/RocketLockPuzzle");
        boardImage.preserveAspect = true;
        boardImage.raycastTarget = false;
        if (boardImage.sprite == null)
            Debug.LogError("RocketLockPuzzle sprite is missing from Resources/MoonItems.");

        barRects = new RectTransform[3];
        fillRects = new RectTransform[3];
        buttonRects = new RectTransform[3];
        circuitButtons = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject bar = new GameObject("Circuit " + (i + 1) + " progress track", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(board.transform, false);
            barRects[i] = (RectTransform)bar.transform;
            Image barImage = bar.GetComponent<Image>();
            barImage.color = new Color(.012f, .016f, .024f, .99f);
            barImage.raycastTarget = false;

            GameObject fill = new GameObject("Circuit " + (i + 1) + " charge", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            fillRects[i] = (RectTransform)fill.transform;
            fillRects[i].anchorMin = Vector2.zero;
            fillRects[i].anchorMax = new Vector2(0, 1);
            fillRects[i].pivot = new Vector2(0, .5f);
            fillRects[i].offsetMin = Vector2.zero;
            fillRects[i].offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = CircuitColors[i];
            fillImage.raycastTarget = false;

            GameObject buttonObject = new GameObject("Circuit " + (i + 1) + " touch button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(board.transform, false);
            buttonRects[i] = (RectTransform)buttonObject.transform;
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 0);
            buttonImage.raycastTarget = true;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1, 1, 1, 0);
            colors.highlightedColor = new Color(1, 1, 1, .08f);
            colors.pressedColor = new Color(1, 1, 1, .18f);
            colors.selectedColor = new Color(1, 1, 1, .08f);
            colors.disabledColor = new Color(1, 1, 1, 0);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            int circuit = i;
            button.onClick.AddListener(() => ActivateCircuit(circuit));
            circuitButtons[i] = button;
        }

        GameObject statusPlate = new GameObject("Circuit status plate", typeof(RectTransform), typeof(Image));
        statusPlate.transform.SetParent(board.transform, false);
        Image statusPlateImage = statusPlate.GetComponent<Image>();
        statusPlateImage.color = new Color(.035f, .055f, .075f, .94f);
        statusPlateImage.raycastTarget = false;

        GameObject statusObject = new GameObject("Circuit status", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(statusPlate.transform, false);
        ParsecResponsiveUI.Stretch((RectTransform)statusObject.transform);
        statusText = statusObject.GetComponent<TextMeshProUGUI>();
        statusText.text = GameLocalization.T("minigame_instructions");
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 15;
        statusText.fontSizeMax = 28;
        statusText.color = Color.white;
        statusText.margin = new Vector4(8, 2, 8, 2);
        statusText.raycastTarget = false;
    }

    private void CreateCloseButton()
    {
        closeButton = CreateTextButton(safeRoot, "Close minigame", "×", new Vector2(76, 68));
        ParsecResponsiveUI.Place((RectTransform)closeButton.transform, new Vector2(0, 1), new Vector2(76, 68), new Vector2(42, -38));
        closeButton.onClick.AddListener(Close);
    }

    private void CreateSuccessPanel()
    {
        successPanel = new GameObject("Flight success", typeof(RectTransform), typeof(Image));
        successPanel.transform.SetParent(safeRoot, false);
        ParsecResponsiveUI.Stretch((RectTransform)successPanel.transform);
        Image shade = successPanel.GetComponent<Image>();
        shade.color = new Color(.015f, .025f, .055f, .82f);
        shade.raycastTarget = true;

        GameObject card = new GameObject("Flight success card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(successPanel.transform, false);
        successCard = (RectTransform)card.transform;
        ParsecResponsiveUI.Place(successCard, new Vector2(.5f, .5f), new Vector2(540, 300), Vector2.zero);
        card.GetComponent<Image>().color = new Color(.08f, .13f, .2f, 1f);

        GameObject label = new GameObject("Flight success message", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(card.transform, false);
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(.06f, .36f);
        labelRect.anchorMax = new Vector2(.94f, .94f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        successText = label.GetComponent<TextMeshProUGUI>();
        successText.text = GameLocalization.T("flight_success");
        successText.alignment = TextAlignmentOptions.Center;
        successText.enableAutoSizing = true;
        successText.fontSizeMin = 24;
        successText.fontSizeMax = 40;
        successText.color = Color.white;
        successText.margin = new Vector4(12, 8, 12, 8);
        successText.raycastTarget = false;

        successButton = CreateTextButton(card.transform, "Finish flight", "Готово", new Vector2(240, 66));
        ParsecResponsiveUI.Place((RectTransform)successButton.transform, new Vector2(.5f, .18f), new Vector2(240, 66), Vector2.zero);
        successButton.onClick.AddListener(Close);
        successPanel.SetActive(false);
    }

    private void CreateLaunchCutscene()
    {
        cutsceneRoot = new GameObject("Rocket launch cutscene", typeof(RectTransform), typeof(Image));
        cutsceneRoot.transform.SetParent(safeRoot, false);
        ParsecResponsiveUI.Stretch((RectTransform)cutsceneRoot.transform);
        Image background = cutsceneRoot.GetComponent<Image>();
        background.color = new Color(.018f, .032f, .075f, 1f);
        background.raycastTarget = true;

        System.Random random = new System.Random(47);
        for (int i = 0; i < 42; i++)
        {
            GameObject star = new GameObject("Launch star " + (i + 1), typeof(RectTransform), typeof(Image));
            star.transform.SetParent(cutsceneRoot.transform, false);
            RectTransform starRect = (RectTransform)star.transform;
            float x = .04f + (float)random.NextDouble() * .92f;
            float y = .12f + (float)random.NextDouble() * .78f;
            ParsecResponsiveUI.Place(starRect, new Vector2(x, y), new Vector2(i % 7 == 0 ? 4 : 2, i % 7 == 0 ? 4 : 2), Vector2.zero);
            Image starImage = star.GetComponent<Image>();
            starImage.color = new Color(.72f, .86f, 1f, i % 3 == 0 ? .9f : .48f);
            starImage.raycastTarget = false;
        }

        CreateLaunchGround();

        GameObject rocketObject = new GameObject("Scene rocket", typeof(RectTransform), typeof(Image));
        rocketObject.transform.SetParent(cutsceneRoot.transform, false);
        cutsceneRocketRect = (RectTransform)rocketObject.transform;
        ParsecResponsiveUI.Place(cutsceneRocketRect, new Vector2(.5f, .46f), Vector2.zero, Vector2.zero);
        cutsceneRocketImage = rocketObject.GetComponent<Image>();
        cutsceneRocketImage.preserveAspect = true;
        cutsceneRocketImage.raycastTarget = false;

        CreateRocketDoor();
        CreateLaunchFlames();
        CreateCutsceneAstronaut();
        cutsceneRoot.SetActive(false);
    }

    private void CreateLaunchGround()
    {
        GameObject ground = new GameObject("Moon launch ground", typeof(RectTransform), typeof(Image));
        ground.transform.SetParent(cutsceneRoot.transform, false);
        lunarGroundRect = (RectTransform)ground.transform;
        lunarGroundRect.anchorMin = new Vector2(0, 0);
        lunarGroundRect.anchorMax = new Vector2(1, 0);
        lunarGroundRect.pivot = new Vector2(.5f, 0);
        lunarGroundRect.anchoredPosition = Vector2.zero;
        Image groundImage = ground.GetComponent<Image>();
        groundImage.sprite = GetLunarGroundSprite();
        groundImage.type = Image.Type.Simple;
        groundImage.preserveAspect = false;
        groundImage.raycastTarget = false;

        GameObject padBase = new GameObject("Rocket launch pad base", typeof(RectTransform), typeof(Image));
        padBase.transform.SetParent(cutsceneRoot.transform, false);
        launchPadBaseRect = (RectTransform)padBase.transform;
        launchPadBaseRect.anchorMin = launchPadBaseRect.anchorMax = new Vector2(.5f, 0);
        launchPadBaseRect.pivot = new Vector2(.5f, 0);
        padBase.GetComponent<Image>().color = new Color(.17f, .22f, .3f, 1f);
        padBase.GetComponent<Image>().raycastTarget = false;

        GameObject padTop = new GameObject("Rocket launch pad surface", typeof(RectTransform), typeof(Image));
        padTop.transform.SetParent(cutsceneRoot.transform, false);
        launchPadTopRect = (RectTransform)padTop.transform;
        launchPadTopRect.anchorMin = launchPadTopRect.anchorMax = new Vector2(.5f, 0);
        launchPadTopRect.pivot = new Vector2(.5f, 0);
        padTop.GetComponent<Image>().color = new Color(.55f, .62f, .71f, 1f);
        padTop.GetComponent<Image>().raycastTarget = false;

        launchPadStripes = new RectTransform[6];
        for (int i = 0; i < launchPadStripes.Length; i++)
        {
            GameObject stripe = new GameObject("Launch pad safety marking " + (i + 1), typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(padTop.transform, false);
            launchPadStripes[i] = (RectTransform)stripe.transform;
            Image stripeImage = stripe.GetComponent<Image>();
            stripeImage.color = i % 2 == 0 ? new Color(1f, .68f, .12f, 1f) : new Color(.13f, .18f, .24f, 1f);
            stripeImage.raycastTarget = false;
        }
    }

    public static Sprite GetLunarGroundSprite()
    {
        if (lunarGroundSprite != null)
            return lunarGroundSprite;

        const int textureWidth = 512;
        const int textureHeight = 128;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "Generated lunar takeoff ground";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[textureWidth * textureHeight];
        for (int x = 0; x < textureWidth; x++)
        {
            float horizon = 29f + Mathf.Sin(x * .021f) * 5f + Mathf.Sin(x * .057f) * 3f + Mathf.Sin(x * .12f) * 1.5f;
            for (int y = 0; y < textureHeight; y++)
            {
                if (y > horizon)
                    continue;

                float depth = y / Mathf.Max(1f, horizon);
                float variation = Mathf.Sin(x * .17f + y * .21f) * .018f + Mathf.Sin(x * .071f - y * .11f) * .015f;
                float shade = Mathf.Lerp(.62f, .36f, depth) + variation;
                if (y >= horizon - 1f)
                    shade = .72f + variation;

                for (int c = 0; c < 4; c++)
                {
                    float craterX = c == 0 ? 64f : c == 1 ? 178f : c == 2 ? 331f : 454f;
                    float craterY = 12f + (c % 2) * 5f;
                    float radius = c % 2 == 0 ? 9f : 13f;
                    float distance = Mathf.Sqrt(Mathf.Pow((x - craterX) / radius, 2f) + Mathf.Pow((y - craterY) / (radius * .38f), 2f));
                    if (distance < .72f)
                        shade -= .13f;
                    else if (distance < 1f)
                        shade += .08f;
                }

                byte gray = (byte)Mathf.RoundToInt(Mathf.Clamp01(shade) * 255f);
                pixels[y * textureWidth + x] = new Color32(gray, (byte)(gray * .98f), (byte)Mathf.Min(255, gray + 10), 255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        lunarGroundSprite = Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(.5f, 0), 100f);
        lunarGroundSprite.name = "Generated lunar takeoff ground";
        lunarGroundSprite.hideFlags = HideFlags.HideAndDontSave;
        return lunarGroundSprite;
    }

    private void CreateRocketDoor()
    {
        GameObject frame = new GameObject("Rocket hatch frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(cutsceneRocketRect, false);
        doorFrameRect = (RectTransform)frame.transform;
        SetNormalizedPoint(doorFrameRect, new Vector2(.575f, .425f));
        frame.GetComponent<Image>().color = new Color(.66f, .73f, .82f, 1f);
        frame.GetComponent<Image>().raycastTarget = false;

        GameObject opening = new GameObject("Dark hatch opening", typeof(RectTransform), typeof(Image));
        opening.transform.SetParent(frame.transform, false);
        RectTransform openingRect = (RectTransform)opening.transform;
        openingRect.anchorMin = new Vector2(.12f, .08f);
        openingRect.anchorMax = new Vector2(.88f, .92f);
        openingRect.offsetMin = openingRect.offsetMax = Vector2.zero;
        opening.GetComponent<Image>().color = new Color(.025f, .04f, .08f, 1f);
        opening.GetComponent<Image>().raycastTarget = false;

        doorLeftRect = CreateDoorLeaf(frame.transform, "Left hatch door");
        doorRightRect = CreateDoorLeaf(frame.transform, "Right hatch door");
    }

    private RectTransform CreateDoorLeaf(Transform parent, string objectName)
    {
        GameObject leaf = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        leaf.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)leaf.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        Image image = leaf.GetComponent<Image>();
        image.color = new Color(.42f, .5f, .6f, 1f);
        image.raycastTarget = false;
        return rect;
    }

    private void CreateLaunchFlames()
    {
        launchFlames = new RectTransform[3];
        launchFlameImages = new Image[3];
        Color[] colors =
        {
            new Color(1f, .24f, .06f, .95f),
            new Color(1f, .65f, .12f, .98f),
            new Color(1f, .92f, .56f, 1f)
        };

        for (int i = 0; i < launchFlames.Length; i++)
        {
            GameObject flame = new GameObject("Rocket exhaust " + (i + 1), typeof(RectTransform), typeof(Image));
            flame.transform.SetParent(cutsceneRocketRect, false);
            launchFlames[i] = (RectTransform)flame.transform;
            launchFlames[i].anchorMin = launchFlames[i].anchorMax = new Vector2(.5f, .02f);
            launchFlames[i].pivot = new Vector2(.5f, 1f);
            launchFlameImages[i] = flame.GetComponent<Image>();
            launchFlameImages[i].color = colors[i];
            launchFlameImages[i].raycastTarget = false;
            flame.SetActive(false);
        }
    }

    private void CreateCutsceneAstronaut()
    {
        GameObject astronaut = new GameObject("Astronaut boarding", typeof(RectTransform), typeof(Image));
        astronaut.transform.SetParent(cutsceneRocketRect, false);
        cutsceneAstronautRect = (RectTransform)astronaut.transform;
        SetNormalizedPoint(cutsceneAstronautRect, new Vector2(.79f, .12f));
        cutsceneAstronautImage = astronaut.GetComponent<Image>();
        cutsceneAstronautImage.preserveAspect = true;
        cutsceneAstronautImage.raycastTarget = false;
    }

    private void ResizeCutscene()
    {
        if (cutsceneRocketRect == null || safeRoot == null)
            return;

        float height = safeRoot.rect.height * .78f;
        float width = Mathf.Min(height * .59f, safeRoot.rect.width * .9f);
        cutsceneRocketRect.sizeDelta = new Vector2(width, height);

        float screenHeight = safeRoot.rect.height;
        if (lunarGroundRect != null)
            lunarGroundRect.sizeDelta = new Vector2(0, screenHeight * .38f);
        if (launchPadBaseRect != null)
        {
            float padWidth = Mathf.Min(safeRoot.rect.width * .78f, screenHeight * .56f);
            float padY = screenHeight * .082f;
            launchPadBaseRect.sizeDelta = new Vector2(padWidth, screenHeight * .06f);
            launchPadBaseRect.anchoredPosition = new Vector2(0, padY);
            launchPadTopRect.sizeDelta = new Vector2(padWidth * .94f, screenHeight * .018f);
            launchPadTopRect.anchoredPosition = new Vector2(0, padY + screenHeight * .035f);

            float stripeWidth = padWidth * .018f;
            for (int i = 0; i < launchPadStripes.Length; i++)
            {
                RectTransform stripe = launchPadStripes[i];
                stripe.anchorMin = stripe.anchorMax = new Vector2(i < 3 ? .08f + i * .045f : .92f - (i - 3) * .045f, .62f);
                stripe.pivot = new Vector2(.5f, .5f);
                stripe.sizeDelta = new Vector2(stripeWidth, screenHeight * .014f);
                stripe.anchoredPosition = Vector2.zero;
            }
        }

        if (cutsceneAstronautRect != null)
            cutsceneAstronautRect.sizeDelta = new Vector2(height * .075f, height * .13f);

        if (doorFrameRect != null)
        {
            doorFrameRect.sizeDelta = new Vector2(width * .12f, height * .105f);
            float leafWidth = doorFrameRect.sizeDelta.x * .38f;
            float leafHeight = doorFrameRect.sizeDelta.y * .8f;
            doorLeftRect.sizeDelta = doorRightRect.sizeDelta = new Vector2(leafWidth, leafHeight);
            doorLeftRect.anchoredPosition = new Vector2(-leafWidth * .52f, 0);
            doorRightRect.anchoredPosition = new Vector2(leafWidth * .52f, 0);
        }

        if (launchFlames != null)
        {
            float[] widths = { .15f, .09f, .04f };
            float[] heights = { .15f, .12f, .09f };
            for (int i = 0; i < launchFlames.Length; i++)
            {
                launchFlames[i].sizeDelta = new Vector2(width * widths[i], height * heights[i]);
                launchFlames[i].anchoredPosition = new Vector2((i - 1) * width * .045f, -height * .015f);
            }
        }
    }

    private static void SetNormalizedPoint(RectTransform rect, Vector2 point)
    {
        rect.anchorMin = rect.anchorMax = point;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
    }

    private Button CreateTextButton(Transform parent, string name, string caption, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(.12f, .22f, .34f, .98f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        ParsecResponsiveUI.Stretch((RectTransform)textObject.transform);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = caption;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22;
        label.fontSizeMax = 34;
        label.color = Color.white;
        label.raycastTarget = false;
        ((RectTransform)buttonObject.transform).sizeDelta = size;
        return button;
    }

    private void ResizeBoard()
    {
        if (boardRect == null || safeRoot == null)
            return;

        float dimension = Mathf.Max(0, Mathf.Min(safeRoot.rect.width - 24, safeRoot.rect.height - 24));
        boardRect.sizeDelta = new Vector2(dimension, dimension);

        float[] progressHeights = BarHeights;
        float[] buttonHeights = ButtonHeights;
        for (int i = 0; i < 3; i++)
        {
            ParsecResponsiveUI.Place(barRects[i], new Vector2(.765f, progressHeights[i]), new Vector2(dimension * .245f, dimension * .034f), Vector2.zero);
            ParsecResponsiveUI.Place(buttonRects[i], new Vector2(.765f, buttonHeights[i]), new Vector2(dimension * .265f, dimension * .105f), Vector2.zero);
        }

        Transform plate = boardRect.Find("Circuit status plate");
        if (plate != null)
            ParsecResponsiveUI.Place((RectTransform)plate, new Vector2(.5f, .115f), new Vector2(dimension * .72f, dimension * .06f), Vector2.zero);

        if (successCard != null)
        {
            float cardWidth = Mathf.Min(540, Mathf.Max(300, safeRoot.rect.width - 32));
            successCard.sizeDelta = new Vector2(cardWidth, Mathf.Min(300, Mathf.Max(220, safeRoot.rect.height - 32)));
        }

        ResizeCutscene();
    }

    private void Open(bool fromMenu)
    {
        if (canvasObject == null)
            PrepareUI();

        openedFromMenu = fromMenu;
        completed = false;
        nextCircuit = 0;
        successPanel.SetActive(false);
        cutsceneRoot.SetActive(false);
        boardRect.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        successButton.interactable = true;
        statusText.gameObject.SetActive(true);
        statusText.text = GameLocalization.T("minigame_instructions");
        statusText.color = Color.white;
        foreach (RectTransform fill in fillRects)
            SetFill(fill, 0);
        foreach (Button button in circuitButtons)
            button.interactable = true;

        player = FindFirstObjectByType<PlayerController>();
        playerWasEnabled = player != null && player.enabled;
        if (player != null)
            player.enabled = false;

        canvasObject.SetActive(true);
        ResizeBoard();
    }

    private void ActivateCircuit(int circuit)
    {
        if (completed)
            return;

        if (circuit != nextCircuit)
        {
            nextCircuit = 0;
            foreach (RectTransform fill in fillRects)
                SetFill(fill, 0);
            statusText.text = GameLocalization.T("minigame_wrong_order");
            statusText.color = new Color(1f, .38f, .32f, 1f);
            return;
        }

        SetFill(fillRects[circuit], 1);
        nextCircuit++;
        if (nextCircuit >= circuitButtons.Length)
        {
            CompletePuzzle();
            return;
        }

        statusText.text = string.Format(GameLocalization.T("minigame_circuit_charged"), nextCircuit);
        statusText.color = CircuitColors[nextCircuit];
    }

    private static void SetFill(RectTransform fill, float amount)
    {
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
    }

    private void CompletePuzzle()
    {
        completed = true;
        ParsecProgress.CompleteLevel(3);
        foreach (Button button in circuitButtons)
            button.interactable = false;
        statusText.text = GameLocalization.T("minigame_all_charged");
        statusText.color = new Color(.55f, 1f, .72f, 1f);
        successButton.GetComponentInChildren<TextMeshProUGUI>().text = GameLocalization.T(openedFromMenu ? "level_menu" : "return_lobby");
        successText.text = GameLocalization.T("flight_success_short");
        successButton.interactable = false;
        successPanel.SetActive(true);
        cutsceneRoutine = StartCoroutine(PlayLaunchCutscene());
    }

    private IEnumerator PlayLaunchCutscene()
    {
        yield return new WaitForSecondsRealtime(.85f);

        SpriteRenderer rocketRenderer = GetComponent<SpriteRenderer>();
        if (rocketRenderer == null)
            rocketRenderer = GetComponentInChildren<SpriteRenderer>(true);
        SpriteRenderer astronautRenderer = player != null ? player.GetComponentInChildren<SpriteRenderer>(true) : null;

        if (rocketRenderer == null || rocketRenderer.sprite == null || astronautRenderer == null || astronautRenderer.sprite == null)
        {
            successText.text = GameLocalization.T("flight_success");
            successPanel.SetActive(true);
            successButton.interactable = true;
            cutsceneRoutine = null;
            yield break;
        }

        cutsceneRocketImage.sprite = rocketRenderer.sprite;
        string selectedSkin = PlayerSkinSystem.SelectedSkin;
        cutsceneAstronautImage.sprite = PlayerSkinSystem.LoadPreview(selectedSkin) ?? astronautRenderer.sprite;
        cutsceneAstronautWalkFrames = PlayerSkinAnimator.LoadCutsceneFrames(selectedSkin, true);
        ResizeCutscene();
        cutsceneRocketRect.anchoredPosition = Vector2.zero;
        cutsceneRocketRect.localScale = Vector3.one;
        SetNormalizedPoint(cutsceneAstronautRect, new Vector2(.79f, .12f));
        cutsceneAstronautRect.localScale = Vector3.one;
        cutsceneAstronautImage.color = Color.white;
        SetDoorOpen(0f);
        foreach (Image flame in launchFlameImages)
        {
            flame.gameObject.SetActive(false);
            flame.color = new Color(flame.color.r, flame.color.g, flame.color.b, 1f);
        }

        successPanel.SetActive(false);
        boardRect.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        cutsceneRoot.SetActive(true);

        Vector2 stairStart = new Vector2(.79f, .12f);
        Vector2 hatchPosition = new Vector2(.575f, .425f);
        float climbDuration = 2.1f;
        float elapsed = 0f;
        if (cutsceneAstronautWalkFrames != null && cutsceneAstronautWalkFrames.Length > 0)
            cutsceneAstronautImage.sprite = cutsceneAstronautWalkFrames[0];
        while (elapsed < climbDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (cutsceneAstronautWalkFrames != null && cutsceneAstronautWalkFrames.Length > 0)
            {
                int frameIndex = Mathf.FloorToInt(elapsed * 10f) % cutsceneAstronautWalkFrames.Length;
                cutsceneAstronautImage.sprite = cutsceneAstronautWalkFrames[frameIndex];
            }
            float progress = Mathf.Clamp01(elapsed / climbDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector2 position = Vector2.Lerp(stairStart, hatchPosition, eased);
            position.y += Mathf.Sin(progress * 34f) * .008f * (1f - progress);
            SetNormalizedPoint(cutsceneAstronautRect, position);
            cutsceneAstronautRect.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-7f, 0f, eased));

            float doorProgress = Mathf.InverseLerp(.48f, .78f, progress);
            SetDoorOpen(doorProgress);

            float fade = Mathf.InverseLerp(.88f, 1f, progress);
            cutsceneAstronautImage.color = new Color(1f, 1f, 1f, 1f - fade);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(.6f);

        elapsed = 0f;
        const float doorCloseDuration = .45f;
        while (elapsed < doorCloseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetDoorOpen(1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / doorCloseDuration)));
            yield return null;
        }
        SetDoorOpen(0f);

        foreach (Image flame in launchFlameImages)
            flame.gameObject.SetActive(true);

        float rocketStartY = cutsceneRocketRect.anchoredPosition.y;
        float rocketHeight = cutsceneRocketRect.rect.height;
        float launchDuration = 2.4f;
        elapsed = 0f;
        while (elapsed < launchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / launchDuration);
            float eased = progress * progress;
            cutsceneRocketRect.anchoredPosition = new Vector2(0, rocketStartY + eased * rocketHeight * 1.45f);
            float rocketScale = Mathf.Lerp(1f, .72f, progress);
            cutsceneRocketRect.localScale = new Vector3(rocketScale, rocketScale, 1f);

            float flamePulse = 1f + Mathf.Sin(elapsed * 24f) * .18f;
            for (int i = 0; i < launchFlames.Length; i++)
                launchFlames[i].localScale = new Vector3(1f, flamePulse * (1f + i * .08f), 1f);
            yield return null;
        }

        cutsceneRoot.SetActive(false);
        successText.text = GameLocalization.T("flight_success");
        successPanel.SetActive(true);
        successButton.interactable = true;
        cutsceneRoutine = null;
    }

    private void SetDoorOpen(float amount)
    {
        if (doorFrameRect == null)
            return;

        float openDistance = doorFrameRect.rect.width * .26f * Mathf.Clamp01(amount);
        doorLeftRect.anchoredPosition = new Vector2(-doorLeftRect.sizeDelta.x * .52f - openDistance, 0);
        doorRightRect.anchoredPosition = new Vector2(doorRightRect.sizeDelta.x * .52f + openDistance, 0);
    }

    private void Close()
    {
        if (cutsceneRoutine != null)
        {
            StopCoroutine(cutsceneRoutine);
            cutsceneRoutine = null;
        }

        if (openedFromMenu || completed)
        {
            openFromLevelMenu = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
            return;
        }

        if (player != null)
            player.enabled = playerWasEnabled;
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }
}
