using UnityEngine;

public class SwordAuraProjectile : MonoBehaviour
{
    [Header("설정")]
    public float speed = 15f;      // 날아가는 속도
    public int damage = 30;        // 데미지
    public float lifeTime = 2.0f;  // 사거리(시간)

    [Header("이펙트")]
    public GameObject hitEffectPrefab; // 7~8번 이미지로 만든 프리팹 연결

    private Vector2 direction;

    // 생성될 때 방향을 설정하는 함수
    public void Setup(Vector2 dir)
    {
        direction = dir;

        // 방향에 따라 이미지 좌우 반전
        if (direction.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // 일정 시간 후 자동 삭제 (너무 멀리 가는 것 방지)
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 설정된 방향으로 계속 이동
        transform.Translate(direction * speed * Time.deltaTime);
    }

    // 적과 부딪혔을 때
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 적 태그 확인
        if (collision.CompareTag("Enemy"))
        {
            EnemyController_2D enemy = collision.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Hit(); // 타격 처리 함수 호출
        }
        // 2. 벽(Ground) 태그 확인 (Wall 태그가 없다면 지우고 Ground만 쓰세요)
        else if (collision.CompareTag("Ground"))
        {
            Hit(); // 타격 처리 함수 호출
        }
    }

    // [중요] 타격 이펙트 생성 및 삭제를 담당하는 함수
    void Hit()
    {
        // 히트 이펙트가 연결되어 있다면 생성
        if (hitEffectPrefab != null)
        {
            // 이펙트 생성
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            // [핵심] 생성된 이펙트를 0.4초 뒤에 삭제 (애니메이션 길이에 맞춰 조절)
            Destroy(effect, 0.4f);
        }

        // 검기 자신은 즉시 삭제
        Destroy(gameObject);
    }
}