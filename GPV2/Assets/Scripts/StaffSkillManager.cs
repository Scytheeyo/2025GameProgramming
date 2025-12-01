using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StaffSkillManager : MonoBehaviour
{
    private Player player;
    private AudioSource audioSource;

    [Header("스킬 설정")]
    // ★ 중요: 인스펙터에서 이 변수를 클릭하고 'Enemy'와 'Boss' 레이어를 둘 다 체크하세요!
    public LayerMask enemyLayer;

    [Header("Lv2: 마력 폭발")]
    public int explosionManaCost = 15;
    public GameObject explosionProjectilePrefab; // Projectile 스크립트가 붙은 프리팹
    public float explosionKnockback = 15f;       // 넉백 강도

    [Header("Lv3: 마나 가드")]
    public float manaGuardDuration = 10f;
    public float manaGuardCostPerSec = 1f;
    public GameObject guardShieldEffect; // 플레이어 주변 쉴드 이펙트
    private Coroutine manaGuardCoroutine = null;

    [Header("Lv4: 레이저")]
    public int laserManaCost = 30;
    public int laserDamage = 50;
    public float laserDuration = 0.3f;
    public float laserRange = 12f;
    public float laserWidth = 1f;
    public AudioClip laserSound;
    public LineRenderer laserLine;

    [Header("Lv5: 시간 정지")]
    public int timeStopManaCost = 60;
    public float timeStopDuration = 10f;
    private bool isTimeStopped = false;
    public float fadeDuration = 1f;
    public AudioClip timeStopSound;
    public CanvasGroup timeStopOverlay;
    public GameObject TimeStopEffect;

    void Start()
    {
        player = GetComponent<Player>();

        // 레이저 라인 렌더러 초기화
        if (laserLine == null)
        {
            laserLine = gameObject.AddComponent<LineRenderer>();
            laserLine.startWidth = laserWidth;
            laserLine.endWidth = laserWidth;
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
            laserLine.startColor = Color.cyan;
            laserLine.endColor = Color.blue;
            laserLine.enabled = false;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (timeStopOverlay != null)
        {
            timeStopOverlay.alpha = 0f;
            timeStopOverlay.gameObject.SetActive(true);
        }

        if (guardShieldEffect != null) guardShieldEffect.SetActive(false);
    }

    void Update()
    {
        // 지팡이 계열 무기 체크
        if (player.equippedWeapon == null || player.equippedWeapon.weaponType != WeaponType.Ranged) return;

        CheckSkillInput();

        // 마나 가드 로직 (매 프레임 마나 소모)
        if (player.isManaGuardOn)
        {
            player.mana -= manaGuardCostPerSec * Time.deltaTime;

            if (player.mana <= 0)
            {
                player.mana = 0;
                player.isManaGuardOn = false;
                if (manaGuardCoroutine != null)
                {
                    StopCoroutine(manaGuardCoroutine);
                    manaGuardCoroutine = null;
                }
            }
        }

        // 마나 가드 이펙트 ON/OFF
        if (guardShieldEffect != null)
        {
            guardShieldEffect.SetActive(player.isManaGuardOn);
        }
    }

    void CheckSkillInput()
    {
        if (player.EquippedWeapon == null) return;

        int currentWeaponLevel = player.EquippedWeapon.weaponLevel;

        // Lv 2: 마력 폭발
        if (currentWeaponLevel >= 2 && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (TryConsumeMana(explosionManaCost)) CastExplosion();
        }

        // Lv 3: 마나 가드
        if (currentWeaponLevel >= 3 && Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleManaGuard();
        }

        // Lv 4: 레이저
        if (currentWeaponLevel >= 4 && Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (TryConsumeMana(laserManaCost)) StartCoroutine(CastLaser());
        }

        // Lv 5: 시간 정지
        if (currentWeaponLevel >= 5 && Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (!isTimeStopped && TryConsumeMana(timeStopManaCost))
                StartCoroutine(CastTimeStop());
        }
    }

    bool TryConsumeMana(int cost)
    {
        if (player.mana >= cost)
        {
            player.mana -= cost;
            return true;
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
            return false;
        }
    }

    // =================================================================================
    // 스킬 구현부
    // =================================================================================

    // 1. 마력 폭발
    void CastExplosion()
    {
        if (explosionProjectilePrefab == null) return;

        Vector3 firePos = player.firePoint != null ? player.firePoint.position : transform.position;
        GameObject projObj = Instantiate(explosionProjectilePrefab, firePos, Quaternion.identity);

        // Projectile 스크립트 설정
        Projectile projectile = projObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            projectile.Setup(dir, explosionKnockback);
        }
    }

    // 2. 마나 가드
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
            if (manaGuardCoroutine != null)
            {
                StopCoroutine(manaGuardCoroutine);
                manaGuardCoroutine = null;
            }
        }
    }

    IEnumerator ManaGuardTimer()
    {
        yield return new WaitForSeconds(manaGuardDuration);

        if (player.isManaGuardOn)
        {
            player.isManaGuardOn = false;
            manaGuardCoroutine = null;
        }
    }

    // 3. 레이저 (보스 적용됨)
    IEnumerator CastLaser()
    {
        if (audioSource != null && laserSound != null)
        {
            audioSource.PlayOneShot(laserSound);
        }
        laserLine.enabled = true;

        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector3 startPos = player.firePoint != null ? player.firePoint.position : transform.position;

        // ★ [핵심] enemyLayer에 Boss 레이어가 체크되어 있다면 보스도 hit에 포함됨
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, dir, laserRange, enemyLayer);

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, startPos + (Vector3)(dir * laserRange));

        foreach (var hit in hits)
        {
            // A. 일반 몬스터 피격
            EnemyController_2D enemy = hit.collider.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(laserDamage);
                Debug.Log($"일반 몹 {hit.collider.name} 레이저 적중!");
            }

            // B. [추가] 보스 몬스터 피격
            Boss_CardCaptain boss = hit.collider.GetComponent<Boss_CardCaptain>();
            if (boss != null)
            {
                boss.TakeDamage(laserDamage);
                Debug.Log("보스 레이저 적중!");
            }
        }

        yield return new WaitForSeconds(laserDuration);
        laserLine.enabled = false;
    }

    // 4. 시간 정지 (보스 적용됨)
    IEnumerator CastTimeStop()
    {
        if (isTimeStopped) yield break;

        if (TimeStopEffect != null)
        {
            Instantiate(TimeStopEffect, Camera.main.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
        }

        isTimeStopped = true;

        if (audioSource != null && timeStopSound != null)
        {
            audioSource.PlayOneShot(timeStopSound);
        }

        // 화면 암전
        StartCoroutine(FadeOverlay(true, fadeDuration));
        yield return new WaitForSeconds(fadeDuration);

        // A. 일반 몬스터 얼리기
        EnemyController_2D[] enemies = FindObjectsOfType<EnemyController_2D>();
        foreach (var enemy in enemies)
        {
            if (enemy != null) enemy.FreezeEnemy(timeStopDuration);
        }

        // B. [추가] 보스 몬스터 얼리기
        // (주의: Boss_CardCaptain 스크립트에 FreezeBoss 함수가 추가되어 있어야 함)
        Boss_CardCaptain[] bosses = FindObjectsOfType<Boss_CardCaptain>();
        foreach (var boss in bosses)
        {
            if (boss != null)
            {
                boss.FreezeBoss(timeStopDuration);
                Debug.Log("보스 시간 정지 적용됨!");
            }
        }

        yield return new WaitForSeconds(timeStopDuration);

        // 화면 복구
        StartCoroutine(FadeOverlay(false, fadeDuration));
        yield return new WaitForSeconds(fadeDuration);

        isTimeStopped = false;
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