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

    private VisualElement m_Healthbar;

    private VisualElement m_WinScreen;
    private VisualElement m_LoseScreen;
    private VisualElement m_Root;
    private Button m_TalkButton;
    private Button m_EncyclopediaButton;
    private Button m_EncyclopediaCloseButton;
    private VisualElement m_EncyclopediaPanel;
    private Label m_EncyclopediaCounter;
    private ScrollView m_EncyclopediaFactList;
    private Label m_DialogueText;
    private string m_DefaultDialogueText;
    private readonly HashSet<string> m_LearnedFacts = new HashSet<string>();
    private bool m_EncyclopediaOpen;
    private Vector2 m_MobileMoveInput;
    private bool m_MobileShootPressed;
    private NonPlayerCharacter m_TalkTarget;
    private bool m_GameEnded;

    public Vector2 MobileMoveInput => m_MobileMoveInput;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        m_Root = uiDocument.rootVisualElement;

        m_Healthbar = m_Root.Q<VisualElement>("HealthBar");
        SetHealthValue(1.0f);

        // НОВЕ (крок 12): ховаємо репліку на старті
        m_NonPlayerDialogue = m_Root.Q<VisualElement>("NPCDialogue");
        if (m_NonPlayerDialogue != null)
            m_NonPlayerDialogue.style.display = DisplayStyle.None;
        m_DialogueText = m_NonPlayerDialogue?.Q<Label>("DialogueText");
        if (m_DialogueText != null)
            m_DefaultDialogueText = m_DialogueText.text;
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

        RefreshEncyclopedia();

        RegisterMovementButton("MoveUp", Vector2.up);
        RegisterMovementButton("MoveDown", Vector2.down);
        RegisterMovementButton("MoveLeft", Vector2.left);
        RegisterMovementButton("MoveRight", Vector2.right);

        Button shootButton = m_Root.Q<Button>("ShootButton");
        if (shootButton != null)
        {
            shootButton.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (!m_GameEnded)
                    m_MobileShootPressed = true;
                evt.StopPropagation();
            });
        }

        m_LoseScreen = m_Root.Q<VisualElement>("LoseScreenContainer");
        m_WinScreen = m_Root.Q<VisualElement>("WinScreenContainer");
    }

    private void OnDestroy()
    {
        if (m_TalkButton != null)
            m_TalkButton.clicked -= OnTalkButtonClicked;
        if (m_EncyclopediaButton != null)
            m_EncyclopediaButton.clicked -= ToggleEncyclopedia;
        if (m_EncyclopediaCloseButton != null)
            m_EncyclopediaCloseButton.clicked -= CloseEncyclopedia;

        if (instance == this)
            instance = null;
    }

    public void SetHealthValue(float percentage)
    {
        m_Healthbar.style.width = Length.Percent(100 * percentage);
    }

    // НОВЕ (крок 12): таймер показу репліки
    private void Update()
    {
        if (m_TimerDisplay > 0)
        {
            m_TimerDisplay -= Time.deltaTime;
            if (m_TimerDisplay < 0 && m_NonPlayerDialogue != null)
            {
                m_NonPlayerDialogue.style.display = DisplayStyle.None;
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
        if (m_TalkButton == null)
            return;

        m_TalkButton.style.display = target == null
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

    private void LearnFact(string factId)
    {
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
            m_EncyclopediaButton.text = $"ФАКТИ МІСЯЦЯ  {learnedCount}/{factCount}";
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

    private void RegisterMovementButton(string buttonName, Vector2 direction)
    {
        Button button = m_Root.Q<Button>(buttonName);
        if (button == null)
            return;

        button.RegisterCallback<PointerDownEvent>(evt =>
        {
            m_MobileMoveInput = direction;
            button.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        button.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (button.HasPointerCapture(evt.pointerId))
                button.ReleasePointer(evt.pointerId);
            if (m_MobileMoveInput == direction)
                m_MobileMoveInput = Vector2.zero;
            evt.StopPropagation();
        });

        button.RegisterCallback<PointerCancelEvent>(evt =>
        {
            if (button.HasPointerCapture(evt.pointerId))
                button.ReleasePointer(evt.pointerId);
            if (m_MobileMoveInput == direction)
                m_MobileMoveInput = Vector2.zero;
            evt.StopPropagation();
        });
    }

    public bool ConsumeMobileShootPressed()
    {
        bool pressed = m_MobileShootPressed;
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

        Vector3 worldPosition = m_TalkTarget.transform.position + Vector3.up * 1.55f;
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

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            m_Root.panel, worldPosition, mainCamera);
        float buttonWidth = m_TalkButton.resolvedStyle.width;
        float buttonHeight = m_TalkButton.resolvedStyle.height;

        if (float.IsNaN(buttonWidth)) buttonWidth = 250f;
        if (float.IsNaN(buttonHeight)) buttonHeight = 92f;

        m_TalkButton.style.left = Mathf.Clamp(panelPosition.x - buttonWidth * 0.5f,
            8f, Mathf.Max(8f, panelWidth - buttonWidth - 8f));
        m_TalkButton.style.top = Mathf.Clamp(panelPosition.y - buttonHeight - 8f,
            8f, Mathf.Max(8f, panelHeight - buttonHeight - 8f));
        m_TalkButton.style.display = DisplayStyle.Flex;
    }

    // НОВЕ (крок 12): показати репліку
    public void DisplayDialogue()
    {
        if (m_NonPlayerDialogue == null)
            return;

        MoonFact fact = m_TalkTarget != null
            ? MoonFactCatalog.Find(m_TalkTarget.encyclopediaFactId)
            : null;
        if (fact != null)
        {
            if (m_DialogueText != null)
            {
                m_DialogueText.text = fact.CharacterName;
                if (!string.IsNullOrWhiteSpace(fact.Text))
                    m_DialogueText.text += $"\n{fact.Text}";
            }
            LearnFact(fact.Id);
            m_TimerDisplay = Mathf.Max(displayTime, 8f);
        }
        else
        {
            if (m_DialogueText != null)
                m_DialogueText.text = m_DefaultDialogueText;
            m_TimerDisplay = displayTime;
        }

        m_NonPlayerDialogue.style.display = DisplayStyle.Flex;
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
