using UnityEngine;
using System.Collections;

public class Caterpillar : EnemyController_2D
{
    [Header("Caterpillar Settings")]
    [Tooltip("기본 걷기 속도")]
    public float walkSpeed = 2.0f;
    [Tooltip("돌진(공격) 시 속도")]
    public float dashSpeed = 6.0f;
    [Tooltip("돌진을 시작하는 거리")]
    public float dashRange = 5.0f;
    [Tooltip("돌진 지속 시간")]
    public float attackDuration = 1.0f;

    [Header("Animation Speed")]
    public float walkAnimSpeed = 1.0f;
    public float dashAnimSpeed = 2.0f;

    // 내부 변수
    private Vector3 targetPos;
    private bool isDashing = false;
    private Vector3 lockedDashDirection;

    // 애니메이터 파라미터 해시
    private readonly int hashTR = Animator.StringToHash("isTR"); // Top-Right (우상)
    private readonly int hashTL = Animator.StringToHash("isTL"); // Top-Left (좌상)
    private readonly int hashBR = Animator.StringToHash("isBR"); // Bottom-Right (우하)
    private readonly int hashBL = Animator.StringToHash("isBL"); // Bottom-Left (좌하)

    protected override void Start()
    {
        // 부모의 Start에서 최상위의 SpriteRenderer(sr)를 찾습니다.
        base.Start();
    }

    protected override void Update()
    {
        if (isDead || player == null) return;

        if (isDashing) return;

        targetPos = player.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance <= dashRange)
        {
            StartCoroutine(AttackRoutine());
        }
        else
        {
            HandleMovementAndRotation();
        }

        // 테스트용
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    void HandleMovementAndRotation()
    {
        animator.speed = walkAnimSpeed;

        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction == Vector3.zero) return;

        // 이동
        transform.position += direction * walkSpeed * Time.deltaTime;

        // 애니메이션 & 회전
        UpdateAnimationAndRotation(direction);
    }

    IEnumerator AttackRoutine()
    {
        isDashing = true;
        animator.speed = dashAnimSpeed;

        lockedDashDirection = (player.position - transform.position).normalized;
        UpdateAnimationAndRotation(lockedDashDirection);

        float timer = 0f;
        while (timer < attackDuration)
        {
            if (isDead) yield break;

            transform.position += lockedDashDirection * dashSpeed * Time.deltaTime;

            timer += Time.deltaTime;
            yield return null;
        }

        animator.speed = walkAnimSpeed;
        yield return new WaitForSeconds(0.5f);

        isDashing = false;
    }

    void UpdateAnimationAndRotation(Vector3 dir)
    {
        if (dir == Vector3.zero) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float baseAngle = 0f;

        // 4방향 판정
        if (angle >= 0 && angle < 90)
        {
            SetDirectionAnim(hashTR);
            baseAngle = 45f;
        }
        else if (angle >= 90 && angle <= 180)
        {
            SetDirectionAnim(hashTL);
            baseAngle = 135f;
        }
        else if (angle >= -180 && angle < -90)
        {
            SetDirectionAnim(hashBL);
            baseAngle = -135f;
        }
        else
        {
            SetDirectionAnim(hashBR);
            baseAngle = -45f;
        }

        // 각도 보정
        float rotationOffset = angle - baseAngle;
        transform.rotation = Quaternion.Euler(0, 0, rotationOffset);
    }

    void SetDirectionAnim(int activeHash)
    {
        animator.SetBool(hashTR, activeHash == hashTR);
        animator.SetBool(hashTL, activeHash == hashTL);
        animator.SetBool(hashBR, activeHash == hashBR);
        animator.SetBool(hashBL, activeHash == hashBL);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (other.CompareTag("Player"))
        {
            Debug.Log($"🐛 애벌레 몸통 박치기! 데미지: {damage}");
        }
    }

    // ★ [수정됨] protected -> public으로 변경
    public override void Die()
    {
        if (isDead) return;
        isDead = true;

        // 애니메이션 속도 원복 및 코루틴 정지
        animator.speed = 1f;
        StopAllCoroutines();

        // 물리 비활성화
        if (rb != null) rb.velocity = Vector2.zero;
        if (myCollider != null) myCollider.enabled = false;

        // 아이템 드랍
        DropItem();

        // 서서히 사라지는 연출 시작
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        float fadeTime = 1.0f;
        float t = 0;
        Color startColor = sr != null ? sr.color : Color.white;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float newAlpha = Mathf.Lerp(startColor.a, 0f, t / fadeTime);
                Color c = sr.color;
                c.a = newAlpha;
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    // [피격 연출 오버라이드] 부모의 TakeDamage에 의해 호출됨
    protected override IEnumerator HitFlashRoutine()
    {
        if (sr != null)
        {
            sr.color = hitColor;
            yield return new WaitForSeconds(flashDuration);
            sr.color = Color.white;
        }
    }
}