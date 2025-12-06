using UnityEngine;
using System.Collections;

// [참고] 투사체가 제대로 충돌을 감지하려면, 
// 이 오브젝트나 부딪히는 대상 둘 중 하나는 반드시 Rigidbody2D를 가지고 있어야 합니다.
// 보통 투사체에 Rigidbody2D(Body Type: Kinematic, Is Trigger: Check)를 넣는 것이 일반적입니다.
public class CloverProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    // 이 damage 값은 발사하는 적(Enemy_Clover)이 발사 순간에 덮어씌우게 됩니다.
    public int damage = 10;
    public float lifeTime = 5f;    // 5초 지나면 자동 삭제
    private float currentLifeTime = 0f; // ★ 현재 흐른 시간 (수동 관리용)

    [Header("Effects")]
    public GameObject hitEffectPrefab; // 피격 이펙트 프리팹

    private Rigidbody2D rb;
    private Vector2 savedVelocity;
    private bool isFrozen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 생성 후 설정된 시간(lifeTime)이 지나면 스스로 파괴되어 메모리 관리
        //Destroy(gameObject, lifeTime);
        currentLifeTime = 0f;
    }

    void Update()
    {
        // ★ [핵심] 시간이 멈췄으면(isFrozen), 수명도 줄어들지 않음 -> 리턴
        if (isFrozen) return;

        // 시간이 멈추지 않았을 때만 시간을 흐르게 함
        currentLifeTime += Time.deltaTime;

        // ★ 수명이 다 되었는지 직접 체크해서 삭제
        if (currentLifeTime >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
    public void FreezeEnemyBullet(float duration)
    {
        if (isFrozen) return; // 중복 실행 방지
        StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;

        // 1. 현재 속도 저장 및 물리 정지
        if (rb != null)
        {
            savedVelocity = rb.velocity;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // 2. 시간 정지 지속시간 동안 대기
        // (이 동안 Update()에서는 currentLifeTime이 증가하지 않음 = 수명 연장)
        yield return new WaitForSeconds(duration);

        // 3. 다시 움직임
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = savedVelocity; // 저장해둔 속도로 다시 발사
        }

        isFrozen = false;
    }
    // Is Trigger가 체크된 콜라이더와 충돌했을 때 호출
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFrozen) return;
        // 1. 플레이어에게 닿았을 때
        if (collision.CompareTag("Player"))
        {
            // ================================================================
            // [수정됨] 실제 데미지 적용 로직
            // ================================================================
            // 플레이어 오브젝트에 있는 'TakeDamage'라는 함수를 찾아서 실행하고 damage 값을 전달합니다.
            // SendMessageOptions.DontRequireReceiver: 만약 플레이어에게 해당 함수가 없더라도 에러를 내지 않고 무시합니다.
            collision.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // 로그 확인용 (필요 없으면 주석 처리)
            // Debug.Log($"클로버 투사체 명중! {collision.name}에게 데미지: {damage}");

            // 적중 처리 (이펙트 생성 및 삭제)
            Hit();
        }
        // 2. 벽이나 땅(Ground 태그)에 닿았을 때
        // (만약 레이어를 사용한다면 LayerMask.NameToLayer("Ground") 등을 사용해야 합니다)
        else if (collision.CompareTag("Ground")) // "Wall" 태그도 추가 예시
        {
            // 벽에 맞았을 때도 적중 처리
            Hit();
        }
    }

    // 공통된 적중 처리 함수
    void Hit()
    {
        // 피격 이펙트 프리팹이 연결되어 있다면 생성
        if (hitEffectPrefab != null)
        {
            // 이펙트 생성 (위치는 현재 투사체 위치, 회전은 불필요하므로 identity)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 임무를 다했으니 투사체 자기 자신 삭제
        Destroy(gameObject);
    }
}