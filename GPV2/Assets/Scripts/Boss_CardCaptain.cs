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
    // 빨간색으로 깜빡임 (원하면 Color.white로 바꿔서 하얗게도 가능)
    public Color hitColor = new Color(1f, 0.4f, 0.4f);
    public float flashDuration = 0.1f; // 깜빡이는 시간

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
    private SpriteRenderer sr; // [추가] 색상을 바꾸기 위해 필요

    private bool isActing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>(); // [추가] 컴포넌트 가져오기

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
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    // ====================================================
    // [수정] 피격 함수 (색상 깜빡임 추가)
    // ====================================================
    public void TakeDamage(int dmg)
    {
        // [삭제됨] if (isActing) return;  <-- 이 줄 때문에 공격 중에 무적이었던 겁니다.

        // 1. 체력 감소 (언제든 데미지 입음)
        currentHealth -= dmg;

        // 2. 피격 효과 (색상 깜빡임) - 공격 중이어도 빨개짐
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }

        // 3. 피격 모션 (Hit 애니메이션)
        // [중요] 공격이나 소환 중(isActing)이 아닐 때만 움찔거려야 행동이 안 끊김
        if (!isActing)
        {
            animator.SetTrigger("Hit"); 
        }

        // 4. 사망 처리
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
                Transform point = (summonPoints != null && summonPoints.Length > 0)
                                  ? summonPoints[i % summonPoints.Length]
                                  : transform;
                Instantiate(minionPrefabs[i % minionPrefabs.Length], point.position, Quaternion.identity);
            }
        }

        lastSummonTime = Time.time;
        animator.SetBool("IsSummoning", false);
        yield return new WaitForSeconds(0.5f);
        isActing = false;
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
        // 죽을 땐 원래 색으로 돌려놓기
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