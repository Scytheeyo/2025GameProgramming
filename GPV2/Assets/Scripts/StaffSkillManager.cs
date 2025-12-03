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

    // ========================================================================
    // [Lv2: 마력 폭발 (심플 버전)]
    // ========================================================================
    [Header("Lv2: 마력 폭발")]
    public int explosionManaCost = 15;
    public int explosionDamage = 20;

    [Tooltip("폭발 범위 (반지름)")]
    public float explosionRadius = 3.0f;

    [Tooltip("X축으로 밀어내는 힘")]
    public float explosionKnockback = 15f;

    public AudioClip explosionSound;
    public float explosionHitTiming = 0.2f; // 3~4번 이미지가 나오는 타이밍

    // (이펙트 프리팹 변수 삭제함 - 플레이어 애니메이션으로 처리하니까 필요 없음)

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
    public bool reverseLaserDirection = false;
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

    // (호환성용 - 안 씀)
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

        if (level >= 3 && Input.GetKeyDown(KeyCode.Alpha2)) { ToggleManaGuard(); }
        if (level >= 4 && Input.GetKeyDown(KeyCode.Alpha3)) { if (TryConsumeMana(laserManaCost)) StartCoroutine(CastLaserSequence()); }
        if (level >= 5 && Input.GetKeyDown(KeyCode.Alpha4)) { if (!isTimeStopped && TryConsumeMana(timeStopManaCost)) StartCoroutine(CastTimeStop()); }
    }

    bool TryConsumeMana(int cost)
    {
        if (player.mana >= cost) { player.mana -= cost; return true; }
        return false;
    }

    // ========================================================================
    // [Lv2: 마력 폭발 로직 (완전 심플 버전)]
    // ========================================================================
    IEnumerator CastExplosionSequence()
    {
        // 1. 플레이어 멈춤
        if (player != null)
        {
            player.isSkillActive = true;
            player.VelocityZero();
        }

        // 2. 애니메이션 재생 (1~4번 프레임 통합 재생)
        if (playerAnim != null) playerAnim.SetTrigger(AnimExplosion);
        if (audioSource != null && explosionSound != null) audioSource.PlayOneShot(explosionSound);

        // 3. 폭발 타이밍 대기 (1, 2번 프레임 지나고 3번 나올 때쯤)
        yield return new WaitForSeconds(explosionHitTiming);

        // 4. 범위 내 적 감지 (원형)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyController_2D enemy = hit.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                // 데미지 적용
                enemy.TakeDamage(explosionDamage);

                // [핵심] X축 넉백 (Y축은 아주 살짝만 띄움)
                // 적이 플레이어보다 오른쪽에 있으면 1, 왼쪽에 있으면 -1
                float pushDirX = (enemy.transform.position.x > transform.position.x) ? 1f : -1f;

                // 대각선 위로 살짝 날아가야 자연스러우므로 Y에 0.5 정도 줌
                Vector2 knockbackDir = new Vector2(pushDirX, 0.5f).normalized;

                // EnemyController에 있는 넉백 함수 호출
                enemy.BeginKnockback(knockbackDir, explosionKnockback);
            }
        }

        // 5. 애니메이션 끝날 때까지 대기 후 복구
        yield return new WaitForSeconds(0.4f); // 전체 애니메이션 길이만큼 대기
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

    // --- Lv4: 레이저 ---
    IEnumerator CastLaserSequence()
    {
        if (player != null)
        {
            player.isSkillActive = true; player.VelocityZero();
            if (reverseLaserDirection) { Vector3 s = player.transform.localScale; s.x *= -1; player.transform.localScale = s; }
        }

        if (playerAnim != null) playerAnim.SetTrigger(AnimLaserShot);
        if (audioSource != null && laserSound != null) audioSource.PlayOneShot(laserSound);

        yield return new WaitForSeconds(laserCastDelay);

        float facingDir = player.transform.localScale.x > 0 ? 1f : -1f;
        Vector2 direction = new Vector2(facingDir, 0);
        Vector3 spawnPos = player.transform.position + new Vector3(laserOffset.x * facingDir, laserOffset.y, 0);

        if (laserPrefab != null)
        {
            GameObject laser = Instantiate(laserPrefab, spawnPos, Quaternion.identity);
            if (facingDir < 0) laser.transform.rotation = Quaternion.Euler(0, 180, 0); else laser.transform.rotation = Quaternion.identity;

            float targetDistance = laserMaxRange;
            RaycastHit2D hit = Physics2D.Raycast(spawnPos, direction, laserMaxRange, groundLayer);
            if (hit.collider != null) targetDistance = hit.distance;

            float currentLength = 0f;
            while (currentLength < targetDistance)
            {
                currentLength += laserGrowSpeed * Time.deltaTime;
                if (currentLength > targetDistance) currentLength = targetDistance;
                laser.transform.localScale = new Vector3(currentLength, laserThickness, 1);
                yield return null;
            }

            Vector2 boxCenter = (Vector2)spawnPos + (direction * (targetDistance / 2));
            Vector2 boxSize = new Vector2(targetDistance, laserThickness);
            debugBoxCenter = boxCenter; debugBoxSize = boxSize;

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyLayer);
            foreach (Collider2D col in hits) { EnemyController_2D enemy = col.GetComponent<EnemyController_2D>(); if (enemy != null) enemy.TakeDamage(laserDamage); }

            yield return new WaitForSeconds(laserDuration);
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
        // 레이저 범위
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(debugBoxCenter, debugBoxSize);

        // [확인] 마력 폭발 범위 (노란색 원)
        Gizmos.color = Color.yellow;
        if (player != null) Gizmos.DrawWireSphere(player.transform.position, explosionRadius);
    }
}