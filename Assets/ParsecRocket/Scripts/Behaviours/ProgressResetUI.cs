using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressResetUI : MonoBehaviour
{
    [SerializeField] private MenuManagerBehaviour menu;
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject confirmation;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    private TMP_Text confirmationLabel;
    private TMP_Text cancelLabel;
    private TMP_Text confirmLabel;
    private void Start()
    {
        PrepareScene();
        resetButton.onClick.AddListener(ShowConfirmation);
        confirmButton.onClick.AddListener(ConfirmReset);
        cancelButton.onClick.AddListener(CancelReset);
        GameLocalization.LanguageChanged += RefreshLocalizedText;
        RefreshLocalizedText();
        confirmation.SetActive(false);
    }
    public void PrepareScene()
    {
        if(content!=null)return;
        menu=FindFirstObjectByType<MenuManagerBehaviour>();
        var canvas=GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=150;
        var scaler=GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(960,640);
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var safe=ParsecResponsiveUI.CreateSafeRoot(canvas);
        content=safe.gameObject;
        resetButton=CreateButton(safe,"Reset progress",GameLocalization.T("reset_progress"),new Vector2(230,64));
        ParsecResponsiveUI.Place((RectTransform)resetButton.transform,new Vector2(1,0),new Vector2(230,64),new Vector2(-115,32));
        confirmation=new GameObject("Reset confirmation",typeof(RectTransform),typeof(Image));
        confirmation.transform.SetParent(safe,false);ParsecResponsiveUI.Stretch((RectTransform)confirmation.transform);
        confirmation.GetComponent<Image>().color=new Color(0,0,0,.8f);
        var card=new GameObject("Confirmation card",typeof(RectTransform),typeof(Image));card.transform.SetParent(confirmation.transform,false);
        ParsecResponsiveUI.Place((RectTransform)card.transform,new Vector2(.5f,.5f),new Vector2(540,280),Vector2.zero);
        card.GetComponent<Image>().color=new Color(.08f,.12f,.2f,1);
        confirmationLabel=CreateText(card.transform,GameLocalization.T("reset_prompt"));
        ParsecResponsiveUI.Place(confirmationLabel.rectTransform,new Vector2(.5f,1),new Vector2(490,160),new Vector2(0,-85));
        confirmationLabel.fontSizeMax=28;
        cancelButton=CreateButton(card.transform,"Cancel reset",GameLocalization.T("cancel"),new Vector2(220,64));
        cancelLabel=cancelButton.GetComponentInChildren<TMP_Text>();
        ParsecResponsiveUI.Place((RectTransform)cancelButton.transform,new Vector2(.5f,0),new Vector2(220,64),new Vector2(-120,54));
        confirmButton=CreateButton(card.transform,"Confirm reset",GameLocalization.T("confirm_reset"),new Vector2(220,64));
        confirmLabel=confirmButton.GetComponentInChildren<TMP_Text>();
        ParsecResponsiveUI.Place((RectTransform)confirmButton.transform,new Vector2(.5f,0),new Vector2(220,64),new Vector2(120,54));
        confirmButton.GetComponent<Image>().color=new Color(.6f,.18f,.2f,1);
        confirmation.SetActive(false);
        content.SetActive(false);
    }
    private static TMP_Text CreateText(Transform parent,string caption)
    {
        var obj=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));obj.transform.SetParent(parent,false);
        var label=obj.GetComponent<TextMeshProUGUI>();label.text=caption;label.fontSizeMin=20;label.fontSizeMax=26;
        label.enableAutoSizing=true;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        label.margin=new Vector4(12,8,12,8);return label;
    }
    private static Button CreateButton(Transform parent,string name,string caption,Vector2 size)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));obj.transform.SetParent(parent,false);
        ((RectTransform)obj.transform).sizeDelta=size;
        obj.GetComponent<Image>().color=new Color(.2f,.29f,.4f,1);
        var button=obj.GetComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();
        var label=CreateText(obj.transform,caption);ParsecResponsiveUI.Stretch(label.rectTransform);
        return button;
    }
    private void Update()
    {
        if(content==null)return;
        bool show=menu!=null&&menu.homeMenu!=null&&menu.homeMenu.activeInHierarchy;
        if(!show&&confirmation!=null)confirmation.SetActive(false);
        content.SetActive(show);
    }
    public void ShowConfirmation(){confirmation.SetActive(true);}
    public void CancelReset(){confirmation.SetActive(false);}
    public void ConfirmReset()
    {
        if(!confirmation.activeInHierarchy)return;
        MoonInventory.ResetProgress();
        confirmation.SetActive(false);
        var browser=FindFirstObjectByType<LevelBrowserBehaviour>();
        if(browser!=null)browser.RefreshButtons();
    }
    private void RefreshLocalizedText()
    {
        if(resetButton!=null)
        {
            TMP_Text label=resetButton.GetComponentInChildren<TMP_Text>();
            if(label!=null)label.text=GameLocalization.T("reset_progress");
        }
        if(confirmationLabel!=null)confirmationLabel.text=GameLocalization.T("reset_prompt");
        if(cancelLabel!=null)cancelLabel.text=GameLocalization.T("cancel");
        if(confirmLabel!=null)confirmLabel.text=GameLocalization.T("confirm_reset");
    }
    private void OnDestroy(){GameLocalization.LanguageChanged-=RefreshLocalizedText;}
}
