using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManagerBehaviour : MonoBehaviour
{
    private static bool openLevelSelectorOnNextStart;

    public int levelID;
    public GameObject homeMenu;
    public GameObject levelMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameLocalization.LanguageChanged += RefreshLocalizedText;
        RefreshLocalizedText();

        if (openLevelSelectorOnNextStart)
        {
            openLevelSelectorOnNextStart = false;
            GoToLevelSelector();
        }
    }

    public static void OpenLevelSelectorOnNextStart()
    {
        openLevelSelectorOnNextStart = true;
    }

    private void OnDestroy() { GameLocalization.LanguageChanged -= RefreshLocalizedText; }

    private void RefreshLocalizedText()
    {
        if (homeMenu == null) return;
        Button play = homeMenu.GetComponentInChildren<Button>(true);
        if (play == null) return;
        TMP_Text tmp = play.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = GameLocalization.T("play");
        Text legacy = play.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = GameLocalization.T("play");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GoToLevelSelector()
    {
        levelMenu?.SetActive(true);
        homeMenu?.SetActive(false);
    }

    public void GoToLevelByID(int lvlID)
    {
        SceneManager.LoadSceneAsync($"Level{lvlID}Scene");
    }

    public void GoToSelectedLevel()
    {
        GoToLevelByID(levelID);
    }
}
