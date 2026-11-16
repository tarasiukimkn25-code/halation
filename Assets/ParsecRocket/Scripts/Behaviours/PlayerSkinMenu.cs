using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Lobby-only skin and language controls, built at runtime to fit desktop and mobile.</summary>
public sealed class PlayerSkinMenu : MonoBehaviour
{
    private RectTransform safeRoot;
    private Button languageButton;
    private Button skinsButton;
    private GameObject panel;
    private TMP_Text lobbyCrystalText;
    private TMP_Text crystalText;
    private TMP_Text feedbackText;
    private TMP_Text[] cardNameLabels = new TMP_Text[3];
    private TMP_Text[] cardStateLabels = new TMP_Text[3];
    private Button[] cardButtons = new Button[3];

    private void Start()
    {
        Build();
        Refresh();
    }

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += Refresh;
        MoonInventory.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= Refresh;
        MoonInventory.Changed -= Refresh;
    }

    private void Build()
    {
        Canvas canvas = GetComponent<Canvas>();
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 640);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        safeRoot = ParsecResponsiveUI.CreateSafeRoot(canvas);

        CreateLobbyCrystalBadge();
        languageButton = CreateButton(safeRoot, "Language toggle", new Vector2(1, 1), new Vector2(190, 60), new Vector2(-115, -38));
        languageButton.onClick.AddListener(GameLocalization.ToggleLanguage);

        skinsButton = CreateButton(safeRoot, "Skin selection", new Vector2(0, 0), new Vector2(220, 64), new Vector2(122, 40));
        skinsButton.onClick.AddListener(OpenPanel);

        panel = new GameObject("Skin selection panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(safeRoot, false);
        ParsecResponsiveUI.Stretch((RectTransform)panel.transform);
        panel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.055f, .90f);
        panel.SetActive(false);

        GameObject card = new GameObject("Skin selection card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panel.transform, false);
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.anchorMin = new Vector2(.04f, .12f);
        cardRect.anchorMax = new Vector2(.96f, .88f);
        cardRect.offsetMin = cardRect.offsetMax = Vector2.zero;
        card.GetComponent<Image>().color = new Color(.075f, .105f, .18f, .99f);

        AddText(card.transform, "Skin title", "", new Vector2(.12f, .86f), new Vector2(.88f, .98f), 34, FontStyles.Bold);
        crystalText = AddText(card.transform, "Skin crystal count", "", new Vector2(.08f, .78f), new Vector2(.75f, .87f), 24, FontStyles.Bold);
        Button close = CreateButton(card.transform, "Close skin selection", new Vector2(1, 1), new Vector2(64, 58), new Vector2(-44, -38));
        close.GetComponentInChildren<TMP_Text>().text = "×";
        close.onClick.AddListener(() => panel.SetActive(false));

        AddCard(card.transform, 0, PlayerSkinSystem.DefaultSkin, new Vector2(.025f, .08f), new Vector2(.32f, .77f));
        AddCard(card.transform, 1, PlayerSkinSystem.FreeSkin, new Vector2(.345f, .08f), new Vector2(.66f, .77f));
        AddCard(card.transform, 2, PlayerSkinSystem.PremiumSkin, new Vector2(.68f, .08f), new Vector2(.975f, .77f));
        feedbackText = AddText(card.transform, "Skin feedback", "", new Vector2(.1f, .01f), new Vector2(.9f, .09f), 22, FontStyles.Bold);
        Refresh();
    }

    private void CreateLobbyCrystalBadge()
    {
        GameObject badge = new GameObject("Lobby crystal balance", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(safeRoot, false);
        ParsecResponsiveUI.Place((RectTransform)badge.transform, new Vector2(0, 1), new Vector2(210, 68), new Vector2(112, -40));
        Image background = badge.GetComponent<Image>();
        background.color = new Color(.12f, .1f, .22f, .96f);
        background.raycastTarget = false;

        GameObject iconObject = new GameObject("Crystal icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(badge.transform, false);
        ParsecResponsiveUI.Place((RectTransform)iconObject.transform, new Vector2(0, .5f), new Vector2(48, 48), new Vector2(34, 0));
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = Resources.Load<Sprite>("MoonItems/Crystal");
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject textObject = new GameObject("Crystal balance", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(badge.transform, false);
        ParsecResponsiveUI.Place((RectTransform)textObject.transform, new Vector2(1, .5f), new Vector2(130, 52), new Vector2(-68, 0));
        lobbyCrystalText = textObject.GetComponent<TMP_Text>();
        lobbyCrystalText.fontSize = 30;
        lobbyCrystalText.fontSizeMin = 22;
        lobbyCrystalText.fontSizeMax = 30;
        lobbyCrystalText.enableAutoSizing = true;
        lobbyCrystalText.alignment = TextAlignmentOptions.MidlineLeft;
        lobbyCrystalText.raycastTarget = false;
    }

    private void AddCard(Transform parent, int index, string skinId, Vector2 min, Vector2 max)
    {
        GameObject card = new GameObject("Skin card " + skinId, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)card.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        card.GetComponent<Image>().color = new Color(.12f, .17f, .27f, .98f);

        GameObject previewObject = new GameObject("Preview", typeof(RectTransform), typeof(Image));
        previewObject.transform.SetParent(card.transform, false);
        RectTransform previewRect = (RectTransform)previewObject.transform;
        previewRect.anchorMin = new Vector2(.14f, .39f);
        previewRect.anchorMax = new Vector2(.86f, .92f);
        previewRect.offsetMin = previewRect.offsetMax = Vector2.zero;
        Image preview = previewObject.GetComponent<Image>();
        preview.sprite = PlayerSkinSystem.LoadPreview(skinId);
        preview.preserveAspect = true;
        preview.raycastTarget = false;

        TMP_Text name = AddText(card.transform, "Name", PlayerSkinSystem.DisplayName(skinId), new Vector2(.04f, .28f), new Vector2(.96f, .40f), 24, FontStyles.Bold);
        name.enableAutoSizing = true;
        cardNameLabels[index] = name;
        TMP_Text state = AddText(card.transform, "State", "", new Vector2(.04f, .19f), new Vector2(.96f, .29f), 18, FontStyles.Normal);
        state.enableAutoSizing = true;
        cardStateLabels[index] = state;

        Button button = CreateButton(card.transform, "Choose " + skinId, new Vector2(.5f, 0), new Vector2(220, 58), new Vector2(0, 38));
        RectTransform buttonRect = (RectTransform)button.transform;
        buttonRect.anchorMin = new Vector2(.04f, 0);
        buttonRect.anchorMax = new Vector2(.96f, 0);
        buttonRect.sizeDelta = new Vector2(0, 58);
        buttonRect.anchoredPosition = new Vector2(0, 38);
        cardButtons[index] = button;
        button.onClick.AddListener(() => HandleCardButton(skinId));
    }

    private void HandleCardButton(string skinId)
    {
        if (skinId == PlayerSkinSystem.PremiumSkin && !PlayerSkinSystem.IsPremiumOwned)
        {
            if (!PlayerSkinSystem.PurchasePremium())
            {
                if (feedbackText != null) feedbackText.text = GameLocalization.T("skin_need_crystals");
                return;
            }
        }
        else if (!PlayerSkinSystem.Select(skinId))
        {
            return;
        }

        if (feedbackText != null)
            feedbackText.text = string.Format(GameLocalization.T("skin_selected"), PlayerSkinSystem.DisplayName(skinId));
        Refresh();
    }

    private void OpenPanel()
    {
        if (!PlayerSkinSystem.IsSkinMenuUnlocked) return;
        feedbackText.text = string.Empty;
        panel.SetActive(true);
        Refresh();
    }

    private void Refresh()
    {
        if (languageButton != null)
            languageButton.GetComponentInChildren<TMP_Text>().text = GameLocalization.T("language_button");
        if (lobbyCrystalText != null)
            lobbyCrystalText.text = "× " + MoonInventory.Crystals;
        if (skinsButton != null)
        {
            bool unlocked = PlayerSkinSystem.IsSkinMenuUnlocked;
            skinsButton.interactable = unlocked;
            skinsButton.GetComponentInChildren<TMP_Text>().text = GameLocalization.T(unlocked ? "skins" : "skins_locked");
        }
        if (panel == null) return;

        TMP_Text title = panel.transform.Find("Skin selection card/Skin title")?.GetComponent<TMP_Text>();
        if (title != null) title.text = GameLocalization.T("skin_title");
        if (crystalText != null) crystalText.text = string.Format(GameLocalization.T("crystals_count"), MoonInventory.Crystals);
        if (feedbackText != null && string.IsNullOrEmpty(feedbackText.text))
        {
            if (!PlayerSkinSystem.IsSkinMenuUnlocked)
                feedbackText.text = GameLocalization.T("skin_unlock_hint");
            else if (PlayerSkinSystem.SelectedSkin != PlayerSkinSystem.DefaultSkin)
                feedbackText.text = string.Format(GameLocalization.T("skin_selected"), PlayerSkinSystem.DisplayName(PlayerSkinSystem.SelectedSkin));
        }

        RefreshCard(0, PlayerSkinSystem.DefaultSkin, "skin_default_desc");
        RefreshCard(1, PlayerSkinSystem.FreeSkin, "skin_free_desc");
        RefreshCard(2, PlayerSkinSystem.PremiumSkin, "skin_paid_desc");
    }

    private void RefreshCard(int index, string skinId, string descriptionKey)
    {
        bool owned = PlayerSkinSystem.IsOwned(skinId);
        bool selected = PlayerSkinSystem.SelectedSkin == skinId;
        if (cardStateLabels[index] != null)
        {
            string text = GameLocalization.T(descriptionKey);
            if (selected) text += "\n" + GameLocalization.T("skin_active");
            else if (skinId == PlayerSkinSystem.PremiumSkin && owned) text += "\n" + GameLocalization.T("skin_owned");
            cardStateLabels[index].text = text;
        }
        if (cardNameLabels[index] != null)
            cardNameLabels[index].text = PlayerSkinSystem.DisplayName(skinId);
        if (cardButtons[index] == null) return;
        cardButtons[index].interactable = !selected && (owned || PlayerSkinSystem.IsSkinMenuUnlocked);
        TMP_Text label = cardButtons[index].GetComponentInChildren<TMP_Text>();
        if (label == null) return;
        label.text = selected ? GameLocalization.T("skin_active") :
            skinId == PlayerSkinSystem.PremiumSkin && !owned
                ? GameLocalization.T("skin_buy")
                : GameLocalization.T("skin_select");
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        ParsecResponsiveUI.Place((RectTransform)buttonObject.transform, anchor, size, position);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(.12f, .23f, .39f, .98f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        TMP_Text label = AddText(buttonObject.transform, "Label", "", Vector2.zero, Vector2.one, 24, FontStyles.Bold);
        label.enableAutoSizing = true;
        label.fontSizeMin = 17;
        label.fontSizeMax = 28;
        return button;
    }

    private static TMP_Text AddText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, float size, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = style;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16;
        label.fontSizeMax = size;
        label.color = Color.white;
        label.margin = new Vector4(8, 4, 8, 4);
        label.raycastTarget = false;
        return label;
    }
}
