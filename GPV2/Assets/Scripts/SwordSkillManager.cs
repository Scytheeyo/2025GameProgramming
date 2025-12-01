using UnityEngine;
using System.Collections;

public class SwordSkillManager : MonoBehaviour
{
    private Player player;
    private bool isSpatialSlashing = false;

    [Header("스킬 공통 설정")]
    // ★ Inspector에서 Enemy와 Boss 레이어를 모두 체크(Mixed) 해주세요!
    public LayerMask enemyLayer;

    [Header("Lv3: 가드 브레이크")]
    public GameObject SmashPrefab;
    public AudioSource SmashAttack;
    public float SmashRadius = 2.0f;
    public int guardBreakManaCost = 20;
    // 일반 몹에게 주는 퍼센트 데미지 (0.5 = 50%)
    public float normalEnemyDamagePercent = 0.5f;
    // 보스 몹에게 주는 퍼센트 데미지 (0.1 = 10%)
    public float bossEnemyDamagePercent = 0.1f;

    [Header("Lv4: 검기")]
    public int swordAuraManaCost = 15;
    public GameObject swordAuraProjectilePrefab; // 검기 프리팹 (AttackDamage 스크립트가 붙어있어야 함)
    public AudioSource SwordAuraAudio;

    [Header("Lv5: 공간참")]
    public int spatialSlashManaCost = 80;
    public CanvasGroup screenOverlay; // 화면 암전용 Canvas Group 
    public GameObject spatialSlashEffectPrefab; // 공간참 이펙트 프리팹
    public float fadeDuration = 0.2f; // 암전/복구에 걸리는 시간
    public float effectDuration = 0.5f; // 이펙트가 보여지는 시간
    public int spatialSlashBossDamage = 100; // 보스에게 들어가는 공간참 데미지

    void Start()
    {
        player = GetComponent<Player>();

        if (screenOverlay != null)
        {
            screenOverlay.alpha = 0f;
            screenOverlay.gameObject.SetActive(true); // 오브젝트 자체는 켜져 있어야 함
        }
    }

    void Update()
    {
        HandleSkillInput();
    }

    void HandleSkillInput()
    {
        // 검(Melee/Hybrid)을 장착했는지 확인
        if (player.EquippedWeapon == null ||
           (player.EquippedWeapon.weaponType != WeaponType.Melee && player.EquippedWeapon.weaponType != WeaponType.Hybrid))
        {
            return;
        }

        int level = player.EquippedWeapon.weaponLevel;

        // Lv 3: 가드 브레이크
        if (level >= 3 && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (TryConsumeMana(guardBreakManaCost)) CastGuardBreak();
        }

        // Lv 4: 검기
        if (level >= 4 && Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (TryConsumeMana(swordAuraManaCost)) CastSwordAura();
        }

        // Lv 5: 공간참
        if (level >= 5 && Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (TryConsumeMana(spatialSlashManaCost)) CastSpatialSlash();
        }
    }

    // --- 유틸리티: 마나 소모 ---
    bool TryConsumeMana(int cost)
    {
        if (player.mana >= cost)
        {
            player.mana -= cost;
            return true;
        }
        // Debug.Log("마나가 부족합니다!");
        return false;
    }

    // --- 스킬 구현 ---

    // 1. 가드 브레이크 (수정됨: 보스 데미지 적용)
    void CastGuardBreak()
    {
        if (SmashAttack != null) SmashAttack.Play();

        player.PerformSkillSwing();

        if (SmashPrefab != null)
        {
            Vector3 spawnPosition = player.firePoint.position;
            Vector3 effectScale = Vector3.one;
            if (!player.isRight)
            {
                effectScale.x = -1; // 이펙트 뒤집기
            }
            GameObject effectInstance = Instantiate(SmashPrefab, spawnPosition, Quaternion.identity);
            effectInstance.transform.localScale = effectScale;
        }

        // 범위 내 적 감지 (Enemy + Boss 레이어)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(player.firePoint.position, SmashRadius, enemyLayer);

        foreach (Collider2D hit in hitEnemies)
        {
            // A. 일반 몬스터 처리
            EnemyController_2D enemy = hit.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakePercentDamage(normalEnemyDamagePercent);
                Debug.Log($"일반 몹 {hit.name} 가드 브레이크 적중");
            }

            // B. [추가] 보스 몬스터 처리
            Boss_CardCaptain boss = hit.GetComponent<Boss_CardCaptain>();
            if (boss != null)
            {
                // 보스에게는 최대 체력의 n% 만큼 데미지 (혹은 고정 데미지로 변경 가능)
                int dmg = Mathf.RoundToInt(boss.maxHealth * bossEnemyDamagePercent);
                boss.TakeDamage(dmg);
                Debug.Log($"보스 가드 브레이크 적중! 데미지: {dmg}");
            }
        }
    }

    // 2. 검기 (투사체 발사)
    void CastSwordAura()
    {
        player.PerformSkillSwing();
        if (swordAuraProjectilePrefab == null) return;

        if (SwordAuraAudio != null) SwordAuraAudio.Play();

        Vector3 firePos = player.firePoint.position;
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        GameObject projObj = Instantiate(swordAuraProjectilePrefab, firePos, Quaternion.identity);
        Projectile projectile = projObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            // 검기는 넉백 없이 (0f) 설정
            projectile.Setup(dir, 0f);
        }

        // ★ 중요: 발사되는 검기 프리팹(swordAuraProjectilePrefab)에 붙은
        // AttackDamage.cs 스크립트가 보스를 인식하도록 수정되어 있어야 합니다.
    }

    // 3. 공간참 (수정됨: 보스 데미지 적용)
    void CastSpatialSlash()
    {
        if (isSpatialSlashing) return;
        StartCoroutine(SpatialSlashSequence());
    }

    IEnumerator SpatialSlashSequence()
    {
        isSpatialSlashing = true;

        // 화면 암전 시작
        yield return StartCoroutine(FadeOverlay(true, fadeDuration));

        // 이펙트 생성
        if (spatialSlashEffectPrefab != null)
        {
            Instantiate(spatialSlashEffectPrefab, Camera.main.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
        }

        yield return new WaitForSeconds(effectDuration);

        // A. 모든 일반 적 찾아서 즉사 (Die)
        EnemyController_2D[] allEnemies = FindObjectsOfType<EnemyController_2D>();
        foreach (var enemy in allEnemies)
        {
            // 보스가 아닌 일반 몹만 즉사
            if (enemy != null && !enemy.isBoss)
            {
                enemy.Die();
            }
        }

        // B. [추가] 보스 찾아서 큰 데미지 (TakeDamage)
        Boss_CardCaptain[] bosses = FindObjectsOfType<Boss_CardCaptain>();
        foreach (var boss in bosses)
        {
            if (boss != null)
            {
                boss.TakeDamage(spatialSlashBossDamage); // 예: 100 데미지
                Debug.Log("보스에게 공간참 적중! (대미지 적용)");
            }
        }

        // 화면 복구
        yield return StartCoroutine(FadeOverlay(false, fadeDuration));

        isSpatialSlashing = false;
    }

    IEnumerator FadeOverlay(bool fadeIn, float duration)
    {
        if (screenOverlay == null) yield break;

        float startAlpha = fadeIn ? 0f : screenOverlay.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float normalizedTime = time / duration;
            screenOverlay.alpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            yield return null;
        }

        screenOverlay.alpha = endAlpha;
    }
}