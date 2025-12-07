using System.Collections;
using UnityEngine;

public class Enemy_Heart : EnemyController_2D
{
    [Header("Heal Settings")]
    public int healAmount = 30;          // 1회 힐량
    public float healInterval = 5f;      // 힐 주기
    private float lastHealTime = 0f;
    // 힐 중인지 체크하는 변수 추가
    private bool isHealing = false;

    protected override void Update()
    {
        if (isFrozen) return;
        if (isDead) return;

        // 1. 힐 중이면 이동 로직 실행 안 함 (멈춰있어야 함)
        if (isHealing)
        {
            rb.velocity = Vector2.zero; // 확실하게 정지
            return;
        }

        // 2. 힐 쿨타임 체크
        if (Time.time >= lastHealTime + healInterval)
        {
            StartCoroutine(HealRoutine()); // 코루틴으로 변경
            lastHealTime = Time.time;
            return; // 힐 시작했으면 이번 프레임은 이동 스킵
        }

        // 3. 이동 로직 (힐 중이 아닐 때만 실행됨)
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            // 추적 범위보다 가까우면 도망감
            if (distance < chaseRange)
            {
                Vector2 dir = (transform.position - player.position).normalized;
                rb.velocity = dir * moveSpeed * 0.5f; // 50% 속도로 도망
                animator?.SetFloat("Speed", 1);
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator?.SetFloat("Speed", 0);
            }
        }
    }

    // 힐 과정을 코루틴으로 처리 (시간 제어 용이)
    IEnumerator HealRoutine()
    {
        isHealing = true; // 힐 시작 상태 온

        rb.velocity = Vector2.zero;
        animator?.SetFloat("Speed", 0);
        animator?.SetTrigger("Heal");

        // 힐 애니메이션 길이만큼 대기 (대략 1초라고 가정, 필요하면 조정)
        yield return new WaitForSeconds(1.0f);

        // 실제 회복 로직 실행
        HealLogic();

        isHealing = false; // 힐 끝남, 다시 이동 가능
    }

    private void HealLogic()
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
            // 나 자신은 제외하고 다른 적만 여기서 치료할지, 포함할지 결정
            // ally != this 조건을 넣으면 타인만 치료
            if (ally != null && !ally.isDead)
            {
                ally.Heal(healAmount);
                healedAny = true;
            }
        }

        if (!healedAny) HealSelf();
    }

    private void HealSelf()
    {
        Heal(healAmount);
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
