using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StaffSkillManager : MonoBehaviour
{
    private Player player;
    private AudioSource audioSource; 

    [Header("스킬 설정")]
    public LayerMask enemyLayer; // 적 레이어 지정 필수

    [Header("Lv2: 마력 폭발")]
    public int explosionManaCost = 15;
    public GameObject explosionProjectilePrefab; //Projectile 스크립트가 붙은 프리팹 연결
    public float explosionKnockback = 15f;       //넉백 강도 설정

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
    public LineRenderer laserLine; // 컴포넌트 필요

    [Header("Lv5: 시간 정지")]
    public int timeStopManaCost = 60;
    public float timeStopDuration = 10f;
    private bool isTimeStopped = false;
    public float fadeDuration = 1f;      
    public AudioClip timeStopSound;         
    public CanvasGroup timeStopOverlay;     

    void Start()
    {
        player = GetComponent<Player>();

        // 레이저 초기화
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
            timeStopOverlay.gameObject.SetActive(true); // 오브젝트는 켜둬야 함
        }

        if (guardShieldEffect != null) guardShieldEffect.SetActive(false);
    }

    void Update()
    {
        // 지팡이 계열 무기를 들고 있을 때만 스킬 사용 가능 체크 (옵션)
        if (player.equippedWeapon == null || player.equippedWeapon.weaponType != WeaponType.Ranged) return;

        CheckSkillInput();

        if (player.isManaGuardOn)
        {
            player.mana -= manaGuardCostPerSec *Time.deltaTime ;

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

        // 마나 가드 상태 시각적 표현 유지 (플레이어 위치 따라가기 등)
        if (player.isManaGuardOn && guardShieldEffect != null)
        {
            guardShieldEffect.SetActive(true);
        }
        else if (!player.isManaGuardOn && guardShieldEffect != null)
        {
            guardShieldEffect.SetActive(false);
        }
    }

    void CheckSkillInput()
    {
        if (player.EquippedWeapon == null || player.EquippedWeapon.weaponType != WeaponType.Ranged)
        {
            return; 
        }

        if (player.EquippedWeapon == null) return;


        int currentWeaponLevel = player.EquippedWeapon.weaponLevel;



        if (currentWeaponLevel >= 2 && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (TryConsumeMana(explosionManaCost)) CastExplosion();
        }

        if (currentWeaponLevel >= 3 && Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleManaGuard();
        }

        if (currentWeaponLevel >= 4 && Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (TryConsumeMana(laserManaCost)) StartCoroutine(CastLaser());
        }

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
            Debug.Log($"마나 소모: -{cost} (남은 마나: {player.mana})");
            return true;
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
            return false;
        }
    }

    // 1. 마력 폭발 (Knockback)
    void CastExplosion()
    {
        if (explosionProjectilePrefab == null) return;

        Vector3 firePos = player.firePoint != null ? player.firePoint.position : transform.position;
        GameObject projObj = Instantiate(explosionProjectilePrefab, firePos, Quaternion.identity);

        // Projectile 스크립트 가져오기
        Projectile projectile = projObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            // 방향 결정
            Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            projectile.Setup(dir, explosionKnockback);
        }

    }

    // 2. 마나 가드 (토글 및 10초 시간 제한)
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

    // 3. 레이저 (Raycast)
    IEnumerator CastLaser()
    {
        if (audioSource != null && laserSound != null)
        {
            audioSource.PlayOneShot(laserSound);
        }
        laserLine.enabled = true;

        // 플레이어가 보는 방향 (Player 스크립트의 isRight 변수 활용은 private이라 transform.localScale로 판단)
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector3 startPos = player.firePoint != null ? player.firePoint.position : transform.position;

        // 레이캐스트
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, dir, laserRange, enemyLayer);

        // 라인 그리기
        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, startPos + (Vector3)(dir * laserRange));

        foreach (var hit in hits)
        {
            // 적 소멸 또는 큰 데미지
            Debug.Log(hit.collider.name + " 레이저 적중!");
            Destroy(hit.collider.gameObject); // 적 소멸
        }

        yield return new WaitForSeconds(laserDuration);
        laserLine.enabled = false;
    }

    IEnumerator CastTimeStop()
    {
        if (isTimeStopped) yield break; // 중복 실행 방지

        isTimeStopped = true;
        Debug.Log("시간 정지! (암전)");

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
        // GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyProjectile");
        // foreach (var bullet in enemyBullets) { ... }

        // 4. 스킬 지속 시간만큼 대기 (이 시간 동안 화면은 계속 어두움)
        yield return new WaitForSeconds(timeStopDuration);


        StartCoroutine(FadeOverlay(false, fadeDuration));
        yield return new WaitForSeconds(fadeDuration); // 페이드아웃 완료까지 대기

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