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

    // 기본값 설정 수정: knockbackValue가 없으면 0으로 초기화
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
        if (other.CompareTag("Enemy")|| other.CompareTag("Boss"))
        {
            IDamageable enemy = other.GetComponent<IDamageable>();

            if (enemy != null)
            {
                // 데미지 적용
                enemy.TakeDamage(damage);

                if (knockbackForce > 0)
                {
                    // 문법 설명: "enemy가 만약 EnemyController_2D 타입이라면, 그 정보를 targetEnemy 변수에 담고 true를 반환해라"
                    if (enemy is EnemyController_2D targetEnemy)
                    {
                        Vector2 knockbackDir = (other.transform.position - transform.position).normalized;

                        // 이제 targetEnemy는 EnemyController_2D 타입이므로 BeginKnockback을 사용할 수 있습니다.
                        targetEnemy.BeginKnockback(knockbackDir, knockbackForce);
                    }
                }
                Destroy(gameObject);
                PlayExplosion();
            }
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    void PlayExplosion()
    {
        if (ExplosionGo == null) return; // 폭발 프리팹이 없으면 리턴 (안전장치)

        GameObject explosion = (GameObject)Instantiate(ExplosionGo);
        explosion.transform.position = transform.position;
    }
    
}