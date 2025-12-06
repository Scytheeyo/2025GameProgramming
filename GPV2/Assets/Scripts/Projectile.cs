using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("투사체 설정")]
    public float speed = 10f;      // 날아가는 속도
    public int damage = 10;        // 데미지
    public float lifeTime = 2.0f;  // 자동 삭제 시간

    [Header("이펙트 (선택)")]
    public GameObject hitEffect;   // 맞았을 때 터지는 이펙트 프리팹

    private Vector2 moveDirection;

    // Player.cs에서 호출하는 함수입니다.
    public void Setup(Vector2 dir)
    {
        moveDirection = dir;

        // 왼쪽을 보고 쏘면 이미지도 좌우 반전
        if (moveDirection.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -1;
            transform.localScale = scale;
        }

        // 일정 시간 후 자동 삭제 (메모리 관리)
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 설정된 방향으로 계속 이동
        transform.Translate(moveDirection * speed * Time.deltaTime);
    }

    // Is Trigger가 켜진 콜라이더와 충돌했을 때 실행
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 적과 충돌했는지 확인
        if (collision.CompareTag("Enemy"))
        {
            EnemyController_2D enemy = collision.GetComponent<EnemyController_2D>();

            // 만약 Collider가 자식에 있다면 부모에서 스크립트 찾기
            if (enemy == null) enemy = collision.GetComponentInParent<EnemyController_2D>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage); // 데미지 주기
            }

            HitAndDestroy();
        }
        // 땅이나 벽에 닿았을 때
        else if (collision.CompareTag("Ground"))
        {
            HitAndDestroy();
        }
    }

    void HitAndDestroy()
    {
        // 피격 이펙트가 있다면 생성
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // 투사체 삭제
        Destroy(gameObject);
    }
}