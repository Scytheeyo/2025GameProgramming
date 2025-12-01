using UnityEngine;
using System.Collections;

public class Boss_HeartQueen : MonoBehaviour
{
    [Header("기본 설정")]
    public float moveSpeed = 3f;
    public float chaseRange = 8f;
    public float attackRange = 1.0f;
    public int maxHealth = 500;
    public int currentHealth;

    [Header("피격 효과 설정")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f); // 살짝 연한 빨강
    public float flashDuration = 0.1f; // 깜빡이는 시간

    [Header("일반 공격")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime = -999f;
    private int attackComboIndex = 0;

    [Header("소환 패턴 (복구됨)")]
    public float summonCooldown = 30f; // 30초마다 소환
    private float lastSummonTime = -999f;
    public GameObject[] minionPrefabs; // 소환할 쫄병들
    public Transform[] summonPoints;   // 소환 위치

    [Header("돌진 공격")]
    public float dashSpeed = 15f;
    public float reloadDelay = 3.0f;
    private float lastDashEndTime = -999f;
    [SerializeField] private bool isDashReady = false;

    [Header("점프 찍기 (필살기)")]
    public int jumpDamage = 30;
    public float jumpHeight = 15f;
    private int nextJumpHealth;

    [Header("연결")]
    public Transform attackPoint;
    public Transform player;
    public GameObject landEffectPrefab;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D myCollider;
    private SpriteRenderer sr; // [추가] 색상 변경용

    private bool isActing = false;  // 행동 중 잠금
    private bool isDashing = false;
    private Vector2 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>(); // [추가] 컴포넌트 가져오기

        currentHealth = maxHealth;
        isDashReady = false; // 시작 시 돌진 끄기
        lastSummonTime = Time.time; // 시작하자마자 소환하지 않게 초기화

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (attackPoint == null) attackPoint = transform;

        // 점프 패턴 체력 설정
        nextJumpHealth = maxHealth - 100;
    }

    void Update()
    {
        // J키 테스트 (삭제 가능)
        if (Input.GetKeyDown(KeyCode.J) && !isActing) StartCoroutine(JumpSmashRoutine());
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
        if (player == null) return;

        // 1. 행동 중이면 정지
        if (isActing)
        {
            if (!isDashing) rb.velocity = Vector2.zero;
            else rb.velocity = dashDirection * dashSpeed;
            return;
        }

        // 2. 거리 계산
        float distToPlayer = Vector2.Distance(attackPoint.position, player.position);
        float distFromBody = Vector2.Distance(transform.position, player.position);

        // --- 장전 로직 ---
        if (distFromBody <= chaseRange)
        {
            if (Time.time >= lastDashEndTime + reloadDelay)
            {
                if (!isDashReady) isDashReady = true;
            }
        }

        // ====================================================
        // [1순위] 소환 패턴 체크 (쿨타임 되면 무조건 실행)
        // ====================================================
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            StartCoroutine(SummonRoutine());
            return; // 소환하러 갔으니 아래 로직 무시
        }

        // ====================================================
        // [2순위] 행동 결정 (공격 / 돌진 / 이동)
        // ====================================================
        if (distToPlayer <= attackRange)
        {
            StopMovement();
            if (Time.time >= lastAttackTime + attackCooldown)
                StartCoroutine(AttackRoutine());
        }
        else if (distFromBody > chaseRange)
        {
            if (isDashReady)
            {
                StartCoroutine(DashPatternRoutine());
                isDashReady = false;
            }
            else MoveTowardsPlayer();
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    public void TakeDamage(int dmg)
    {
        // [수정] 점프(필살기) 패턴 중일 때만 무적 (화면 밖으로 나갔을 때)
        // (Kinematic 상태면 점프 중이라고 판단)
        if (rb.bodyType == RigidbodyType2D.Kinematic) return;

        // 1. 체력 감소 (행동 중이라도 데미지는 입어야 함)
        currentHealth -= dmg;

        // 2. 피격 효과 (색상 깜빡임) - 행동 중이라도 빨개져야 함
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }

        // 3. 피격 모션 (Hit 애니메이션)
        // [중요] 행동 중(공격/돌진)이 아닐 때만 움찔거려야 공격이 안 끊김
        if (!isActing)
        {
            animator.SetTrigger("Hit");
        }

        // 4. 점프 패턴 발동 체크
        if (currentHealth <= nextJumpHealth)
        {
            nextJumpHealth -= 100;

            // 하던거 멈추고 강제 패턴 발동
            StopAllCoroutines();
            if (sr != null) sr.color = Color.white; // 색깔 복구
            StartCoroutine(JumpSmashRoutine());
        }

        // 5. 사망 체크
        if (currentHealth <= 0) Die();
    }

    // ====================================================
    // [신규] 깜빡임 코루틴
    // ====================================================
    IEnumerator HitFlashRoutine()
    {
        sr.color = hitColor; // 빨간색
        yield return new WaitForSeconds(flashDuration); // 0.1초 대기
        sr.color = Color.white; // 원상복구
    }

    // ====================================================
    // 소환 코루틴 (복구됨)
    // ====================================================
    IEnumerator SummonRoutine()
    {
        isActing = true; // 행동 잠금
        rb.velocity = Vector2.zero;
        animator.SetFloat("Speed", 0);

        // 소환 애니메이션 시작 (Bool 사용)
        animator.SetBool("IsSummoning", true);

        // 애니메이션 재생될 때까지 대기 (예: 1초)
        yield return new WaitForSeconds(1.0f);

        // 쫄병 생성
        if (minionPrefabs != null && minionPrefabs.Length > 0)
        {
            for (int i = 0; i < minionPrefabs.Length; i++)
            {
                Transform point = (summonPoints != null && summonPoints.Length > 0)
                                  ? summonPoints[i % summonPoints.Length]
                                  : transform; // 포인트 없으면 내 위치에

                Instantiate(minionPrefabs[i % minionPrefabs.Length], point.position, Quaternion.identity);
            }
            Debug.Log("여왕: 나와라 카드병사들!");
        }

        // 쿨타임 갱신
        lastSummonTime = Time.time;

        // 애니메이션 종료
        animator.SetBool("IsSummoning", false);
        yield return new WaitForSeconds(0.5f); // 후딜레이

        isActing = false; // 행동 잠금 해제
    }

    // ====================================================
    // 점프 찍기 코루틴
    // ====================================================
    IEnumerator JumpSmashRoutine()
    {
        isActing = true;
        isDashing = false;

        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (myCollider != null) myCollider.enabled = false;

        animator.ResetTrigger("AttackV");
        animator.ResetTrigger("AttackH");
        animator.ResetTrigger("Dash");
        animator.ResetTrigger("Hit");
        animator.SetBool("IsSummoning", false); // 소환 중이었다면 끄기

        animator.SetTrigger("Jump");
        yield return new WaitForSeconds(0.5f);

        float startY = transform.position.y;
        float targetY = startY + jumpHeight;
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(transform.position.x, targetY, transform.position.z);

        while (elapsed < 0.5f)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        yield return new WaitForSeconds(3.0f);

        if (player != null)
        {
            Vector3 groundPos = new Vector3(player.position.x, startY, player.position.z);
            Vector3 skyPos = new Vector3(player.position.x, targetY, player.position.z);

            transform.position = skyPos;
            animator.SetTrigger("Fall");

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                transform.position = Vector3.Lerp(skyPos, groundPos, elapsed / 0.2f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = groundPos;
        }

        if (landEffectPrefab != null)
            Instantiate(landEffectPrefab, transform.position, Quaternion.identity);

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, 2.5f, LayerMask.GetMask("Player"));
        foreach (Collider2D p in hitPlayers)
        {
            Debug.Log("쿵! 데미지: " + jumpDamage);
            // p.GetComponent<PlayerHealth>().TakeDamage(jumpDamage);
        }

        yield return new WaitForSeconds(1.0f);

        rb.bodyType = RigidbodyType2D.Dynamic;
        if (myCollider != null) myCollider.enabled = true;

        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Fall");
        isActing = false;
    }

    IEnumerator DashPatternRoutine()
    {
        isActing = true;
        LookAtPlayer();
        rb.velocity = Vector2.zero;
        dashDirection = (player.position - transform.position).normalized;
        animator.SetTrigger("Dash");
        yield return new WaitForSeconds(1.0f);
        if (isActing) EndDashMove();
    }

    IEnumerator AttackRoutine()
    {
        isActing = true;
        rb.velocity = Vector2.zero;
        animator.SetFloat("Speed", 0);
        LookAtPlayer();

        if (attackComboIndex == 0) { animator.SetTrigger("AttackV"); attackComboIndex = 1; }
        else { animator.SetTrigger("AttackH"); attackComboIndex = 0; }

        lastAttackTime = Time.time;
        yield return new WaitForSeconds(0.8f);
        animator.ResetTrigger("AttackV"); animator.ResetTrigger("AttackH");
        isActing = false;
    }

    public void StartDashMove() { isDashing = true; }
    public void EndDashMove() { isDashing = false; isActing = false; rb.velocity = Vector2.zero; lastDashEndTime = Time.time; }
    void MoveTowardsPlayer() { animator.SetFloat("Speed", 1); LookAtPlayer(); float dirX = Mathf.Sign(player.position.x - transform.position.x); rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y); }
    void StopMovement() { rb.velocity = Vector2.zero; animator.SetFloat("Speed", 0); }
    void LookAtPlayer() { float sizeX = Mathf.Abs(transform.localScale.x); if (player.position.x > transform.position.x) transform.localScale = new Vector3(sizeX, transform.localScale.y, transform.localScale.z); else transform.localScale = new Vector3(-sizeX, transform.localScale.y, transform.localScale.z); }

    void Die()
    {
        StopAllCoroutines();
        isActing = true;

        // [추가] 죽을 때 원래 색으로 복구
        if (sr != null) sr.color = Color.white;

        animator.SetTrigger("Die");
        rb.velocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;
    }

    void OnDrawGizmos() { if (isDashReady) Gizmos.color = Color.green; else Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, chaseRange); if (attackPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(attackPoint.position, attackRange); } }
}