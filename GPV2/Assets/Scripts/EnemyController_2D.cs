using UnityEngine;
using System.Collections;

public class EnemyController_2D : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isBoss = false; // [추가됨] 보스 여부 확인

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float chaseRange = 10f;
    public float attackCooldown = 1f;
    public int damage = 10;

    [Header("Hit Effect")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f); // 피격 시 색상
    public float flashDuration = 0.1f; // 깜빡이는 시간
    private Coroutine flashCoroutine;  // 깜빡임 코루틴 제어 변수

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
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        sr.flipX = (player.position.x < transform.position.x);
        UpdateAttackPointDirection();

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
            isAttacking = false;
            animator.SetBool("IsAttacking", false);
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

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol && myCollider)
            Physics2D.IgnoreCollision(playerCol, myCollider, true);

        Invoke(nameof(EndAttack), 0.6f);
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
        // Debug.Log("Enemy hit! 현재 체력: " + currentHealth);

        // [추가] 피격 깜빡임 효과 (연속 피격 시 기존 코루틴 취소 후 재실행)
        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

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

    // [추가] Material 색상 변경 코루틴 (애니메이션 간섭 무시)
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

    // [수정됨] protected -> public으로 변경 (외부에서 호출 가능하도록)
    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // [추가] 죽을 때 색상 원상복구 및 코루틴 정지
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

    // [추가됨] 넉백 시작 함수
    public void BeginKnockback(Vector2 direction, float force)
    {
        if (isBoss || isDead) return; // 보스는 밀리지 않음
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    // [추가됨] 퍼센트 데미지 함수
    public void TakePercentDamage(float percent)
    {
        if (isDead) return;
        int dmg = Mathf.RoundToInt(maxHealth * percent);
        if (dmg < 1) dmg = 1;
        TakeDamage(dmg);
    }

    // [추가됨] 적 정지(빙결) 함수
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}