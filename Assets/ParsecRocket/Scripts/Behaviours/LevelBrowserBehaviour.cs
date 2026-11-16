using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelBrowserBehaviour : MonoBehaviour
{
    public GameObject btnPrefab;
    public GameObject[] levelButtons;
    public const int LEVEL_COUNT = 3;
    private void OnEnable()
    {
        GameLocalization.LanguageChanged += RefreshButtons;
        RefreshButtons();
    }
    private void OnDisable() { GameLocalization.LanguageChanged -= RefreshButtons; }
    public void RefreshButtons()
    {
        if (levelButtons != null)
            foreach (var item in levelButtons) if (item != null) Destroy(item);
        string[] scenes = { "Level1Scene", "MainScene", "Level2Scene" };
        string[] labelKeys = { "level1", "level2", "level3" };
        levelButtons = new GameObject[LEVEL_COUNT];
        for (int i = 0; i < LEVEL_COUNT; i++)
        {
            string scene = scenes[i];
            int level = i + 1;
            bool unlocked = ParsecProgress.IsUnlocked(level);
            string caption = GameLocalization.T(labelKeys[i]) + (ParsecProgress.IsCompleted(level) ? GameLocalization.T("completed") : unlocked ? "" : GameLocalization.T("locked"));
            var item = Instantiate(btnPrefab, transform);
            levelButtons[i] = item;
            var button = item.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.interactable = unlocked;
            button.onClick.AddListener(() =>
            {
                if (!ParsecProgress.IsUnlocked(level)) return;
                if (level == 3) RocketLaunchMinigame.OpenFromLevelMenu();
                else
                {
                    if (level == 2) RocketArrivalCutscene.QueueForNextEntry();
                    SceneManager.LoadScene(scene);
                }
            });
            var label = item.GetComponentInChildren<TMP_Text>();
            if (label != null) { label.text = caption; label.enableAutoSizing = true; label.fontSizeMin = 22; label.fontSizeMax = 28; label.raycastTarget = false; label.margin = new Vector4(12, 8, 12, 8); }
            var legacy = item.GetComponentInChildren<Text>();
            if (legacy != null) legacy.text = caption;
        }
    }
}
