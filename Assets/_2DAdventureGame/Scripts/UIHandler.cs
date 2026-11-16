using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class UIHandler : MonoBehaviour
{
    public static UIHandler instance { get; private set; }

    // НОВЕ (крок 12): поля репліки
    public float displayTime = 4.0f;
    public AudioSource audioSource;
    public AudioClip talkSound;

    private VisualElement m_NonPlayerDialogue;
    private VisualElement m_DialogueCard;
    private float m_TimerDisplay;

    private VisualElement m_WinScreen;
    private VisualElement m_LoseScreen;
    private VisualElement m_Root;
    private Button m_TalkButton;
    private Button m_DialogueCloseButton;
    private Image m_DialogueSpeakerPortrait;
    private Button m_EncyclopediaButton;
    private Button m_LevelMenuButton;
    private Button m_ClaimArtifactButton;
    private int m_PendingArtifact = -1;
    private bool m_WandererDialogue;
    private bool m_RocketDialogue;
    private Button m_EncyclopediaCloseButton;
    private VisualElement m_EncyclopediaPanel;
    private Label m_EncyclopediaCounter;
    private ScrollView m_EncyclopediaFactList;
    private Label m_DialogueText;
    private bool m_DialogueOpen;
    private readonly HashSet<string> m_LearnedFacts = new HashSet<string>();
    private readonly Dictionary<string, Sprite> m_NpcPortraits = new Dictionary<string, Sprite>();
    private bool m_EncyclopediaOpen;
    private Vector2 m_MobileMoveInput;
    private bool m_MobileShootPressed;
    private bool m_MobileShootHeld;
    private int m_ShootPointerId = -1;
    private float m_LastShootTriggerTime = -1f;
    private NonPlayerCharacter m_TalkTarget;
    private bool m_GameEnded;

    private Button m_MoveUpBtn;
    private Button m_MoveDownBtn;
    private Button m_MoveLeftBtn;
    private Button m_MoveRightBtn;
    private Button m_ShootBtn;

    // Контейнер лічильника кристалів (знаходимо автоматично)
    private VisualElement m_CrystalHudContainer;

    private sealed class MiniMapMarker
    {
        public Transform target;
        public VisualElement visual;
    }

    private VisualElement m_MiniMap;
    private VisualElement m_MiniMapMarkers;
    private PlayerController m_MiniMapPlayer;
    private readonly Dictionary<int, MiniMapMarker> m_MiniMapEntries = new Dictionary<int, MiniMapMarker>();
    private Bounds m_MiniMapBounds;
    private bool m_MiniMapHasBounds;
    private float m_NextMiniMapRefresh;

    // Віртуальний джойстик (кружочок)
    private VisualElement m_JoystickBase;
    private VisualElement m_JoystickKnob;
    private int m_JoystickPointerId = -1;
    private float m_JoystickRadius = 90f;
    private float m_KnobRadius = 38f;

    public Vector2 MobileMoveInput => m_MobileMoveInput;
    public bool MobileShootHeld => m_MobileShootHeld;

    private void Awake()
    {
        instance = this;
        GameLocalization.LanguageChanged += HandleLanguageChanged;
    }

    void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        m_Root = uiDocument.rootVisualElement;
        InitializeMiniMap();
        // Empty layout containers must not intercept buttons behind them.
        var mobileControls = m_Root.Q<VisualElement>("MobileControls");
        if (mobileControls != null) mobileControls.pickingMode = PickingMode.Ignore;

        // НОВЕ (крок 12): ховаємо репліку на старті та налаштовуємо адаптивність
        m_NonPlayerDialogue = m_Root.Q<VisualElement>("NPCDialogue");
        if (m_NonPlayerDialogue != null)
        {
            m_DialogueCard = m_NonPlayerDialogue.Q<VisualElement>("DialogueCard");
            m_NonPlayerDialogue.style.display = DisplayStyle.None;
            m_NonPlayerDialogue.style.position = Position.Absolute;
            m_NonPlayerDialogue.style.top = StyleKeyword.Auto;

            // Закриття при натисканні на саме вікно діалогу
            m_NonPlayerDialogue.RegisterCallback<PointerDownEvent>(evt => {
                var target = evt.target as VisualElement;
                if (m_ClaimArtifactButton != null && (target == m_ClaimArtifactButton || (target != null && m_ClaimArtifactButton.Contains(target)))) return;
                CloseDialogue();
            });
            m_ClaimArtifactButton = m_NonPlayerDialogue.Q<Button>("ClaimArtifactButton")
                ?? new Button { name = "ClaimArtifactButton", text = "Забрати артефакт" };
            m_ClaimArtifactButton.clicked += ClaimArtifact;
            m_ClaimArtifactButton.style.position = Position.Absolute;
            m_ClaimArtifactButton.style.bottom = 12;
            m_ClaimArtifactButton.style.right = 14;
            m_ClaimArtifactButton.style.width = Length.Percent(60);
            m_ClaimArtifactButton.style.maxWidth = 420;
            m_ClaimArtifactButton.style.height = 58;
            m_ClaimArtifactButton.style.fontSize = 22;
            m_ClaimArtifactButton.style.backgroundColor = new Color(.16f,.38f,.43f,.98f);
            m_ClaimArtifactButton.style.color = Color.white;
            SetCircleCorners(m_ClaimArtifactButton, 12);
            m_ClaimArtifactButton.style.display = DisplayStyle.None;
            var dialogueCard = m_NonPlayerDialogue.Q<VisualElement>("DialogueCard") ?? m_NonPlayerDialogue;
            dialogueCard.Add(m_ClaimArtifactButton);
        }

        m_DialogueText = m_NonPlayerDialogue?.Q<Label>("DialogueText");
        if (m_DialogueText != null)
        {
            m_DialogueText.pickingMode = PickingMode.Ignore;
            m_DialogueText.style.whiteSpace = WhiteSpace.Normal;
            m_DialogueText.style.unityTextAlign = TextAnchor.MiddleCenter;

            m_DialogueText.style.position = Position.Absolute;
            m_DialogueText.style.top = 10;
            m_DialogueText.style.bottom = 10;
            m_DialogueText.style.left = 14;
            m_DialogueText.style.right = 14;
            m_DialogueText.style.width = StyleKeyword.Auto;
            m_DialogueText.style.height = StyleKeyword.Auto;
        }

        // Приховуємо аватар NPC у діалозі
        m_DialogueSpeakerPortrait = m_NonPlayerDialogue?.Q<Image>("DialogueSpeakerPortrait");
        if (m_DialogueSpeakerPortrait != null)
        {
            m_DialogueSpeakerPortrait.scaleMode = ScaleMode.ScaleToFit;
            m_DialogueSpeakerPortrait.pickingMode = PickingMode.Ignore;
            m_DialogueSpeakerPortrait.style.display = DisplayStyle.None;
        }

        // Приховуємо хрестик закриття
        m_DialogueCloseButton = m_NonPlayerDialogue?.Q<Button>("DialogueCloseButton");
        if (m_DialogueCloseButton != null)
        {
            m_DialogueCloseButton.style.display = DisplayStyle.None;
            m_DialogueCloseButton.clicked += CloseDialogue;
        }

        m_TimerDisplay = -1.0f;

        m_TalkButton = m_Root.Q<Button>("TalkButton");
        if (m_TalkButton != null)
        {
            m_TalkButton.style.display = DisplayStyle.None;
            m_TalkButton.clicked += OnTalkButtonClicked;
        }

        m_LevelMenuButton = m_Root.Q<Button>("MoonLevelMenuButton")
            ?? new Button { name = "MoonLevelMenuButton", text = "Level menu" };
        m_LevelMenuButton.clicked += () => {
            Time.timeScale = 1f;
            MenuManagerBehaviour.OpenLevelSelectorOnNextStart();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
        };
        m_Root.Add(m_LevelMenuButton);
        m_LevelMenuButton.style.backgroundColor = new Color(.10f, .14f, .27f, .96f);
        m_LevelMenuButton.style.color = Color.white;
        SetCircleCorners(m_LevelMenuButton, 14f);
        m_EncyclopediaButton = m_Root.Q<Button>("MoonEncyclopediaButton");
        m_EncyclopediaPanel = m_Root.Q<VisualElement>("MoonEncyclopediaPanel");
        m_EncyclopediaCounter = m_Root.Q<Label>("MoonEncyclopediaCounter");
        m_EncyclopediaFactList = m_Root.Q<ScrollView>("MoonEncyclopediaFactList");
        m_EncyclopediaCloseButton = m_Root.Q<Button>("MoonEncyclopediaCloseButton");

        if (m_EncyclopediaButton != null)
            m_EncyclopediaButton.clicked += ToggleEncyclopedia;
        if (m_EncyclopediaCloseButton != null)
            m_EncyclopediaCloseButton.clicked += CloseEncyclopedia;

        FindCrystalHudElement();
        RefreshEncyclopedia();

        // Знаходимо старі кнопки руху, щоб приховати їх (замість них буде кружечок-джойстик)
        m_MoveUpBtn = m_Root.Q<Button>("MoveUp");
        m_MoveDownBtn = m_Root.Q<Button>("MoveDown");
        m_MoveLeftBtn = m_Root.Q<Button>("MoveLeft");
        m_MoveRightBtn = m_Root.Q<Button>("MoveRight");

        CreateVirtualJoystick();

        m_ShootBtn = m_Root.Q<Button>("ShootButton");
        SetupShootButton();

        // Автоматично підлаштовуємо діалог, верхні лічильники і мобільне керування під телефон або ПК
        ApplyResponsiveLayout();
        m_Root.RegisterCallback<GeometryChangedEvent>(evt => ApplyResponsiveLayout());

        m_LoseScreen = m_Root.Q<VisualElement>("LoseScreenContainer");
        m_WinScreen = m_Root.Q<VisualElement>("WinScreenContainer");
        var endScreen = m_Root.Q<VisualElement>("EndScreen");
        if (endScreen != null) endScreen.pickingMode = PickingMode.Ignore;
        if (m_WinScreen != null) m_WinScreen.style.display = DisplayStyle.None;
        if (m_LoseScreen != null) m_LoseScreen.style.display = DisplayStyle.None;
        ApplyLocalizedLabels();
    }

    private void ApplyLocalizedLabels()
    {
        if (m_LevelMenuButton != null) m_LevelMenuButton.text = GameLocalization.T("level_menu");
        if (m_TalkButton != null)
            m_TalkButton.text = GameLocalization.T(m_TalkTarget != null && m_TalkTarget.GetComponent<RocketBehaviour>() != null ? "unlock_flight" : "talk");
        if (m_EncyclopediaPanel != null)
        {
            Label title = m_EncyclopediaPanel.Q<Label>("MoonEncyclopediaTitle");
            if (title != null) title.text = GameLocalization.T("encyclopedia");
        }
        Button shoot = m_Root != null ? m_Root.Q<Button>("ShootButton") : null;
        if (shoot != null) shoot.tooltip = GameLocalization.T("shoot");
        RefreshEncyclopedia();
    }

    private void HandleLanguageChanged()
    {
        ApplyLocalizedLabels();
        if (!m_DialogueOpen || m_TalkTarget == null) return;
        if (m_WandererDialogue) ShowWandererDialogue();
        else if (m_RocketDialogue) ShowRocketDialogue();
        else DisplayDialogue();
    }

    private void FindCrystalHudElement()
    {
        if (m_Root == null)
            return;

        m_Root.Query<Label>().ForEach(lbl =>
        {
            if (m_CrystalHudContainer != null)
                return;

            string txt = lbl.text != null ? lbl.text.Trim() : string.Empty;
            if (txt.StartsWith("x ") || txt.StartsWith("х ") || (lbl.name != null && lbl.name.ToLower().Contains("crystal")))
            {
                m_CrystalHudContainer = lbl.parent != null && lbl.parent != m_Root
                    ? lbl.parent
                    : lbl;
            }
        });
    }

    private void InitializeMiniMap()
    {
        m_MiniMap = m_Root?.Q<VisualElement>("MoonMiniMap");
        m_MiniMapMarkers = m_MiniMap?.Q<VisualElement>("MoonMiniMapMarkers");
        if (m_MiniMap == null || m_MiniMapMarkers == null)
            return;

        m_MiniMap.pickingMode = PickingMode.Ignore;
        m_MiniMapMarkers.pickingMode = PickingMode.Ignore;
        m_MiniMapMarkers.style.position = Position.Absolute;
        m_MiniMapMarkers.style.left = 0;
        m_MiniMapMarkers.style.right = 0;
        m_MiniMapMarkers.style.top = 0;
        m_MiniMapMarkers.style.bottom = 0;
        RefreshMiniMapBounds();
    }

    private void RefreshMiniMapBounds()
    {
        bool foundBounds = false;
        Bounds combined = new Bounds();
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || tilemap.cellBounds.size == Vector3Int.zero)
                continue;

            TilemapRenderer tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            Bounds candidate = tilemapRenderer != null
                ? tilemapRenderer.bounds
                : new Bounds(tilemap.transform.TransformPoint(tilemap.localBounds.center), tilemap.localBounds.size);
            if (candidate.size.x <= 0.01f || candidate.size.y <= 0.01f)
                continue;

            if (!foundBounds)
            {
                combined = candidate;
                foundBounds = true;
            }
            else
            {
                combined.Encapsulate(candidate);
            }
        }

        if (!foundBounds)
        {
            CrystalUI crystalUI = FindFirstObjectByType<CrystalUI>();
            if (crystalUI != null)
            {
                Vector2 min = Vector2.Min(crystalUI.randomCrystalAreaMin, crystalUI.randomCrystalAreaMax);
                Vector2 max = Vector2.Max(crystalUI.randomCrystalAreaMin, crystalUI.randomCrystalAreaMax);
                combined = new Bounds((min + max) * 0.5f, max - min);
                foundBounds = true;
            }
        }

        if (!foundBounds)
            combined = new Bounds(Vector3.zero, new Vector3(48f, 30f, 1f));

        combined.Expand(new Vector3(1f, 1f, 0f));
        m_MiniMapBounds = combined;
        m_MiniMapHasBounds = true;
    }

    private void UpdateMiniMapMarkers()
    {
        if (m_MiniMapMarkers == null || Time.unscaledTime < m_NextMiniMapRefresh)
            return;

        m_NextMiniMapRefresh = Time.unscaledTime + 0.2f;
        if (!m_MiniMapHasBounds)
            RefreshMiniMapBounds();

        if (m_MiniMapPlayer == null)
            m_MiniMapPlayer = FindFirstObjectByType<PlayerController>();

        HashSet<int> visibleEntries = new HashSet<int>();
        if (m_MiniMapPlayer != null)
            UpdateMiniMapMarker(m_MiniMapPlayer.transform, new Color(0.2f, 0.95f, 1f), "Player", 10f, visibleEntries);

        NonPlayerCharacter[] characters = FindObjectsByType<NonPlayerCharacter>(FindObjectsSortMode.None);
        foreach (NonPlayerCharacter character in characters)
            if (character != null)
                UpdateMiniMapMarker(character.transform, new Color(1f, 0.72f, 0.25f), "Explorer", 7f, visibleEntries);

        BreakableCrystal[] crystals = FindObjectsByType<BreakableCrystal>(FindObjectsSortMode.None);
        foreach (BreakableCrystal crystal in crystals)
            if (crystal != null)
                UpdateMiniMapMarker(crystal.transform, new Color(0.78f, 0.42f, 1f), "Crystal", 6f, visibleEntries);

        List<int> staleEntries = null;
        foreach (KeyValuePair<int, MiniMapMarker> entry in m_MiniMapEntries)
        {
            if (!visibleEntries.Contains(entry.Key))
            {
                entry.Value.visual.RemoveFromHierarchy();
                if (staleEntries == null) staleEntries = new List<int>();
                staleEntries.Add(entry.Key);
            }
        }

        if (staleEntries != null)
            foreach (int id in staleEntries) m_MiniMapEntries.Remove(id);
    }

    private void UpdateMiniMapMarker(Transform target, Color color, string tooltip, float size, HashSet<int> visibleEntries)
    {
        if (target == null)
            return;

        int id = target.GetInstanceID();
        visibleEntries.Add(id);
        if (!m_MiniMapEntries.TryGetValue(id, out MiniMapMarker marker))
        {
            VisualElement dot = new VisualElement();
            dot.name = "MiniMap-" + tooltip;
            dot.pickingMode = PickingMode.Ignore;
            dot.style.position = Position.Absolute;
            dot.style.width = size;
            dot.style.height = size;
            dot.style.backgroundColor = color;
            dot.style.borderTopLeftRadius = Length.Percent(50);
            dot.style.borderTopRightRadius = Length.Percent(50);
            dot.style.borderBottomLeftRadius = Length.Percent(50);
            dot.style.borderBottomRightRadius = Length.Percent(50);
            dot.style.borderTopWidth = 1;
            dot.style.borderRightWidth = 1;
            dot.style.borderBottomWidth = 1;
            dot.style.borderLeftWidth = 1;
            dot.style.borderTopColor = new Color(0.05f, 0.1f, 0.18f, 0.95f);
            dot.style.borderRightColor = dot.style.borderTopColor;
            dot.style.borderBottomColor = dot.style.borderTopColor;
            dot.style.borderLeftColor = dot.style.borderTopColor;
            dot.tooltip = tooltip;
            m_MiniMapMarkers.Add(dot);
            marker = new MiniMapMarker { target = target, visual = dot };
            m_MiniMapEntries.Add(id, marker);
        }

        marker.target = target;
        float width = m_MiniMapMarkers.layout.width;
        float height = m_MiniMapMarkers.layout.height;
        if (width <= 0f || height <= 0f)
            return;

        Vector3 position = target.position;
        float x = Mathf.Clamp01((position.x - m_MiniMapBounds.min.x) / Mathf.Max(0.01f, m_MiniMapBounds.size.x));
        float y = Mathf.Clamp01((position.y - m_MiniMapBounds.min.y) / Mathf.Max(0.01f, m_MiniMapBounds.size.y));
        marker.visual.style.left = x * width - size * 0.5f;
        marker.visual.style.top = (1f - y) * height - size * 0.5f;
    }

    private void SetupShootButton()
    {
        if (m_ShootBtn == null)
            return;

        foreach (VisualElement child in m_ShootBtn.Children())
            child.pickingMode = PickingMode.Ignore;

        m_ShootBtn.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (m_GameEnded)
                return;

            m_ShootPointerId = evt.pointerId;
            m_ShootBtn.CapturePointer(evt.pointerId);
            m_MobileShootHeld = true;
            TriggerMobileShoot();
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        m_ShootBtn.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (m_ShootPointerId == evt.pointerId)
            {
                if (m_ShootBtn.HasPointerCapture(evt.pointerId))
                    m_ShootBtn.ReleasePointer(evt.pointerId);
                m_ShootPointerId = -1;
                m_MobileShootHeld = false;
            }
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        m_ShootBtn.RegisterCallback<PointerCancelEvent>(evt =>
        {
            if (m_ShootPointerId == evt.pointerId)
            {
                if (m_ShootBtn.HasPointerCapture(evt.pointerId))
                    m_ShootBtn.ReleasePointer(evt.pointerId);
                m_ShootPointerId = -1;
                m_MobileShootHeld = false;
            }
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        m_ShootBtn.clicked += () =>
        {
            if (!m_GameEnded && Time.unscaledTime - m_LastShootTriggerTime > 0.08f)
                TriggerMobileShoot();
        };
    }

    private void TriggerMobileShoot()
    {
        m_MobileShootPressed = true;
        m_LastShootTriggerTime = Time.unscaledTime;
    }

    private void CreateVirtualJoystick()
    {
        if (m_Root == null)
            return;

        m_JoystickBase = new VisualElement();
        m_JoystickBase.name = "VirtualJoystickBase";
        m_JoystickBase.style.position = Position.Absolute;
        m_JoystickBase.style.backgroundColor = new Color(0.12f, 0.16f, 0.24f, 0.45f);
        m_JoystickBase.style.borderTopWidth = 4;
        m_JoystickBase.style.borderBottomWidth = 4;
        m_JoystickBase.style.borderLeftWidth = 4;
        m_JoystickBase.style.borderRightWidth = 4;
        Color borderColor = new Color(1f, 1f, 1f, 0.55f);
        m_JoystickBase.style.borderTopColor = borderColor;
        m_JoystickBase.style.borderBottomColor = borderColor;
        m_JoystickBase.style.borderLeftColor = borderColor;
        m_JoystickBase.style.borderRightColor = borderColor;

        m_JoystickKnob = new VisualElement();
        m_JoystickKnob.name = "VirtualJoystickKnob";
        m_JoystickKnob.pickingMode = PickingMode.Ignore;
        m_JoystickKnob.style.position = Position.Absolute;
        m_JoystickKnob.style.backgroundColor = new Color(1f, 1f, 1f, 0.85f);

        m_JoystickBase.Add(m_JoystickKnob);
        m_Root.Add(m_JoystickBase);

        m_JoystickBase.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (m_GameEnded)
                return;

            m_JoystickPointerId = evt.pointerId;
            m_JoystickBase.CapturePointer(evt.pointerId);
            UpdateJoystickFromLocalPosition(evt.localPosition);
            evt.StopPropagation();
        });

        m_JoystickBase.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (m_JoystickPointerId != evt.pointerId)
                return;

            UpdateJoystickFromLocalPosition(evt.localPosition);
            evt.StopPropagation();
        });

        m_JoystickBase.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (m_JoystickPointerId != evt.pointerId)
                return;

            ResetJoystick(evt.pointerId);
            evt.StopPropagation();
        });

        m_JoystickBase.RegisterCallback<PointerCancelEvent>(evt =>
        {
            if (m_JoystickPointerId != evt.pointerId)
                return;

            ResetJoystick(evt.pointerId);
            evt.StopPropagation();
        });
    }

    private void UpdateJoystickFromLocalPosition(Vector2 localPos)
    {
        Vector2 center = new Vector2(m_JoystickRadius, m_JoystickRadius);
        Vector2 delta = localPos - center;
        float maxTravel = m_JoystickRadius - m_KnobRadius * 0.65f;

        if (delta.magnitude > maxTravel)
            delta = delta.normalized * maxTravel;

        if (m_JoystickKnob != null)
        {
            m_JoystickKnob.style.left = center.x + delta.x - m_KnobRadius;
            m_JoystickKnob.style.top = center.y + delta.y - m_KnobRadius;
        }

        Vector2 normalized = maxTravel > 0f ? delta / maxTravel : Vector2.zero;
        m_MobileMoveInput = new Vector2(normalized.x, -normalized.y);

        if (m_MobileMoveInput.magnitude < 0.12f)
            m_MobileMoveInput = Vector2.zero;
    }

    private void ResetJoystick(int pointerId)
    {
        if (m_JoystickBase != null && m_JoystickBase.HasPointerCapture(pointerId))
            m_JoystickBase.ReleasePointer(pointerId);

        m_JoystickPointerId = -1;
        m_MobileMoveInput = Vector2.zero;
        CenterJoystickKnob();
    }

    private void CenterJoystickKnob()
    {
        if (m_JoystickKnob == null)
            return;

        m_JoystickKnob.style.left = m_JoystickRadius - m_KnobRadius;
        m_JoystickKnob.style.top = m_JoystickRadius - m_KnobRadius;
    }

    private void ApplyResponsiveLayout()
    {
        float width = m_Root != null && m_Root.layout.width > 0 ? m_Root.layout.width : Screen.width;
        float height = m_Root != null && m_Root.layout.height > 0 ? m_Root.layout.height : Screen.height;
        bool isPortrait = height > width;

        // Показуємо сенсорні кнопки тільки на телефоні або у вертикальному вікні (Simulator)
        bool showMobileControls = Application.isMobilePlatform || isPortrait;

        UpdateTopCornerHUD(isPortrait);
        UpdateMobileControls(showMobileControls, width, height, isPortrait);

        if (m_NonPlayerDialogue == null)
            return;

        if (isPortrait)
        {
            // На телефоні картка нижча й компактніша, щоб персонаж залишався видимим.
            m_NonPlayerDialogue.style.bottom = Length.Percent(18);
            m_NonPlayerDialogue.style.left = Length.Percent(3);
            m_NonPlayerDialogue.style.right = Length.Percent(3);
            m_NonPlayerDialogue.style.height = Length.Percent(28);

            if (m_DialogueCard != null)
            {
                m_DialogueCard.style.left = Length.Percent(2);
                m_DialogueCard.style.right = Length.Percent(2);
                m_DialogueCard.style.top = Length.Percent(2);
                m_DialogueCard.style.bottom = Length.Percent(2);
            }

            UpdateDialogueTextSize(true);
        }
        else
        {
            // Ноутбук / горизонтальний екран
            float canvasScale = Mathf.Min(Screen.width / 960f, Screen.height / 640f);
            float reservedBottom = (Screen.safeArea.yMin + 112f * canvasScale) * height / Mathf.Max(1, Screen.height);
            m_NonPlayerDialogue.style.bottom = showMobileControls ? Length.Percent(38) : new Length(reservedBottom);
            m_NonPlayerDialogue.style.left = Length.Percent(10);
            m_NonPlayerDialogue.style.right = Length.Percent(10);
            m_NonPlayerDialogue.style.height = Length.Percent(35);

            if (m_DialogueCard != null)
            {
                m_DialogueCard.style.left = Length.Percent(12);
                m_DialogueCard.style.right = Length.Percent(12);
                m_DialogueCard.style.top = Length.Percent(14);
                m_DialogueCard.style.bottom = Length.Percent(10);
            }

            UpdateDialogueTextSize(false);
        }
    }

    private void UpdateDialogueTextSize(bool isPortrait)
    {
        if (m_DialogueText == null)
            return;

        int textLength = m_DialogueText.text != null ? m_DialogueText.text.Length : 0;
        m_DialogueText.style.fontSize = !isPortrait ? 30 : textLength > 320 ? 32 : textLength > 220 ? 34 : 36;
    }

    private void UpdateTopCornerHUD(bool isPortrait)
    {
        if (m_CrystalHudContainer == null)
            FindCrystalHudElement();

        float panelWidth = m_Root.layout.width;
        float panelHeight = m_Root.layout.height;
        float unit = Mathf.Max(1f, Mathf.Min(panelWidth, panelHeight) / 640f);
        float safeTop = (Screen.height - Screen.safeArea.yMax) * panelHeight / Mathf.Max(1, Screen.height);
        float safeLeft = Screen.safeArea.xMin * panelWidth / Mathf.Max(1, Screen.width);
        float safeRight = (Screen.width - Screen.safeArea.xMax) * panelWidth / Mathf.Max(1, Screen.width);
        float inset = 16f * unit;
        float safeWidth = Screen.safeArea.width * panelWidth / Mathf.Max(1, Screen.width);
        float miniMapWidth = Mathf.Min((isPortrait ? 142f : 154f) * unit, safeWidth * .33f);
        float miniMapHeight = (isPortrait ? 92f : 104f) * unit;

        if (m_MiniMap != null)
        {
            m_MiniMap.style.position = Position.Absolute;
            m_MiniMap.style.left = safeLeft + inset;
            m_MiniMap.style.top = safeTop + inset;
            m_MiniMap.style.width = miniMapWidth;
            m_MiniMap.style.height = miniMapHeight;
        }

        // 1. Лівий верхній кут: лічильник кристалів
        if (m_CrystalHudContainer != null)
        {
            m_CrystalHudContainer.style.position = Position.Absolute;
            m_CrystalHudContainer.style.top = m_MiniMap != null
                ? safeTop + inset + miniMapHeight * 0.5f - 22f * unit
                : (isPortrait ? 36f : 18f);
            m_CrystalHudContainer.style.left = m_MiniMap != null
                ? safeLeft + inset + miniMapWidth + 10f * unit
                : (isPortrait ? 24f : 18f);
            m_CrystalHudContainer.style.right = StyleKeyword.Auto;
            m_CrystalHudContainer.style.bottom = StyleKeyword.Auto;

            float hudScale = isPortrait ? 3.3f : 1.15f;
            m_CrystalHudContainer.style.scale = new Scale(new Vector3(hudScale, hudScale, 1f));
            m_CrystalHudContainer.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0));
        }

        inset = 20f * unit;
        const float topHudScale = 1.25f;
        if (m_LevelMenuButton != null)
        {
            m_LevelMenuButton.style.position = Position.Absolute;
            m_LevelMenuButton.style.top = safeTop + inset;
            m_LevelMenuButton.style.right = safeRight + inset;
            m_LevelMenuButton.style.width = 164f * unit * topHudScale;
            m_LevelMenuButton.style.height = 48f * unit * topHudScale;
            m_LevelMenuButton.style.fontSize = 18f * unit * topHudScale;
        }

        // 2. Правий верхній кут: кнопка енциклопедії ("ДОСЛІДЖЕННЯ 0/5")
        if (m_EncyclopediaButton != null)
        {
            m_EncyclopediaButton.style.position = Position.Absolute;
            m_EncyclopediaButton.style.top = safeTop + inset + 60f * unit * topHudScale;
            m_EncyclopediaButton.style.right = safeRight + inset;
            m_EncyclopediaButton.style.left = StyleKeyword.Auto;
            m_EncyclopediaButton.style.bottom = StyleKeyword.Auto;

            m_EncyclopediaButton.style.fontSize = 17f * unit * topHudScale;
            m_EncyclopediaButton.style.width = 240f * unit * topHudScale;
            m_EncyclopediaButton.style.height = 48f * unit * topHudScale;
            m_EncyclopediaButton.style.paddingLeft = 12;
            m_EncyclopediaButton.style.paddingRight = 12;
            m_EncyclopediaButton.style.paddingTop = 6;
            m_EncyclopediaButton.style.paddingBottom = 6;
        }

        // 3. Вікно відкритої енциклопедії на телефоні
        if (m_EncyclopediaPanel != null && isPortrait)
        {
            m_EncyclopediaPanel.style.left = Length.Percent(5);
            m_EncyclopediaPanel.style.right = Length.Percent(5);
            m_EncyclopediaPanel.style.top = safeTop + inset + 150f * unit * topHudScale;
            m_EncyclopediaPanel.style.width = StyleKeyword.Auto;
        }
    }

    private void UpdateMobileControls(bool showMobileControls, float screenW, float screenH, bool isPortrait)
    {
        if (m_MoveUpBtn != null) m_MoveUpBtn.style.display = DisplayStyle.None;
        if (m_MoveDownBtn != null) m_MoveDownBtn.style.display = DisplayStyle.None;
        if (m_MoveLeftBtn != null) m_MoveLeftBtn.style.display = DisplayStyle.None;
        if (m_MoveRightBtn != null) m_MoveRightBtn.style.display = DisplayStyle.None;

        DisplayStyle display = showMobileControls ? DisplayStyle.Flex : DisplayStyle.None;
        if (m_JoystickBase != null) m_JoystickBase.style.display = display;
        if (m_ShootBtn != null) m_ShootBtn.style.display = display;

        if (!showMobileControls || m_Root == null)
            return;

        if (m_ShootBtn != null && m_ShootBtn.parent != m_Root)
            m_Root.Add(m_ShootBtn);

        float baseUnit = isPortrait ? screenW : screenH;
        float joyDiameter = Mathf.Clamp(baseUnit * 0.30f, 180f, 340f);
        m_JoystickRadius = joyDiameter * 0.5f;
        m_KnobRadius = m_JoystickRadius * 0.42f;

        float joyLeft = isPortrait ? screenW * 0.10f : 70f;
        float joyBottom = isPortrait ? screenH * 0.17f : 60f;

        if (m_JoystickBase != null)
        {
            m_JoystickBase.style.left = joyLeft;
            m_JoystickBase.style.bottom = joyBottom;
            m_JoystickBase.style.right = StyleKeyword.Auto;
            m_JoystickBase.style.top = StyleKeyword.Auto;
            m_JoystickBase.style.width = joyDiameter;
            m_JoystickBase.style.height = joyDiameter;
            SetCircleCorners(m_JoystickBase, m_JoystickRadius);
        }

        if (m_JoystickKnob != null)
        {
            float knobDiameter = m_KnobRadius * 2f;
            m_JoystickKnob.style.width = knobDiameter;
            m_JoystickKnob.style.height = knobDiameter;
            SetCircleCorners(m_JoystickKnob, m_KnobRadius);
            if (m_JoystickPointerId == -1)
                CenterJoystickKnob();
        }

        if (m_ShootBtn != null)
        {
            float shootSize = joyDiameter * 0.72f;
            float shootRight = isPortrait ? screenW * 0.11f : 80f;
            float shootBottom = joyBottom + (joyDiameter - shootSize) * 0.5f;

            m_ShootBtn.style.position = Position.Absolute;
            m_ShootBtn.style.right = shootRight;
            m_ShootBtn.style.bottom = shootBottom;
            m_ShootBtn.style.left = StyleKeyword.Auto;
            m_ShootBtn.style.top = StyleKeyword.Auto;
            m_ShootBtn.style.width = shootSize;
            m_ShootBtn.style.height = shootSize;
            m_ShootBtn.style.fontSize = Mathf.RoundToInt(shootSize * 0.35f);
            m_ShootBtn.style.scale = new Scale(Vector3.one);
            SetCircleCorners(m_ShootBtn, shootSize * 0.5f);
        }
    }

    private void SetCircleCorners(VisualElement element, float radius)
    {
        if (element == null)
            return;

        element.style.borderTopLeftRadius = radius;
        element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = radius;
        element.style.borderBottomRightRadius = radius;
    }

    private void OnDestroy()
    {
        GameLocalization.LanguageChanged -= HandleLanguageChanged;
        if (m_TalkButton != null)
            m_TalkButton.clicked -= OnTalkButtonClicked;
        if (m_DialogueCloseButton != null)
            m_DialogueCloseButton.clicked -= CloseDialogue;
        if (m_EncyclopediaButton != null)
            m_EncyclopediaButton.clicked -= ToggleEncyclopedia;
        if (m_EncyclopediaCloseButton != null)
            m_EncyclopediaCloseButton.clicked -= CloseEncyclopedia;

        if (instance == this)
            instance = null;
    }

    // НОВЕ (крок 12): таймер показу репліки
    private void Update()
    {
        UpdateMiniMapMarkers();

        if (m_TimerDisplay > 0)
        {
            m_TimerDisplay -= Time.deltaTime;
            if (m_TimerDisplay <= 0 && m_NonPlayerDialogue != null)
            {
                m_NonPlayerDialogue.style.display = DisplayStyle.None;
                m_DialogueOpen = false;
            }
        }
    }

    private void LateUpdate()
    {
        UpdateTalkButtonPosition();
    }

    public void SetTalkTarget(NonPlayerCharacter target)
    {
        if (m_GameEnded)
            target = null;

        m_TalkTarget = target;

        if (target == null && m_DialogueOpen)
        {
            CloseDialogue();
            return;
        }

        if (m_TalkButton == null)
            return;

        bool isRocket = target != null && target.GetComponent<RocketBehaviour>() != null;
        m_TalkButton.text = GameLocalization.T(isRocket ? "unlock_flight" : "talk");
        m_TalkButton.style.width = isRocket ? 260 : 210;
        m_TalkButton.style.fontSize = isRocket ? 20 : 24;

        m_TalkButton.style.display = target == null || m_DialogueOpen
            ? DisplayStyle.None
            : DisplayStyle.Flex;

        if (target != null)
            UpdateTalkButtonPosition();
    }

    private void OnTalkButtonClicked()
    {
        if (m_TalkTarget != null)
            DisplayDialogue();
    }

    private void CloseDialogue()
    {
        m_DialogueOpen = false;
        m_TimerDisplay = -1f;
        if (m_NonPlayerDialogue != null)
            m_NonPlayerDialogue.style.display = DisplayStyle.None;

        if (m_TalkTarget != null)
            UpdateTalkButtonPosition();
        else if (m_TalkButton != null)
            m_TalkButton.style.display = DisplayStyle.None;
    }

    private void ToggleEncyclopedia()
    {
        m_EncyclopediaOpen = !m_EncyclopediaOpen;
        if (m_EncyclopediaPanel != null)
            m_EncyclopediaPanel.style.display = m_EncyclopediaOpen
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    private void CloseEncyclopedia()
    {
        m_EncyclopediaOpen = false;
        if (m_EncyclopediaPanel != null)
            m_EncyclopediaPanel.style.display = DisplayStyle.None;
    }

    private void LearnFact(string factId, Sprite portrait)
    {
        if (!string.IsNullOrEmpty(factId) && portrait != null)
            m_NpcPortraits[factId] = portrait;

        MoonFact fact = MoonFactCatalog.Find(factId);
        if (fact == null || !m_LearnedFacts.Add(fact.Id))
            return;

        RefreshEncyclopedia();
    }

    private void RefreshEncyclopedia()
    {
        int learnedCount = m_LearnedFacts.Count;
        int factCount = MoonFactCatalog.Facts.Count;

        if (m_EncyclopediaButton != null)
            m_EncyclopediaButton.text = string.Format(GameLocalization.T("research"), learnedCount, factCount);
        if (m_EncyclopediaCounter != null)
            m_EncyclopediaCounter.text = string.Format(GameLocalization.T("learned"), learnedCount, factCount);
        if (m_EncyclopediaFactList == null)
            return;

        m_EncyclopediaFactList.Clear();
        foreach (MoonFact fact in MoonFactCatalog.Facts)
        {
            bool learned = m_LearnedFacts.Contains(fact.Id);
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.marginBottom = 7;
            row.style.backgroundColor = new Color(0.08f, 0.12f, 0.22f, 0.64f);

            Label checkmark = new Label(learned ? "✓" : "□");
            checkmark.style.width = 28;
            checkmark.style.flexShrink = 0;
            checkmark.style.fontSize = 24;
            checkmark.style.color = learned
                ? new Color(0.44f, 0.88f, 0.62f)
                : new Color(0.68f, 0.73f, 0.80f);
            row.Add(checkmark);

            Sprite portrait = GetNpcPortrait(fact.Id);
            if (portrait != null)
            {
                Image portraitIcon = new Image();
                portraitIcon.sprite = portrait;
                portraitIcon.scaleMode = ScaleMode.ScaleToFit;
                portraitIcon.pickingMode = PickingMode.Ignore;
                portraitIcon.style.width = 48;
                portraitIcon.style.height = 48;
                portraitIcon.style.flexShrink = 0;
                portraitIcon.style.marginRight = 10;
                row.Add(portraitIcon);
            }

            VisualElement textColumn = new VisualElement();
            textColumn.style.flexGrow = 1;

            Label characterName = new Label(fact.CharacterName);
            characterName.style.fontSize = 18;
            characterName.style.unityFontStyleAndWeight = FontStyle.Normal;
            characterName.style.color = Color.white;
            characterName.style.whiteSpace = WhiteSpace.Normal;
            textColumn.Add(characterName);

            row.Add(textColumn);
            m_EncyclopediaFactList.Add(row);
        }
    }

    public bool ConsumeMobileShootPressed()
    {
        bool pressed = m_MobileShootPressed || m_MobileShootHeld;
        m_MobileShootPressed = false;
        return pressed;
    }

    private void UpdateTalkButtonPosition()
    {
        if (m_TalkTarget == null || m_TalkButton == null || m_Root == null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        if (m_DialogueOpen)
        {
            m_TalkButton.style.display = DisplayStyle.None;
            return;
        }

        bool isRocket = m_TalkTarget.GetComponent<RocketBehaviour>() != null;
        Vector3 worldPosition = m_TalkTarget.transform.position + Vector3.up * 0.85f;
        SpriteRenderer npcSprite = m_TalkTarget.GetComponentInChildren<SpriteRenderer>();
        if (npcSprite != null)
        {
            worldPosition = isRocket
                ? npcSprite.bounds.center
                : new Vector3(
                    npcSprite.bounds.center.x,
                    npcSprite.bounds.max.y + 0.15f,
                    m_TalkTarget.transform.position.z
                );
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0)
        {
            m_TalkButton.style.display = DisplayStyle.None;
            return;
        }

        float panelWidth = m_Root.layout.width;
        float panelHeight = m_Root.layout.height;
        if (panelWidth <= 0 || panelHeight <= 0 || m_Root.panel == null)
            return;

        bool isPortrait = panelHeight > panelWidth;
        float talkScale = isPortrait ? (isRocket ? 1.4f : 2.5f) : 1.15f;

        m_TalkButton.style.transformOrigin = new TransformOrigin(Length.Percent(50), isRocket ? Length.Percent(50) : Length.Percent(100));
        m_TalkButton.style.scale = new Scale(new Vector3(talkScale, talkScale, 1f));

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            m_Root.panel, worldPosition, mainCamera);
        float buttonWidth = m_TalkButton.resolvedStyle.width;
        float buttonHeight = m_TalkButton.resolvedStyle.height;

        if (float.IsNaN(buttonWidth) || buttonWidth <= 0f) buttonWidth = 210f;
        if (float.IsNaN(buttonHeight) || buttonHeight <= 0f) buttonHeight = 68f;

        float scaledHalfW = buttonWidth * 0.5f * talkScale;
        float scaledH = buttonHeight * talkScale;
        float scaledVerticalInset = isRocket ? (scaledH - buttonHeight) * .5f : scaledH - buttonHeight;

        m_TalkButton.style.left = Mathf.Clamp(panelPosition.x - buttonWidth * 0.5f,
            scaledHalfW - buttonWidth * 0.5f + 8f,
            Mathf.Max(8f, panelWidth - buttonWidth * 0.5f - scaledHalfW - 8f));
        float verticalOffset = isRocket ? buttonHeight * .5f : buttonHeight;
        m_TalkButton.style.top = Mathf.Clamp(panelPosition.y - verticalOffset,
            scaledVerticalInset + 8f,
            Mathf.Max(8f, panelHeight - buttonHeight - (isRocket ? scaledVerticalInset : 0f) - 8f));
        m_TalkButton.style.display = DisplayStyle.Flex;
    }

    // НОВЕ (крок 12): показати репліку
    private void ClaimArtifact()
    {
        if (m_RocketDialogue)
        {
            RocketBehaviour rocket = m_TalkTarget != null ? m_TalkTarget.GetComponent<RocketBehaviour>() : null;
            if (rocket == null || !rocket.TryOpenMinigame())
            {
                ShowRocketDialogue();
                return;
            }

            m_RocketDialogue = false;
            m_DialogueOpen = false;
            if (m_NonPlayerDialogue != null)
                m_NonPlayerDialogue.style.display = DisplayStyle.None;
            return;
        }

        if (m_WandererDialogue)
        {
            MoonInventory.DeliverToWanderer();
            ShowWandererDialogue();
            return;
        }
        if (m_PendingArtifact < 0 || !MoonInventory.Collect(m_PendingArtifact)) return;
        m_PendingArtifact = -1;
        m_ClaimArtifactButton.style.display = DisplayStyle.None;
        if (m_DialogueText != null) m_DialogueText.style.bottom = 10;
        m_TimerDisplay = 4f;
    }

    private void ShowWandererDialogue()
    {
        m_RocketDialogue = false;
        m_WandererDialogue = true;
        m_PendingArtifact = -1;
        if (m_DialogueText != null)
            m_DialogueText.text = GameLocalization.T("wanderer_name") + "\n" + LunarWanderer.GetDialogue();
        UpdateDialogueTextSize(m_Root != null && m_Root.layout.height > m_Root.layout.width);
        m_ClaimArtifactButton.text = GameLocalization.T("give_artifacts");
        m_ClaimArtifactButton.SetEnabled(MoonInventory.CanDeliver);
        m_ClaimArtifactButton.style.display = MoonInventory.Delivered ? DisplayStyle.None : DisplayStyle.Flex;
        if (m_DialogueText != null) m_DialogueText.style.bottom = MoonInventory.Delivered ? 10 : 82;
        m_TimerDisplay = -1f;
        m_NonPlayerDialogue.BringToFront();
        m_NonPlayerDialogue.style.display = DisplayStyle.Flex;
    }

    private void ShowRocketDialogue()
    {
        m_RocketDialogue = true;
        m_WandererDialogue = false;
        m_PendingArtifact = -1;
        bool unlocked = MoonInventory.Delivered;
        if (m_DialogueText != null)
            m_DialogueText.text = GameLocalization.T("rocket_name") + "\n" + RocketBehaviour.GetDialogue();
        UpdateDialogueTextSize(m_Root != null && m_Root.layout.height > m_Root.layout.width);
        m_ClaimArtifactButton.text = GameLocalization.T(unlocked ? "open_minigame" : "need_rocket_key");
        m_ClaimArtifactButton.SetEnabled(unlocked);
        m_ClaimArtifactButton.style.display = DisplayStyle.Flex;
        if (m_DialogueText != null) m_DialogueText.style.bottom = 82;
        m_TimerDisplay = -1f;
        m_NonPlayerDialogue.BringToFront();
        m_NonPlayerDialogue.style.display = DisplayStyle.Flex;
    }

    public void DisplayDialogue()
    {
        if (m_NonPlayerDialogue == null)
        return;

        if (audioSource != null && talkSound != null)
        audioSource.PlayOneShot(talkSound);

        ApplyResponsiveLayout();

        m_DialogueOpen = true;
        if (m_TalkButton != null)
            m_TalkButton.style.display = DisplayStyle.None;

        m_RocketDialogue = m_TalkTarget != null && m_TalkTarget.GetComponent<RocketBehaviour>() != null;
        if (m_RocketDialogue)
        {
            ShowRocketDialogue();
            return;
        }

        m_WandererDialogue = m_TalkTarget != null && m_TalkTarget.GetComponent<LunarWanderer>() != null;
        if (m_WandererDialogue)
        {
            ShowWandererDialogue();
            return;
        }
        m_RocketDialogue = false;
        m_ClaimArtifactButton.text = GameLocalization.T("claim_artifact");
        m_ClaimArtifactButton.SetEnabled(true);
        MoonFact fact = m_TalkTarget != null
            ? MoonFactCatalog.Find(m_TalkTarget.encyclopediaFactId)
            : null;

        Sprite portrait = m_TalkTarget != null ? GetNpcPortrait(m_TalkTarget) : null;
        if (m_DialogueSpeakerPortrait != null)
            m_DialogueSpeakerPortrait.style.display = DisplayStyle.None;

        if (fact != null)
        {
            if (m_DialogueText != null)
            {
                m_DialogueText.text = fact.CharacterName;
                if (!string.IsNullOrWhiteSpace(fact.Text))
                    m_DialogueText.text += $"\n{fact.Text}";
                UpdateDialogueTextSize(m_Root != null && m_Root.layout.height > m_Root.layout.width);
            }
            LearnFact(fact.Id, portrait);
            m_TimerDisplay = Mathf.Max(displayTime, Mathf.Clamp(fact.Text.Length * 0.07f, 8f, 30f));
        }
        else
        {
            if (m_DialogueText != null)
            {
                m_DialogueText.text = m_TalkTarget != null
                    ? m_TalkTarget.gameObject.name
                    : string.Empty;
                UpdateDialogueTextSize(m_Root != null && m_Root.layout.height > m_Root.layout.width);
            }
            m_TimerDisplay = displayTime;
        }

        m_PendingArtifact = -1;
        if (fact != null)
            for (int i = 0; i < MoonFactCatalog.Facts.Count; i++)
                if (MoonFactCatalog.Facts[i].Id == fact.Id && !MoonInventory.HasClaimed(i))
                    m_PendingArtifact = i;
        bool canClaim = m_PendingArtifact >= 0;
        if (m_ClaimArtifactButton != null)
        {
            m_ClaimArtifactButton.style.display = canClaim ? DisplayStyle.Flex : DisplayStyle.None;
            m_ClaimArtifactButton.BringToFront();
        }
        if (m_DialogueText != null) m_DialogueText.style.bottom = canClaim ? 82 : 10;
        if (canClaim) m_TimerDisplay = -1f;
        m_NonPlayerDialogue.BringToFront();
        m_NonPlayerDialogue.style.display = DisplayStyle.Flex;
    }

    private Sprite GetNpcPortrait(NonPlayerCharacter npc)
    {
        if (npc == null)
            return null;

        SpriteRenderer renderer = npc.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = npc.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }

    private Sprite GetNpcPortrait(string factId)
    {
        if (m_NpcPortraits.TryGetValue(factId, out Sprite learnedPortrait) && learnedPortrait != null)
            return learnedPortrait;

        NonPlayerCharacter[] npcs = FindObjectsByType<NonPlayerCharacter>(FindObjectsSortMode.None);
        foreach (NonPlayerCharacter npc in npcs)
        {
            if (npc != null && npc.encyclopediaFactId == factId)
            {
                Sprite portrait = GetNpcPortrait(npc);
                if (portrait != null)
                    m_NpcPortraits[factId] = portrait;
                return portrait;
            }
        }

        return null;
    }

    public void DisplayWinScreen()
    {
        m_GameEnded = true;
        SetTalkTarget(null);
        m_WinScreen.style.display = DisplayStyle.Flex;
        m_WinScreen.BringToFront();
        m_WinScreen.style.opacity = 1.0f;
    }

    public void DisplayLoseScreen()
    {
        m_GameEnded = true;
        SetTalkTarget(null);
        m_LoseScreen.style.display = DisplayStyle.Flex;
        m_LoseScreen.BringToFront();
        m_LoseScreen.style.opacity = 1.0f;
    }
}
