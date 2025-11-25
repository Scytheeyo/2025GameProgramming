using UnityEngine;

public class EnemyController_2D : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float chaseRange = 10f;
    public float attackCooldown = 1f;
    public int damage = 10;

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
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(999);
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
        //Debug.Log($"[Enemy] Speed 파라미터 값: {animator.GetFloat("Speed")}");

        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
    }

    // === 회복 전용 함수 (최대 체력 초과 방지) ===
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

    private void OnTriggerEnter2D(Collider2D other)
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
        Debug.Log("Enemy hit! 현재 체력: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hit");
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetBool("IsDead", true);
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 0);

        rb.velocity = Vector2.zero;

        // 죽었을 때는 중력 꺼도 됨 (시체가 굴러다니는 거 방지)
        // 만약 시체가 바닥에 툭 떨어지게 하고 싶으면 simulated는 true로 두세요.
        rb.simulated = false;
        myCollider.enabled = false;

        if (dropItemPrefab != null)
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
    }

    // 애니메이션 이벤트 연결용
    public void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    public void DropItem()
    {
        if (dropItemPrefab != null)
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}