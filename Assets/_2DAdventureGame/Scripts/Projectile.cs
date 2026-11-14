using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D rigidbody2d;

    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {

        if (transform.position.magnitude > 100.0f)
        {
            Destroy(gameObject);
        }

    }

    public void Launch(Vector2 direction, float force)
    {
        // Даємо фізичний поштовх (ваш старий код)
        rigidbody2d.AddForce(direction * force);

        // НОВИЙ КОД: Повертаємо спрайт лазера в напрямку польоту
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Fix();
        }

        BreakableCrystal crystal = other.GetComponent<BreakableCrystal>();
        if (crystal != null)
        {
            crystal.Shatter();
        }

        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        BreakableCrystal crystal = collision.gameObject.GetComponent<BreakableCrystal>();
        if (crystal != null)
        {
            crystal.Shatter();
        }

        Destroy(gameObject);
    }
}