using UnityEngine;

public class EnemyController_2D : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float chaseRange = 6f;
    public float attackCooldown = 1f;
    public int damage = 10;

    [Header("References")]
    public Transform player;
    public Transform attackPoint;
    public GameObject dropItemPrefab; // Inspector에서 아이템 프리팹 연결

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Collider2D myCollider;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool playerInAttackRange = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        if (player == null)
        {
            Player playerComponent = FindObjectOfType<Player>();
            if (playerComponent != null)
            {
                player = playerComponent.transform;
            }
            else
            {
                Debug.LogError("씬에 Player 오브젝트가 없습니다!");
            }
        }

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;
    }

    void Update()
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

    void Idle()
    {
        if (isDead) return;
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 0);
        rb.velocity = Vector2.zero;
    }

    void ChasePlayer()
    {
        if (isDead) return;
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 1);

        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);
    }

    void UpdateAttackPointDirection()
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
        rb.isKinematic = true;

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
        rb.isKinematic = false;
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
        rb.isKinematic = true;
        myCollider.enabled = false;

        // 아이템은 사망 애니메이션의 마지막 프레임에서 DropItem() 호출
        // (Animation Event로 연결)
    }

    // 애니메이션 이벤트용
    public void DropItem()
    {
        if (dropItemPrefab != null)
        {
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
            Debug.Log("아이템 드롭!");
        }

        Destroy(gameObject, 0.2f); // 약간의 여유 후 제거
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
