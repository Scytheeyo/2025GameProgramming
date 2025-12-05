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

    // 부모 클래스의 Start를 사용하여 Player와 기본 변수를 초기화합니다.
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        if (isDead || player == null || isDashing) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 플레이어 방향 보기
        if (transform.position.x > player.position.x) sr.flipX = true;
        else sr.flipX = false;

        // [중요] 부모의 AttackPoint 방향도 같이 갱신해줘야 함 (필요 시)
        UpdateAttackPointDirection();

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
                rb.velocity = Vector2.zero;
                if (animator != null) animator.SetFloat("Speed", 0);
            }
        }
        else
        {
            // 추격 범위 밖이면 대기
            Idle();
        }
    }

    IEnumerator DashAttackRoutine()
    {
        // 1. 준비
        isAttacking = true;
        rb.velocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetBool("IsAttacking", true);
        }

        // 깜빡이거나 준비 동작 딜레이
        yield return new WaitForSeconds(dashPrepTime);

        // 2. 돌진 시작
        isDashing = true;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * dashSpeed;

        // 거리 기반 시간 계산 (속도 * 시간 = 거리 -> 시간 = 거리 / 속도)
        float calculatedDuration = dashDistance / dashSpeed;

        // 돌진하는 동안 대기
        yield return new WaitForSeconds(calculatedDuration);

        // 3. 정지 및 종료
        rb.velocity = Vector2.zero;
        isDashing = false;
        isAttacking = false;

        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    // [수정됨] 충돌 시 데미지 적용 로직
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 돌진 중에만 데미지를 줌
        if (isDashing && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"[다이아몬드] 돌진 적중! 데미지: {damage}");

            // 플레이어에게 데미지 전달 (부모 클래스의 damage 변수 사용)
            collision.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // 충돌 후 멈출지, 뚫고 지나갈지는 선택 (여기선 멈춤 처리)
            rb.velocity = Vector2.zero;
            isDashing = false; // 충돌 즉시 돌진 상태 해제
        }
    }
}