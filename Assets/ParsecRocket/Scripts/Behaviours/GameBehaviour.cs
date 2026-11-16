using Assets.Scripts;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameBehaviour : MonoBehaviour
{
    const string ENDGAME_MENU_NAME = "EndgameMenu";
    const string MIDTEXT_LABEL_NAME = "MidText";
    const string GLOBALTIME_LABEL_NAME = "GlobalTime";
    const string ROCKET_OBJ_NAME = "Rocket";
    const string MAINCAMERA_NAME = "MainCamera";
    const string MAINCONTAINER_NAME = "Main";
    const string SOUNDPLAYER_NAME = "SoundPlayer";
    const float FLIGHT_SPEED = .1f;
    const float MIN_TAPTIME_GAP = .1f;

    bool isManualTimingOn;
    bool isGameOn;
    bool hasEnded;
    public bool HasWon { get; private set; }
    float gameSessionTime;
    float gameLevelTime;

    BeatFlagController beatFlagCtrl;
    GameObject pnlEndgame;

    public static GameBehaviour Instance { get; private set; }

    private void Awake()
    {
        EnsureSingleton();
    }

    void EnsureSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance.isGameOn = false;
        Instance.isManualTimingOn = false;
        Instance.beatFlagCtrl = new BeatFlagController("timeflags");

        Instance.gameSessionTime = 0;
        Instance.gameLevelTime = 0;

        SetUIText(MIDTEXT_LABEL_NAME, GameLocalization.T("tap_to_start"));

        Instance.pnlEndgame = GameObject.Find(ENDGAME_MENU_NAME);
        if (Instance.pnlEndgame != null)
            if (Instance.pnlEndgame.gameObject.activeSelf)
                Instance.pnlEndgame.gameObject.SetActive(false);

        BeatFlagController.ClearProblematicFlagIndexes(MIN_TAPTIME_GAP);
    }

    // Update is called once per frame
    void Update()
    {
        if (hasEnded) return;
        // RELENTLESS ACTIONS
        Instance.gameSessionTime += Time.deltaTime;

        // CONDITIONAL ACTIONS
        switch (Instance.isGameOn)
        {
            case true:
                GameObject rocket = GameObject.Find(ROCKET_OBJ_NAME);

                if (rocket != null)
                {
                    Instance.gameLevelTime += Time.deltaTime;

                    if (isManualTimingOn)
                        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
                        {
                            float timepoint = Instance.gameLevelTime;
                            BeatFlagController.AppendTimeFlag(new BeatFlagItem(timepoint, 0.5f));
                            Debug.Log($"New flag on {timepoint} written");
                        }

                    Vector3 v = rocket.transform.position;
                    v.y += FLIGHT_SPEED * 60f * Time.deltaTime;
                    rocket.transform.position = v;

                    if (Instance.beatFlagCtrl.IsUntakenFlagExists(Instance.gameLevelTime))
                    {
                        Debug.Log($"SPAN: {Instance.gameLevelTime}");
                        SetUIText(MIDTEXT_LABEL_NAME, GameLocalization.T("tap"));
                        SetUIText(GLOBALTIME_LABEL_NAME, Instance.beatFlagCtrl.TimeLeftThisSpan(Instance.gameLevelTime).ToString("F2"));

                        if (TapPanelBehaviour.Instance != null && TapPanelBehaviour.Instance.IsClicked)
                        {
                            if (Instance.beatFlagCtrl.IsUntakenFlagExists(Instance.gameLevelTime))
                            {
                                Instance.beatFlagCtrl.SetFlagTaken(Instance.gameLevelTime);
                                Debug.Log($"FLAG: {Instance.gameLevelTime}");
                            }
                        }
                    }
                    else
                    {
                        SetUIText(MIDTEXT_LABEL_NAME, string.Empty);
                        SetUIText(GLOBALTIME_LABEL_NAME, string.Empty);
                    }


                    if (Instance.beatFlagCtrl.IsWinAchieved())
                        SetUIText(MIDTEXT_LABEL_NAME, GameLocalization.T("win"));
                    else if (Instance.beatFlagCtrl.IsLoseAchieved(Instance.gameLevelTime))
                        SetUIText(MIDTEXT_LABEL_NAME, GameLocalization.T("lose"));

                    if (Instance.beatFlagCtrl.IsGameEnded(Instance.gameLevelTime))
                    {
                        hasEnded = true;
                        HasWon = Instance.beatFlagCtrl.IsWinAchieved();
                        if (HasWon)
                        {
                            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                            ParsecProgress.CompleteLevel(scene == "Level1Scene" ? 1 : 3);
                        }
                        SetUIText(GLOBALTIME_LABEL_NAME, string.Empty); // Simplication can lead to delayed timer vanish
                        GameObject.Find(MAINCAMERA_NAME).gameObject.transform.parent = GameObject.Find(MAINCONTAINER_NAME).gameObject.transform;

                        if (Instance.pnlEndgame != null)
                            if (!Instance.pnlEndgame.gameObject.activeSelf)
                                Instance.pnlEndgame.gameObject.SetActive(true);
                    }
                }
                else
                {
                    Debug.Log("ROCKET does not exist");
                    return;
                }

                break;

            case false:
            default:
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    StartGame();
                break;
        }

    }
    bool SetUIText(string textCntnName, string val)
    {
        // --- ����������� ������ (GlobalTime) ---
        GameObject globalTimeObj = GameObject.Find(textCntnName);
        if (globalTimeObj != null)
        {
            // �������: ������������� TextMeshProUGUI, ���� �� ��'��� ����������� � Canvas (UI)
            TextMeshProUGUI tmp = globalTimeObj.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                // "F2" ������ 2 ����� ���� ���� (���������: 10.54)
                tmp.text = val;
                return true;
            }
            else
                Debug.Log("THERE IS NO TEXT MESH");
        }
        else
            Debug.Log("THERE IS NO GLOBAL TIME OBJ");


        return false;
    }

    #region TO SEPARATE CLASS
    void PlayMusicLevel1()
    {
        GameObject player = GameObject.Find(SOUNDPLAYER_NAME);
        if (player != null)
        {
            AudioSource src = player.GetComponent<AudioSource>();
            if (src != null)
                src.Play();
            else
                Debug.Log($"Could not get source of \"{SOUNDPLAYER_NAME}\"");
        }
        else
            Debug.Log($"Could not get object \"{SOUNDPLAYER_NAME}\"");
    }
    #endregion

    #region GET/SET
    public void StartGame()
    {
        if (isGameOn || hasEnded) return;
        Instance.isGameOn = true;
        var pregame = FindFirstObjectByType<PregameMenuBehaviour>();
        if (pregame != null) pregame.gameObject.SetActive(false);
        PlayMusicLevel1();
    }
    #endregion
}
