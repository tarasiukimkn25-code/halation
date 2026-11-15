using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIHandler : MonoBehaviour
{
    public static UIHandler instance { get; private set; }

    // НОВЕ (крок 12): поля репліки
    public float displayTime = 4.0f;
    private VisualElement m_NonPlayerDialogue;
    private float m_TimerDisplay;

    private VisualElement m_WinScreen;
    private VisualElement m_LoseScreen;
    private VisualElement m_Root;
    private Button m_TalkButton;
    private Button m_DialogueCloseButton;
    private Image m_DialogueSpeakerPortrait;
    private Button m_EncyclopediaButton;
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
    }

    void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        m_Root = uiDocument.rootVisualElement;

        // НОВЕ (крок 12): ховаємо репліку на старті та налаштовуємо адаптивність
        m_NonPlayerDialogue = m_Root.Q<VisualElement>("NPCDialogue");
        if (m_NonPlayerDialogue != null)
        {
            m_NonPlayerDialogue.style.display = DisplayStyle.None;
            m_NonPlayerDialogue.style.position = Position.Absolute;
            m_NonPlayerDialogue.style.top = StyleKeyword.Auto;

            // Закриття при натисканні на саме вікно діалогу
            m_NonPlayerDialogue.RegisterCallback<PointerDownEvent>(evt => CloseDialogue());
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
            // Вертикальний телефон: ставимо вікно вище кнопок керування (bottom = 42%)
            m_NonPlayerDialogue.style.bottom = Length.Percent(42);
            m_NonPlayerDialogue.style.left = Length.Percent(4);
            m_NonPlayerDialogue.style.right = Length.Percent(4);
            m_NonPlayerDialogue.style.height = Length.Percent(24);

            if (m_DialogueText != null)
                m_DialogueText.style.fontSize = 36;
        }
        else
        {
            // Ноутбук / горизонтальний екран
            m_NonPlayerDialogue.style.bottom = showMobileControls ? Length.Percent(38) : Length.Percent(3);
            m_NonPlayerDialogue.style.left = Length.Percent(10);
            m_NonPlayerDialogue.style.right = Length.Percent(10);
            m_NonPlayerDialogue.style.height = Length.Percent(35);

            if (m_DialogueText != null)
                m_DialogueText.style.fontSize = 26;
        }
    }

    private void UpdateTopCornerHUD(bool isPortrait)
    {
        if (m_CrystalHudContainer == null)
            FindCrystalHudElement();

        // 1. Лівий верхній кут: лічильник кристалів
        if (m_CrystalHudContainer != null)
        {
            m_CrystalHudContainer.style.position = Position.Absolute;
            m_CrystalHudContainer.style.top = isPortrait ? 36 : 18;
            m_CrystalHudContainer.style.left = isPortrait ? 24 : 18;
            m_CrystalHudContainer.style.right = StyleKeyword.Auto;
            m_CrystalHudContainer.style.bottom = StyleKeyword.Auto;

            float hudScale = isPortrait ? 3.3f : 1.15f;
            m_CrystalHudContainer.style.scale = new Scale(new Vector3(hudScale, hudScale, 1f));
            m_CrystalHudContainer.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0));
        }

        // 2. Правий верхній кут: кнопка енциклопедії ("ДОСЛІДЖЕННЯ 0/5")
        if (m_EncyclopediaButton != null)
        {
            m_EncyclopediaButton.style.position = Position.Absolute;
            m_EncyclopediaButton.style.top = isPortrait ? 36 : 18;
            m_EncyclopediaButton.style.right = isPortrait ? 24 : 18;
            m_EncyclopediaButton.style.left = StyleKeyword.Auto;
            m_EncyclopediaButton.style.bottom = StyleKeyword.Auto;

            if (isPortrait)
            {
                m_EncyclopediaButton.style.fontSize = 42;
                m_EncyclopediaButton.style.paddingLeft = 34;
                m_EncyclopediaButton.style.paddingRight = 34;
                m_EncyclopediaButton.style.paddingTop = 22;
                m_EncyclopediaButton.style.paddingBottom = 22;
                m_EncyclopediaButton.style.height = StyleKeyword.Auto;
                m_EncyclopediaButton.style.width = StyleKeyword.Auto;
            }
            else
            {
                m_EncyclopediaButton.style.fontSize = 18;
                m_EncyclopediaButton.style.paddingLeft = 14;
                m_EncyclopediaButton.style.paddingRight = 14;
                m_EncyclopediaButton.style.paddingTop = 8;
                m_EncyclopediaButton.style.paddingBottom = 8;
            }
        }

        // 3. Вікно відкритої енциклопедії на телефоні
        if (m_EncyclopediaPanel != null && isPortrait)
        {
            m_EncyclopediaPanel.style.left = Length.Percent(5);
            m_EncyclopediaPanel.style.right = Length.Percent(5);
            m_EncyclopediaPanel.style.top = 155;
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
            m_EncyclopediaButton.text = $"ДОСЛІДЖЕННЯ  {learnedCount}/{factCount}";
        if (m_EncyclopediaCounter != null)
            m_EncyclopediaCounter.text = $"Вивчено: {learnedCount} із {factCount}";
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

        // Жорстка прив'язка прямо до верхньої межі спрайта самого NPC
        Vector3 worldPosition = m_TalkTarget.transform.position + Vector3.up * 0.85f;
        SpriteRenderer npcSprite = m_TalkTarget.GetComponentInChildren<SpriteRenderer>();
        if (npcSprite != null)
        {
            worldPosition = new Vector3(
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
        float talkScale = isPortrait ? 2.5f : 1.15f;

        m_TalkButton.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
        m_TalkButton.style.scale = new Scale(new Vector3(talkScale, talkScale, 1f));

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            m_Root.panel, worldPosition, mainCamera);
        float buttonWidth = m_TalkButton.resolvedStyle.width;
        float buttonHeight = m_TalkButton.resolvedStyle.height;

        if (float.IsNaN(buttonWidth) || buttonWidth <= 0f) buttonWidth = 210f;
        if (float.IsNaN(buttonHeight) || buttonHeight <= 0f) buttonHeight = 68f;

        float scaledHalfW = buttonWidth * 0.5f * talkScale;
        float scaledH = buttonHeight * talkScale;

        m_TalkButton.style.left = Mathf.Clamp(panelPosition.x - buttonWidth * 0.5f,
            scaledHalfW - buttonWidth * 0.5f + 8f,
            Mathf.Max(8f, panelWidth - buttonWidth * 0.5f - scaledHalfW - 8f));
        m_TalkButton.style.top = Mathf.Clamp(panelPosition.y - buttonHeight,
            scaledH - buttonHeight + 8f,
            Mathf.Max(8f, panelHeight - buttonHeight - 8f));
        m_TalkButton.style.display = DisplayStyle.Flex;
    }

    // НОВЕ (крок 12): показати репліку
    public void DisplayDialogue()
    {
        if (m_NonPlayerDialogue == null)
            return;

        ApplyResponsiveLayout();

        m_DialogueOpen = true;
        if (m_TalkButton != null)
            m_TalkButton.style.display = DisplayStyle.None;

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
            }
            LearnFact(fact.Id, portrait);
            m_TimerDisplay = Mathf.Max(displayTime, Mathf.Clamp(fact.Text.Length * 0.07f, 8f, 30f));
        }
        else
        {
            if (m_DialogueText != null)
                m_DialogueText.text = m_TalkTarget != null
                    ? m_TalkTarget.gameObject.name
                    : string.Empty;
            m_TimerDisplay = displayTime;
        }

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
        m_WinScreen.style.opacity = 1.0f;
    }

    public void DisplayLoseScreen()
    {
        m_GameEnded = true;
        SetTalkTarget(null);
        m_LoseScreen.style.opacity = 1.0f;
    }
}