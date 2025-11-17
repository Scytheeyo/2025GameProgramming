using UnityEngine;

public class Enemy_Heart : EnemyController_2D
{
    [Header("Heal Settings")]
    public int healAmount = 30;          // 1ȸ ����
    public float healInterval = 5f;      // �� �ֱ�
    private float lastHealTime = 0f;

    protected override void Update()
    {
        if (isDead) return;

        // ���� �ֱ�� �� �õ�
        if (Time.time >= lastHealTime + healInterval)
        {
            HealAlliesOrSelf();
            lastHealTime = Time.time;
        }

        // �÷��̾� ȸ�� �̵�
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
                Debug.Log($"{name} �� {ally.name} ȸ�� +{healAmount}");
                healedAny = true;
            }
        }

        // Ȥ�� �ƹ��� ���� �� ���� ��� ���
        if (!healedAny)
            HealSelf();

        animator?.SetTrigger("Heal");
    }


    private void HealSelf()
    {
        Heal(healAmount);
        Debug.Log($"{name} �ڱ� �ڽ� ȸ�� +{healAmount}");
    }

    // ȸ�� ���� �Լ� (�ִ� ü�� �ʰ� ����)
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{name} ���� ü��: {currentHealth}/{maxHealth}");
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
