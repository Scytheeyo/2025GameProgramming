using UnityEngine;

public class Enemy_Heart : EnemyController_2D
{
    [Header("Heal Settings")]
    public int healAmount = 30;          // 1회 힐량
    public float healInterval = 5f;      // 힐 주기
    private float lastHealTime = 0f;

    protected override void Update()
    {
        if (isDead) return;

        // 일정 주기로 힐 시도
        if (Time.time >= lastHealTime + healInterval)
        {
            HealAlliesOrSelf();
            lastHealTime = Time.time;
        }

        // 플레이어 회피 이동
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance < chaseRange)
            {
                Vector2 dir = (transform.position - player.position).normalized;
                rb.velocity = dir * moveSpeed * 0.5f;
                animator?.SetFloat("Speed", 1);
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator?.SetFloat("Speed", 0);
            }
        }
    }

    private void HealAlliesOrSelf()
    {
        if (attackPoint == null)
        {
            HealSelf();
            return;
        }

        CircleCollider2D circle = attackPoint.GetComponent<CircleCollider2D>();
        float radius = circle != null ? circle.radius : 5f;

        Collider2D[] allies = Physics2D.OverlapCircleAll(attackPoint.position, radius);
        bool healedAny = false;

        foreach (var col in allies)
        {
            EnemyController_2D ally = col.GetComponent<EnemyController_2D>();
            if (ally != null && !ally.isDead)
            {
                ally.Heal(healAmount); 
                Debug.Log($"{name} → {ally.name} 회복 +{healAmount}");
                healedAny = true;
            }
        }

        // 혹시 아무도 감지 안 됐을 경우 대비
        if (!healedAny)
            HealSelf();

        animator?.SetTrigger("Heal");
    }


    private void HealSelf()
    {
        Heal(healAmount);
        Debug.Log($"{name} 자기 자신 회복 +{healAmount}");
    }

    // 회복 전용 함수 (최대 체력 초과 방지)
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{name} 현재 체력: {currentHealth}/{maxHealth}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.green;
            CircleCollider2D c = attackPoint.GetComponent<CircleCollider2D>();
            if (c != null)
                Gizmos.DrawWireSphere(attackPoint.position, c.radius);
        }
    }

}
