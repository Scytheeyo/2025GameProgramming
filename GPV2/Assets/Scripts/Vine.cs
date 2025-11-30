using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vine : MonoBehaviour
{
    [Header("설정")]
    public float vineJumpPower = 25f; // 점프 힘
    public float climbSpeed = 5f;     // ★ 덩굴 오르내리는 속도
    public float regrabDelay = 0.3f;  // 재부착 대기 시간

    private bool isPlayerAttached = false;
    private Player playerScript;
    private Rigidbody2D playerRb;

    // 플레이어의 원래 스탯 저장
    private float originalGravity;
    private float originalMoveSpeed;
    private bool canGrab = true;

    // 입력값 저장용 변수
    private float verticalInput;

    void Update()
    {
        if (isPlayerAttached && playerScript != null)
        {
            // 방향키 입력 받기
            float h = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical"); // ★ 위아래 입력 저장

            // 점프 (스페이스바)
            if (Input.GetButtonDown("Jump"))
            {
                DoVineJump(h);
            }
        }
    }

    void FixedUpdate()
    {
        if (isPlayerAttached && playerRb != null)
        {
            // ★ 수정됨: 무조건 멈추는 게 아니라, 위아래 입력만큼 속도를 줍니다.
            // 좌우(x)는 0으로 고정하고, 상하(y)는 입력값 * 속도
            playerRb.velocity = new Vector2(0f, verticalInput * climbSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerAttached || !canGrab) return;

        if (other.CompareTag("Player"))
        {
            AttachPlayer(other.gameObject);
        }
    }

    // ★ 추가됨: 덩굴 끝까지 올라가거나 내려가서 범위를 벗어나면 자동으로 놓아주기
    private void OnTriggerExit2D(Collider2D other)
    {
        if (isPlayerAttached && other.CompareTag("Player"))
        {
            DetachPlayer(); // 점프 힘 없이 그냥 놓아주기
        }
    }

    void AttachPlayer(GameObject playerObj)
    {
        playerScript = playerObj.GetComponent<Player>();
        playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (playerScript != null && playerRb != null)
        {
            isPlayerAttached = true;
            verticalInput = 0f; // 초기화

            originalGravity = playerRb.gravityScale;
            originalMoveSpeed = playerScript.moveSpeed;

            playerRb.gravityScale = 0f;
            playerRb.velocity = Vector2.zero;
            playerScript.moveSpeed = 0f;

            Vector3 snappedPos = playerObj.transform.position;
            snappedPos.x = transform.position.x;
            playerObj.transform.position = snappedPos;
        }
    }

    // 플레이어를 덩굴에서 분리하고 스탯을 복구하는 공통 함수
    void DetachPlayer()
    {
        if (!isPlayerAttached) return;

        isPlayerAttached = false;

        // 스탯 복구
        if (playerRb != null) playerRb.gravityScale = originalGravity;
        if (playerScript != null) playerScript.moveSpeed = originalMoveSpeed;
    }

    void DoVineJump(float xDir)
    {
        // 1. 일단 분리 (스탯 복구 포함)
        DetachPlayer();

        // 2. 재부착 방지 쿨타임
        canGrab = false;

        // 3. 점프 힘 적용
        Vector2 jumpDir = new Vector2(xDir, 1f).normalized;
        playerRb.velocity = Vector2.zero; // 기존 기어오르던 속도 초기화
        playerRb.AddForce(jumpDir * vineJumpPower, ForceMode2D.Impulse);

        // 4. 플레이어 방향 전환
        if (xDir != 0)
        {
            bool isRight = (xDir > 0);
            playerScript.isRight = isRight;
            Vector3 scale = playerScript.transform.localScale;
            scale.x = isRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            playerScript.transform.localScale = scale;
        }

        StartCoroutine(CooldownCoroutine());
    }

    IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(regrabDelay);
        canGrab = true;
    }
}
