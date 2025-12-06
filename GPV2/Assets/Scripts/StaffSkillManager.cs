using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StaffSkillManager : MonoBehaviour
{
    private Player player;
    private Animator playerAnim;
    private AudioSource audioSource;

    [Header("스킬 공통 설정")]
    public LayerMask enemyLayer;
    public LayerMask groundLayer;

    [Header("스프라이트 설정 (중요)")]
    [Tooltip("플레이어 캐릭터의 원본 그림이 왼쪽을 보고 있다면 체크하세요.")]
    public bool defaultSpriteFaceLeft = true; // [추가] 기본값 True

    // ========================================================================
    // [Lv2: 마력 폭발]
    // ========================================================================
    [Header("Lv2: 마력 폭발")]
    public int explosionManaCost = 15;
    public int explosionDamage = 20;
    public float explosionRadius = 3.0f;
    public float explosionKnockback = 15f;
    public AudioClip explosionSound;
    public float explosionHitTiming = 0.2f;

    // ========================================================================
    // [Lv3: 마나 가드]
    // ========================================================================
    [Header("Lv3: 마나 가드")]
    public float manaGuardDuration = 10f;
    public float manaGuardCostPerSec = 1f;
    public GameObject guardShieldEffect;
    private Coroutine manaGuardCoroutine = null;

    // ========================================================================
    // [Lv4: 레이저 설정]
    // ========================================================================
    [Header("Lv4: 레이저 설정")]
    public int laserManaCost = 30;
    public int laserDamage = 50;
    public float laserMaxRange = 12f;
    public float laserDuration = 0.5f;
    public float laserGrowSpeed = 50f;
    public float laserCastDelay = 0.2f;
    public float laserThickness = 1.0f;
    public Vector2 laserOffset = new Vector2(0.5f, 0.2f);
    public AudioClip laserSound;
    public GameObject laserPrefab;
    private Vector2 debugBoxCenter;
    private Vector2 debugBoxSize;

    // ========================================================================
    // [Lv5: 시간 정지]
    // ========================================================================
    [Header("Lv5: 시간 정지")]
    public int timeStopManaCost = 60;
    public float timeStopDuration = 10f;
    public AudioClip timeStopSound;
    public GameObject timeStopBackgroundObject;
    public GameObject TimeStopEffect;
    private bool isTimeStopped = false;

    // 애니메이션 해시
    private static readonly int AnimTimeStop = Animator.StringToHash("TimeStop");
    private static readonly int AnimLaserShot = Animator.StringToHash("LaserShot");
    private static readonly int AnimExplosion = Animator.StringToHash("Explosion");

    // (사용 안 함, 호환성 유지용)
    public LineRenderer laserLine;
    public GameObject explosionProjectilePrefab;

    void Start()
    {
        player = GetComponent<Player>();
        playerAnim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (timeStopBackgroundObject != null) timeStopBackgroundObject.SetActive(false);
        if (guardShieldEffect != null) guardShieldEffect.SetActive(false);
        if (laserLine != null) laserLine.enabled = false;
    }

    void Update()
    {
        if (player.EquippedWeapon == null || player.EquippedWeapon.weaponType != WeaponType.Ranged) return;

        CheckSkillInput();

        if (player.isManaGuardOn)
        {
            player.mana -= manaGuardCostPerSec * Time.deltaTime;
            if (player.mana <= 0)
            {
                player.mana = 0;
                player.isManaGuardOn = false;
                if (manaGuardCoroutine != null) StopCoroutine(manaGuardCoroutine);
            }
        }

        if (guardShieldEffect != null) guardShieldEffect.SetActive(player.isManaGuardOn);
    }

    void CheckSkillInput()
    {
        int level = player.EquippedWeapon.weaponLevel;

        if (level >= 2 && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (TryConsumeMana(explosionManaCost)) StartCoroutine(CastExplosionSequence());
        }

        if (level >= 3 && Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleManaGuard();
        }

        if (level >= 4 && Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (TryConsumeMana(laserManaCost)) StartCoroutine(CastLaserSequence());
        }

        if (level >= 5 && Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (!isTimeStopped && TryConsumeMana(timeStopManaCost)) StartCoroutine(CastTimeStop());
        }
    }

    bool TryConsumeMana(int cost)
    {
        if (player.mana >= cost) { player.mana -= cost; return true; }
        return false;
    }

    // [헬퍼 함수] 방향 계산 (왼쪽 원본 스프라이트 고려)
    // Scale.x가 양수(1)일 때: 원본이 왼쪽이면 왼쪽(-1), 원본이 오른쪽이면 오른쪽(1)
    float GetFacingDirection()
    {
        float scaleX = player.transform.localScale.x;
        // 스케일 부호(1 or -1)
        float sign = Mathf.Sign(scaleX);

        // 원본이 왼쪽을 보고 있다면, 스케일이 양수일 때 왼쪽(-1)이 됨
        if (defaultSpriteFaceLeft) return sign * -1f;

        return sign;
    }

    // ========================================================================
    // [Lv2: 마력 폭발]
    // ========================================================================
    IEnumerator CastExplosionSequence()
    {
        // [방향 고정용] 현재 스케일 저장
        Vector3 lockedScale = player.transform.localScale;

        if (player != null)
        {
            player.isSkillActive = true;
            player.VelocityZero();
        }

        if (playerAnim != null) playerAnim.SetTrigger(AnimExplosion);
        if (audioSource != null && explosionSound != null) audioSource.PlayOneShot(explosionSound);

        float elapsed = 0f;
        bool hasExploded = false;

        // 애니메이션 재생 중 방향 고정 루프
        while (elapsed < 0.4f) // 전체 시간
        {
            elapsed += Time.deltaTime;

            // [핵심] 매 프레임 방향을 강제로 고정해서 애니메이션이 뒤집는 걸 막음
            if (player != null) player.transform.localScale = lockedScale;

            if (!hasExploded && elapsed >= explosionHitTiming)
            {
                hasExploded = true;
                // 폭발 처리
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);
                foreach (Collider2D hit in hits)
                {
                    EnemyController_2D enemy = hit.GetComponent<EnemyController_2D>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(explosionDamage);
                        float pushDirX = (enemy.transform.position.x > transform.position.x) ? 1f : -1f;
                        Vector2 knockbackDir = new Vector2(pushDirX, 0.5f).normalized;
                        enemy.BeginKnockback(knockbackDir, explosionKnockback);
                    }
                }
            }
            yield return null;
        }

        if (player != null) player.isSkillActive = false;
    }

    // --- Lv3: 마나 가드 ---
    void ToggleManaGuard()
    {
        if (player.isManaGuardOn == false)
        {
            player.isManaGuardOn = true;
            if (manaGuardCoroutine != null) StopCoroutine(manaGuardCoroutine);
            manaGuardCoroutine = StartCoroutine(ManaGuardTimer());
        }
        else
        {
            player.isManaGuardOn = false;
            if (manaGuardCoroutine != null) { StopCoroutine(manaGuardCoroutine); manaGuardCoroutine = null; }
        }
    }

    IEnumerator ManaGuardTimer()
    {
        yield return new WaitForSeconds(manaGuardDuration);
        player.isManaGuardOn = false;
        manaGuardCoroutine = null;
    }

    // ========================================================================
    // [Lv4: 레이저 로직] (수정됨)
    // ========================================================================
    IEnumerator CastLaserSequence()
    {
        // [중요] 시작할 때의 스케일(방향)을 저장
        Vector3 lockedScale = player.transform.localScale;

        // [수정] 그림판에서 이미지를 돌렸으므로, 복잡한 계산 없이 현재 플레이어의 Scale 방향을 그대로 믿습니다.
        // Scale X가 1이면 오른쪽(1), -1이면 왼쪽(-1)으로 설정
        float facingDir = Mathf.Sign(player.transform.localScale.x);

        Vector2 direction = new Vector2(facingDir, 0);

        // 1. 플레이어 멈춤
        if (player != null)
        {
            player.isSkillActive = true;
            player.VelocityZero();
        }

        // 2. 애니메이션 재생
        if (playerAnim != null) playerAnim.SetTrigger(AnimLaserShot);
        if (audioSource != null && laserSound != null) audioSource.PlayOneShot(laserSound);

        yield return new WaitForSeconds(laserCastDelay);

        // 발사 위치 계산 (facingDir가 정직하게 플레이어 방향을 가리킴)
        Vector3 spawnPos = player.transform.position + new Vector3(laserOffset.x * facingDir, laserOffset.y, 0);

        // 5. 레이저 생성 및 늘리기
        if (laserPrefab != null)
        {
            GameObject laser = Instantiate(laserPrefab, spawnPos, Quaternion.identity);

            // 레이저 회전 (Pivot: Left 기준이라고 가정)
            // 오른쪽(1)이면 0도, 왼쪽(-1)이면 180도 회전
            if (facingDir < 0) laser.transform.rotation = Quaternion.Euler(0, 180, 0);
            else laser.transform.rotation = Quaternion.identity;

            float targetDistance = laserMaxRange;
            RaycastHit2D hit = Physics2D.Raycast(spawnPos, direction, laserMaxRange, groundLayer);
            if (hit.collider != null) targetDistance = hit.distance;

            float currentLength = 0f;
            while (currentLength < targetDistance)
            {
                currentLength += laserGrowSpeed * Time.deltaTime;
                if (currentLength > targetDistance) currentLength = targetDistance;
                laser.transform.localScale = new Vector3(currentLength, laserThickness, 1);

                // 늘어나는 동안 플레이어 방향 고정
                if (player != null) player.transform.localScale = lockedScale;

                yield return null;
            }

            // 데미지 판정
            Vector2 boxCenter = (Vector2)spawnPos + (direction * (targetDistance / 2));
            Vector2 boxSize = new Vector2(targetDistance, laserThickness);
            debugBoxCenter = boxCenter; debugBoxSize = boxSize;

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyLayer);
            foreach (Collider2D col in hits)
            {
                EnemyController_2D enemy = col.GetComponent<EnemyController_2D>();
                if (enemy != null) enemy.TakeDamage(laserDamage);
            }

            // 유지 시간
            float elapsed = 0f;
            while (elapsed < laserDuration)
            {
                elapsed += Time.deltaTime;
                if (player != null) player.transform.localScale = lockedScale;
                yield return null;
            }

            Destroy(laser);
        }

        if (player != null) player.isSkillActive = false;
    }

    // --- Lv5: 시간 정지 ---
    IEnumerator CastTimeStop()
    {
        if (isTimeStopped) yield break;
        isTimeStopped = true;

        if (playerAnim != null) playerAnim.SetTrigger(AnimTimeStop);
        if (audioSource != null && timeStopSound != null) audioSource.PlayOneShot(timeStopSound);
        if (timeStopBackgroundObject != null) timeStopBackgroundObject.SetActive(true);
        if (TimeStopEffect != null) Instantiate(TimeStopEffect, transform.position, Quaternion.identity);

        EnemyController_2D[] enemies = FindObjectsOfType<EnemyController_2D>();
        foreach (var enemy in enemies) { if (enemy != null) enemy.FreezeEnemy(timeStopDuration); }

        yield return new WaitForSeconds(timeStopDuration);

        if (timeStopBackgroundObject != null) timeStopBackgroundObject.SetActive(false);
        isTimeStopped = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(debugBoxCenter, debugBoxSize);

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.transform.position, explosionRadius);
        }
    }
}