using UnityEngine;
using System.Collections;

public class Enemy_Clover : EnemyController_2D
{
    [Header("Clover Settings")]
    public GameObject projectilePrefab;   // 발사체 프리팹
    public float shootCooldown = 2f;      // 발사 쿨타임 (초)
    private float lastShootTime = 0f;     // 마지막 발사 시점 기록

    protected override void Update()
    {
        if (isDead || player == null) return;

        // 플레이어 거리 계산
        float distance = Vector2.Distance(transform.position, player.position);
        sr.flipX = (player.position.x < transform.position.x); // 바라보는 방향 조정
        UpdateAttackPointDirection();

        // ① 플레이어가 추격 범위 안에 있을 때만 발사
        if (distance <= chaseRange)
        {
            rb.velocity = Vector2.zero; // 이동 없음
            animator?.SetFloat("Speed", 0);

            // 쿨타임 체크 후 발사
            if (Time.time >= lastShootTime + shootCooldown)
            {
                StartCoroutine(ShootProjectile());
                lastShootTime = Time.time;
            }
        }
        else
        {
            // 범위 밖이면 Idle 유지
            rb.velocity = Vector2.zero;
            animator?.SetFloat("Speed", 0);
        }
    }

    IEnumerator ShootProjectile()
    {
        animator?.SetTrigger("Attack");
        yield return new WaitForSeconds(0.3f); // 시전 시간 (애니메이션 타이밍)

        if (projectilePrefab != null && attackPoint != null)
        {
            // 발사체 생성
            GameObject proj = Instantiate(projectilePrefab, attackPoint.position, Quaternion.identity);

            // 방향 계산 (플레이어 위치 기준)
            Vector2 dir = (player.position - attackPoint.position).normalized;

            // Rigidbody2D 적용
            Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                prb.velocity = dir * 8f; // 발사 속도 조정
            }

            // 발사체 회전 방향 맞추기 (선택)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 탐지 범위(공격 사거리) 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
