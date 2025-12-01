using UnityEngine;

public class CloverProjectile : MonoBehaviour
{
    public int damage = 10;
    public GameObject hitEffectPrefab; // 피격 이펙트 (우측 하단 이미지용)
    public float lifeTime = 5f;        // 5초 지나면 자동 삭제

    void Start()
    {
        // 너무 오래 날아가면 메모리 낭비니 삭제
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어에게 닿았을 때
        if (collision.CompareTag("Player"))
        {
            // 플레이어 데미지 처리 (플레이어 스크립트에 따라 수정 필요)
            // Example: collision.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Debug.Log("플레이어 명중! 데미지: " + damage);

            Hit();
        }
        // 2. 벽(Ground)에 닿았을 때
        else if (collision.CompareTag("Ground"))
        {
            Hit();
        }
    }

    void Hit()
    {
        // 피격 이펙트 생성 (우측 하단 이미지 활용)
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 투사체 삭제
        Destroy(gameObject);
    }
}