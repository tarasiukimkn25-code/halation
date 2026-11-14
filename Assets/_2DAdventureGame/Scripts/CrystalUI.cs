using System.Collections;
using UnityEngine;
using TMPro;

public class CrystalUI : MonoBehaviour
{
    public static CrystalUI instance;
    public TextMeshProUGUI crystalText;
    public Transform iconTransform; // Сюди перетягнемо іконку кристала
    private int crystalCount = 0;

    void Awake()
    {
        instance = this;
    }

    public void AddCrystal(int amount)
    {
        crystalCount += amount;
        crystalText.text = "x " + crystalCount.ToString();

        if (iconTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(PunchIcon());
        }
    }

    IEnumerator PunchIcon()
    {
        iconTransform.localScale = Vector3.one * 1.3f;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            iconTransform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, elapsed / duration);
            yield return null;
        }
        iconTransform.localScale = Vector3.one;
    }
}