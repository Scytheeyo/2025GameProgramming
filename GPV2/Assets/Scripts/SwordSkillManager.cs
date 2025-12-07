using UnityEngine;
using System.Collections;

public class SwordSkillManager : MonoBehaviour
{
    private Player player;
    private Animator playerAnim;
    private Rigidbody2D playerRb;

    // 내부 로직용
    public bool isSpatialSlashing = false;

    [Header("Dependencies")]
    public CameraFX cameraFX;

    // ========================================================================
    // [Lv3: 가드 브레이크]
    // ========================================================================
    [Header("Lv3: 가드 브레이크")]
    public int guardBreakManaCost = 20;
    public float guardBreakCastDelay = 0.1f;
    public Vector2 guardBreakBoxSize = new Vector2(2.5f, 1.0f);
    public GameObject guardBreakHitEffectPrefab;
    public AudioSource SmashAttack;
    public LayerMask enemyLayer;

    // ========================================================================
    // [Lv4: 검기]
    // ========================================================================
    [Header("Lv4: 검기")]
    public int swordAuraManaCost = 15;
    public GameObject swordAuraProjectilePrefab;
    public AudioSource SwordAuraAudio;
    public float auraCastDelay = 0.2f;

    // ========================================================================
    // [Lv5: 공간참]
    // ========================================================================
    [Header("Lv5: 공간참")]
    public int spatialSlashManaCost = 80;
    public CanvasGroup screenOverlay;
    public GameObject spatialSlashEffectPrefab;
    public float effectZOffset = -5.0f;
    public float dashDistance = 8.0f;
    public float hitStopDuration = 0.2f;
    public float chargeTime = 0.5f;
    public float dashDuration = 0.1f;
    public bool spawnAtCameraCenter = true;

    // 애니메이션 해시
    private static readonly int AnimCharge = Animator.StringToHash("Charge");
    private static readonly int AnimSlash = Animator.StringToHash("Slash");
    private static readonly int AnimRecovery = Animator.StringToHash("Recovery");
    private static readonly int AnimSwordAura = Animator.StringToHash("SwordAura");
    private static readonly int AnimGuardBreak = Animator.StringToHash("GuardBreak");

    void Start()
    {
        player = GetComponent<Player>();
        playerAnim = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody2D>();

        if (screenOverlay != null) { screenOverlay.alpha = 0f; screenOverlay.gameObject.SetActive(true); }
    }

    void Update()
    {
        HandleSkillInput();
    }

    void HandleSkillInput()
    {
        if (isSpatialSlashing) return;
        if (player.EquippedWeapon == null || (player.EquippedWeapon.weaponType != WeaponType.Melee && player.EquippedWeapon.weaponType != WeaponType.Hybrid)) return;

        int level = player.EquippedWeapon.weaponLevel;

        if (level >= 3 && Input.GetKeyDown(KeyCode.Alpha1)) { if (TryConsumeMana(guardBreakManaCost)) CastGuardBreak(); }
        if (level >= 4 && Input.GetKeyDown(KeyCode.Alpha2)) { if (TryConsumeMana(swordAuraManaCost)) CastSwordAura(); }
        if (level >= 5 && Input.GetKeyDown(KeyCode.Alpha3)) { if (TryConsumeMana(spatialSlashManaCost)) CastSpatialSlash(); }
    }

    bool TryConsumeMana(int cost)
    {
        if (player.mana >= cost) { player.mana -= cost; return true; }
        return false;
    }

    // ========================================================================
    // [Lv3: 가드 브레이크]
    // ========================================================================
    void CastGuardBreak() { StartCoroutine(GuardBreakSequence()); }

    IEnumerator GuardBreakSequence()
    {
        // [핵심] 스킬 도중 움직임/방향전환 차단
        player.isSkillActive = true;
        player.VelocityZero();

        if (playerAnim) playerAnim.SetTrigger(AnimGuardBreak);
        if (SmashAttack != null) SmashAttack.Play();

        yield return new WaitForSeconds(guardBreakCastDelay);

        // 방향 확인 (별도 변수 없이 현재 플레이어 상태 그대로 따라감)
        float dirX = Mathf.Sign(player.transform.localScale.x);

        Vector2 boxCenter = (Vector2)player.transform.position + new Vector2(dirX * 1.0f, 0);
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(boxCenter, guardBreakBoxSize, 0f, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController_2D enemy = enemyCollider.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(30 * 1.5f));
                if (guardBreakHitEffectPrefab != null)
                {
                    Vector3 hitPos = enemy.transform.position + new Vector3(0, 0.5f, 0);
                    GameObject effect = Instantiate(guardBreakHitEffectPrefab, hitPos, Quaternion.identity);
                    Destroy(effect, 0.5f);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);
        player.isSkillActive = false;
    }

    // ========================================================================
    // [Lv4: 검기]
    // ========================================================================
    void CastSwordAura() { StartCoroutine(SwordAuraSequence()); }

    IEnumerator SwordAuraSequence()
    {
        // 검기는 이동하면서 쏠 수 있게 하려면 isSkillActive를 켜지 않습니다.
        // 만약 검기 쏠 때도 멈추게 하고 싶으면 아래 주석을 해제하세요.
        // player.isSkillActive = true; player.VelocityZero();

        if (playerAnim) playerAnim.SetTrigger(AnimSwordAura);
        if (SwordAuraAudio != null) SwordAuraAudio.Play();
        yield return new WaitForSeconds(auraCastDelay);

        if (swordAuraProjectilePrefab != null)
        {
            Vector3 spawnPos = player.transform.position + new Vector3(0, 0.5f, 0);
            GameObject aura = Instantiate(swordAuraProjectilePrefab, spawnPos, Quaternion.identity);

            SwordAuraProjectile projScript = aura.GetComponent<SwordAuraProjectile>();
            if (projScript != null)
            {
                float dirX = Mathf.Sign(player.transform.localScale.x);
                projScript.Setup(new Vector2(dirX, 0));
            }
        }

        // player.isSkillActive = false;
    }

    // ========================================================================
    // [Lv5: 공간참]
    // ========================================================================
    void CastSpatialSlash() { StartCoroutine(SpatialSlashSequence()); }

    IEnumerator SpatialSlashSequence()
    {
        isSpatialSlashing = true;
        // [핵심] 스킬 도중 움직임/방향전환 차단
        player.isSkillActive = true;
        player.VelocityZero();

        float dirX = Mathf.Sign(player.transform.localScale.x);

        StartCoroutine(FadeOverlay(true, 0.2f));
        if (playerAnim) playerAnim.SetTrigger(AnimCharge);
        yield return new WaitForSeconds(chargeTime);

        if (playerAnim) playerAnim.SetTrigger(AnimSlash);
        yield return new WaitForSeconds(0.05f);

        // 돌진
        if (playerRb != null)
        {
            float dashSpeed = dashDistance / dashDuration;
            playerRb.velocity = new Vector2(dirX * dashSpeed, 0);
        }
        yield return new WaitForSeconds(dashDuration);

        // Player.cs에서 FixedUpdate에 isSkillActive 체크를 넣었으므로
        // 여기서 속도를 0으로 만들어도 Player Update문과 충돌하지 않습니다.
        if (playerRb != null) playerRb.velocity = Vector2.zero;
        if (cameraFX != null) cameraFX.Shake(0.3f, 0.5f);

        Vector3 camPos = Camera.main.transform.position;
        Vector3 spawnPos = new Vector3(camPos.x, camPos.y + 1.0f, effectZOffset);

        if (spatialSlashEffectPrefab != null)
        {
            GameObject tear = Instantiate(spatialSlashEffectPrefab, spawnPos, Quaternion.identity);
            Vector3 scale = tear.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * dirX;
            tear.transform.localScale = scale;
        }

        yield return new WaitForSeconds(0.1f);
        if (cameraFX != null) cameraFX.FlashInvert();
        KillEnemiesOnScreen();

        yield return new WaitForSeconds(hitStopDuration);
        if (playerAnim) playerAnim.SetTrigger(AnimRecovery);

        StartCoroutine(FadeOverlay(false, 0.5f));
        yield return new WaitForSeconds(0.5f);

        player.isSkillActive = false;
        isSpatialSlashing = false;
    }

    void KillEnemiesOnScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        EnemyController_2D[] allEnemies = FindObjectsOfType<EnemyController_2D>();
        foreach (var enemy in allEnemies)
        {
            if (enemy == null || enemy.isBoss) continue;
            Vector3 viewPos = cam.WorldToViewportPoint(enemy.transform.position);
            if (viewPos.x >= -0.1f && viewPos.x <= 1.1f && viewPos.y >= -0.1f && viewPos.y <= 1.1f) enemy.Die();
        }
    }

    IEnumerator FadeOverlay(bool fadeIn, float duration)
    {
        if (screenOverlay == null) yield break;
        float startAlpha = screenOverlay.alpha;
        float endAlpha = fadeIn ? 0.7f : 0f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            screenOverlay.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            yield return null;
        }
        screenOverlay.alpha = endAlpha;
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            float dirX = Mathf.Sign(player.transform.localScale.x);
            Vector3 center = player.transform.position + new Vector3(dirX * 1.0f, 0, 0);
            Gizmos.DrawWireCube(center, guardBreakBoxSize);
        }
    }
}