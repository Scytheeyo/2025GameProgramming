using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public static Player instance;

    // [핵심] 스킬 매니저에서 제어하는 변수
    [HideInInspector] public bool isSkillActive = false;

    [Header("이동 및 점프")]
    public float moveSpeed = 5f;
    public float jumpForce = 30f;
    private float hMove = 0f;
    private bool isGrounded = false;
    public bool isRight = true;

    [Header("점프 시너지")]
    public int maxJumpCount = 1;
    public int currentJumpCount = 0;
    public float jumpMultiplier = 1.0f;

    [Header("전투 기본")]
    public GameObject attackHitbox;
    public float attackDelay = 0.2f;
    public float attackDuration = 0.4f;

    [Header("원거리 공격")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.1f;
    private float nextFireTime = 0f;

    [Header("상태 및 스탯")]
    public int baseMaxHealth = 100;
    public int maxHealth;
    public int health;
    public int baseMaxMana = 100;
    public int maxMana;
    public float mana;
    public float baseAttackDamage = 10f;
    public float currentAttackDamage;
    public float baseDefense = 0f;
    public float currentDefense;

    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;
    private bool isDead = false;

    [Header("장비 및 오디오")]
    public Transform staffSlot;
    public Transform swordSlot;
    public Weapon equippedWeapon = null;
    public AudioSource DoorOpen;
    public AudioSource normalAttack;
    public Weapon EquippedWeapon { get { return equippedWeapon; } }

    [Header("무기 데이터베이스")]
    public List<GameObject> allWeaponPrefabs = new List<GameObject>();

    [Header("전투 스킬")]
    public bool isManaGuardOn = false;
    private bool isSwinging = false;
    private bool isCharging = false;
    private float fire1HoldTime = 0f;
    public float chargehold = 2f;
    public float currentAttackMultiplier = 1.0f;

    // [중요] 강한 공격 이펙트
    public GameObject strongAttackEffectPrefab;
    public float strongAttackRadius = 2.0f;
    public LayerMask enemyLayer;

    public GameObject chargeEffectPrefab;
    public GameObject chargedEffectPrefab;
    public AudioSource StrongAttack;
    private GameObject chargeEffectInstance = null;
    private GameObject chargedEffectInstance = null;
    private SpriteRenderer sr;

    [Header("강한 공격 세부 설정")]
    [Tooltip("체크하면 공격 이펙트의 생성 방향과 이미지를 반대로 뒤집습니다.")]
    public bool reverseStrongAttackDir = false;

    [Tooltip("플레이어 몸체 중심에서 얼마나 떨어진 곳에 생성할지 설정 (X: 앞쪽 거리, Y: 높이)")]
    public Vector2 strongAttackOffset = new Vector2(1.5f, -0.8f);

    [Header("덱 시너지 효과")]
    public bool hasResurrection = false;
    public bool isResurrectionUsed = false;
    public float cooldownReduction = 0f;
    public bool hasDoubleStrike = false;
    public bool hasStatBuff = false;
    public bool hasUltimate = false;

    [Header("카드 및 인벤토리")]
    public List<CardData> collectedCards = new List<CardData>();
    public List<CardData> activeDeck = new List<CardData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, Sprite> knownItemSprites = new Dictionary<string, Sprite>();

    [Header("UI")]
    public InventoryUI inventoryUIManager;
    public GameObject cardListWindow;
    private Animator cardListAnimator;
    public GameObject optionWindow;
    private Animator optionUIAnimator;

    // ---------------------------------------------------------
    // 애니메이터 파라미터 해시값 캐싱
    // ---------------------------------------------------------
    private static readonly int AnimIsMoving = Animator.StringToHash("isMoving");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimDoJump = Animator.StringToHash("doJump");
    private static readonly int AnimDoLand = Animator.StringToHash("doLand");
    private static readonly int AnimDoAttack1 = Animator.StringToHash("doAttack1");
    private static readonly int AnimDoAttack2 = Animator.StringToHash("doAttack2");
    private static readonly int AnimDoShoot = Animator.StringToHash("doShoot");
    private static readonly int AnimDoStrongAttack = Animator.StringToHash("doStrongAttack");

    // 스킬 관련 파라미터
    private static readonly int AnimCharge = Animator.StringToHash("Charge");
    private static readonly int AnimSlash = Animator.StringToHash("Slash");
    private static readonly int AnimRecovery = Animator.StringToHash("Recovery");
    private static readonly int AnimSwordAura = Animator.StringToHash("SwordAura");
    private static readonly int AnimGuardBreak = Animator.StringToHash("GuardBreak");
    private static readonly int AnimTimeStop = Animator.StringToHash("TimeStop");
    private static readonly int AnimLaserShot = Animator.StringToHash("LaserShot");
    private static readonly int AnimExplosion = Animator.StringToHash("Explosion");


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);
        sr = GetComponentInChildren<SpriteRenderer>();

        if (cardListWindow != null) { cardListAnimator = cardListWindow.GetComponent<Animator>(); cardListWindow.SetActive(false); }
        if (optionWindow != null) { optionUIAnimator = optionWindow.GetComponent<Animator>(); optionWindow.SetActive(false); }

        RecalculateStats();
        health = maxHealth;
        mana = maxMana;
    }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject startPoint = GameObject.Find("StartPoint");

        if (startPoint != null)
        {
            transform.position = startPoint.transform.position;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (isSkillActive)
        {
            anim.SetBool(AnimIsMoving, false);
            return;
        }

        HandleUIInput();

        if (IsUIOpen() || IsPointerOverUI())
        {
            anim.SetBool(AnimIsMoving, false);
            return;
        }

        hMove = Input.GetAxisRaw("Horizontal");

        CheckDeckSynergy();

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded || currentJumpCount < maxJumpCount) DoJump();
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (equippedWeapon != null)
            {
                isCharging = true;
                fire1HoldTime = 0f;
            }
        }

        if (isCharging)
        {
            fire1HoldTime += Time.deltaTime;
            float effectiveTime = fire1HoldTime * (1f + cooldownReduction);

            if (effectiveTime >= 0.5f && chargeEffectInstance == null && equippedWeapon.weaponLevel >= 2 && chargeEffectPrefab != null && effectiveTime < 2f)
            {
                chargeEffectInstance = Instantiate(chargeEffectPrefab, transform.position, Quaternion.identity, transform);
                chargeEffectInstance.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            }
            if (chargeEffectInstance != null && effectiveTime >= 2f)
            {
                Destroy(chargeEffectInstance); chargeEffectInstance = null;
                chargedEffectInstance = Instantiate(chargedEffectPrefab, transform.position, Quaternion.identity, transform);
                chargedEffectInstance.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            }
        }

        if (Input.GetButtonUp("Fire1"))
        {
            if (isCharging)
            {
                isCharging = false;
                if (chargeEffectInstance != null) { Destroy(chargeEffectInstance); chargeEffectInstance = null; }
                if (chargedEffectInstance != null) { Destroy(chargedEffectInstance); chargedEffectInstance = null; }

                if (equippedWeapon != null)
                {
                    float effectiveTime = fire1HoldTime * (1f + cooldownReduction);

                    // 강공격 (차징) 조건 우선 체크
                    if (equippedWeapon.weaponType == WeaponType.Melee && equippedWeapon.weaponLevel >= 2 && effectiveTime >= chargehold)
                    {
                        // 강공격은 스윙 중이 아닐 때만 발동
                        if (!isSwinging)
                        {
                            currentAttackMultiplier = equippedWeapon.strongAttackMultiplier;
                            StartStrongAttack();
                            currentAttackMultiplier = 1.0f;
                        }
                    }
                    else
                    {
                        // [수정된 부분] 콤보 및 기본 공격 로직

                        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

                        // 1. 현재 'Alice_Attack1' 애니메이션이 재생 중이라면 -> 2타(콤보) 발동
                        // (isSwinging 여부와 상관없이 애니메이션 상태를 최우선으로 확인)
                        if (stateInfo.IsName("Alice_Attack1"))
                        {
                            Debug.Log("Combo Triggered: Attack 1 -> Attack 2");
                            anim.SetTrigger(AnimDoAttack2);

                            // 무기 오브젝트의 물리적 회전도 다시 시작
                            StopCoroutine("SwingWeapon");
                            StartCoroutine(SwingWeapon());
                        }
                        // 2. 공격 중이 아닐 때 (기본 상태) -> 1타 발동
                        // (혹시 모를 중복 실행 방지를 위해 Attack2 상태도 아닐 때만)
                        else if (!isSwinging && !stateInfo.IsName("Alice_Attack2"))
                        {
                            Debug.Log("Normal Attack: Attack 1");
                            normalAttack.Play();
                            currentAttackMultiplier = 1.0f;

                            anim.SetTrigger(AnimDoAttack1);
                            StartCoroutine(SwingWeapon());
                        }
                    }
                }
            }
        }

        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime && HasRangedWeaponReady())
        {
            float rate = Mathf.Max(0.0001f, GetCurrentFireRate()) / (1f - cooldownReduction);
            nextFireTime = Time.time + (1f / rate); Shoot();
        }

        if (Input.GetKeyDown(KeyCode.Z) && hasUltimate) CastUltimate();

        anim.SetBool(AnimIsMoving, hMove != 0);
        anim.SetBool(AnimIsGrounded, isGrounded);
        if (Input.GetKeyDown(KeyCode.W) && isAtEvent) Interaction = true;
    }

    void FixedUpdate()
    {
        if (isDead || IsUIOpen() || isSkillActive)
        {
            if (!isSkillActive) rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = new Vector2(hMove * moveSpeed, rb.velocity.y);
        Flip(hMove);
    }

    void HandleUIInput()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Animator invAnim = inventoryUIManager.GetComponent<Animator>();
            if (invAnim == null && inventoryUIManager.uiAnimator != null) invAnim = inventoryUIManager.uiAnimator;

            ToggleUI(inventoryUIManager.gameObject, invAnim, cardListWindow, optionWindow);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleUI(cardListWindow, cardListAnimator, inventoryUIManager.gameObject, optionWindow);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionWindow != null && optionWindow.activeSelf)
            {
                OptionPage optionScript = optionWindow.GetComponent<OptionPage>();
                if (optionScript != null) optionScript.ClickConfirm();

                Animator optAnim = optionWindow.GetComponent<Animator>();
                if (optAnim != null) optAnim.SetTrigger("doClose");
                else optionWindow.SetActive(false);
            }
            else
            {
                if (inventoryUIManager.gameObject.activeSelf) inventoryUIManager.gameObject.SetActive(false);
                if (cardListWindow.activeSelf) cardListWindow.SetActive(false);

                optionWindow.SetActive(true);
                if (optionUIAnimator != null) optionUIAnimator.SetTrigger("doOpen");
                UpdateGamePauseState();
            }
        }
    }

    void ToggleUI(GameObject targetUI, Animator targetAnim, GameObject otherUI1, GameObject otherUI2)
    {
        if (targetUI.activeSelf)
        {
            ResetCombatTriggers();

            if (targetAnim != null)
            {
                targetAnim.SetTrigger("doClose");
            }
            else
            {
                targetUI.SetActive(false);
                UpdateGamePauseState();
            }
        }
        else
        {
            if (otherUI1 != null) otherUI1.SetActive(false);
            if (otherUI2 != null) otherUI2.SetActive(false);

            targetUI.SetActive(true);
            if (targetAnim != null) targetAnim.SetTrigger("doOpen");

            UpdateGamePauseState();
        }
    }

    void ResetCombatTriggers()
    {
        if (anim != null)
        {
            anim.ResetTrigger(AnimDoAttack1);
            anim.ResetTrigger(AnimDoAttack2);
            anim.ResetTrigger(AnimDoShoot);
            anim.ResetTrigger(AnimDoStrongAttack);
            anim.ResetTrigger(AnimCharge);
        }
        isCharging = false;
        isSwinging = false;
        if (chargeEffectInstance != null) Destroy(chargeEffectInstance);
        if (chargedEffectInstance != null) Destroy(chargedEffectInstance);
    }

    bool IsUIOpen()
    {
        return (inventoryUIManager != null && inventoryUIManager.gameObject.activeSelf) ||
               (cardListWindow != null && cardListWindow.activeSelf) ||
               (optionWindow != null && optionWindow.activeSelf);
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public void CheckDeckSynergy()
    {
        maxJumpCount = 1;
        jumpMultiplier = 1.0f;
        hasResurrection = false;
        cooldownReduction = 0f;
        hasDoubleStrike = false;
        hasStatBuff = false;
        hasUltimate = false;

        if (activeDeck.Count < 5)
        {
            CheckPartialSynergy();
            RecalculateStats();
            return;
        }

        var sortedDeck = activeDeck.OrderBy(c => c.number).ToList();
        var numberGroups = activeDeck.GroupBy(c => c.number).ToList();
        var suitGroups = activeDeck.GroupBy(c => c.suit).ToList();

        bool isStraight = true;
        for (int i = 0; i < 4; i++)
        {
            if (sortedDeck[i + 1].number - sortedDeck[i].number != 1)
            {
                isStraight = false;
                break;
            }
        }

        bool isFlush = suitGroups.Any(g => g.Count() == 5);
        bool isFourCard = numberGroups.Any(g => g.Count() == 4);
        bool isFullHouse = numberGroups.Any(g => g.Count() == 3) && numberGroups.Any(g => g.Count() == 2);

        if (isStraight && isFlush) hasUltimate = true;
        else if (isFourCard) hasStatBuff = true;
        else if (isFullHouse) hasDoubleStrike = true;
        else if (isFlush) cooldownReduction = 0.2f;
        else if (isStraight) hasResurrection = true;
        else CheckPartialSynergy();

        RecalculateStats();
    }

    void CheckPartialSynergy()
    {
        var numberGroups = activeDeck.GroupBy(c => c.number).ToList();
        bool isTriple = numberGroups.Any(g => g.Count() >= 3);
        bool isPair = numberGroups.Any(g => g.Count() == 2);

        if (isTriple)
        {
            maxJumpCount = 2;
            jumpMultiplier = 1.3f;
        }
        else if (isPair)
        {
            maxJumpCount = 2;
            jumpMultiplier = 1.0f;
        }
    }

    public void RecalculateStats()
    {
        float addedAttack = 0;
        int addedHealth = 0;
        float addedDefense = 0;
        int addedMana = 0;

        foreach (CardData card in collectedCards)
        {
            switch (card.suit)
            {
                case CardSuit.Spade: addedAttack += card.number; break;
                case CardSuit.Heart: addedHealth += card.number; break;
                case CardSuit.Diamond: addedDefense += card.number; break;
                case CardSuit.Clover: addedMana += card.number; break;
            }
        }

        float tempAttack = baseAttackDamage + addedAttack;
        float tempHealth = baseMaxHealth + addedHealth;
        float tempDefense = baseDefense + addedDefense;
        float tempMana = baseMaxMana + addedMana;

        if (hasStatBuff)
        {
            tempAttack *= 1.1f;
            tempHealth *= 1.1f;
            tempDefense *= 1.1f;
            tempMana *= 1.1f;
        }

        currentAttackDamage = tempAttack;
        maxHealth = (int)tempHealth;
        currentDefense = tempDefense;
        maxMana = (int)tempMana;

        if (health > maxHealth) health = maxHealth;
        if (mana > maxMana) mana = maxMana;
    }

    void DoJump()
    {
        float finalJumpForce = jumpForce;
        if (!isGrounded) finalJumpForce *= jumpMultiplier;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, finalJumpForce), ForceMode2D.Impulse);

        anim.SetTrigger(AnimDoJump);

        isGrounded = false;
        currentJumpCount++;
    }

    void CastUltimate()
    {
        Debug.Log("Ultimate Activated: Royal Straight Flush!");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy.name.Contains("Boss") || enemy.name.Contains("King") || enemy.name.Contains("Queen"))
            {
                enemy.SendMessage("TakeDamage", 1000, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Destroy(enemy);
            }
        }
    }

    public void PerformSkillSwing()
    {
        if (!isSwinging && equippedWeapon != null)
        {
            StartCoroutine(SwingWeapon());
        }
    }

    void Shoot()
    {
        int manaCost = GetCurrentProjectileManaCost();
        if (mana <= manaCost) return;

        GameObject prefab = GetCurrentProjectilePrefab();
        if (prefab == null) return;

        anim.SetTrigger(AnimDoShoot);

        GameObject projectileObject = Instantiate(prefab, firePoint.position, Quaternion.identity);
        Vector2 shootDirection = isRight ? Vector2.right : Vector2.left;

        var proj = projectileObject.GetComponent<Projectile>();
        if (proj != null) proj.Setup(shootDirection);

        mana -= manaCost;
    }

    private void ActivateHitbox()
    {
        attackHitbox.SetActive(true);
        Invoke("DisableHitbox", attackDuration);
    }

    private void DisableHitbox()
    {
        attackHitbox.SetActive(false);
    }

    public void EquipWeapon(Weapon w)
    {
        if (equippedWeapon != null) Destroy(equippedWeapon.gameObject);

        equippedWeapon = w;
        if (w != null) w.SetOwner(this);

        w.transform.SetParent(swordSlot);
        SpriteRenderer sr = w.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
        }
        if (w.weaponType == WeaponType.Ranged)
            w.transform.localPosition = new Vector3(0.35f, 0.1f, 0f);
        else
            w.transform.localPosition = new Vector3(0.35f, 0f, 0f);

        if (!isRight)
            w.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        else
            w.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        Collider2D col = w.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb2 = w.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.simulated = false;
    }

    private IEnumerator SwingWeapon()
    {
        isSwinging = true;
        Collider2D weaponCollider = equippedWeapon.GetComponent<Collider2D>();
        Rigidbody2D weaponRb = equippedWeapon.GetComponent<Rigidbody2D>();
        if (weaponCollider != null) weaponCollider.enabled = true;
        if (weaponRb != null) weaponRb.simulated = true;

        float duration = equippedWeapon.swingDuration;
        float startAngle = equippedWeapon.swingStartAngle;
        float endAngle = equippedWeapon.swingEndAngle;
        float timer = 0f;
        Quaternion baseRotation = swordSlot.localRotation;
        Quaternion startRot = baseRotation * Quaternion.Euler(0, 0, startAngle);
        Quaternion endRot = baseRotation * Quaternion.Euler(0, 0, endAngle);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            swordSlot.localRotation = Quaternion.Lerp(startRot, endRot, progress);
            yield return null;
        }

        swordSlot.localRotation = baseRotation;
        if (weaponCollider != null) weaponCollider.enabled = false;
        if (weaponRb != null) weaponRb.simulated = false;
        isSwinging = false;
        currentAttackMultiplier = 1.0f;
    }

    private void Flip(float h)
    {
        if ((h < 0 && isRight) || (h > 0 && !isRight))
        {
            isRight = !isRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }

    void StartStrongAttack() { StartCoroutine(StrongAttackSequence()); }

    IEnumerator StrongAttackSequence()
    {
        isSwinging = true;
        anim.SetTrigger(AnimDoStrongAttack);
        if (StrongAttack != null) StrongAttack.Play();

        yield return new WaitForSeconds(0.15f);

        float facingDir = transform.localScale.x > 0 ? 1f : -1f;
        if (reverseStrongAttackDir) facingDir *= -1;

        Vector3 spawnPos = transform.position + new Vector3(strongAttackOffset.x * facingDir, strongAttackOffset.y, 0);

        if (strongAttackEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(strongAttackEffectPrefab, spawnPos, Quaternion.identity);
            Vector3 scale = effectInstance.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDir;
            effectInstance.transform.localScale = scale;
            Destroy(effectInstance, 0.5f);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPos, strongAttackRadius, enemyLayer);
        float baseDmg = equippedWeapon.damage + currentAttackDamage;
        if (hasDoubleStrike) baseDmg *= 2.0f;
        int finalDamage = Mathf.RoundToInt(baseDmg * currentAttackMultiplier);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            IDamageable target = enemyCollider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(finalDamage);
            }
            else
            {
                var enemy = enemyCollider.GetComponent<EnemyController_2D>();
                if (enemy != null) enemy.SendMessage("TakeDamage", finalDamage, SendMessageOptions.DontRequireReceiver);
            }
        }

        yield return new WaitForSeconds(attackDelay);
        isSwinging = false;
        currentAttackMultiplier = 1.0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Enemy"))
        {
            if (collision.GetContact(0).normal.y > 0.7f)
            {
                isGrounded = true;
                currentJumpCount = 0;
                anim.SetTrigger(AnimDoLand);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Enemy"))
        {
            isGrounded = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Door" || other.tag == "Chest") isAtEvent = true;

        if (other.CompareTag("Weapon"))
        {
            Weapon w = other.GetComponent<Weapon>();

            string weaponName = w.gameObject.name.Replace("(Clone)", "").Trim();
            SpriteRenderer sr = w.GetComponent<SpriteRenderer>();
            AddItemToInventory(weaponName, 1, sr != null ? sr.sprite : null);

            if (w != null && w != equippedWeapon) EquipWeapon(w);
        }
        else if (other.tag == "RedPotion" || other.tag == "BluePotion")
        {
            string itemTag = other.tag;
            if (itemTag == "RedPotion") TakeRedPotion();
            else if (itemTag == "BluePotion") TakeBluePotion();

            if (!knownItemSprites.ContainsKey(itemTag))
            {
                SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) knownItemSprites.Add(itemTag, sr.sprite);
            }
            AddItemToInventory(itemTag, 1);
            Destroy(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Door") isAtEvent = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Door doorComponent = other.GetComponent<Door>();
        if (isAtEvent && Interaction && other.CompareTag("Door"))
        {
            Interaction = false;
            if (doorComponent != null) doorComponent.InitiateTransition();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int finalDamage = Mathf.Max(1, damage - (int)currentDefense);

        if (isManaGuardOn && mana > 0)
        {
            if (mana >= finalDamage)
            {
                mana -= finalDamage;
                Debug.Log($"마나 가드 방어!");
                return;
            }
            else
            {
                int remainingDmg = finalDamage - (int)mana;
                mana = 0;
                isManaGuardOn = false;
                health -= remainingDmg;
                Debug.Log("마나 가드 파괴!");
            }
        }
        else
        {
            health -= finalDamage;
        }

        if (health <= 0)
        {
            if (hasResurrection && !isResurrectionUsed)
            {
                health = maxHealth / 2;
                isResurrectionUsed = true;
                Debug.Log("Resurrected!");
            }
            else
            {
                health = 0;
                Die();
            }
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
    }

    public void VelocityZero()
    {
        rb.velocity = Vector2.zero;
    }

    private bool HasRangedWeaponReady()
    {
        if (equippedWeapon == null) return projectilePrefab != null;
        if (equippedWeapon.weaponType == WeaponType.Ranged || equippedWeapon.weaponType == WeaponType.Hybrid)
            return equippedWeapon.projectilePrefab != null;
        return false;
    }

    private float GetCurrentFireRate()
    {
        return (equippedWeapon != null) ? equippedWeapon.fireRate : fireRate;
    }

    private GameObject GetCurrentProjectilePrefab()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null)
            ? equippedWeapon.projectilePrefab : projectilePrefab;
    }

    private int GetCurrentProjectileManaCost()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.ManaCost : 10;
    }

    public void TakeRedPotion()
    {
        if (health < maxHealth)
        {
            health += maxHealth / 2;
            if (health > maxHealth) health = maxHealth;
        }
    }

    public void TakeBluePotion()
    {
        if (mana < maxMana)
        {
            mana += (float)(maxMana / 2);
            if (mana > maxMana) mana = maxMana;
        }
    }

    public void AddItemToInventory(string itemName, int amount, Sprite itemSprite = null)
    {
        if (itemSprite != null && !knownItemSprites.ContainsKey(itemName))
        {
            knownItemSprites.Add(itemName, itemSprite);
        }

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName] += amount;
        }
        else
        {
            inventory.Add(itemName, amount);
        }

        if (inventoryUIManager != null)
        {
            inventoryUIManager.RefreshInventoryUI();
        }
    }

    public void UpdateGamePauseState()
    {
        if ((inventoryUIManager != null && inventoryUIManager.gameObject.activeSelf) ||
            (cardListWindow != null && cardListWindow.activeSelf) ||
            (optionWindow != null && optionWindow.activeSelf))
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    public void UseItem(string itemTag)
    {
        if (!inventory.ContainsKey(itemTag) || inventory[itemTag] <= 0) return;

        bool itemUsed = false;
        if (itemTag == "RedPotion")
        {
            if (health < maxHealth) { health += 20; if (health > maxHealth) health = maxHealth; itemUsed = true; }
        }
        else if (itemTag == "BluePotion")
        {
            if (mana < maxMana) { mana += 20; if (mana > maxMana) mana = maxMana; itemUsed = true; }
        }
        else
        {
            GameObject weaponPrefab = allWeaponPrefabs.Find(w => w.name == itemTag);

            if (weaponPrefab != null)
            {
                GameObject newWeaponObj = Instantiate(weaponPrefab);
                newWeaponObj.name = weaponPrefab.name;

                Weapon newWeapon = newWeaponObj.GetComponent<Weapon>();
                if (newWeapon != null)
                {
                    EquipWeapon(newWeapon);
                    Debug.Log($"{itemTag} 장착 완료!");
                    if (inventoryUIManager != null)
                    {
                        inventoryUIManager.RefreshEquippedWeaponUI();
                    }
                }
            }
            else
            {
                Debug.LogWarning($"무기 데이터를 찾을 수 없습니다: {itemTag}");
            }
        }

        if (itemUsed)
        {
            inventory[itemTag]--;
            if (inventory[itemTag] <= 0) inventory.Remove(itemTag);
        }
    }

    public void AddCardToCollection(CardData newCard)
    {
        bool alreadyExists = collectedCards.Any(card =>
            card.suit == newCard.suit &&
            card.number == newCard.number
        );

        if (!alreadyExists)
        {
            collectedCards.Add(newCard);
            Debug.Log(newCard.suit + " " + newCard.number + " 카드를 획득했습니다.");
            RecalculateStats();
        }
        else
        {
            Debug.Log(newCard.suit + " " + newCard.number + " 카드는 이미 보유 중이라 무시합니다.");
        }
    }

    // --------------------------------------------------------------------------------
    // 스킬 매니저나 외부 스크립트에서 호출할 애니메이션 트리거 메서드들
    // --------------------------------------------------------------------------------

    public void TriggerSlash() { anim.SetTrigger(AnimSlash); }
    public void TriggerCharge() { anim.SetTrigger(AnimCharge); }
    public void TriggerRecovery() { anim.SetTrigger(AnimRecovery); }
    public void TriggerSwordAura() { anim.SetTrigger(AnimSwordAura); }
    public void TriggerGuardBreak() { anim.SetTrigger(AnimGuardBreak); }
    public void TriggerTimeStop() { anim.SetTrigger(AnimTimeStop); }
    public void TriggerLaserShot() { anim.SetTrigger(AnimLaserShot); }
    public void TriggerExplosion() { anim.SetTrigger(AnimExplosion); }
}