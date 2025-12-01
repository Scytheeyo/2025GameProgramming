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
        if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {
            IDamageable enemy = other.GetComponent<IDamageable>();
            if (enemy != null)
            {
<<<<<<< Updated upstream
                enemy.TakeDamage(damage);

                if (knockbackForce > 0)
                {
                    enemy.BeginKnockback(0.3f);

                    Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                        enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
=======
                // 1. 데미지는 인터페이스를 가진 모든 대상에게 적용
                enemy.TakeDamage(damage);

                // 2. 넉백 처리 (EnemyController_2D 인지 확인)
                if (knockbackForce > 0)
                {
                    // 문법 설명: "enemy가 만약 EnemyController_2D 타입이라면, 그 정보를 targetEnemy 변수에 담고 true를 반환해라"
                    if (enemy is EnemyController_2D targetEnemy)
                    {
                        Vector2 knockbackDir = (other.transform.position - transform.position).normalized;

                        // 이제 targetEnemy는 EnemyController_2D 타입이므로 BeginKnockback을 사용할 수 있습니다.
                        targetEnemy.BeginKnockback(knockbackDir, knockbackForce);
>>>>>>> Stashed changes
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