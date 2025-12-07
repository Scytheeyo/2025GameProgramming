using UnityEngine;
using System.Collections;

public class Boss_CardCaptain : MonoBehaviour, IDamageable
{
    [Header("�⺻ ����")]
    public float moveSpeed = 2.5f;
    public float chaseRange = 10f;
    public float attackRange = 1.2f;
    public int maxHealth = 300;
    public int currentHealth;

    [Header("���ݷ� ����")] // [�ű�] ������ ������ ����
    public int attackDamage = 15; // ���⼭ ������ ��ġ�� �����ϼ���!

    [Header("�ǰ� ȿ�� ����")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f);
    public float flashDuration = 0.1f;

    [Header("3�� �޺� ����")]
    public float attackCooldown = 2.0f;
    private float lastAttackTime = -999f;
    private int attackComboIndex = 0;

    [Header("��ȯ ����")]
    public float summonCooldown = 20f;
    private float lastSummonTime = -999f;
    public GameObject[] minionPrefabs;
    public Transform[] summonPoints;

    [Header("����")]
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

        // �׽�Ʈ�� (KŰ ������ ����)
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

    public void Die()
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
    // [����] �÷��̾� ������ ���� �Լ� (�ִϸ��̼� �̺�Ʈ��)
    // ====================================================
    public void DealDamage()
    {
        // 1. �ݰ� �� ��� �ݶ��̴� ���� (���̾� ������� �ϴ� �� ������)
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitObjects)
        {
            // 2. �±װ� "Player"���� ���� Ȯ��
            if (col.CompareTag("Player"))
            {
                Debug.Log($"�÷��̾�({col.name}) ������! ������ {attackDamage} ���� �õ�.");

                // [��� A] �÷��̾� ��ũ��Ʈ�� ã�Ƽ� TakeDamage ���� (��õ)
                // ���� �÷��̾� ��ũ��Ʈ �̸��� PlayerController��� �Ʒ� �ּ��� �����ϰ� ������.
                // PlayerController pc = col.GetComponent<PlayerController>();
                // if (pc != null) pc.TakeDamage(attackDamage);

                // [��� B] ��ũ��Ʈ �̸��� ��� �Լ� �̸��� ������ ���� (������)
                // �÷��̾� ��ũ��Ʈ�� 'public void TakeDamage(int damage)' �Լ��� �־�� �մϴ�.
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
    public void TakePercentDamage(float percent)
    {
        int dmg = Mathf.RoundToInt(maxHealth * percent);
        if (dmg < 1) dmg = 1;
        TakeDamage(dmg);
    }
}