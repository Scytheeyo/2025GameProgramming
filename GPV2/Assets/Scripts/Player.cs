using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    // [핵심] 스킬 사용 중인지 체크하는 변수
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

    [Header("전투 스킬")]
    public bool isManaGuardOn = false;
    private bool isSwinging = false;
    private bool isCharging = false;
    private float fire1HoldTime = 0f;
    public float chargehold = 2f;
    public float currentAttackMultiplier = 1.0f;

    // [중요] 강한 공격 이펙트 (이미지 3~4번 프리팹)
    public GameObject strongAttackEffectPrefab;
    public float strongAttackRadius = 2.0f;
    public LayerMask enemyLayer;

    public GameObject chargeEffectPrefab;
    public GameObject chargedEffectPrefab;
    public AudioSource StrongAttack;
    private GameObject chargeEffectInstance = null;
    private GameObject chargedEffectInstance = null;
    private SpriteRenderer sr;

    // --------------------------------------------------------------------------------
    // [추가됨] 강한 공격 위치/방향 세부 설정
    // --------------------------------------------------------------------------------
    [Header("강한 공격 세부 설정")]
    [Tooltip("체크하면 공격 이펙트의 생성 방향과 이미지를 반대로 뒤집습니다.")]
    public bool reverseStrongAttackDir = false;

    [Tooltip("플레이어 몸체 중심에서 얼마나 떨어진 곳에 생성할지 설정 (X: 앞쪽 거리, Y: 높이)")]
    public Vector2 strongAttackOffset = new Vector2(1.5f, -0.8f); // 기본값: 앞쪽 1.5, 아래로 -0.8
    // --------------------------------------------------------------------------------

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

    // 애니메이션 해시 (최적화)
    private static readonly int AnimStrongAttack = Animator.StringToHash("doStrongAttack");

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

    void Update()
    {
        if (isDead) return;

        // 스킬 사용 중이면 입력 차단
        if (isSkillActive)
        {
            anim.SetBool("isMoving", false);
            return;
        }

        HandleUIInput();

        if (IsUIOpen() || IsPointerOverUI())
        {
            anim.SetBool("isMoving", false);
            return;
        }

        hMove = Input.GetAxisRaw("Horizontal");

        CheckDeckSynergy();

        if (Input.GetButtonDown("Jump")) { if (isGrounded || currentJumpCount < maxJumpCount) DoJump(); }

        if (Input.GetButtonDown("Fire1")) { if (!isSwinging && equippedWeapon != null) { isCharging = true; fire1HoldTime = 0f; } }

        if (isCharging)
        {
            fire1HoldTime += Time.deltaTime;
            float effectiveTime = fire1HoldTime * (1f + cooldownReduction);
            if (effectiveTime >= 0.5f && chargeEffectInstance == null && chargeEffectPrefab != null && effectiveTime < 2f)
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

                if (!isSwinging && equippedWeapon != null)
                {
                    float effectiveTime = fire1HoldTime * (1f + cooldownReduction);
                    if (equippedWeapon.weaponType == WeaponType.Melee && equippedWeapon.weaponLevel >= 2 && effectiveTime >= chargehold)
                    {
                        currentAttackMultiplier = equippedWeapon.strongAttackMultiplier;
                        StartStrongAttack(); // [수정] 강한 공격 호출
                        currentAttackMultiplier = 1.0f;
                    }
                    else { normalAttack.Play(); currentAttackMultiplier = 1.0f; StartCoroutine(SwingWeapon()); }
                }
            }
        }

        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime && HasRangedWeaponReady())
        {
            float rate = Mathf.Max(0.0001f, GetCurrentFireRate()) / (1f - cooldownReduction);
            nextFireTime = Time.time + (1f / rate); Shoot();
        }

        if (Input.GetKeyDown(KeyCode.Z) && hasUltimate) CastUltimate();

        anim.SetBool("isMoving", hMove != 0);
        anim.SetBool("isGrounded", isGrounded);
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

    public void VelocityZero() { rb.velocity = Vector2.zero; }

    // --------------------------------------------------------------------------------
    // [핵심 로직] 강한 공격 (FirePoint 미사용, Offset 사용)
    // --------------------------------------------------------------------------------
    void StartStrongAttack()
    {
        StartCoroutine(StrongAttackSequence());
    }

    IEnumerator StrongAttackSequence()
    {
        isSwinging = true;

        // 1. 애니메이션 재생
        anim.SetTrigger(AnimStrongAttack);

        // 2. 사운드
        if (StrongAttack != null) StrongAttack.Play();

        // 3. 타이밍 대기 (칼 내려치는 순간까지)
        yield return new WaitForSeconds(0.15f);

        // ------------------------------------------------------------------
        // [위치 계산] Player 위치 기준 Offset 적용
        // ------------------------------------------------------------------
        // 현재 보는 방향 (오른쪽 1, 왼쪽 -1)
        float facingDir = transform.localScale.x > 0 ? 1f : -1f;

        // 반전 체크박스 적용
        if (reverseStrongAttackDir) facingDir *= -1;

        // 생성 위치: 내 위치 + (앞쪽 거리 * 방향) + 높이
        Vector3 spawnPos = transform.position + new Vector3(strongAttackOffset.x * facingDir, strongAttackOffset.y, 0);
        // ------------------------------------------------------------------

        // 4. 이펙트 생성
        if (strongAttackEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(strongAttackEffectPrefab, spawnPos, Quaternion.identity);

            // 이펙트 이미지 좌우 반전 (방향에 맞게)
            Vector3 scale = effectInstance.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDir;
            effectInstance.transform.localScale = scale;

            // 0.5초 뒤 삭제
            Destroy(effectInstance, 0.5f);
        }

        // 5. 데미지 판정 (생성된 위치 spawnPos 기준)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPos, strongAttackRadius, enemyLayer);

        float baseDmg = equippedWeapon.damage + currentAttackDamage;
        if (hasDoubleStrike) baseDmg *= 2.0f;
        int finalDamage = Mathf.RoundToInt(baseDmg * currentAttackMultiplier);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController_2D enemy = enemyCollider.GetComponent<EnemyController_2D>();
            if (enemy != null) enemy.TakeDamage(finalDamage);
        }

        // 6. 후딜레이
        yield return new WaitForSeconds(attackDelay);

        isSwinging = false;
        currentAttackMultiplier = 1.0f;
    }
    // --------------------------------------------------------------------------------

    // --- (이하 나머지 코드는 기존과 동일) ---
    void HandleUIInput() { if (Input.GetKeyDown(KeyCode.I)) { Animator invAnim = inventoryUIManager.GetComponent<Animator>(); if (invAnim == null && inventoryUIManager.uiAnimator != null) invAnim = inventoryUIManager.uiAnimator; ToggleUI(inventoryUIManager.gameObject, invAnim, cardListWindow, optionWindow); } if (Input.GetKeyDown(KeyCode.S)) ToggleUI(cardListWindow, cardListAnimator, inventoryUIManager.gameObject, optionWindow); if (Input.GetKeyDown(KeyCode.Escape)) { if (optionWindow != null && optionWindow.activeSelf) { OptionPage optionScript = optionWindow.GetComponent<OptionPage>(); if (optionScript != null) optionScript.ClickConfirm(); Animator optAnim = optionWindow.GetComponent<Animator>(); if (optAnim != null) optAnim.SetTrigger("doClose"); else optionWindow.SetActive(false); } else { if (inventoryUIManager.gameObject.activeSelf) inventoryUIManager.gameObject.SetActive(false); if (cardListWindow.activeSelf) cardListWindow.SetActive(false); optionWindow.SetActive(true); if (optionUIAnimator != null) optionUIAnimator.SetTrigger("doOpen"); UpdateGamePauseState(); } } }
    void ToggleUI(GameObject targetUI, Animator targetAnim, GameObject otherUI1, GameObject otherUI2) { if (targetUI.activeSelf) { ResetCombatTriggers(); if (targetAnim != null) targetAnim.SetTrigger("doClose"); else { targetUI.SetActive(false); UpdateGamePauseState(); } } else { if (otherUI1 != null) otherUI1.SetActive(false); if (otherUI2 != null) otherUI2.SetActive(false); targetUI.SetActive(true); if (targetAnim != null) targetAnim.SetTrigger("doOpen"); UpdateGamePauseState(); } }
    void ResetCombatTriggers() { if (anim != null) anim.ResetTrigger("doAttack"); isCharging = false; isSwinging = false; if (chargeEffectInstance != null) Destroy(chargeEffectInstance); if (chargedEffectInstance != null) Destroy(chargedEffectInstance); }
    bool IsUIOpen() { return (inventoryUIManager != null && inventoryUIManager.gameObject.activeSelf) || (cardListWindow != null && cardListWindow.activeSelf) || (optionWindow != null && optionWindow.activeSelf); }
    bool IsPointerOverUI() { return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(); }
    public void CheckDeckSynergy() { maxJumpCount = 1; jumpMultiplier = 1.0f; hasResurrection = false; cooldownReduction = 0f; hasDoubleStrike = false; hasStatBuff = false; hasUltimate = false; if (activeDeck.Count < 5) { CheckPartialSynergy(); RecalculateStats(); return; } var sortedDeck = activeDeck.OrderBy(c => c.number).ToList(); var numberGroups = activeDeck.GroupBy(c => c.number).ToList(); var suitGroups = activeDeck.GroupBy(c => c.suit).ToList(); bool isStraight = true; for (int i = 0; i < 4; i++) { if (sortedDeck[i + 1].number - sortedDeck[i].number != 1) { isStraight = false; break; } } bool isFlush = suitGroups.Any(g => g.Count() == 5); bool isFourCard = numberGroups.Any(g => g.Count() == 4); bool isFullHouse = numberGroups.Any(g => g.Count() == 3) && numberGroups.Any(g => g.Count() == 2); if (isStraight && isFlush) hasUltimate = true; else if (isFourCard) hasStatBuff = true; else if (isFullHouse) hasDoubleStrike = true; else if (isFlush) cooldownReduction = 0.2f; else if (isStraight) hasResurrection = true; else CheckPartialSynergy(); RecalculateStats(); }
    void CheckPartialSynergy() { var numberGroups = activeDeck.GroupBy(c => c.number).ToList(); bool isTriple = numberGroups.Any(g => g.Count() >= 3); bool isPair = numberGroups.Any(g => g.Count() == 2); if (isTriple) { maxJumpCount = 2; jumpMultiplier = 1.3f; } else if (isPair) { maxJumpCount = 2; jumpMultiplier = 1.0f; } }
    public void RecalculateStats() { float addedAttack = 0; int addedHealth = 0; float addedDefense = 0; int addedMana = 0; foreach (CardData card in collectedCards) { switch (card.suit) { case CardSuit.Spade: addedAttack += card.number; break; case CardSuit.Heart: addedHealth += card.number; break; case CardSuit.Diamond: addedDefense += card.number; break; case CardSuit.Clover: addedMana += card.number; break; } } float tempAttack = baseAttackDamage + addedAttack; float tempHealth = baseMaxHealth + addedHealth; float tempDefense = baseDefense + addedDefense; float tempMana = baseMaxMana + addedMana; if (hasStatBuff) { tempAttack *= 1.1f; tempHealth *= 1.1f; tempDefense *= 1.1f; tempMana *= 1.1f; } currentAttackDamage = tempAttack; maxHealth = (int)tempHealth; currentDefense = tempDefense; maxMana = (int)tempMana; if (health > maxHealth) health = maxHealth; if (mana > maxMana) mana = maxMana; }
    void DoJump() { float finalJumpForce = jumpForce; if (!isGrounded) finalJumpForce *= jumpMultiplier; rb.velocity = new Vector2(rb.velocity.x, 0); rb.AddForce(new Vector2(0, finalJumpForce), ForceMode2D.Impulse); isGrounded = false; currentJumpCount++; }
    void CastUltimate() { Debug.Log("Ultimate Activated!"); GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); foreach (GameObject enemy in enemies) { if (enemy.name.Contains("Boss") || enemy.name.Contains("King") || enemy.name.Contains("Queen")) enemy.SendMessage("TakeDamage", 1000, SendMessageOptions.DontRequireReceiver); else Destroy(enemy); } }
    public void PerformSkillSwing() { if (!isSwinging && equippedWeapon != null) StartCoroutine(SwingWeapon()); }
    void Shoot() { int manaCost = GetCurrentProjectileManaCost(); if (mana <= manaCost) return; GameObject prefab = GetCurrentProjectilePrefab(); if (prefab == null) return; GameObject projectileObject = Instantiate(prefab, firePoint.position, Quaternion.identity); Vector2 shootDirection = isRight ? Vector2.right : Vector2.left; var proj = projectileObject.GetComponent<Projectile>(); if (proj != null) proj.Setup(shootDirection); mana -= manaCost; }
    private void ActivateHitbox() { attackHitbox.SetActive(true); Invoke("DisableHitbox", attackDuration); }
    private void DisableHitbox() { attackHitbox.SetActive(false); }
    private void EquipWeapon(Weapon w) { if (equippedWeapon != null) Destroy(equippedWeapon.gameObject); equippedWeapon = w; if (w != null) w.SetOwner(this); w.transform.SetParent(swordSlot); if (w.weaponType == WeaponType.Ranged) w.transform.localPosition = new Vector3(0.35f, 0.1f, 0f); else w.transform.localPosition = new Vector3(0.35f, 0f, 0f); if (!isRight) w.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); else w.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); Collider2D col = w.GetComponent<Collider2D>(); if (col != null) col.enabled = false; Rigidbody2D rb2 = w.GetComponent<Rigidbody2D>(); if (rb2 != null) rb2.simulated = false; }
    private IEnumerator SwingWeapon() { isSwinging = true; Collider2D weaponCollider = equippedWeapon.GetComponent<Collider2D>(); Rigidbody2D weaponRb = equippedWeapon.GetComponent<Rigidbody2D>(); if (weaponCollider != null) weaponCollider.enabled = true; if (weaponRb != null) weaponRb.simulated = true; float duration = equippedWeapon.swingDuration; float startAngle = equippedWeapon.swingStartAngle; float endAngle = equippedWeapon.swingEndAngle; float timer = 0f; Quaternion baseRotation = swordSlot.localRotation; Quaternion startRot = baseRotation * Quaternion.Euler(0, 0, startAngle); Quaternion endRot = baseRotation * Quaternion.Euler(0, 0, endAngle); while (timer < duration) { timer += Time.deltaTime; float progress = timer / duration; swordSlot.localRotation = Quaternion.Lerp(startRot, endRot, progress); yield return null; } swordSlot.localRotation = baseRotation; if (weaponCollider != null) weaponCollider.enabled = false; if (weaponRb != null) weaponRb.simulated = false; isSwinging = false; currentAttackMultiplier = 1.0f; }
    private void OnCollisionEnter2D(Collision2D collision) { if (collision.gameObject.CompareTag("Ground")) { isGrounded = true; currentJumpCount = 0; } }
    private void OnCollisionExit2D(Collision2D collision) { if (collision.gameObject.CompareTag("Ground")) isGrounded = false; }
    void OnTriggerEnter2D(Collider2D other) { if (other.tag == "Door" || other.tag == "Chest") isAtEvent = true; if (other.CompareTag("Weapon")) { Weapon w = other.GetComponent<Weapon>(); if (w != null && w != equippedWeapon) EquipWeapon(w); } if (other.tag == "RedPotion" || other.tag == "BluePotion") { string itemTag = other.tag; if (itemTag == "RedPotion") TakeRedPotion(); else if (itemTag == "BluePotion") TakeBluePotion(); if (!knownItemSprites.ContainsKey(itemTag)) { SpriteRenderer sr = other.GetComponent<SpriteRenderer>(); if (sr != null && sr.sprite != null) knownItemSprites.Add(itemTag, sr.sprite); } AddItemToInventory(itemTag, 1); Destroy(other.gameObject); } }
    void OnTriggerExit2D(Collider2D other) { if (other.tag == "Door") isAtEvent = false; }
    void OnTriggerStay2D(Collider2D other) { Door doorComponent = other.GetComponent<Door>(); if (isAtEvent && Interaction && other.CompareTag("Door")) { Interaction = false; if (doorComponent != null) doorComponent.InitiateTransition(); } }
    public void TakeDamage(int damage) { if (isDead) return; int finalDamage = Mathf.Max(1, damage - (int)currentDefense); if (isManaGuardOn && mana > 0) { if (mana >= finalDamage) { mana -= finalDamage; Debug.Log($"마나 가드 방어!"); return; } else { int remainingDmg = finalDamage - (int)mana; mana = 0; isManaGuardOn = false; health -= remainingDmg; Debug.Log("마나 가드 파괴!"); } } else health -= finalDamage; if (health <= 0) { if (hasResurrection && !isResurrectionUsed) { health = maxHealth / 2; isResurrectionUsed = true; Debug.Log("Resurrected!"); } else { health = 0; Die(); } } }
    void Die() { isDead = true; Debug.Log("플레이어 사망"); }
    private bool HasRangedWeaponReady() { if (equippedWeapon == null) return projectilePrefab != null; if (equippedWeapon.weaponType == WeaponType.Ranged || equippedWeapon.weaponType == WeaponType.Hybrid) return equippedWeapon.projectilePrefab != null; return false; }
    private float GetCurrentFireRate() { return (equippedWeapon != null) ? equippedWeapon.fireRate : fireRate; }
    private GameObject GetCurrentProjectilePrefab() { return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.projectilePrefab : projectilePrefab; }
    private int GetCurrentProjectileManaCost() { return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.ManaCost : 10; }
    public void TakeRedPotion() { if (health < maxHealth) { health += maxHealth / 2; if (health > maxHealth) health = maxHealth; } }
    public void TakeBluePotion() { if (mana < maxMana) { mana += (float)(maxMana / 2); if (mana > maxMana) mana = maxMana; } }
    public void AddItemToInventory(string itemName, int amount) { if (inventory.ContainsKey(itemName)) inventory[itemName] += amount; else inventory.Add(itemName, amount); }
    public void UpdateGamePauseState() { if ((inventoryUIManager != null && inventoryUIManager.gameObject.activeSelf) || (cardListWindow != null && cardListWindow.activeSelf) || (optionWindow != null && optionWindow.activeSelf)) Time.timeScale = 0f; else Time.timeScale = 1f; }
    public void UseItem(string itemTag) { if (!inventory.ContainsKey(itemTag) || inventory[itemTag] <= 0) return; bool itemUsed = false; switch (itemTag) { case "RedPotion": if (health < maxHealth) { health += 20; if (health > maxHealth) health = maxHealth; itemUsed = true; } break; case "BluePotion": if (mana < maxMana) { mana += 20; if (mana > maxMana) mana = maxMana; itemUsed = true; } break; } if (itemUsed) { inventory[itemTag]--; if (inventory[itemTag] <= 0) inventory.Remove(itemTag); } }
    public void AddCardToCollection(CardData newCard) { bool alreadyExists = collectedCards.Any(card => card.suit == newCard.suit && card.number == newCard.number); if (!alreadyExists) { collectedCards.Add(newCard); RecalculateStats(); } }
}