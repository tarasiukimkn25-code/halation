using UnityEngine;

public class BreakableCrystal : MonoBehaviour
{
    public AudioClip breakSound;

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

        // Програємо звук розбиття у позиції кристала
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        if (CrystalUI.instance != null)
        {
            CrystalUI.instance.AddCrystal(1);
        }

        col.enabled = false;
        animator.SetTrigger("Break");
    }

    // Цей метод викличеться в кінці анімації Crystal_Break через Animation Event
    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}