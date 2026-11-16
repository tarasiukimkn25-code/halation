using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ParsecSafeArea : MonoBehaviour
{
    private Rect lastSafe;
    private Vector2Int lastSize;
    public static Rect GetPaddedArea(Rect safe, float scale)
    {
        float padding = 24f * scale;
        return new Rect(safe.x + padding, safe.y + padding,
            Mathf.Max(1, safe.width - padding * 2), Mathf.Max(1, safe.height - padding * 2));
    }
    private void LateUpdate()
    {
        var size = new Vector2Int(Screen.width, Screen.height);
        if (size.x <= 0 || size.y <= 0) return;
        var canvas = GetComponentInParent<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = size.x < size.y ? new Vector2(640, 960) : new Vector2(960, 640);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        var safe = GetPaddedArea(Screen.safeArea, canvas.scaleFactor);
        if (size == lastSize && safe == lastSafe) return;
        lastSize = size;
        lastSafe = safe;
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
        rect.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
