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
    public float attackRange = 1.0f; // 공격 판정 범위 (반지름)
    public float attackCooldown = 1f;
    public int damage = 10;

    [Header("Hit Effect")]
    public Color hitColor = new Color(1f, 0.4f, 0.4f);
    public float flashDuration = 0.1f;
    private Coroutine flashCoroutine;

    [Header("References")]
    public Transform player;
    public Transform attackPoint; // [유지] 자식 클래스(클로버 등)에서 발사 위치로 사용
    public GameObject dropItemPrefab;

    [Header("Card Drop Settings")]
    [Tooltip("이 적이 주는 카드의 문양 (기본: 스페이드)")]
    public CardSuit enemySuit = CardSuit.Spade;
    public float cardDropChance = 1.0f;
    public int maxCardNumber = 13;

    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer sr;
    protected Collider2D myCollider;

    public bool isDead = false;
    protected bool isAttacking = false;
    protected bool playerInAttackRange = false;
    protected float lastAttackTime = 0f;

    // [유지] AttackPoint 반전용 변수
    private float attackPointAbsX;

    protected bool isKnockbacked = false;
    protected bool isFrozen = false;

    protected virtual void Start()
    {
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

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;

        // [유지] AttackPoint가 없으면 자기 자신을 할당 (에러 방지)
        if (attackPoint == null) attackPoint = transform;

        // [유지] 초기 X 거리 저장 (반전을 위해)
        attackPointAbsX = Mathf.Abs(attackPoint.localPosition.x);
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;
        if (isKnockbacked || isFrozen) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (!isAttacking)
        {
            sr.flipX = (player.position.x < transform.position.x);
            // [유지] 방향에 따라 AttackPoint 반전 (클로버 등이 올바르게 쏘기 위해 필요)
            UpdateAttackPointDirection();
        }

        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 거리 기반 공격 판정 (Collider Trigger 대신 사용)
        if (distance <= attackRange)
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

    // [유지] AttackPoint 위치 반전 로직
    protected void UpdateAttackPointDirection()
    {
        if (attackPoint == null || attackPoint == transform) return;

        float newX = sr.flipX ? -attackPointAbsX : attackPointAbsX;
        attackPoint.localPosition = new Vector3(newX, attackPoint.localPosition.y, 0);
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

    // Trigger 함수들은 거리 계산 방식으로 대체되었으나 호환성을 위해 남겨둠
    protected virtual void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) playerInAttackRange = true; }
    private void OnTriggerStay2D(Collider2D other) { if (other.CompareTag("Player")) playerInAttackRange = true; }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) playerInAttackRange = false; }

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

        // 충돌 무시 로직 제거됨 (플레이어가 통과 못함)

        Invoke(nameof(EndAttack), 1.0f);
    }

    // ------------------------------------------------------------------------
    // [핵심 변경] 데미지 판정을 '몸통 중심(transform.position)'으로 변경
    // ------------------------------------------------------------------------
    public void DealDamage()
    {
        // AttackPoint 위치가 아니라, 내 몸(transform.position) 주변을 검사합니다.
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D col in hitObjects)
        {
            if (col.CompareTag("Player"))
            {
                col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    // ------------------------------------------------------------------------

    void EndAttack()
    {
        isAttacking = false;
        animator.SetBool("IsAttacking", false);
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

        GiveCardToPlayer();
        DropItem();
    }

    protected void GiveCardToPlayer()
    {
        if (player == null) return;
        Player playerScript = player.GetComponent<Player>();
        if (playerScript == null) return;

        if (Random.value > cardDropChance) return;

        int pickedNumber = GetWeightedRandomNumber(maxCardNumber);
        CardData newCard = new CardData(enemySuit, pickedNumber);
        playerScript.AddCardToCollection(newCard);

        Debug.Log($"[적 처치] {enemySuit} {pickedNumber} 획득");
    }

    int GetWeightedRandomNumber(int maxNum)
    {
        int totalWeight = 0;
        for (int i = 1; i <= maxNum; i++) totalWeight += (maxNum - i + 1);
        int randomValue = Random.Range(0, totalWeight);
        for (int i = 1; i <= maxNum; i++)
        {
            int weight = (maxNum - i + 1);
            if (randomValue < weight) return i;
            randomValue -= weight;
        }
        return 1;
    }

    public void DestroyEnemy() { Destroy(gameObject); }
    public void DropItem() { if (dropItemPrefab != null) Instantiate(dropItemPrefab, transform.position, Quaternion.identity); }
    public void BeginKnockback(Vector2 direction, float force) { if (!isBoss && !isDead && !isFrozen) StartCoroutine(KnockbackRoutine(direction, force)); }
    private IEnumerator KnockbackRoutine(Vector2 direction, float force) { isKnockbacked = true; isAttacking = false; animator.SetBool("IsAttacking", false); direction.y = 0; direction.Normalize(); rb.velocity = Vector2.zero; rb.AddForce(direction * Mathf.Max(force, 30f), ForceMode2D.Impulse); yield return new WaitForSeconds(0.4f); rb.velocity = Vector2.zero; isKnockbacked = false; }
    public void FreezeEnemy(float duration) { if (!isBoss && !isDead) StartCoroutine(FreezeRoutine(duration)); }
    private IEnumerator FreezeRoutine(float duration) { isFrozen = true; bool temp = rb.isKinematic; rb.velocity = Vector2.zero; rb.isKinematic = true; if (animator) animator.speed = 0f; yield return new WaitForSeconds(duration); if (rb) rb.isKinematic = temp; if (animator) animator.speed = 1f; isFrozen = false; }
    public void TakePercentDamage(float percent) { if (isDead) return; int dmg = Mathf.RoundToInt(maxHealth * percent); if (dmg < 1) dmg = 1; TakeDamage(dmg); }
    public void Heal(int amount) { if (isDead) return; currentHealth = Mathf.Min(currentHealth + amount, maxHealth); }

    // [수정] Gizmos: 공격 범위(빨강)를 몸통 기준으로 그림
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 실제 데미지 판정 범위 (몸통 기준)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // (참고용) AttackPoint 위치 표시 (파란 점)
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, 0.1f);
        }
    }
}