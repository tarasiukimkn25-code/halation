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
    public float npcInteractionRadius = 2.6f;

    public AudioClip laserSound;
    private AudioSource audioSource;
    public AudioSource footstepsSource;

    private NonPlayerCharacter lastNonPlayerCharacter;

    // --- ДОДАНО ЗМІННУ ДЛЯ ГРАФІКИ ---
    private SpriteRenderer spriteRenderer;

    void Start()
    {   
        LaunchAction.Enable();
        MoveAction.Enable();
        TalkAction.Enable();

        animator = GetComponent<Animator>();
        rigidbody2d = GetComponent<Rigidbody2D>();

        // Отримуємо компонент для віддзеркалення спрайту
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Клавіатура й екранний D-pad керують одним рухом.
        move = MoveAction.ReadValue<Vector2>();
        UIHandler uiHandler = UIHandler.instance;
        if (uiHandler != null && uiHandler.MobileMoveInput.sqrMagnitude > 0f)
            move = uiHandler.MobileMoveInput;

        // --- ДОДАНА ЛОГІКА ВІДДЗЕРКАЛЕННЯ (FLIP) ---
        if (move.x < 0) 
        {
            spriteRenderer.flipX = true;  // Дивимось вліво
        }
        else if (move.x > 0) 
        {
            spriteRenderer.flipX = false; // Дивимось вправо
        }

        // --- ДОДАНА ЛОГІКА ПЕРЕМИКАННЯ АНІМАЦІЇ (isRunning) ---
        if (move.magnitude > 0)
        {
            animator.SetBool("isRunning", true);

            if (footstepsSource != null && !footstepsSource.isPlaying)
                footstepsSource.Play();
        }
        else
        {
            animator.SetBool("isRunning", false);

            if (footstepsSource != null && footstepsSource.isPlaying)
                footstepsSource.Stop();
        }

        // Ваша оригінальна логіка напрямку (для стрільби та променів)
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
        bool mobileShootPressed = uiHandler != null && uiHandler.ConsumeMobileShootPressed();
        if (LaunchAction.WasPressedThisFrame() || mobileShootPressed)
        {
            Launch();
        }
        
        // Кнопка взаємодії з'являється, коли NPC потрапляє у збільшений радіус гравця.
        Collider2D[] nearbyNpcColliders = Physics2D.OverlapCircleAll(
            rigidbody2d.position + Vector2.up * 0.2f,
            npcInteractionRadius,
            LayerMask.GetMask("NPC"));

        NonPlayerCharacter npc = null;
        float closestNpcDistance = float.PositiveInfinity;
        foreach (Collider2D npcCollider in nearbyNpcColliders)
        {
            NonPlayerCharacter nearbyNpc = npcCollider.GetComponent<NonPlayerCharacter>() ??
                npcCollider.GetComponentInParent<NonPlayerCharacter>();
            if (nearbyNpc == null)
                continue;

            float distance = ((Vector2)nearbyNpc.transform.position - rigidbody2d.position).sqrMagnitude;
            if (distance < closestNpcDistance)
            {
                closestNpcDistance = distance;
                npc = nearbyNpc;
            }
        }

        if (npc != null)
        {
            if (lastNonPlayerCharacter != null && lastNonPlayerCharacter != npc &&
                lastNonPlayerCharacter.dialogueBubble != null)
            {
                lastNonPlayerCharacter.dialogueBubble.SetActive(false);
            }

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

    // Цей метод викликається натисканням кнопки (тільки запускає анімацію)
    void Launch()
    {
        animator.SetTrigger("Launch");
    }

    // А ЦЕЙ метод ми прив'яжемо до конкретного кадру анімації!
    public void SpawnBulletOnFrame()
    {
        GameObject projectileObject = Instantiate(projectilePrefab,
            rigidbody2d.position + Vector2.up * 0.2f, 
            Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(moveDirection, 300); // 300 або ваша швидкість

        if (audioSource != null && laserSound != null)
            audioSource.PlayOneShot(laserSound);
    }

    void FindFriend()
    {
        if (TalkAction.WasPressedThisFrame() && UIHandler.instance != null)
            UIHandler.instance.DisplayDialogue();
    }
}
