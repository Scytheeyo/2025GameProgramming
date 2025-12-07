using UnityEngine;
using System.Collections;

public class Boss_HeartQueen : MonoBehaviour, IDamageable
{
    [Header("�⺻ ����")]
    public float moveSpeed = 3f;
    public float chaseRange = 8f;
    public float attackRange = 1.0f;
    public int maxHealth = 500;
    public int currentHealth;

    [Header("�ǰ� ȿ�� ����")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f);
    public float flashDuration = 0.1f;

    [Header("�Ϲ� ����")]
    public int attackDamage = 20; // [�ű�] �Ϲ� ���� ������
    public float attackCooldown = 1.5f;
    private float lastAttackTime = -999f;
    private int attackComboIndex = 0;

    [Header("��ȯ ����")]
    public float summonCooldown = 30f;
    private float lastSummonTime = -999f;
    public GameObject[] minionPrefabs;
    public Transform[] summonPoints;

    [Header("���� ����")]
    public float dashSpeed = 15f;
    public float reloadDelay = 3.0f;
    private float lastDashEndTime = -999f;
    [SerializeField] private bool isDashReady = false;

    [Header("���� ��� (�ʻ��)")]
    public int jumpDamage = 30; // ���� ���� ������
    public float jumpHeight = 15f;
    private int nextJumpHealth;

    [Header("����")]
    public Transform attackPoint;
    public Transform player;
    public GameObject landEffectPrefab;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D myCollider;
    private SpriteRenderer sr;

    private bool isActing = false;
    private bool isDashing = false;
    private Vector2 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
        isDashReady = false;
        lastSummonTime = Time.time;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform.Find("PlayerObject");
            }
            else
            {
                Debug.LogError("씬에 'Player' 태그를 가진 오브젝트가 없습니다!");
            }
        }
        if (attackPoint == null) attackPoint = transform;

        nextJumpHealth = maxHealth - 100;
    }

    void Update()
    {
        // �׽�Ʈ�� Ű
        if (Input.GetKeyDown(KeyCode.J) && !isActing) StartCoroutine(JumpSmashRoutine());
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);

        if (player == null) return;

        // 1. �ൿ ���̸� ����
        if (isActing)
        {
            if (!isDashing) rb.velocity = Vector2.zero;
            else rb.velocity = dashDirection * dashSpeed;
            return;
        }

        // 2. �Ÿ� ���
        float distToPlayer = Vector2.Distance(attackPoint.position, player.position);
        float distFromBody = Vector2.Distance(transform.position, player.position);

        // --- ���� ���� ---
        if (distFromBody <= chaseRange)
        {
            if (Time.time >= lastDashEndTime + reloadDelay)
            {
                if (!isDashReady) isDashReady = true;
            }
        }

        // [1����] ��ȯ ����
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            StartCoroutine(SummonRoutine());
            return;
        }

        // [2����] �ൿ ����
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
        // ���� ��(Kinematic)�� �� ����
        if (rb.bodyType == RigidbodyType2D.Kinematic) return;

        currentHealth -= dmg;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }

        if (!isActing)
        {
            animator.SetTrigger("Hit");
        }

        // ���� ���� �ߵ� üũ
        if (currentHealth <= nextJumpHealth)
        {
            nextJumpHealth -= 100;
            StopAllCoroutines();
            if (sr != null) sr.color = Color.white;
            StartCoroutine(JumpSmashRoutine());
        }

        if (currentHealth <= 0) Die();
    }

    IEnumerator HitFlashRoutine()
    {
        sr.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        sr.color = Color.white;
    }

    IEnumerator SummonRoutine()
    {
        isActing = true;
        rb.velocity = Vector2.zero;
        animator.SetFloat("Speed", 0);
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
            Debug.Log("����: ���Ͷ� ī�庴���!");
        }

        lastSummonTime = Time.time;
        animator.SetBool("IsSummoning", false);
        yield return new WaitForSeconds(0.5f);

        isActing = false;
    }

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
        animator.SetBool("IsSummoning", false);

        animator.SetTrigger("Jump");
        yield return new WaitForSeconds(0.5f);

        float startY = transform.position.y;
        float targetY = startY + jumpHeight;
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(transform.position.x, targetY, transform.position.z);

        // ���� ���
        while (elapsed < 0.5f)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        // ���� ���
        yield return new WaitForSeconds(3.0f);

        if (player != null)
        {
            Vector3 groundPos = new Vector3(player.position.x, startY, player.position.z);
            Vector3 skyPos = new Vector3(player.position.x, targetY, player.position.z);

            transform.position = skyPos;
            animator.SetTrigger("Fall");

            // ����
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

        // [������] ��� ������ ���� (���̾� ����, �±� Ȯ��)
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, 2.5f);
        foreach (Collider2D p in hitPlayers)
        {
            if (p.CompareTag("Player"))
            {
                Debug.Log($"���� ���� ����! ������: {jumpDamage}");
                p.SendMessage("TakeDamage", jumpDamage, SendMessageOptions.DontRequireReceiver);
            }
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

    // ====================================================
    // [�ű�] �ִϸ��̼� �̺�Ʈ�� �Ϲ� ���� �Լ�
    // ====================================================
    public void DealDamage()
    {
        // 1. ���� ���� �� ��� ��ü ����
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitObjects)
        {
            // 2. �±װ� Player���� Ȯ�� (���̾� ��� X)
            if (col.CompareTag("Player"))
            {
                Debug.Log($"���� �Ϲ� ����! ������: {attackDamage}");
                col.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void StartDashMove() { isDashing = true; }
    public void EndDashMove() { isDashing = false; isActing = false; rb.velocity = Vector2.zero; lastDashEndTime = Time.time; }
    void MoveTowardsPlayer() { animator.SetFloat("Speed", 1); LookAtPlayer(); float dirX = Mathf.Sign(player.position.x - transform.position.x); rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y); }
    void StopMovement() { rb.velocity = Vector2.zero; animator.SetFloat("Speed", 0); }
    void LookAtPlayer() { float sizeX = Mathf.Abs(transform.localScale.x); if (player.position.x > transform.position.x) transform.localScale = new Vector3(sizeX, transform.localScale.y, transform.localScale.z); else transform.localScale = new Vector3(-sizeX, transform.localScale.y, transform.localScale.z); }

    public void Die()
    {
        StopAllCoroutines();
        isActing = true;
        if (sr != null) sr.color = Color.white;
        animator.SetTrigger("Die");
        rb.velocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;
    }

    void OnDrawGizmos()
    {
        if (isDashReady) Gizmos.color = Color.green; else Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
    public void TakePercentDamage(float percent)
    {
        int dmg = Mathf.RoundToInt(maxHealth * percent);
        if (dmg < 1) dmg = 1;
        TakeDamage(dmg);
    }
}