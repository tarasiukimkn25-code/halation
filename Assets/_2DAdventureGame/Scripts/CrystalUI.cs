using UnityEngine;
using TMPro; // Бібліотека для роботи з TextMeshPro

public class CrystalUI : MonoBehaviour
{
    public static CrystalUI instance;
    public TextMeshProUGUI crystalText;
    
    private int crystalCount = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddCrystal(int amount = 1)
    {
        crystalCount += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (crystalText != null)
        {
            crystalText.text = crystalCount.ToString();
        }
    }
}