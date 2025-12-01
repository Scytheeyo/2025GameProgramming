using UnityEngine;
using System.Collections;
using System.Linq;
public class SwordSkillManager : MonoBehaviour
{
    private Player player;
    private bool isSpatialSlashing = false;

    [Header("Lv3: 가드 브레이크")]
    public GameObject SmashPrefab;
    public AudioSource SmashAttack;
    public float SmashRadius = 2.0f;
    public int guardBreakManaCost = 20;
    public float guardBreakRange = 1.5f; // 전방 1.5m
    public float normalEnemyDamagePercent = 0.5f; // 일반몹 50%
    public float bossEnemyDamagePercent = 0.1f;  // 보스몹 10%
    public LayerMask enemyLayer;

    [Header("Lv4: 검기")]
    public int swordAuraManaCost = 15;
    public GameObject swordAuraProjectilePrefab; // 검기 프리팹 
    public AudioSource SwordAuraAudio;

    [Header("Lv5: 공간참")]
    public int spatialSlashManaCost = 80;
    public CanvasGroup screenOverlay; // 화면 암전용 Canvas Group 
    public GameObject spatialSlashEffectPrefab; // 공간참 이펙트 프리팹
    public float fadeDuration = 0.2f; // 암전/복구에 걸리는 시간
    public float effectDuration = 0.5f; // 이펙트가 보여지는 시간

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
        Debug.Log("마나가 부족합니다!");
        return false;
    }

    // --- 스킬 구현 ---

    // 1. 가드 브레이크
    void CastGuardBreak()
{
        SmashAttack.Play();
        player.PerformSkillSwing();
        if (SmashPrefab == null) return;

        Vector3 spawnPosition = player.firePoint.position;
        Vector3 effectScale = Vector3.one;
        if (!player.isRight)
        {
            effectScale.x = -1; // 이펙트의 x 스케일을 -1로 만들어서 뒤집음
        }
        GameObject effectInstance = Instantiate(SmashPrefab, spawnPosition, Quaternion.identity);
        effectInstance.transform.localScale = effectScale;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPosition, SmashRadius, enemyLayer);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            IDamageable[] target = FindObjectsOfType<MonoBehaviour>().OfType<IDamageable>().ToArray();
            foreach (var enemy in target)
            {
                if (enemy != null) //&& !enemy.isBoss)
                {
                    bool isBoss = (enemy as Component).CompareTag("Boss");
                    float percent = isBoss ? bossEnemyDamagePercent : normalEnemyDamagePercent;
                    enemy.TakePercentDamage(percent);
                }
            }
        }
    }

    // 2. 검기
    void CastSwordAura()
    {
        player.PerformSkillSwing();
        if (swordAuraProjectilePrefab == null) return;

        SwordAuraAudio.Play();
        Vector3 firePos = player.firePoint.position;
        Vector2 dir = player.transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        GameObject projObj = Instantiate(swordAuraProjectilePrefab, firePos, Quaternion.identity);
        Projectile projectile = projObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            // 검기는 넉백 없이 (0f)
            projectile.Setup(dir, 0f);
        }
    }

    // 3. 공간참
    void CastSpatialSlash()
    {
        if (isSpatialSlashing) return;
        StartCoroutine(SpatialSlashSequence());
    }

    IEnumerator SpatialSlashSequence()
    {
        isSpatialSlashing = true;
        yield return StartCoroutine(FadeOverlay(true, fadeDuration));

        if (spatialSlashEffectPrefab != null)
        {
            Instantiate(spatialSlashEffectPrefab, Camera.main.transform.position + new Vector3(0, 0, 10), Quaternion.identity);
        }
        yield return new WaitForSeconds(effectDuration);

        IDamageable[] target = FindObjectsOfType<MonoBehaviour>().OfType<IDamageable>().ToArray();
        foreach (var enemy in target)
        {
            if (enemy != null) //&& !enemy.isBoss)
            {
                enemy.Die();
            }
        }
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