using UnityEngine;
using System.Collections;

public class EnemyController_2D : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isBoss = false;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float chaseRange = 10f;
    public float attackRange = 1.0f; // [신규] 공격 판정 범위 (Gizmos로 확인 가능)
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

    protected virtual void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
            }
            else
            {
                Debug.LogError("씬에 'Player' 태그를 가진 오브젝트가 없습니다!");
            }
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

        float distance = Vector2.Distance(transform.position, player.position);

        // 공격 중이 아닐 때만 바라보는 방향 전환
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

        // 테스트용
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
        if (other.CompareTag("Player"))
        {
            playerInAttackRange = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInAttackRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInAttackRange = false;
        }
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

        // 플레이어와 충돌 무시는 선택 사항 (필요 없으면 삭제 가능)
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol && myCollider)
            Physics2D.IgnoreCollision(playerCol, myCollider, true);

        // 안전장치: 애니메이션 이벤트가 씹히거나 없을 때를 대비해 일정 시간 후 공격 상태 해제
        Invoke(nameof(EndAttack), 1.0f);
    }

    // [신규] 애니메이션 이벤트에서 호출할 데미지 함수
    public void DealDamage()
    {
        if (attackPoint == null) return;

        // 1. 범위 내 모든 콜라이더 감지
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitObjects)
        {
            // 2. 태그 확인 (레이어 무시)
            if (col.CompareTag("Player"))
            {
                // Debug.Log($"{name}이(가) 플레이어 공격! 데미지: {damage}");
                col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        animator.SetBool("IsAttacking", false);

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol && myCollider)
            Physics2D.IgnoreCollision(playerCol, myCollider, false);
    }

    // === 체력 및 사망 처리 ===
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;

        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hit");
        }
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

        if (dropItemPrefab != null)
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
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

    public void BeginKnockback(Vector2 direction, float force)
    {
        if (isBoss || isDead) return;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    public void TakePercentDamage(float percent)
    {
        if (isDead) return;
        int dmg = Mathf.RoundToInt(maxHealth * percent);
        if (dmg < 1) dmg = 1;
        TakeDamage(dmg);
    }

    public void FreezeEnemy(float duration)
    {
        if (isBoss || isDead) return;
        StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        float originalSpeed = moveSpeed;
        moveSpeed = 0f;
        if (animator != null) animator.speed = 0f;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed;
        if (animator != null) animator.speed = 1f;
    }

    void OnDrawGizmosSelected()
    {
        // 추적 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 공격 범위 (빨간색)
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}