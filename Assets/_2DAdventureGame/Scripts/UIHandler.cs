using UnityEngine;
using UnityEngine.UIElements;

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
    private NonPlayerCharacter m_TalkTarget;
    private bool m_GameEnded;

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
        m_TimerDisplay = -1.0f;

        // Кнопка з'являється над NPC, коли гравець повернутий до нього.
        m_TalkButton = m_Root.Q<Button>("TalkButton");
        if (m_TalkButton != null)
        {
            m_TalkButton.style.display = DisplayStyle.None;
            m_TalkButton.clicked += OnTalkButtonClicked;
        }

        m_LoseScreen = m_Root.Q<VisualElement>("LoseScreenContainer");
        m_WinScreen = m_Root.Q<VisualElement>("WinScreenContainer");
    }

    private void OnDestroy()
    {
        if (m_TalkButton != null)
            m_TalkButton.clicked -= OnTalkButtonClicked;

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
            if (m_TimerDisplay < 0)
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

        m_TalkButton.style.display = DisplayStyle.Flex;

        float panelWidth = m_Root.layout.width;
        float panelHeight = m_Root.layout.height;
        if (panelWidth <= 0 || panelHeight <= 0)
            return;

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            m_Root.panel, worldPosition, mainCamera);
        float x = panelPosition.x;
        float y = panelPosition.y;
        float buttonWidth = m_TalkButton.resolvedStyle.width;
        float buttonHeight = m_TalkButton.resolvedStyle.height;

        if (float.IsNaN(buttonWidth)) buttonWidth = 250f;
        if (float.IsNaN(buttonHeight)) buttonHeight = 92f;

        float left = Mathf.Clamp(x - buttonWidth * 0.5f, 8f,
            Mathf.Max(8f, panelWidth - buttonWidth - 8f));
        float top = Mathf.Clamp(y - buttonHeight - 8f, 8f,
            Mathf.Max(8f, panelHeight - buttonHeight - 8f));

        m_TalkButton.style.left = left;
        m_TalkButton.style.top = top;
    }

    // НОВЕ (крок 12): показати репліку
    public void DisplayDialogue()
    {
        if (m_NonPlayerDialogue == null)
            return;

        m_NonPlayerDialogue.style.display = DisplayStyle.Flex;
        m_TimerDisplay = displayTime;
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
