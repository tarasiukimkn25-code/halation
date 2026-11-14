using UnityEngine;

public class BreakableCrystal : MonoBehaviour
{
    private Animator animator;
    private Collider2D col;
    private bool isBroken = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    public void Shatter()
    {
        if (isBroken) return;
        isBroken = true;

        // Додаємо 1 кристал у лічильник UI
        if (CrystalUI.instance != null)
        {
            CrystalUI.instance.AddCrystal(1);
        }

        // Вимикаємо колайдер, щоб нові кулі не застрягали в уламках
        col.enabled = false;
        
        // Запускаємо анімацію розбиття
        animator.SetTrigger("Break");
    }

    // Цей метод викличеться в кінці анімації Crystal_Break через Animation Event
    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}