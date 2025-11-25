using UnityEngine;
using System.Collections;

public class Boss_HeartQueen : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 500;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 12f;

    [Header("Summon Settings")]
    public float summonCooldown = 30f;
    private float lastSummonTime = 0f;

    [Tooltip("소환할 카드병사 프리팹들")]
    public GameObject[] minionPrefabs;

    [Tooltip("소환 위치들")]
    public Transform[] summonPoints;

    [Header("References")]
    public Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Collider2D myCollider; // 콜라이더 제어를 위해 추가

    private bool isDead = false;
    private bool isSummoning = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>(); // 컴포넌트 가져오기

        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(9999);
        }
        // 죽었으면 아무것도 안 함
        if (isDead || player == null) return;

        // Summoning 애니메이션 강제 OFF (꼬임 방지)
        if (!isSummoning)
            animator.SetBool("IsSummoning", false);

        // 소환 중이면 완전 정지
        if (isSummoning)
        {
            rb.velocity = Vector2.zero;
            animator.SetFloat("Speed", 0);
            return;
        }

        // 플레이어 바라보기 Flip 처리
        if (transform.position.x > player.position.x) sr.flipX = true;
        else sr.flipX = false;

        // 소환 쿨타임 체크 (경과 시간 기반)
        if (Time.time - lastSummonTime >= summonCooldown)
        {
            StartCoroutine(SummonMinions());
            lastSummonTime = Time.time;
            return;
        }

        // ===== 추격 거리 판정 =====
        float dist = Mathf.Abs(player.position.x - transform.position.x);

        // chaseRange 밖이면 추격, 안이면 멈춤 (원거리 공격 보스인 경우)
        if (dist >= chaseRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            Idle();
        }
    }

    void MoveTowardsPlayer()
    {
        animator.SetFloat("Speed", 1);
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);
    }

    void Idle()
    {
        animator.SetFloat("Speed", 0);
        rb.velocity = Vector2.zero;
    }

    IEnumerator SummonMinions()
    {
        isSummoning = true;

        rb.velocity = Vector2.zero; // 소환 시작 시 정지
        animator.SetFloat("Speed", 0);
        animator.SetBool("IsSummoning", true);

        // 애니메이션이 실제로 전환될 때까지 한 프레임 대기
        yield return null;

        // 현재 재생 중인 애니메이션(소환 모션)의 길이 가져오기
        // (주의: Animator의 상태 이름이 "Summon"인지 "Attack"인지 확인 필요)
        float waitTime = 1.0f; // 기본값
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        // 만약 현재 상태가 소환 상태라면 그 길이를 사용
        if (info.IsName("Summon") || info.IsTag("Summon"))
        {
            waitTime = info.length;
        }

        yield return new WaitForSeconds(waitTime);

        // 죽지 않았을 때만 소환 실행
        if (!isDead)
        {
            for (int i = 0; i < minionPrefabs.Length; i++)
            {
                if (summonPoints.Length > 0)
                {
                    Transform point = summonPoints[i % summonPoints.Length];
                    Instantiate(minionPrefabs[i % minionPrefabs.Length], point.position, Quaternion.identity);
                }
            }
        }

        animator.SetBool("IsSummoning", false);
        isSummoning = false;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;

        if (currentHealth <= 0)
            Die();
        else
            animator.SetTrigger("Hit");
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. 진행 중인 모든 행동(소환 등) 중단
        StopAllCoroutines();
        isSummoning = false;

        // 2. 애니메이션 설정
        animator.SetBool("IsDead", true);
        animator.SetBool("IsSummoning", false);
        animator.SetFloat("Speed", 0);

        // 3. 물리 및 충돌 끄기 (시체가 밀리지 않게)
        rb.velocity = Vector2.zero;
        rb.simulated = false; // 중력 및 물리 연산 제외
        if (myCollider != null) myCollider.enabled = false;
    }

    public void DestroyBoss()
    {
        // 보스 사망 효과(폭발 이펙트 등)가 있다면 여기서 생성
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}