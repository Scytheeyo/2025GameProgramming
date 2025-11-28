using UnityEngine;
using System.Collections;

public class CheshireCat : MonoBehaviour
{
    // --- 컴포넌트 ---
    private Animator animator;
    private Renderer bodyRenderer;
    private Transform player;

    [Header("Movement Settings")]
    public float moveSpeed = 2.5f;
    public float turnSpeed = 200.0f;
    public float dashSpeed = 10.0f;

    [Header("Alpha Settings")]
    public float alphaUpdateInterval = 0.1f;
    public float alphaStep = 0.1f;

    [Header("AI Settings")]
    public float invisibleDistance = 20.0f;
    public float attackRange = 10.0f;
    public float appearAnimDuration = 2.0f;

    [Tooltip("돌진 공격이 지속되는 시간 (초)")]
    public float attackDuration = 1.0f; // ★ 다시 추가: 돌진 지속 시간

    // --- 상태 변수 ---
    private Material bodyMat;
    private Vector3 targetPos;

    private enum State { Awake, Idle, Walk, AttackReady, Attack }
    private State currentState = State.Awake;

    void Awake()
    {
        animator = GetComponent<Animator>();
        Transform bodyTr = transform.Find("body");
        if (bodyTr != null)
        {
            bodyRenderer = bodyTr.GetComponent<Renderer>();
            bodyMat = bodyRenderer.material;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Start()
    {
        if (bodyMat != null) SetAlphaImmediate(0f);

        StartCoroutine(BehaviorSequence());
        StartCoroutine(AlphaControlRoutine());
        StartCoroutine(TargetUpdateRoutine());
    }

    IEnumerator AlphaControlRoutine()
    {
        while (true)
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
        while (true)
        {
            if (player != null) targetPos = player.position;
            yield return new WaitForSeconds(0.1f);
        }
    }

    void Update()
    {
        if (player == null) return;
        if (currentState == State.Awake || currentState == State.Idle || currentState == State.Attack) return;

        float distance = Vector3.Distance(transform.position, targetPos);

        if (currentState == State.Walk && distance <= attackRange)
        {
            currentState = State.AttackReady;
            animator.SetBool("isWalk", false);
            animator.SetBool("isReady", true);
        }

        if (currentState == State.Walk)
        {
            MoveAndRotate2D();
        }
        else if (currentState == State.AttackReady)
        {
            if (bodyMat.color.a >= 0.99f)
            {
                StartCoroutine(AttackRoutine());
            }
        }
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

    IEnumerator AttackRoutine()
    {
        currentState = State.Attack;

        animator.SetBool("isReady", false);
        animator.SetBool("isDash", true); // 애니메이션 시작 (반복 재생됨)

        float timer = 0f;
        // 설정한 시간(attackDuration)만큼만 돌진
        while (timer < attackDuration)
        {
            transform.Translate(Vector3.down * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // 시간이 다 되면 즉시 애니메이션 해제
        animator.SetBool("isDash", false);

        currentState = State.Walk;
        animator.SetBool("isWalk", true);
    }

    void SetAlphaImmediate(float value)
    {
        Color color = bodyMat.color;
        color.a = value;
        bodyMat.color = color;
    }
}