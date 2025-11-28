using UnityEngine;
using System.Collections;

public class CheshireCat : EnemyController_2D
{
    [Header("Cheshire Specific Settings")]
    public float turnSpeed = 200.0f;
    public float dashSpeed = 10.0f;

    [Header("Alpha Settings")]
    public float alphaUpdateInterval = 0.1f;
    public float alphaStep = 0.1f;

    [Header("AI Settings")]
    public float invisibleDistance = 20.0f;
    public float appearAnimDuration = 2.0f;
    public float attackDuration = 1.0f;

    // --- 체셔 고양이 전용 ---
    private Renderer bodyRenderer;
    private Material bodyMat;
    private Vector3 targetPos;

    private enum State { Awake, Idle, Walk, AttackReady, Attack }
    private State currentState = State.Awake;

    protected override void Start()
    {
        base.Start();

        // 렌더러 연결
        Transform bodyTr = transform.Find("body");
        if (bodyTr != null)
        {
            bodyRenderer = bodyTr.GetComponent<Renderer>();
            bodyMat = bodyRenderer.material;
            SetAlphaImmediate(0f);
        }
        else
        {
            if (sr != null) bodyMat = sr.material;
        }

        StartCoroutine(BehaviorSequence());
        StartCoroutine(AlphaControlRoutine());
        StartCoroutine(TargetUpdateRoutine());
    }

    protected override void Update()
    {
        if (isDead || player == null) return;

        if (currentState == State.Awake || currentState == State.Idle || currentState == State.Attack) return;

        float distance = Vector3.Distance(transform.position, targetPos);

        // 공격 사거리 체크
        if (currentState == State.Walk && distance <= base.chaseRange)
        {
            currentState = State.AttackReady;
            animator.SetBool("isWalk", false);
            animator.SetBool("isReady", true);
            rb.angularVelocity = 0f; // 회전 관성 정지
        }

        if (currentState == State.Walk)
        {
            MoveAndRotate2D();
        }
        else if (currentState == State.AttackReady)
        {
            // ★ [수정됨] 서서히 돌지 않고, 즉시 플레이어 방향으로 고정
            LookAtTargetImmediate();

            // 공격 시작 조건: 투명도가 완전히 찼는가?
            // (방향은 이미 즉시 맞췄으므로 각도 체크는 불필요)
            if (bodyMat != null && bodyMat.color.a >= 0.99f)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        // 테스트용
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10);
    }

    // ★ [수정됨] 즉시 방향 전환 함수
    void LookAtTargetImmediate()
    {
        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle += 90f; // 스프라이트 방향 보정 (아래쪽이 앞)

        // RotateTowards 없이 바로 각도 대입
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void MoveAndRotate2D()
    {
        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle += 90f;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (currentState == State.Attack && other.CompareTag("Player"))
        {
            // 실제 데미지 적용 시 아래 주석 해제 및 수정
            // other.GetComponent<PlayerController>()?.TakeDamage(damage);
            Debug.Log($"😼 체셔 고양이의 돌진 공격! 데미지: {damage}");
        }
    }

    // ★ 사용자의 요청에 따라 경고 방지용 오버라이드 함수들(OnTriggerExit2D, TakeDamage)을 삭제했습니다.
    // 이제 부모 클래스의 함수가 그대로 호출됩니다.

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        myCollider.enabled = false;

        DropItem();
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        float fadeTime = 1.5f;
        float t = 0;
        Color startColor = bodyMat != null ? bodyMat.color : Color.white;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            if (bodyMat != null)
            {
                float newAlpha = Mathf.Lerp(startColor.a, 0f, t / fadeTime);
                Color c = bodyMat.color;
                c.a = newAlpha;
                bodyMat.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator AlphaControlRoutine()
    {
        while (!isDead)
        {
            if (bodyMat != null && player != null)
            {
                float currentAlpha = bodyMat.color.a;
                float targetAlpha = 0f;
                float distance = Vector3.Distance(transform.position, targetPos);

                switch (currentState)
                {
                    case State.Awake:
                    case State.Idle:
                        targetAlpha = 0f;
                        break;
                    case State.Walk:
                        if (distance > invisibleDistance) targetAlpha = 1f;
                        else targetAlpha = 0f;
                        break;
                    case State.AttackReady:
                    case State.Attack:
                        targetAlpha = 1f;
                        break;
                }

                if (Mathf.Abs(currentAlpha - targetAlpha) > 0.001f)
                {
                    float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, alphaStep);
                    SetAlphaImmediate(newAlpha);
                }
            }
            yield return new WaitForSeconds(alphaUpdateInterval);
        }
    }

    IEnumerator AttackRoutine()
    {
        currentState = State.Attack;
        isAttacking = true;

        animator.SetBool("isReady", false);
        animator.SetBool("isDash", true);

        rb.velocity = Vector2.zero;
        Vector2 dashDir = -transform.up;

        float timer = 0f;
        while (timer < attackDuration)
        {
            if (isDead) yield break;
            rb.velocity = dashDir * dashSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        animator.SetBool("isDash", false);

        currentState = State.Idle;
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
        currentState = State.Walk;
        animator.SetBool("isWalk", true);
    }

    IEnumerator BehaviorSequence()
    {
        currentState = State.Awake;
        animator.SetTrigger("awake");
        yield return new WaitForSeconds(appearAnimDuration);

        currentState = State.Idle;
        yield return new WaitForSeconds(1.0f);

        currentState = State.Walk;
        animator.SetBool("isWalk", true);
    }

    IEnumerator TargetUpdateRoutine()
    {
        while (!isDead)
        {
            if (player != null) targetPos = player.position;
            yield return new WaitForSeconds(0.1f);
        }
    }

    void SetAlphaImmediate(float value)
    {
        if (bodyMat == null) return;
        Color color = bodyMat.color;
        color.a = value;
        bodyMat.color = color;
    }

    protected override IEnumerator HitFlashRoutine()
    {
        if (bodyMat != null)
        {
            Color originalColor = bodyMat.color;
            Color flashColor = hitColor;
            flashColor.a = originalColor.a;

            bodyMat.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            Color restoreColor = Color.white;
            restoreColor.a = bodyMat.color.a;
            bodyMat.color = restoreColor;
        }
    }
}