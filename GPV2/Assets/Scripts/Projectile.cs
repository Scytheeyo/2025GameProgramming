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
        if (other.CompareTag("Enemy"))
        {
            EnemyController_2D enemy = other.GetComponent<EnemyController_2D>();

            if (enemy != null)
            {
                // 데미지 적용
                enemy.TakeDamage(damage);

                // 넉백 적용 (힘이 0보다 클 때만)
                if (knockbackForce > 0)
                {
                    // 1. 밀려날 방향 계산 (적 위치 - 투사체 위치)
                    Vector2 knockbackDir = (other.transform.position - transform.position).normalized;

                    // 2. EnemyController의 함수 호출 (방향과 힘을 전달)
                    // 기존 코드의 enemy.BeginKnockback(0.3f) 에러 수정됨
                    enemy.BeginKnockback(knockbackDir, knockbackForce);
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
        if (ExplosionGo == null) return; // 폭발 프리팹이 없으면 리턴 (안전장치)

        GameObject explosion = (GameObject)Instantiate(ExplosionGo);
        explosion.transform.position = transform.position;
    }
}