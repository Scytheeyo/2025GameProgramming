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

    [Header("공격력 설정")]
    public int attackDamage = 15;

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

    // 행동 중인지 체크하는 변수 (이게 true면 이동 안 함)
    private bool isActing = false;

    void OnEnable()
    {
        isActing = false;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;

        // =======================================================
        // [핵심 수정] Player 태그를 찾은 뒤 -> 그 안의 자식 "PlayerObject"를 찾아 연결
        // =======================================================
        GameObject mainPlayer = GameObject.FindGameObjectWithTag("Player");

        if (mainPlayer != null)
        {
            // 메인 플레이어 안에서 "PlayerObject"라는 이름의 자식을 찾습니다.
            // (이름이 띄어쓰기 없이 정확히 일치해야 합니다!)
            Transform targetChild = mainPlayer.transform.Find("PlayerObject");

            if (targetChild != null)
            {
                player = targetChild; // 자식을 타겟으로 설정
            }
            else
            {
                // 자식이 없으면 에러를 띄우고 임시로 본체를 타겟으로 잡습니다.
                Debug.LogError("CardCaptain: Player 태그는 찾았으나 자식 'PlayerObject'가 없습니다!");
                player = mainPlayer.transform;
            }
        }
        else
        {
            Debug.LogError("CardCaptain: 'Player' 태그를 가진 오브젝트를 아예 찾을 수 없습니다!");
        }
        // =======================================================

        if (attackPoint == null) attackPoint = transform;
    }

    void Update()
    {
        if (player == null) return;

        // 행동(공격/소환) 중이면 움직이지 않음
        if (isActing)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distToPlayer = Vector2.Distance(attackPoint.position, player.position);

        // 1. 소환 패턴 (쿨타임 되면 최우선 실행)
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            StartCoroutine(SummonRoutine());
            return;
        }

        // 2. 공격 범위 안에 있음 -> 공격 시도
        if (distToPlayer <= attackRange)
        {
            StopMovement(); // 공격 범위니까 일단 멈춤

            // 공격 쿨타임이 됐으면 공격 시작
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        // 3. 공격 범위 밖 -> 추격
        else if (distToPlayer <= chaseRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            StopMovement(); // 추격 범위 밖이면 대기
        }

        // 테스트용 자해 키
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        // 애니메이션 없이 색깔만 깜빡임
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }

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

    IEnumerator AttackRoutine()
    {
        isActing = true;
        StopMovement();
        LookAtPlayer();

        // [핵심 수정] 공격 시작 전에 기존에 켜져있던 트리거를 싹 다 끕니다.
        // 이게 없으면 트리거가 쌓여서 애니메이션이 꼬이고 이동도 못하게 됩니다.
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");

        // 콤보 순서대로 트리거 켜기
        if (attackComboIndex == 0)
        {
            animator.SetTrigger("Attack1");
            attackComboIndex = 1;
        }
        else if (attackComboIndex == 1)
        {
            animator.SetTrigger("Attack2");
            attackComboIndex = 2;
        }
        else
        {
            animator.SetTrigger("Attack3");
            attackComboIndex = 0;
        }

        lastAttackTime = Time.time;

        // 애니메이션이 재생될 시간을 줍니다.
        // 주의: 애니메이션 클립 길이가 1초보다 길면 이 시간을 늘려야 합니다.
        yield return new WaitForSeconds(1.0f);

        // [안전장치] 끝나고 나서도 혹시 켜져있을 트리거를 다시 끕니다.
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");

        isActing = false; // 행동 종료 -> 이제 다시 Update에서 이동 가능
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
        // 이동 애니메이션 켜기
        animator.SetFloat("Speed", 1);
        LookAtPlayer();

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);
    }

    void StopMovement()
    {
        rb.velocity = Vector2.zero;
        // 이동 애니메이션 끄기
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

    public void Die()
    {
        StopAllCoroutines();
        if (sr != null) sr.color = Color.white;

        isActing = true;
        animator.SetTrigger("Die");
        rb.velocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 2.0f);
    }

    public void DealDamage()
    {
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitObjects)
        {
            if (col.CompareTag("Player"))
            {
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