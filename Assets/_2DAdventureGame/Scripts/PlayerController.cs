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

    // --- ЗВУК ЛАЗЕРА ТА КРОКІВ ---
    public AudioClip laserSound;
    private AudioSource audioSource;
    public AudioSource footstepsSource; // <--- ДОДАНО ДЛЯ КРОКІВ

    private NonPlayerCharacter lastNonPlayerCharacter;

    // --- ЗМІННІ ДЛЯ СЕНСОРА ТА МИШІ ---
    private Vector2 touchStartPos;
    private bool isTouching = false;

    // --- ЗМІННА ДЛЯ ГРАФІКИ ---
    private SpriteRenderer spriteRenderer;

    void Start()
    {   
        LaunchAction.Enable();
        MoveAction.Enable();
        TalkAction.Enable();

        animator = GetComponent<Animator>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 1. Зчитуємо рух з клавіатури
        move = MoveAction.ReadValue<Vector2>();

        // 2. СЕНСОРНЕ УПРАВЛІННЯ
        if (Pointer.current != null)
        {
            if (Pointer.current.press.wasPressedThisFrame)
            {
                touchStartPos = Pointer.current.position.ReadValue();
                isTouching = true;
            }
            else if (Pointer.current.press.isPressed && isTouching)
            {
                Vector2 touchCurrentPos = Pointer.current.position.ReadValue();
                Vector2 swipeDirection = touchCurrentPos - touchStartPos;
                
                if (swipeDirection.magnitude > 20f)
                {
                    move = swipeDirection.normalized;
                }
            }
            else if (Pointer.current.press.wasReleasedThisFrame)
            {
                isTouching = false;
            }
        }

        // Віддзеркалення спрайта
        if (move.x < 0) 
        {
            spriteRenderer.flipX = true;
        }
        else if (move.x > 0) 
        {
            spriteRenderer.flipX = false;
        }

        // Анімація бігу та звук кроків <--- ОНОВЛЕНО ТУТ
        if (move.magnitude > 0)
        {
            animator.SetBool("isRunning", true);

            if (footstepsSource != null && !footstepsSource.isPlaying)
            {
                footstepsSource.Play();
            }
        }
        else
        {
            animator.SetBool("isRunning", false);

            if (footstepsSource != null && footstepsSource.isPlaying)
            {
                footstepsSource.Stop();
            }
        }

        // Напрямок для стрільби та променів
        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            moveDirection.Set(move.x, move.y);
            moveDirection.Normalize();
        }
        
        // Логіка невразливості
        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }
        
        // Стрільба
        if (LaunchAction.WasPressedThisFrame())
        {
            Launch();
        }
        
        // Взаємодія з NPC
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
        // UIHandler.instance.SetHealthValue(currentHealth / (float)maxHealth);
    }

    void Launch()
    {
        animator.SetTrigger("Launch");
    }

    // Викликається в момент пострілу на кадрі анімації
    public void SpawnBulletOnFrame()
    {
        GameObject projectileObject = Instantiate(projectilePrefab,
            rigidbody2d.position + Vector2.up * 0.2f, 
            Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(moveDirection, 300);

        if (audioSource != null && laserSound != null)
        {
            audioSource.PlayOneShot(laserSound);
        }
    }

    void FindFriend()
    {
        if (TalkAction.WasPressedThisFrame() && UIHandler.instance != null)
            UIHandler.instance.DisplayDialogue();
    }
}
