using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MoonCollectibles : MonoBehaviour
{
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject crystalBadge;
    [SerializeField] private TMP_Text crystalLabel;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Sprite relic;
    [SerializeField] private MenuManagerBehaviour menu;
    private readonly Dictionary<int, SpriteRenderer> artifactMarkers = new Dictionary<int, SpriteRenderer>();

    private void Start()
    {
        PrepareScene();
        MoonInventory.Changed += Refresh;
        Refresh();
    }
    public void PrepareScene()
    {
        if (hud != null || crystalBadge != null) return;
        relic = Resources.Load<Sprite>("MoonItems/Relic");
        var canvasObject = new GameObject("Collectibles HUD", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 640);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var safe = ParsecResponsiveUI.CreateSafeRoot(canvas);
        bool isMoon = gameObject.scene.name == "MainScene";
        if (isMoon) CreateHotbar(safe);
        else
        {
            menu = FindFirstObjectByType<MenuManagerBehaviour>();
            CreateCrystalBadge(safe);
        }
        if (slotIcons != null) foreach(var icon in slotIcons) icon.enabled = false;
    }
    private void CreateHotbar(RectTransform safe)
    {
        hud = new GameObject("Artifact hotbar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hud.transform.SetParent(safe,false);
        ParsecResponsiveUI.Place((RectTransform)hud.transform,new Vector2(.5f,0),new Vector2(336,64),new Vector2(0,32));
        var layout = hud.GetComponent<HorizontalLayoutGroup>();
        layout.spacing=4;layout.childControlWidth=false;layout.childControlHeight=false;
        layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
        slotIcons=new Image[MoonInventory.Capacity];
        for(int i=0;i<slotIcons.Length;i++)
        {
            var slot = new GameObject("Artifact slot "+(i+1),typeof(RectTransform),typeof(Image),typeof(Outline));
            slot.transform.SetParent(hud.transform,false);
            ((RectTransform)slot.transform).sizeDelta=new Vector2(64,64);
            var background=slot.GetComponent<Image>();background.color=new Color(.08f,.10f,.16f,.92f);background.raycastTarget=false;
            var outline=slot.GetComponent<Outline>();outline.effectColor=new Color(.48f,.55f,.66f,.9f);outline.effectDistance=new Vector2(1,-1);
            var icon=new GameObject("Artifact icon",typeof(RectTransform),typeof(Image));icon.transform.SetParent(slot.transform,false);
            ParsecResponsiveUI.Stretch((RectTransform)icon.transform);
            ((RectTransform)icon.transform).offsetMin=new Vector2(8,8);((RectTransform)icon.transform).offsetMax=new Vector2(-8,-8);
            slotIcons[i]=icon.GetComponent<Image>();slotIcons[i].preserveAspect=true;slotIcons[i].raycastTarget=false;
        }
        CreateArtifactMarkers();
    }
    private void CreateArtifactMarkers()
    {
        if (relic == null)
            relic = Resources.Load<Sprite>("MoonItems/Relic");
        if (relic == null)
            return;

        NonPlayerCharacter[] characters = FindObjectsByType<NonPlayerCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NonPlayerCharacter character in characters)
        {
            if (character == null || string.IsNullOrEmpty(character.encyclopediaFactId))
                continue;

            int factIndex = -1;
            for (int i = 0; i < MoonFactCatalog.Facts.Count; i++)
            {
                if (MoonFactCatalog.Facts[i].Id == character.encyclopediaFactId)
                {
                    factIndex = i;
                    break;
                }
            }
            if (factIndex < 0 || artifactMarkers.ContainsKey(factIndex))
                continue;

            SpriteRenderer characterSprite = character.GetComponent<SpriteRenderer>();
            if (characterSprite == null || characterSprite.sprite == null)
                continue;

            GameObject marker = new GameObject("Artifact available marker", typeof(SpriteRenderer));
            marker.transform.SetParent(character.transform, false);
            marker.transform.localPosition = new Vector3(0.08f, characterSprite.sprite.bounds.extents.y + 0.34f, -0.1f);
            float desiredHeight = 0.48f;
            float inheritedScale = Mathf.Max(0.01f, character.transform.lossyScale.y);
            float localScale = desiredHeight / (relic.bounds.size.y * inheritedScale);
            marker.transform.localScale = Vector3.one * localScale;

            SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
            markerRenderer.sprite = relic;
            markerRenderer.color = MoonInventory.ColorFor(factIndex);
            markerRenderer.sortingLayerID = characterSprite.sortingLayerID;
            markerRenderer.sortingOrder = characterSprite.sortingOrder + 20;
            artifactMarkers.Add(factIndex, markerRenderer);
        }
    }
    private void CreateCrystalBadge(RectTransform safe)
    {
        crystalBadge=new GameObject("Level selection crystals",typeof(RectTransform),typeof(Image));
        crystalBadge.transform.SetParent(safe,false);
        ParsecResponsiveUI.Place((RectTransform)crystalBadge.transform,new Vector2(1,1),new Vector2(180,64),new Vector2(-90,-32));
        var background=crystalBadge.GetComponent<Image>();background.color=new Color(.12f,.1f,.22f,.95f);background.raycastTarget=false;
        var icon=new GameObject("Crystal icon",typeof(RectTransform),typeof(Image));icon.transform.SetParent(crystalBadge.transform,false);
        ParsecResponsiveUI.Place((RectTransform)icon.transform,new Vector2(0,.5f),new Vector2(44,44),new Vector2(32,0));
        var image=icon.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("MoonItems/Crystal");image.preserveAspect=true;image.raycastTarget=false;
        var text=new GameObject("Crystal count",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(crystalBadge.transform,false);
        ParsecResponsiveUI.Place((RectTransform)text.transform,new Vector2(1,.5f),new Vector2(112,48),new Vector2(-60,0));
        crystalLabel=text.GetComponent<TextMeshProUGUI>();crystalLabel.fontSize=28;crystalLabel.enableAutoSizing=true;crystalLabel.fontSizeMin=18;crystalLabel.fontSizeMax=28;
        crystalLabel.alignment=TextAlignmentOptions.MidlineLeft;crystalLabel.raycastTarget=false;
        crystalBadge.SetActive(false);
    }
    private void Refresh()
    {
        if(crystalLabel!=null) crystalLabel.text="× "+MoonInventory.Crystals;
        foreach (KeyValuePair<int, SpriteRenderer> marker in artifactMarkers)
            if (marker.Value != null)
                marker.Value.enabled = !MoonInventory.HasClaimed(marker.Key);
        if(slotIcons==null)return;
        var items=MoonInventory.Artifacts;
        for(int i=0;i<slotIcons.Length;i++)
        {
            slotIcons[i].enabled=i<items.Count;
            if(i<items.Count){slotIcons[i].sprite=relic;slotIcons[i].color=MoonInventory.ColorFor(items[i]);}
        }
    }
    private void Update()
    {
        if(crystalBadge!=null) crystalBadge.SetActive(menu!=null && menu.levelMenu!=null && menu.levelMenu.activeInHierarchy);
    }
    private void OnDestroy(){MoonInventory.Changed-=Refresh;}
}
