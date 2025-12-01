using UnityEngine;
using System.Collections;

public class Boss_CardCaptain : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 2.5f;
    public float chaseRange = 10f;
    public float attackRange = 1.2f;
    public int maxHealth = 300;
    public int currentHealth;

    [Header("피격 효과 설정")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f);
    public float flashDuration = 0.1f;

    [Header("3단 콤보 공격")]
    public float attackCooldown = 2.0f;
    private float lastAttackTime = -999f;
    private int attackComboIndex = 0;

    [Header("소환 패턴")]
    public float summonCooldown = 20f;
    private float lastSummonTime = -999f;
    public GameObject[] minionPrefabs;
    public Transform[] summonPoints;

    [Header("연결")]
    public Transform attackPoint;
    public Transform player;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D myCollider;
    private SpriteRenderer sr;

    private bool isActing = false;
    // ★ [추가] 시간 정지 상태 변수
    private bool isFrozen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (attackPoint == null) attackPoint = transform;
    }

    void Update()
    {
        if (player == null) return;

        // ★ [추가] 얼어있으면(시간 정지) 모든 동작 정지
        if (isFrozen)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (isActing)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distToPlayer = Vector2.Distance(attackPoint.position, player.position);

        if (Time.time >= lastSummonTime + summonCooldown)
        {
            StartCoroutine(SummonRoutine());
            return;
        }

        if (distToPlayer <= attackRange)
        {
            StopMovement();
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            MoveTowardsPlayer();
        }

        // 테스트용 (K키로 자해)
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    // ====================================================
    // 피격 처리
    // ====================================================
    public void TakeDamage(int dmg)
    {
        // 1. 체력 감소
        currentHealth -= dmg;

        // 2. 피격 효과 (색상 깜빡임)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }

        // 3. 피격 모션 (공격 중이거나 얼어있지 않을 때만)
        if (!isActing && !isFrozen)
        {
            animator.SetTrigger("Hit");
        }

        // 4. 사망 처리
        if (currentHealth <= 0) Die();
    }

    IEnumerator HitFlashRoutine()
    {
        if (sr != null)
        {
            sr.color = hitColor;
            yield return new WaitForSeconds(flashDuration);
            sr.color = Color.white;
        }
    }

    // ====================================================
    // ★ [신규] 시간 정지 (StaffSkillManager에서 호출)
    // ====================================================
    public void FreezeBoss(float duration)
    {
        if (!isFrozen)
        {
            StartCoroutine(FreezeRoutine(duration));
        }
    }

    IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;

        // 1. 물리 정지 (밀리지 않게)
        Vector2 originalVelocity = rb.velocity;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        // 2. 애니메이션 멈춤
        float originalAnimSpeed = animator.speed;
        animator.speed = 0;

        yield return new WaitForSeconds(duration);

        // 3. 복구
        rb.isKinematic = false;
        animator.speed = originalAnimSpeed;
        isFrozen = false;
    }

    // ====================================================
    // 공격 패턴
    // ====================================================
    IEnumerator AttackRoutine()
    {
        isActing = true;
        StopMovement();
        LookAtPlayer();

        if (attackComboIndex == 0) { animator.SetTrigger("Attack1"); attackComboIndex = 1; }
        else if (attackComboIndex == 1) { animator.SetTrigger("Attack2"); attackComboIndex = 2; }
        else { animator.SetTrigger("Attack3"); attackComboIndex = 0; }

        lastAttackTime = Time.time;
        yield return new WaitForSeconds(1.0f);

        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");

        isActing = false;
    }

    // ====================================================
    // 소환 패턴 (수정됨: Missing Prefab 방지)
    // ====================================================
    IEnumerator SummonRoutine()
    {
        isActing = true;
        StopMovement();
        animator.SetBool("IsSummoning", true);

        yield return new WaitForSeconds(1.0f);

        if (minionPrefabs != null && minionPrefabs.Length > 0)
        {
            for (int i = 0; i < minionPrefabs.Length; i++)
            {
                // ★ [안전장치] 프리팹이 Missing이거나 비어있으면 건너뜀 (에러 방지)
                if (minionPrefabs[i % minionPrefabs.Length] == null)
                {
                    Debug.LogWarning($"[Boss] Minion Prefab Index {i} is Missing or Null!");
                    continue;
                }

                Transform point = (summonPoints != null && summonPoints.Length > 0)
                                  ? summonPoints[i % summonPoints.Length]
                                  : transform;

                Instantiate(minionPrefabs[i % minionPrefabs.Length], point.position, Quaternion.identity);
            }
        }

        lastSummonTime = Time.time;
        animator.SetBool("IsSummoning", false);
        yield return new WaitForSeconds(0.5f);

        isActing = false; // 동작 완료
    }

    void MoveTowardsPlayer()
    {
        animator.SetFloat("Speed", 1);
        LookAtPlayer();
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);
    }

    void StopMovement()
    {
        rb.velocity = Vector2.zero;
        animator.SetFloat("Speed", 0);
    }

    void LookAtPlayer()
    {
        float sizeX = Mathf.Abs(transform.localScale.x);
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(sizeX, transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-sizeX, transform.localScale.y, transform.localScale.z);
    }

    void Die()
    {
        StopAllCoroutines();
        sr.color = Color.white;
        isActing = true;
        animator.SetTrigger("Die");
        rb.velocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.0f);
    }

    public void DealDamage()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, LayerMask.GetMask("Player"));
        foreach (Collider2D p in hitPlayers)
        {
            // 플레이어에게 데미지 주는 로직 (Player 스크립트의 TakeDamage 호출 필요)
            Player pc = p.GetComponent<Player>();
            if (pc != null) pc.TakeDamage(10);
            Debug.Log("플레이어 피격!");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}