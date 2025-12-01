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

    [Header("공격력 설정")] // [신규] 데미지 조절용 변수
    public int attackDamage = 15; // 여기서 데미지 수치를 조정하세요!

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

        // 테스트용 (K키 누르면 자해)
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }

        if (!isActing)
        {
            animator.SetTrigger("Hit");
        }

        if (currentHealth <= 0) Die();
    }

    IEnumerator HitFlashRoutine()
    {
        sr.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        sr.color = Color.white;
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
                if (minionPrefabs[i % minionPrefabs.Length] == null) continue;

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
        sr.color = Color.white;

        isActing = true;
        animator.SetTrigger("Die");
        rb.velocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.0f);
    }

    // ====================================================
    // [수정] 플레이어 데미지 적용 함수 (애니메이션 이벤트용)
    // ====================================================
    public void DealDamage()
    {
        // 1. 반경 내 모든 콜라이더 감지 (레이어 상관없이 일단 다 가져옴)
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitObjects)
        {
            // 2. 태그가 "Player"인지 직접 확인
            if (col.CompareTag("Player"))
            {
                Debug.Log($"플레이어({col.name}) 감지됨! 데미지 {attackDamage} 적용 시도.");

                // [방법 A] 플레이어 스크립트를 찾아서 TakeDamage 실행 (추천)
                // 만약 플레이어 스크립트 이름이 PlayerController라면 아래 주석을 해제하고 쓰세요.
                // PlayerController pc = col.GetComponent<PlayerController>();
                // if (pc != null) pc.TakeDamage(attackDamage);

                // [방법 B] 스크립트 이름을 몰라도 함수 이름만 맞으면 실행 (범용적)
                // 플레이어 스크립트에 'public void TakeDamage(int damage)' 함수가 있어야 합니다.
                col.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
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