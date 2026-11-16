using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParsecResponsiveUI : MonoBehaviour
{
    private RectTransform safe;
    private GridLayoutGroup grid;
    private Vector2 lastSize;

    public static RectTransform CreateSafeRoot(Canvas canvas)
    {
        var root = new GameObject("Safe content", typeof(RectTransform), typeof(ParsecSafeArea));
        root.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)root.transform;
        Stretch(rect);
        return rect;
    }
    public static void Stretch(RectTransform rect)
    {
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
    private void Awake()
    {
        var canvas = GetComponent<Canvas>();
        var children = new Transform[transform.childCount];
        for(int i=0;i<children.Length;i++) children[i]=transform.GetChild(i);
        safe = CreateSafeRoot(canvas);
        foreach(var child in children) child.SetParent(safe, false);
        var menu = GetComponent<MenuManagerBehaviour>();
        if(menu != null)
        {
            var play = menu.homeMenu.GetComponentInChildren<Button>(true);
            var home = new GameObject("Responsive home", typeof(RectTransform));
            home.transform.SetParent(safe,false);
            Stretch((RectTransform)home.transform);
            play.transform.SetParent(home.transform,false);
            Place((RectTransform)play.transform,new Vector2(.5f,.5f),new Vector2(280,88),Vector2.zero);
            menu.homeMenu.SetActive(false);
            menu.homeMenu=home;
        }
        grid=GetComponentInChildren<GridLayoutGroup>(true);
        foreach(var label in GetComponentsInChildren<TMP_Text>(true))
        {
            label.raycastTarget=false;
            label.enableAutoSizing=true;
            label.fontSizeMin=24;
            label.fontSizeMax=32;
            if(label.name == "MidText" || label.name == "GlobalTime")
            {
                bool timer=label.name=="GlobalTime";
                Place(label.rectTransform,new Vector2(.5f,timer?.76f:.5f),new Vector2(480,110),Vector2.zero);
                label.fontSizeMax=timer?56:64;
                label.alignment=TextAlignmentOptions.Center;
            }
        }
        foreach(var button in GetComponentsInChildren<Button>(true))
            if(button.name=="ButtonReturn")
            {
                Place((RectTransform)button.transform,new Vector2(.5f,.25f),new Vector2(280,80),Vector2.zero);
                var tmp=button.GetComponentInChildren<TMP_Text>(true);
                if(tmp!=null)tmp.text=GameLocalization.T("level_menu");
                var legacy=button.GetComponentInChildren<Text>(true);
                if(legacy!=null)legacy.text=GameLocalization.T("level_menu");
            }
    }
    private void LateUpdate()
    {
        if(grid==null || safe.rect.size==lastSize) return;
        lastSize=safe.rect.size;
        bool portrait=lastSize.x<lastSize.y;
        int columns=portrait?1:3;
        float width=Mathf.Min(lastSize.x, portrait?560:900);
        float cellWidth=(width-20*(columns-1))/columns;
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount=columns;
        grid.cellSize=new Vector2(cellWidth,96);
        grid.spacing=new Vector2(20,20);
        grid.childAlignment=TextAnchor.MiddleCenter;
        Place((RectTransform)grid.transform,new Vector2(.5f,.5f),new Vector2(width,portrait?328:96),Vector2.zero);
    }
}
