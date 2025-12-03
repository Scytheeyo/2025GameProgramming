using UnityEngine;
using System.Collections;

public class SwordSkillManager : MonoBehaviour
{
    private Player player;
    private Animator playerAnim;
    private Rigidbody2D playerRb;

    public bool isSpatialSlashing = false;

    [Header("Dependencies")]
    public CameraFX cameraFX;

    // ========================================================================
    // [Lv3: 가드 브레이크 설정]
    // ========================================================================
    [Header("Lv3: 가드 브레이크 설정")]
    public int guardBreakManaCost = 20;
    public float guardBreakCastDelay = 0.1f; // 찌르는 모션 타이밍 (프레임 5->6 넘어가는 시간)

    [Tooltip("찌르기 공격의 범위 (가로, 세로)")]
    public Vector2 guardBreakBoxSize = new Vector2(2.5f, 1.0f);

    [Tooltip("적중 시 생성될 이펙트 (이미지 7~8번 프리팹)")]
    public GameObject guardBreakHitEffectPrefab;

    public AudioSource SmashAttack; // 찌르기 사운드 (변수명 재사용)
    public LayerMask enemyLayer;
    // ========================================================================

    [Header("Lv4: 검기 설정")]
    public int swordAuraManaCost = 15;
    public GameObject swordAuraProjectilePrefab;
    public AudioSource SwordAuraAudio;
    public float auraCastDelay = 0.2f;
    public bool reverseAuraDirection = true;

    [Header("Lv5: 공간참 설정")]
    public int spatialSlashManaCost = 80;
    public CanvasGroup screenOverlay;
    public GameObject spatialSlashEffectPrefab;
    public float effectZOffset = -5.0f;
    public float dashDistance = 8.0f;
    public float hitStopDuration = 0.2f;
    public float chargeTime = 0.5f;
    public float dashDuration = 0.1f;
    public bool reverseDashDirection = true;

    public bool spawnAtCameraCenter = true;

    // 애니메이션 파라미터 해시
    private static readonly int AnimCharge = Animator.StringToHash("Charge");
    private static readonly int AnimSlash = Animator.StringToHash("Slash");
    private static readonly int AnimRecovery = Animator.StringToHash("Recovery");
    private static readonly int AnimSwordAura = Animator.StringToHash("SwordAura");

    // [추가] 가드 브레이크 애니메이션 파라미터
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
    // [Lv3: 가드 브레이크 로직 구현]
    // ========================================================================
    void CastGuardBreak()
    {
        StartCoroutine(GuardBreakSequence());
    }

    IEnumerator GuardBreakSequence()
    {
        // 1. 플레이어 애니메이션 재생 (이미지 5~6번)
        // Animator에 "GuardBreak" Trigger 파라미터가 있어야 합니다.
        if (playerAnim) playerAnim.SetTrigger(AnimGuardBreak);

        // 2. 사운드 재생
        if (SmashAttack != null) SmashAttack.Play();

        // 3. 찌르는 타이밍 대기 (칼을 뻗는 순간까지)
        yield return new WaitForSeconds(guardBreakCastDelay);

        // 4. 공격 범위 계산 (플레이어 앞쪽 네모난 범위)
        float dirX = player.transform.localScale.x > 0 ? 1f : -1f;

        // 박스 중심점: 플레이어 위치에서 앞쪽으로 조금(1.0f) 이동한 곳
        Vector2 boxCenter = (Vector2)player.transform.position + new Vector2(dirX * 1.0f, 0);

        // 범위 내 적 감지
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(boxCenter, guardBreakBoxSize, 0f, enemyLayer);

        // 5. 적 처리
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController_2D enemy = enemyCollider.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                // 데미지 주기 (기본 공격력의 1.5배 등 설정 가능)
                int dmg = Mathf.RoundToInt(30 * 1.5f); // 예시 데미지
                enemy.TakeDamage(dmg);

                // [핵심] 적 위치에 '방패 파괴 이펙트(7~8번)' 생성
                if (guardBreakHitEffectPrefab != null)
                {
                    // 적의 몸통(Center) 위치에 생성
                    Vector3 hitPos = enemy.transform.position + new Vector3(0, 0.5f, 0);
                    GameObject effect = Instantiate(guardBreakHitEffectPrefab, hitPos, Quaternion.identity);

                    // 0.5초 뒤 삭제
                    Destroy(effect, 0.5f);
                }
            }
        }
    }

    // (기즈모: 에디터에서 공격 범위를 눈으로 확인하는 용도)
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            float dirX = player.transform.localScale.x > 0 ? 1f : -1f;
            Vector3 center = player.transform.position + new Vector3(dirX * 1.0f, 0, 0);
            Gizmos.DrawWireCube(center, guardBreakBoxSize);
        }
    }
    // ========================================================================


    // --- Lv4: 검기 ---
    void CastSwordAura() { StartCoroutine(SwordAuraSequence()); }

    IEnumerator SwordAuraSequence()
    {
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
                float dirX = player.transform.localScale.x > 0 ? 1f : -1f;
                if (reverseAuraDirection) dirX *= -1f;
                projScript.Setup(new Vector2(dirX, 0));
            }
        }
    }

    // --- Lv5: 공간참 ---
    void CastSpatialSlash() { StartCoroutine(SpatialSlashSequence()); }

    // (Lv5 공간참 관련 코드는 기존 그대로 유지 - 너무 길어서 생략하지만 위쪽 코드 그대로 두시면 됩니다)
    IEnumerator SpatialSlashSequence()
    {
        isSpatialSlashing = true;
        player.isSkillActive = true;
        player.VelocityZero();

        float scaleDir = player.transform.localScale.x > 0 ? 1f : -1f;
        float finalDirection = reverseDashDirection ? -scaleDir : scaleDir;

        StartCoroutine(FadeOverlay(true, 0.2f));
        if (playerAnim) playerAnim.SetTrigger(AnimCharge);
        yield return new WaitForSeconds(chargeTime);

        if (playerAnim) playerAnim.SetTrigger(AnimSlash);
        yield return new WaitForSeconds(0.05f);

        if (playerRb != null)
        {
            float dashSpeed = dashDistance / dashDuration;
            playerRb.velocity = new Vector2(finalDirection * dashSpeed, 0);
        }
        yield return new WaitForSeconds(dashDuration);

        if (playerRb != null) playerRb.velocity = Vector2.zero;
        if (cameraFX != null) cameraFX.Shake(0.3f, 0.5f);

        Vector3 camPos = Camera.main.transform.position;
        Vector3 spawnPos = new Vector3(camPos.x, camPos.y + 1.0f, effectZOffset);

        if (spatialSlashEffectPrefab != null)
        {
            GameObject tear = Instantiate(spatialSlashEffectPrefab, spawnPos, Quaternion.identity);
            Vector3 scale = tear.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * finalDirection;
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
}