using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ParsecLevelNavigation : MonoBehaviour
{
    private GameObject overlay;
    private GameObject lobbyCanvas;
    private GameObject continueButton;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject("Parsec level navigation");
        DontDestroyOnLoad(host);
        var navigation = host.AddComponent<ParsecLevelNavigation>();
        SceneManager.sceneLoaded += navigation.OnSceneLoaded;
        navigation.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (overlay != null) Destroy(overlay);
        if (lobbyCanvas != null) Destroy(lobbyCanvas);
        continueButton = null;
        bool rocket = scene.name == "Level1Scene" || scene.name == "Level2Scene";
        if (scene.name == "MenuScene" || rocket)
        {
            foreach (var player in Object.FindObjectsByType<MusicPlayer>(FindObjectsSortMode.None)) Destroy(player.gameObject);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.isRootCanvas && canvas.renderMode==RenderMode.ScreenSpaceOverlay && canvas.GetComponent<ParsecResponsiveUI>()==null && canvas.GetComponent<ProgressResetUI>()==null)
                    canvas.gameObject.AddComponent<ParsecResponsiveUI>();
        }
        if ((scene.name == "MenuScene" || scene.name == "MainScene") && FindFirstObjectByType<MoonCollectibles>() == null)
            new GameObject("Moon collectibles").AddComponent<MoonCollectibles>();
        if (scene.name == "MenuScene")
        {
            lobbyCanvas = new GameObject("Lobby options canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerSkinMenu));
            Canvas lobby = lobbyCanvas.GetComponent<Canvas>();
            lobby.renderMode = RenderMode.ScreenSpaceOverlay;
            lobby.sortingOrder = 160;
            CanvasScaler lobbyScaler = lobbyCanvas.GetComponent<CanvasScaler>();
            lobbyScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            lobbyScaler.referenceResolution = new Vector2(960, 640);
            lobbyScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            lobbyScaler.matchWidthOrHeight = .5f;
        }
        if (scene.name == "MainScene")
            RocketArrivalCutscene.PlayIfQueued();
        if (!rocket) return;
        overlay=new GameObject("Navigation canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var overlayCanvas=overlay.GetComponent<Canvas>();
        overlayCanvas.renderMode=RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder=100;
        var safe=ParsecResponsiveUI.CreateSafeRoot(overlayCanvas);
        var menu=CreateButton(safe,GameLocalization.T("level_menu"),()=>
        {
            Time.timeScale = 1;
            MenuManagerBehaviour.OpenLevelSelectorOnNextStart();
            SceneManager.LoadScene("MenuScene");
        });
        ParsecResponsiveUI.Place((RectTransform)menu.transform,new Vector2(0,1),new Vector2(200,80),new Vector2(100,-40));
        if(scene.name=="Level1Scene")
        {
            continueButton=CreateButton(safe,GameLocalization.T("continue_moon"),()=>
            {
                RocketArrivalCutscene.QueueForNextEntry();
                SceneManager.LoadScene("MainScene");
            });
            ParsecResponsiveUI.Place((RectTransform)continueButton.transform,new Vector2(.5f,0),new Vector2(360,88),new Vector2(0,44));
            continueButton.SetActive(false);
        }
    }
    private static GameObject CreateButton(Transform parent,string caption,UnityEngine.Events.UnityAction action)
    {
        var obj=new GameObject(caption,typeof(RectTransform),typeof(Image),typeof(Button));
        obj.transform.SetParent(parent,false);
        obj.GetComponent<Image>().color=new Color(.08f,.16f,.26f,.95f);
        var button=obj.GetComponent<Button>();
        button.targetGraphic=obj.GetComponent<Image>();
        button.onClick.AddListener(action);
        var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
        text.transform.SetParent(obj.transform,false);
        ParsecResponsiveUI.Stretch((RectTransform)text.transform);
        var label=text.GetComponent<TextMeshProUGUI>();
        label.text=caption;label.alignment=TextAlignmentOptions.Center;
        label.enableAutoSizing=true;label.fontSizeMin=22;label.fontSizeMax=28;
        label.margin=new Vector4(12,8,12,8);label.raycastTarget=false;
        return obj;
    }
    private void Update()
    {
        if(continueButton!=null) continueButton.SetActive(GameBehaviour.Instance!=null && GameBehaviour.Instance.HasWon);
    }
    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
}
