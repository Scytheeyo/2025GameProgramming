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
    public bool defaultSpriteFaceLeft = true;

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
    private bool isTimeStopped = false;
    public float fadeDuration = 1f;
    public AudioClip timeStopSound;
    public CanvasGroup timeStopOverlay;
    public GameObject TimeStopEffect;

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

        //if (timeStopOverlay != null) timeStopOverlay.SetActive(false);
        if (guardShieldEffect != null) guardShieldEffect.SetActive(false);
        if (laserLine != null) laserLine.enabled = false;

        if (timeStopOverlay != null)
        {
            timeStopOverlay.alpha = 0f;
            timeStopOverlay.gameObject.SetActive(true); // 오브젝트는 켜둬야 함
        }
    }

    void Update()
    {
        // 무기 조건이 맞지 않으면 (무기 교체 등) 마나 가드를 강제 종료하고 리턴
        if (player.EquippedWeapon == null || player.EquippedWeapon.weaponType != WeaponType.Ranged)
        {
            if (player.isManaGuardOn) TurnOffManaGuard();
            return;
        }

        CheckSkillInput();

        if (player.isManaGuardOn)
        {
            player.mana -= manaGuardCostPerSec * Time.deltaTime;
            if (player.mana <= 0)
            {
                player.mana = 0;
                TurnOffManaGuard(); // 마나 부족 시 종료
            }
        }
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

    // ========================================================================
    // [Lv2: 마력 폭발]
    // ========================================================================
    IEnumerator CastExplosionSequence()
    {
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

        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;

            if (player != null) player.transform.localScale = lockedScale;

            if (!hasExploded && elapsed >= explosionHitTiming)
            {
                hasExploded = true;
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);
                foreach (Collider2D hit in hits)
                {
                    IDamageable target = hit.GetComponent<IDamageable>();

                    if (target != null)
                    {
                        target.TakeDamage(explosionDamage);

                        EnemyController_2D enemy = hit.GetComponent<EnemyController_2D>();
                        if (enemy != null)
                        {
                            float pushDirX = (enemy.transform.position.x > transform.position.x) ? 1f : -1f;
                            Vector2 knockbackDir = new Vector2(pushDirX, 0.5f).normalized;
                            enemy.BeginKnockback(knockbackDir, explosionKnockback);
                        }
                    }
                }
            }
            yield return null;
        }

        if (player != null) player.isSkillActive = false;
    }

    // ========================================================================
    // [Lv3: 마나 가드] (수정됨)
    // ========================================================================
    void ToggleManaGuard()
    {
        if (player.isManaGuardOn == false)
        {
            player.isManaGuardOn = true;

            // 켤 때 확실하게 이펙트 활성화
            if (guardShieldEffect != null) guardShieldEffect.SetActive(true);

            if (manaGuardCoroutine != null) StopCoroutine(manaGuardCoroutine);
            manaGuardCoroutine = StartCoroutine(ManaGuardTimer());
        }
        else
        {
            TurnOffManaGuard();
        }
    }

    void TurnOffManaGuard()
    {
        player.isManaGuardOn = false;

        // 끌 때 확실하게 이펙트 비활성화
        if (guardShieldEffect != null) guardShieldEffect.SetActive(false);

        if (manaGuardCoroutine != null)
        {
            StopCoroutine(manaGuardCoroutine);
            manaGuardCoroutine = null;
        }
    }

    IEnumerator ManaGuardTimer()
    {
        yield return new WaitForSeconds(manaGuardDuration);
        TurnOffManaGuard();
    }

    // ========================================================================
    // [Lv4: 레이저 로직]
    // ========================================================================
    IEnumerator CastLaserSequence()
    {
        Vector3 lockedScale = player.transform.localScale;
        float facingDir = Mathf.Sign(player.transform.localScale.x);
        Vector2 direction = new Vector2(facingDir, 0);

        if (player != null)
        {
            player.isSkillActive = true;
            player.VelocityZero();
        }

        if (playerAnim != null) playerAnim.SetTrigger(AnimLaserShot);
        if (audioSource != null && laserSound != null) audioSource.PlayOneShot(laserSound);

        yield return new WaitForSeconds(laserCastDelay);

        Vector3 spawnPos = player.transform.position + new Vector3(laserOffset.x * facingDir, laserOffset.y, 0);

        if (laserPrefab != null)
        {
            GameObject laser = Instantiate(laserPrefab, spawnPos, Quaternion.identity);

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

                if (player != null) player.transform.localScale = lockedScale;

                yield return null;
            }

            Vector2 boxCenter = (Vector2)spawnPos + (direction * (targetDistance / 2));
            Vector2 boxSize = new Vector2(targetDistance, laserThickness);
            debugBoxCenter = boxCenter; debugBoxSize = boxSize;

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyLayer);
            foreach (Collider2D col in hits)
            {
                IDamageable target = col.GetComponent<IDamageable>();
                if (target != null) target.TakeDamage(laserDamage);
            }

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

    // ========================================================================
    // [Lv5: 시간 정지]
    // ========================================================================
    IEnumerator CastTimeStop()
    {
        if (isTimeStopped) yield break; // 중복 실행 방지

        if (TimeStopEffect != null)
        {
            Instantiate(TimeStopEffect, Camera.main.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
        }

        isTimeStopped = true;
        // 1. 사운드 재생
        if (audioSource != null && timeStopSound != null)
        {
            audioSource.PlayOneShot(timeStopSound);
        }

        // 2. 화면 암전 (Fade In)
        StartCoroutine(FadeOverlay(true, fadeDuration));
        yield return new WaitForSeconds(fadeDuration); // 페이드인 완료까지 대기

        // 3. 모든 적 찾아서 얼리기 (EnemyController_2D.cs의 FreezeEnemy 함수 필요)
        EnemyController_2D[] enemies = FindObjectsOfType<EnemyController_2D>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.FreezeEnemy(timeStopDuration);
            }
        }

        // (팁: 적의 투사체도 멈추고 싶다면 여기서 Tag로 찾아서 Rigidbody를 멈추세요)
        //GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyProjectile");
        //foreach (var bullet in enemyBullets) { ... }

        CloverProjectile[] enemyBullets = FindObjectsOfType<CloverProjectile>();
        foreach (var bullet in enemyBullets)
        {
            if (bullet != null)
            {
                // 방금 만든 함수 호출
                bullet.FreezeEnemyBullet(timeStopDuration);
            }
        }

        yield return new WaitForSeconds(timeStopDuration);


        StartCoroutine(FadeOverlay(false, fadeDuration));
        yield return new WaitForSeconds(fadeDuration); // 페이드아웃 완료까지 대기

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

    IEnumerator FadeOverlay(bool fadeIn, float duration)
    {
        if (timeStopOverlay == null) yield break;

        float startAlpha = fadeIn ? 0f : timeStopOverlay.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float normalizedTime = time / duration;
            timeStopOverlay.alpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            yield return null;
        }

        timeStopOverlay.alpha = endAlpha;
    }

}