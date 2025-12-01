using UnityEngine;
using System.Collections;

public class Enemy_Clover : EnemyController_2D
{
    [Header("Clover Settings")]
    public GameObject projectilePrefab;   // 발사체 프리팹
    public float shootCooldown = 2f;      // 발사 쿨타임
    public float castTime = 0.5f;         // ★ 중요: 마법 시전 애니메이션 길이 (초)
    private float lastShootTime = 0f;

    protected override void Update()
    {
        // 1. 죽었거나, 공격 중(마법 시전 중)이면 아무것도 안 함
        if (isDead || player == null || isAttacking) return;

        // 플레이어 거리 및 방향 계산
        float distance = Vector2.Distance(transform.position, player.position);

        // 바라보는 방향 (플레이어 쪽)
        if (transform.position.x > player.position.x) sr.flipX = true;
        else sr.flipX = false;

        UpdateAttackPointDirection();

        // 2. 추격 범위 안에 있을 때
        if (distance <= chaseRange)
        {
            rb.velocity = Vector2.zero; // 발사할 땐 멈춤
            animator?.SetFloat("Speed", 0);

            // 쿨타임 됐으면 발사 코루틴 시작
            if (Time.time >= lastShootTime + shootCooldown)
            {
                StartCoroutine(ShootProjectile());
                lastShootTime = Time.time;
            }
        }
        else
        {
            // 범위 밖이면 대기 (움직이지 않음 - 클로버 특성상 고정형인듯 함)
            rb.velocity = Vector2.zero;
            animator?.SetFloat("Speed", 0);
        }
    }

    IEnumerator ShootProjectile()
    {
        // === [공격 시작] ===
        isAttacking = true;            // 상태 잠금 (이동/회전 불가)
        rb.velocity = Vector2.zero;    // 확실히 정지

        // 애니메이션: 부모와 통일성을 위해 Trigger 대신 Bool 사용 권장 
        // (원하시면 Trigger로 유지해도 되지만, isAttacking 상태 표현엔 Bool이 유리)
        animator?.SetBool("IsAttacking", true);

        // ★ [대기]: 애니메이션이 끝날 때까지 기다림
        // 인스펙터에서 'Cast Time'을 애니메이션 길이와 똑같이 맞춰주세요!
        yield return new WaitForSeconds(castTime);

        // === [투사체 발사] ===
        Fire();

        // === [공격 종료] ===
        animator?.SetBool("IsAttacking", false); // 애니메이션 끄기
        isAttacking = false;           // 상태 해제 (다시 행동 가능)
    }

    void Fire()
    {
        if (projectilePrefab != null && attackPoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, attackPoint.position, Quaternion.identity);

            // 방향 계산
            Vector2 dir = (player.position - attackPoint.position).normalized;

            // Rigidbody2D 적용
            Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                prb.velocity = dir * 8f; // 투사체 속도
            }

            // 투사체 회전 (날아가는 방향 바라보기)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}