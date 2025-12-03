using UnityEngine;
using System.Collections;

public class EnemyController_2D : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isBoss = false;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float chaseRange = 10f;
    public float attackRange = 1.0f;
    public float attackCooldown = 1f;
    public int damage = 10;

    [Header("Hit Effect")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f);
    public float flashDuration = 0.1f;
    private Coroutine flashCoroutine;

    [Header("References")]
    public Transform player;
    public Transform attackPoint;
    public GameObject dropItemPrefab;

    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer sr;
    protected Collider2D myCollider;

    public bool isDead = false;
    protected bool isAttacking = false;
    protected bool playerInAttackRange = false;
    protected float lastAttackTime = 0f;

    // 상태 제어 변수
    protected bool isKnockbacked = false;
    protected bool isFrozen = false;

    protected virtual void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("씬에 'Player' 태그를 가진 오브젝트가 없습니다!");
        }

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;

        if (attackPoint == null) attackPoint = transform;
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        // 넉백 중이거나 얼어있으면 이동 불가
        if (isKnockbacked || isFrozen) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (!isAttacking)
        {
            sr.flipX = (player.position.x < transform.position.x);
            UpdateAttackPointDirection();
        }

        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (playerInAttackRange)
        {
            TryAttack();
        }
        else if (distance <= chaseRange)
        {
            ChasePlayer();
        }
        else
        {
            Idle();
        }

        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    protected void Idle()
    {
        if (isDead) return;
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 0);
        rb.velocity = Vector2.zero;
    }

    protected void ChasePlayer()
    {
        if (isDead) return;
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 1);

        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{name}이(가) {amount} 회복됨. 현재 체력: {currentHealth}/{maxHealth}");
    }

    protected void UpdateAttackPointDirection()
    {
        if (attackPoint == null) return;
        float xOffset = Mathf.Abs(attackPoint.localPosition.x);
        attackPoint.localPosition = sr.flipX
            ? new Vector3(-xOffset, attackPoint.localPosition.y, 0)
            : new Vector3(xOffset, attackPoint.localPosition.y, 0);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInAttackRange = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInAttackRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInAttackRange = false;
    }

    void TryAttack()
    {
        if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
        {
            StartAttack();
            lastAttackTime = Time.time;
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;
        animator.SetBool("IsAttacking", true);
        animator.SetFloat("Speed", 0);

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol && myCollider) Physics2D.IgnoreCollision(playerCol, myCollider, true);

        Invoke(nameof(EndAttack), 1.0f);
    }

    public void DealDamage()
    {
        if (attackPoint == null) return;
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);
        foreach (Collider2D col in hitObjects)
        {
            if (col.CompareTag("Player"))
            {
                col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        animator.SetBool("IsAttacking", false);
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol && myCollider) Physics2D.IgnoreCollision(playerCol, myCollider, false);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;

        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        if (currentHealth <= 0) Die();
        else animator.SetTrigger("Hit");
    }

    protected virtual IEnumerator HitFlashRoutine()
    {
        if (sr != null)
        {
            sr.material.color = hitColor;
            yield return new WaitForSeconds(flashDuration);
            sr.material.color = Color.white;
        }
        flashCoroutine = null;
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        if (sr != null) sr.material.color = Color.white;

        animator.SetBool("IsDead", true);
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 0);

        rb.velocity = Vector2.zero;
        rb.simulated = false;
        myCollider.enabled = false;

        DropItem();
    }

    public void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    public void DropItem()
    {
        if (dropItemPrefab != null)
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
    }

    // ------------------------------------------------------------------------
    // [넉백 (Knockback)]
    // ------------------------------------------------------------------------
    public void BeginKnockback(Vector2 direction, float force)
    {
        if (isBoss || isDead || isFrozen) return;
        StartCoroutine(KnockbackRoutine(direction, force));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockbacked = true; // AI 차단
        isAttacking = false;
        animator.SetBool("IsAttacking", false);

        // [핵심 수정] Y축 힘 제거 (공중부양 방지)
        direction.y = 0;
        direction.Normalize(); // 방향 재정규화

        // 1. 기존 속도 제거
        rb.velocity = Vector2.zero;

        // 2. 힘 가하기 (최소 힘 30 보장)
        float finalForce = Mathf.Max(force, 30f);
        rb.AddForce(direction * finalForce, ForceMode2D.Impulse);

        // 3. 날아가는 시간
        yield return new WaitForSeconds(0.4f);

        // 4. 정지 및 복구
        rb.velocity = Vector2.zero;
        isKnockbacked = false;
    }

    // ------------------------------------------------------------------------
    // [시간 정지 (Freeze)]
    // ------------------------------------------------------------------------
    public void FreezeEnemy(float duration)
    {
        if (isBoss || isDead) return;
        if (!isFrozen) StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        bool originalKinematic = rb.isKinematic;

        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // 물리 정지
        if (animator) animator.speed = 0f;

        yield return new WaitForSeconds(duration);

        if (rb != null) rb.isKinematic = originalKinematic;
        if (animator != null) animator.speed = 1f;

        isFrozen = false;
    }

    public void TakePercentDamage(float percent)
    {
        if (isDead) return;
        int dmg = Mathf.RoundToInt(maxHealth * percent);
        if (dmg < 1) dmg = 1;
        TakeDamage(dmg);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}