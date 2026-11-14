using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    Rigidbody2D rigidbody2d;
    Vector2 move;
    public float speed = 3.0f;
    
    public int maxHealth = 5;
    public int health { get { return currentHealth; } }
    int currentHealth; 
    
    public float timeInvincible = 2.0f;
    bool isInvincible;
    float damageCooldown;

    Animator animator;
    Vector2 moveDirection = new Vector2(1, 0);

    public GameObject projectilePrefab;
    public InputAction LaunchAction;
    public InputAction TalkAction;

    private NonPlayerCharacter lastNonPlayerCharacter;

    // --- НОВІ ЗМІННІ ДЛЯ СЕНСОРА ТА МИШІ ---
    private Vector2 touchStartPos;
    private bool isTouching = false;

    void Start()
    {   
        LaunchAction.Enable();
        animator = GetComponent<Animator>();
        MoveAction.Enable();
        rigidbody2d = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        TalkAction.Enable();
    }

    void Update()
    {
        // 1. Спочатку зчитуємо рух з клавіатури (залишається як було)
        move = MoveAction.ReadValue<Vector2>();

        // 2. ДОДАЄМО СЕНСОРНЕ УПРАВЛІННЯ (Pointer працює і для миші, і для пальця)
        if (Pointer.current != null)
        {
            // Коли натиснули мишку або торкнулися екрана
            if (Pointer.current.press.wasPressedThisFrame)
            {
                touchStartPos = Pointer.current.position.ReadValue();
                isTouching = true;
            }
            // Поки тримаємо і тягнемо
            else if (Pointer.current.press.isPressed && isTouching)
            {
                Vector2 touchCurrentPos = Pointer.current.position.ReadValue();
                Vector2 swipeDirection = touchCurrentPos - touchStartPos;
                
                // Мертва зона (щоб персонаж не дрижав від мінімального руху)
                if (swipeDirection.magnitude > 20f)
                {
                    move = swipeDirection.normalized; // Рухаємось у напрямку свайпу
                }
            }
            // Коли відпустили
            else if (Pointer.current.press.wasReleasedThisFrame)
            {
                isTouching = false;
            }
        }

        // Далі йде оригінальна логіка обробки напрямку та анімації
        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            moveDirection.Set(move.x, move.y);
            moveDirection.Normalize();
        }
        
        animator.SetFloat("Look X", moveDirection.x);
        animator.SetFloat("Look Y", moveDirection.y);
        animator.SetFloat("Speed", move.magnitude);
        
        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }
        
        if (LaunchAction.WasPressedThisFrame())
        {
            Launch();
        }
        
        RaycastHit2D hit = Physics2D.Raycast(
            rigidbody2d.position + Vector2.up * 0.2f,
            moveDirection, 1.5f, LayerMask.GetMask("NPC"));
            
        NonPlayerCharacter npc = hit.collider != null
            ? hit.collider.GetComponent<NonPlayerCharacter>() ??
              hit.collider.GetComponentInParent<NonPlayerCharacter>()
            : null;

        if (npc != null)
        {
            if (npc.dialogueBubble != null)
                npc.dialogueBubble.SetActive(true);
            lastNonPlayerCharacter = npc;
            if (UIHandler.instance != null)
                UIHandler.instance.SetTalkTarget(npc);
            FindFriend();
        }
        else
        {
            if (lastNonPlayerCharacter != null)
            {
                if (lastNonPlayerCharacter.dialogueBubble != null)
                    lastNonPlayerCharacter.dialogueBubble.SetActive(false);
                lastNonPlayerCharacter = null;
            }

            if (UIHandler.instance != null)
                UIHandler.instance.SetTalkTarget(null);
        }
    }   

    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position + move * speed * Time.deltaTime; 
        rigidbody2d.MovePosition(position);
    }
    public void ChangeHealth(int amount)
    {
        if (amount < 0)
        {
            if (isInvincible) return;
            
            isInvincible = true;
            damageCooldown = timeInvincible;
            animator.SetTrigger("Hit");
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UIHandler.instance.SetHealthValue(currentHealth / (float)maxHealth);
    }

    void Launch()
    {
        GameObject projectileObject = Instantiate(projectilePrefab,
            rigidbody2d.position + Vector2.up * 0.5f,
            Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(moveDirection, 300);

        animator.SetTrigger("Launch");
    }

    void FindFriend()
    {
        if (TalkAction.WasPressedThisFrame())
        {
            UIHandler.instance.DisplayDialogue();
        }
    }
}
