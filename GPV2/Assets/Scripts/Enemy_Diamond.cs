using UnityEngine;
using System.Collections;

public class Enemy_Diamond : EnemyController_2D
{
    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDistance = 5f;
    public float dashPrepTime = 0.5f;

    public float dashCooldown = 3.0f;
    private float lastDashTime = -10f;

    private bool isDashing = false;

    protected override void Start()
    {
        base.Start(); // 부모의 Start 실행 (여기까진 스페이드 상태)

    }

    protected override void Update()
    {
        // ... (나머지 코드는 기존과 동일) ...
        if (isDead || player == null || isDashing) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (transform.position.x > player.position.x) sr.flipX = true;
        else sr.flipX = false;

        UpdateAttackPointDirection();

        if (distance <= chaseRange)
        {
            if (!isAttacking && !isDashing && Time.time >= lastDashTime + dashCooldown)
            {
                StartCoroutine(DashAttackRoutine());
                lastDashTime = Time.time;
            }
            else if (!isAttacking)
            {
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetFloat("Speed", 0);
            }
        }
        else
        {
            Idle();
        }
    }

    IEnumerator DashAttackRoutine()
    {
        // ... (기존 돌진 코드 동일) ...
        isAttacking = true;
        rb.velocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetBool("IsAttacking", true);
        }

        yield return new WaitForSeconds(dashPrepTime);

        isDashing = true;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * dashSpeed;

        float calculatedDuration = dashDistance / dashSpeed;

        yield return new WaitForSeconds(calculatedDuration);

        rb.velocity = Vector2.zero;
        isDashing = false;
        isAttacking = false;

        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ... (기존 충돌 코드 동일) ...
        if (isDashing && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"[다이아몬드] 돌진 적중! 데미지: {damage}");
            collision.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            rb.velocity = Vector2.zero;
            isDashing = false;
        }
    }
}