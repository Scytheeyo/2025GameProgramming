using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject ExplosionGo;

    [Header("기본 설정")]
    public float speed = 20f;
    public int damage = 15;
    public float lifeTime = 3f;

    [Header("넉백 설정")]
    public float knockbackForce = 0f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Setup(Vector2 moveDirection, float knockbackValue = 0f)
    {
        rb.velocity = moveDirection.normalized * speed;
        knockbackForce = knockbackValue;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyController_2D enemy = other.GetComponent<EnemyController_2D>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                if (knockbackForce > 0)
                {
                    enemy.BeginKnockback(0.3f);

                    Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                        enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }
            Destroy(gameObject);
            PlayExplosion();
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    void PlayExplosion()
    {
        if (knockbackForce == 0) return;
        GameObject explosion = (GameObject)Instantiate(ExplosionGo);
        explosion.transform.position = transform.position;
    }
}