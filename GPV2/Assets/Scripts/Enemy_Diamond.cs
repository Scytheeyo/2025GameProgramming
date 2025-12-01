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

    protected override void Update()
    {
        if (isDead || player == null || isDashing) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 플레이어 방향 보기
        if (transform.position.x > player.position.x) sr.flipX = true;
        else sr.flipX = false;

        if (distance <= chaseRange)
        {
            // 1. 공격 가능하면(쿨타임 지남 + 공격 중 아님) -> 돌진
            if (!isAttacking && !isDashing && Time.time >= lastDashTime + dashCooldown)
            {
                StartCoroutine(DashAttackRoutine());
                lastDashTime = Time.time;
            }
            // 2. 쿨타임 중이면 -> 대기 (노려보기)
            else if (!isAttacking)
            {
                // 쿨타임 동안은 움직이지 않고 제자리에서 대기합니다.
                // 만약 쿨타임 동안에도 슬금슬금 다가오게 하려면 여기서 ChasePlayer();를 호출하세요.
                rb.velocity = Vector2.zero;
                animator?.SetFloat("Speed", 0);
            }
        }
        else
        {
            // 아까는 여기가 ChasePlayer()여서 끝없이 쫓아왔던 겁니다.
            Idle();
        }
    }

    IEnumerator DashAttackRoutine()
    {
        // 1. 준비
        isAttacking = true;
        rb.velocity = Vector2.zero;
        animator?.SetFloat("Speed", 0);
        animator?.SetBool("IsAttacking", true);
        yield return new WaitForSeconds(dashPrepTime);

        // 2. 돌진
        isDashing = true;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * dashSpeed;

        // 거리 기반 시간 계산
        float calculatedDuration = dashDistance / dashSpeed;
        yield return new WaitForSeconds(calculatedDuration);

        // 3. 정지
        rb.velocity = Vector2.zero;
        isDashing = false;
        isAttacking = false;
        animator?.SetBool("IsAttacking", false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"[다이아몬드] 돌진 공격 성공! 데미지: {damage}");
            rb.velocity = Vector2.zero;
        }
    }
}